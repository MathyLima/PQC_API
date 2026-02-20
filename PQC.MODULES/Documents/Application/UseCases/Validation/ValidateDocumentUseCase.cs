using PQC.MODULES.Documents.Application.Interfaces.PQCsigner;
using PQC.MODULES.Documents.Infraestructure.Repositories;
using System.Text.Json;
using System.Text.Json.Serialization;

namespace PQC.MODULES.Documents.Application.UseCases.Validation
{
    public class ValidateDocumentUseCase
    {
        private readonly IDocumentRepository _repository;
        private readonly INativePostQuantumSigner _pqcSigner;

        public ValidateDocumentUseCase(
            IDocumentRepository repository,
            INativePostQuantumSigner pqcSigner)
        {
            _repository = repository;
            _pqcSigner = pqcSigner;
        }

        public async Task<DocumentValidationResult> Execute(string javaSignaturesJson)
        {
            Console.WriteLine("\n══════════════════════════════════════════════");
            Console.WriteLine("   INICIANDO VALIDAÇÃO PQC");
            Console.WriteLine("══════════════════════════════════════════════\n");

            // ========================================
            // 1️⃣ Deserializar assinaturas do Java
            // ========================================
            List<JavaSignatureInfo> javaSignatures;

            try
            {
                var options = new JsonSerializerOptions
                {
                    PropertyNameCaseInsensitive = true,
                    NumberHandling = JsonNumberHandling.AllowReadingFromString
                };

                javaSignatures = JsonSerializer.Deserialize<List<JavaSignatureInfo>>(
                    javaSignaturesJson,
                    options
                );

                if (javaSignatures == null || javaSignatures.Count == 0)
                {
                    Console.WriteLine("❌ Nenhuma assinatura encontrada no JSON");
                    return new DocumentValidationResult
                    {
                        IsValid = false,
                        Message = "Nenhuma assinatura encontrada no documento.",
                        SignatureResults = new List<SignatureValidationDetail>(),
                        TotalSignatures = 0
                    };
                }

                Console.WriteLine($"✓ {javaSignatures.Count} assinatura(s) encontrada(s)\n");
            }
            catch (Exception ex)
            {
                Console.WriteLine($"❌ Erro ao deserializar JSON: {ex.Message}");
                return new DocumentValidationResult
                {
                    IsValid = false,
                    Message = $"Erro ao processar assinaturas: {ex.Message}",
                    SignatureResults = new List<SignatureValidationDetail>(),
                    TotalSignatures = 0
                };
            }

            // ========================================
            // 2️⃣ Ordenar da última para primeira
            // ========================================
            var orderedSignatures = javaSignatures
                .OrderByDescending(s => s.Number)
                .ToList();

            Console.WriteLine("📋 Ordem de validação (da mais recente para a mais antiga):");
            foreach (var sig in orderedSignatures)
            {
                Console.WriteLine($"   #{sig.Number}: {sig.Name}");
            }
            Console.WriteLine();

            // ========================================
            // 3️⃣ Validar cada assinatura
            // ========================================
            var results = new List<SignatureValidationDetail>();

            foreach (var javaSig in orderedSignatures)
            {
                Console.WriteLine($"━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━");
                Console.WriteLine($"📝 Validando assinatura #{javaSig.Number}");
                Console.WriteLine($"   Signatário: {javaSig.Name}");
                Console.WriteLine($"━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━");

                var result = await ValidateSingleSignature(javaSig);

                results.Add(result);

                if (!result.IsValid)
                {
                    Console.WriteLine($"\n❌ VALIDAÇÃO FALHOU na assinatura #{javaSig.Number}");
                    Console.WriteLine($"   Motivo: {result.ValidationMessage}");
                    Console.WriteLine($"   As assinaturas anteriores não serão validadas.");
                    Console.WriteLine("━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━\n");
                    break;
                }

                Console.WriteLine($"\n✅ Assinatura #{javaSig.Number} VÁLIDA");
                Console.WriteLine("━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━\n");
            }

            // ========================================
            // 4️⃣ Reverter ordem para exibição
            // ========================================
            results.Reverse();

            // ========================================
            // 5️⃣ Gerar resultado final
            // ========================================
            bool allValid = results.All(r => r.IsValid);

            Console.WriteLine("\n══════════════════════════════════════════════");
            if (allValid)
            {
                Console.WriteLine("   ✅ DOCUMENTO VÁLIDO");
                Console.WriteLine($"   Total de assinaturas verificadas: {results.Count}");
            }
            else
            {
                Console.WriteLine("   ❌ DOCUMENTO INVÁLIDO");
                var invalidCount = results.Count(r => !r.IsValid);
                Console.WriteLine($"   Assinaturas inválidas: {invalidCount}");
                Console.WriteLine($"   Assinaturas válidas: {results.Count - invalidCount}");
            }
            Console.WriteLine("══════════════════════════════════════════════\n");

            return new DocumentValidationResult
            {
                IsValid = allValid,
                Message = allValid
                    ? $"Documento válido com {results.Count} assinatura(s)"
                    : "Documento inválido - uma ou mais assinaturas comprometidas",
                SignatureResults = results,
                TotalSignatures = javaSignatures.Count
            };
        }

        // ======================================================
        // VALIDA UMA ASSINATURA INDIVIDUAL
        // ======================================================
        private async Task<SignatureValidationDetail> ValidateSingleSignature(JavaSignatureInfo javaSig)
        {
            try
            {
                Console.WriteLine($"\n📋 Validando assinatura #{javaSig.Number}");

                if (!javaSig.ByteRangeValid || javaSig.ByteRange?.Length != 4)
                    return Invalid(javaSig, "ByteRange inválido");

                if (string.IsNullOrEmpty(javaSig.SignatureBase64))
                    return Invalid(javaSig, "Assinatura ausente");

                if (string.IsNullOrEmpty(javaSig.ToBeSignedBase64))
                    return Invalid(javaSig, "ToBeSignedBase64 ausente — atualize o serviço Java");

                byte[] bytesToVerify = Convert.FromBase64String(javaSig.ToBeSignedBase64);
                Console.WriteLine($"   Bytes a verificar (ByteRange): {bytesToVerify.Length} bytes");

                // Extrair assinatura ML-DSA pura do envelope PKCS#7
                byte[] pkcs7Bytes = Convert.FromBase64String(javaSig.SignatureBase64);
                //byte[] pkcs7Clean = RemoveZeroPadding(pkcs7Bytes);
                //Console.WriteLine($"   PKCS#7 limpo: {pkcs7Clean.Length} bytes");

                byte[] signature = ExtractRawSignatureFromPkcs7(pkcs7Bytes);
                Console.WriteLine($"   Assinatura ML-DSA extraída: {signature.Length} bytes");

                // Chave pública
                if (string.IsNullOrEmpty(javaSig.PublicKeyBase64))
                    return Invalid(javaSig, "Chave pública ausente");

                byte[] publicKey = Convert.FromBase64String(javaSig.PublicKeyBase64);
                Console.WriteLine($"   Chave pública: {publicKey.Length} bytes");

                Console.WriteLine("🔐 Verificando assinatura PQC...");

                // ✅ Verifica: ML-DSA(bytesToVerify) == signature usando publicKey
                bool valid = await _pqcSigner.VerifyAsync(bytesToVerify, signature, publicKey);

                if (!valid)
                {
                    Console.WriteLine("❌ Assinatura criptográfica inválida");
                    return Invalid(javaSig, "Assinatura criptográfica inválida");
                }

                Console.WriteLine("✅ Assinatura criptográfica válida");

                return new SignatureValidationDetail
                {
                    SignatureOrder = javaSig.Number,
                    SignerName = javaSig.Name,
                    Algorithm = "ML-DSA-44",
                    SignedAt = javaSig.SignDate,
                    PageNumber = 0,
                    IsValid = true,
                    ValidationMessage = "Assinatura válida"
                };
            }
            catch (Exception ex)
            {
                Console.WriteLine($"❌ Erro interno: {ex.Message}");
                Console.WriteLine($"Stack trace: {ex.StackTrace}");
                return Invalid(javaSig, $"Erro interno: {ex.Message}");
            }
        }

        // ======================================================
        // EXTRAIR ASSINATURA ML-DSA DO ENVELOPE PKCS#7
        // ======================================================
        // ======================================================
        // EXTRAIR ASSINATURA ML-DSA DO /Contents
        // ======================================================
        private byte[] ExtractRawSignatureFromPkcs7(byte[] contentsBytes)
        {
            // O /Contents agora contém a assinatura ML-DSA pura (não PKCS#7).
            // O PDFBox retorna os bytes do campo hex como bytes brutos,
            // mas pode vir com zero-padding ao final — remover.

            Console.WriteLine($"   /Contents recebido: {contentsBytes.Length} bytes");

            // Remove zero-padding do PDFBox
            int actualLength = contentsBytes.Length;
            for (int i = contentsBytes.Length - 1; i >= 0; i--)
            {
                if (contentsBytes[i] != 0x00) { actualLength = i + 1; break; }
            }

            var clean = new byte[actualLength];
            Array.Copy(contentsBytes, 0, clean, 0, actualLength);

            Console.WriteLine($"   Assinatura após remover padding: {clean.Length} bytes");

            // Validar tamanho esperado para ML-DSA
            // ML-DSA-44 = 2420, ML-DSA-65 = 3309, ML-DSA-87 = 4627
            int[] expectedSizes = { 2420, 3309, 4627 };
            bool sizeOk = expectedSizes.Contains(clean.Length);
            Console.WriteLine($"   Tamanho válido para ML-DSA: {sizeOk} ({clean.Length} bytes)");

            return clean;
        }

        // ======================================================
        // HELPERS
        // ======================================================
        private SignatureValidationDetail Invalid(JavaSignatureInfo sig, string msg)
        {
            return new SignatureValidationDetail
            {
                SignatureOrder = sig.Number,
                SignerName = sig.Name ?? "Desconhecido",
                Algorithm = "ML-DSA-44",
                SignedAt = sig.SignDate,
                PageNumber = 0,
                IsValid = false,
                ValidationMessage = msg
            };
        }
    }

    // ======================================================
    // MODELS PARA DESERIALIZAR JSON DO JAVA
    // ======================================================
    public class JavaSignatureInfo
    {
        [JsonPropertyName("index")]
        public int Number { get; set; }

        public string Name { get; set; }
        public string Reason { get; set; }
        public string Location { get; set; }

        [JsonPropertyName("signDate")]
        public DateTime? SignDateObj { get; set; }

        [JsonIgnore]
        public DateTime SignDate => SignDateObj ?? DateTime.MinValue;

        public string Filter { get; set; }
        public string SubFilter { get; set; }
        public string PublicKeyBase64 { get; set; }
        public string SignatureBase64 { get; set; }
        public int SignatureSize { get; set; }

        public int[] ByteRange { get; set; }
        public bool ByteRangeValid { get; set; }
        public string ByteRangeHashBase64 { get; set; }
        public string ByteRangeHashHex { get; set; }

        // ✅ NOVO: bytes exatos que o ML-DSA assinou (ByteRange content sem /Contents)
        public string ToBeSignedBase64 { get; set; }
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