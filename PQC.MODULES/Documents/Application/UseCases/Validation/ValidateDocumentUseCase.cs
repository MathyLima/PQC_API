using iText.Kernel.Pdf;
using PQC.MODULES.Documents.Application.Interfaces.PDFcomposer;
using PQC.MODULES.Documents.Application.Interfaces.PQCsigner;
using PQC.MODULES.Documents.Domain.Entities;
using PQC.MODULES.Documents.Infraestructure.DocumentProcessing;
using PQC.MODULES.Documents.Infraestructure.Repositories;
using System.Security.Cryptography;

namespace PQC.MODULES.Documents.Application.UseCases.Validation
{
    public class ValidateDocumentUseCase
    {
        private readonly IDocumentRepository _repository;
        private readonly IXmpMetadataExtractor _metadataExtractor;
        private readonly INativePostQuantumSigner _pqcSigner;
        private readonly IDocumentComposer _documentComposer;

        public ValidateDocumentUseCase(
            IDocumentRepository repository,
            IXmpMetadataExtractor metadataExtractor,
            INativePostQuantumSigner pqcSigner,
            IDocumentComposer documentComposer)
        {
            _repository = repository;
            _metadataExtractor = metadataExtractor;
            _pqcSigner = pqcSigner;
            _documentComposer = documentComposer;
        }

        public async Task<DocumentValidationResult> Execute(byte[] pdfContent)
        {
            Console.WriteLine("\n══════════════════════════════════════════════");
            Console.WriteLine("   INICIANDO VALIDAÇÃO PQC");
            Console.WriteLine("══════════════════════════════════════════════\n");

            // 1️⃣ Extrai assinaturas
            var extraction = await _metadataExtractor.ExtractSignaturesAsync(pdfContent);

            if (!extraction.HasSignatures)
            {
                return new DocumentValidationResult
                {
                    IsValid = false,
                    Message = "Nenhuma assinatura encontrada.",
                };
            }

            Console.WriteLine($"Total: {extraction.Signatures.Count} assinaturas\n");

            var results = new List<SignatureValidationDetail>();

            // ✅ PDF corrente (vai voltando no tempo)
            byte[] currentPdf = pdfContent;

            // Da última pra primeira
            var ordered = extraction.Signatures
                .OrderByDescending(s => s.Order)
                .ToList();

            foreach (var sig in ordered)
            {
                Console.WriteLine($"━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━");
                Console.WriteLine($"Validando #{sig.Order} — {sig.SignerName}");

                var result = await ValidateSingleSignature(sig, currentPdf);

                results.Add(result);

                if (!result.IsValid)
                {
                    Console.WriteLine($"❌ Falha: {result.ValidationMessage}");
                    Console.WriteLine("━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━\n");
                    break;
                }

                // Remove XMP
                currentPdf = await _metadataExtractor.RemoveSignatureMetadataAsync(
                    currentPdf,
                    sig.Order
                );

                // Remove página
                currentPdf = RemoveSignaturePage(currentPdf, sig.PageNumber);

                Console.WriteLine("✔️ OK\n");
            }

            results.Reverse();

            bool allValid = results.All(r => r.IsValid);

            return new DocumentValidationResult
            {
                IsValid = allValid,
                Message = allValid
                    ? $"Documento válido ({results.Count})"
                    : "Documento inválido",
                SignatureResults = results,
                TotalSignatures = extraction.Signatures.Count
            };
        }

        // ======================================================
        // VALIDA UMA ASSINATURA INDIVIDUAL
        // ======================================================

        private async Task<SignatureValidationDetail> ValidateSingleSignature(
            ExtractedSignature signature,
            byte[] pdfAtSignatureMoment)
        {
            try
            {
                Console.WriteLine($"\n🔍 DEBUG - Assinatura #{signature.Order}");
                Console.WriteLine($"━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━");

                // ================================
                // 1️⃣ Busca documento no banco
                // ================================
                Console.WriteLine($"1️⃣ Buscando documento no banco...");
                Console.WriteLine($"   DocumentId: {signature.DocumentId}");

                var stored = await _repository.GetByDocumentIdAsync(signature.DocumentId);

                if (stored == null)
                {
                    Console.WriteLine("   ❌ Documento não encontrado!");
                    return Invalid(signature, "Documento não encontrado no banco.");
                }

                Console.WriteLine($"   ✓ Documento encontrado");
                Console.WriteLine($"   - OriginalPath: {stored.OriginalPath}");
                Console.WriteLine($"   - File exists: {File.Exists(stored.OriginalPath)}");

                // ================================
                // 2️⃣ Carrega PDF ORIGINAL
                // ================================
                Console.WriteLine($"\n2️⃣ Carregando PDF original...");

                if (string.IsNullOrEmpty(stored.OriginalPath) || !File.Exists(stored.OriginalPath))
                {
                    Console.WriteLine("   ❌ Arquivo não encontrado!");
                    return Invalid(signature, "PDF original não localizado.");
                }

                byte[] originalPdf = await File.ReadAllBytesAsync(stored.OriginalPath);
                Console.WriteLine($"   ✓ PDF carregado: {originalPdf.Length} bytes");

                // ================================
                // 3️⃣ Calcula hash do arquivo salvo
                // ================================
                Console.WriteLine($"\n3️⃣ Calculando hash do arquivo salvo...");

                byte[] realHash = SHA256.HashData(originalPdf);
                string realHashBase64 = Convert.ToBase64String(realHash);

                // ================================
                // 4️⃣ Normaliza hashes para comparação
                // ================================
                string xmpHash = Normalize(signature.DocumentHash);
                string dbHash = Normalize(stored.OriginalHash);
                string calcHash = Normalize(realHashBase64);

                Console.WriteLine($"\n4️⃣ Comparando hashes:");
                Console.WriteLine($"   Hash do XMP:       {xmpHash}");
                Console.WriteLine($"   Hash Calculado:    {calcHash}");
                Console.WriteLine($"   Hash do Banco:     {dbHash}");
                Console.WriteLine($"   XMP == Calc?       {xmpHash == calcHash}");
                Console.WriteLine($"   Calc == Banco?     {calcHash == dbHash}");

                // ================================
                // 5️⃣ Confere hash real ↔ banco
                // ================================
                Console.WriteLine($"\n5️⃣ Validando hash contra banco...");

                if (calcHash != dbHash)
                {
                    Console.WriteLine("   ❌ Hash não confere com o banco!");
                    Console.WriteLine($"   Esperado (banco): {dbHash}");
                    Console.WriteLine($"   Calculado:        {calcHash}");
                    return Invalid(signature, "Hash do PDF original não confere com o banco.");
                }

                Console.WriteLine("   ✓ Hash confere com o banco");

                // ================================
                // 6️⃣ Confere hash real ↔ XMP
                // ================================
                Console.WriteLine($"\n6️⃣ Validando hash contra XMP...");

                if (calcHash != xmpHash)
                {
                    Console.WriteLine("   ❌ Hash não confere com o XMP!");
                    Console.WriteLine($"   Esperado (XMP): {xmpHash}");
                    Console.WriteLine($"   Calculado:      {calcHash}");
                    return Invalid(signature, "Hash do XMP não confere com o documento.");
                }

                Console.WriteLine("   ✓ Hash confere com o XMP");

                // ================================
                // 7️⃣ Valida timestamp
                // ================================
                Console.WriteLine($"\n7️⃣ Validando timestamp...");
                Console.WriteLine($"   SignedAt: {signature.SignedAt:yyyy-MM-dd HH:mm:ss}");
                Console.WriteLine($"   Now:      {DateTime.UtcNow:yyyy-MM-dd HH:mm:ss}");

                if (signature.SignedAt > DateTime.UtcNow.AddMinutes(5))
                {
                    Console.WriteLine("   ❌ Timestamp inválido!");
                    return Invalid(signature, "Timestamp inválido.");
                }

                Console.WriteLine("   ✓ Timestamp válido");

                // ================================
                // 8️⃣ Converte hash para bytes
                // ================================
                Console.WriteLine($"\n8️⃣ Preparando dados para validação PQC...");

                byte[] hashBytes = Convert.FromBase64String(xmpHash);
                Console.WriteLine($"   Hash bytes: {hashBytes.Length} bytes");

                // ================================
                // 9️⃣ ✅ CORREÇÃO: Extrai chave pública como PEM original
                // stored.ChavePublicaUsada agora é Base64 do PEM completo
                // ================================
                Console.WriteLine($"\n9️⃣ Extraindo chave pública (PEM original)...");

                byte[] publicKeyPemBytes = Convert.FromBase64String(stored.ChavePublicaUsada);
                Console.WriteLine($"   PublicKey PEM: {publicKeyPemBytes.Length} bytes");
                Console.WriteLine($"   PublicKey PEM preview: {System.Text.Encoding.UTF8.GetString(publicKeyPemBytes).Substring(0, Math.Min(50, publicKeyPemBytes.Length))}...");

                // ================================
                // 🔟 Converte assinatura
                // ================================
                Console.WriteLine($"\n🔟 Convertendo assinatura...");

                byte[] signatureBytes = Convert.FromBase64String(signature.SignatureValue);
                Console.WriteLine($"   Signature: {signatureBytes.Length} bytes");

                // ================================
                // 1️⃣1️⃣ ✅ CORREÇÃO: Passa o PEM original diretamente para VerifyAsync
                // ================================
                Console.WriteLine($"\n1️⃣1️⃣ Validando assinatura PQC ({signature.Algorithm})...");

                bool valid = await _pqcSigner.VerifyAsync(
                    hashBytes,
                    signatureBytes,
                    publicKeyPemBytes  // ✅ PEM original em bytes, não bytes puros da chave
                );

                Console.WriteLine($"   Resultado: {(valid ? "✓ VÁLIDA" : "❌ INVÁLIDA")}");

                if (!valid)
                {
                    Console.WriteLine("   ❌ Assinatura criptográfica inválida!");
                    return Invalid(signature, "Assinatura criptográfica inválida.");
                }

                Console.WriteLine("   ✓ Assinatura PQC válida");

                // ================================
                // 1️⃣2️⃣ Verifica adulteração XMP
                // Compara Base64 do PEM: XMP vs banco (ambos agora são PEM original)
                // ================================
                Console.WriteLine($"\n1️⃣2️⃣ Verificando integridade do XMP...");

                bool keyTampered = Normalize(signature.PublicKey) != Normalize(stored.ChavePublicaUsada);

                if (keyTampered)
                {
                    Console.WriteLine("   ⚠️ Chave pública no XMP foi alterada!");
                }
                else
                {
                    Console.WriteLine("   ✓ Chave pública íntegra");
                }

                // ================================
                // 1️⃣3️⃣ OK
                // ================================
                Console.WriteLine($"\n✅ ASSINATURA #{signature.Order} VÁLIDA!");
                Console.WriteLine("━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━");

                return new SignatureValidationDetail
                {
                    SignatureOrder = signature.Order,
                    SignerName = signature.SignerName,
                    Algorithm = signature.Algorithm,
                    SignedAt = signature.SignedAt,
                    PageNumber = signature.PageNumber,
                    IsValid = true,
                    ValidationMessage = keyTampered
                        ? "Assinatura válida, XMP alterado."
                        : "Assinatura válida."
                };
            }
            catch (Exception ex)
            {
                Console.WriteLine($"\n❌ EXCEÇÃO: {ex.Message}");
                Console.WriteLine($"Stack Trace:\n{ex.StackTrace}");
                return Invalid(signature, $"Erro interno: {ex.Message}");
            }
        }

        // ======================================================
        // REMOVE PÁGINA
        // ======================================================

        private byte[] RemoveSignaturePage(byte[] pdf, int page)
        {
            using var input = new MemoryStream(pdf);
            using var output = new MemoryStream();

            using var reader = new PdfReader(input);
            using var writer = new PdfWriter(output);
            using var doc = new PdfDocument(reader, writer);

            doc.RemovePage(page);
            doc.Close();

            return PdfCleanupHelper.StabilizePdf(output.ToArray());
        }

        // ======================================================
        // HELPERS
        // ======================================================

        private string Normalize(string s)
        {
            if (string.IsNullOrEmpty(s))
                return "";

            return s
                .Replace(" ", "")
                .Replace("\n", "")
                .Replace("\r", "")
                .Replace("\t", "")
                .Trim();
        }

        private SignatureValidationDetail Invalid(
            ExtractedSignature sig,
            string msg)
        {
            return new SignatureValidationDetail
            {
                SignatureOrder = sig.Order,
                SignerName = sig.SignerName,
                Algorithm = sig.Algorithm,
                SignedAt = sig.SignedAt,
                PageNumber = sig.PageNumber,
                IsValid = false,
                ValidationMessage = msg
            };
        }
    }

    // ======================================================
    // RESULT MODELS
    // ======================================================

    public class DocumentValidationResult
    {
        public bool IsValid { get; set; }
        public string Message { get; set; }
        public List<SignatureValidationDetail> SignatureResults { get; set; }
        public int TotalSignatures { get; set; }
    }

    public class SignatureValidationDetail
    {
        public int SignatureOrder { get; set; }
        public string SignerName { get; set; }
        public string Algorithm { get; set; }
        public DateTime SignedAt { get; set; }
        public int PageNumber { get; set; }
        public bool IsValid { get; set; }
        public string ValidationMessage { get; set; }
    }
}