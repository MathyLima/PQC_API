/*
using PQC.MODULES.Documents.Infraestructure.Repositories;

namespace PQC.MODULES.Documents.Application.UseCases.Validation
{
    public class ValidateDocumentUseCase
    {
        private readonly IDocumentRepository _repository;
        private readonly ISignatureMetadataExtractor _metadataExtractor;
        private readonly SignUploadedDocumentUseCase _signUseCase;

        public ValidateDocumentUseCase(
            IDocumentRepository repository,
            ISignatureMetadataExtractor metadataExtractor,
            SignUploadedDocumentUseCase signUseCase)
        {
            _repository = repository;
            _metadataExtractor = metadataExtractor;
            _signUseCase = signUseCase;
        }

        public async Task<ValidationResult> Execute(ValidateDocumentContentJson request)
        {
            var content = request.Content;

            // 1. Extrair todas as assinaturas do documento
            var signatures = await _metadataExtractor.ExtractSignaturesFromPdfAsync(content);

            if (!signatures.Any())
            {
                return new ValidationResult
                {
                    IsValid = false,
                    Message = "Nenhuma assinatura encontrada no documento."
                };
            }

            var validationResults = new List<SignatureValidationResult>();

            // 2. Validar cada assinatura da ÚLTIMA para a PRIMEIRA
            var orderedSignatures = signatures.OrderByDescending(s => s.PageNumber).ToList();
            byte[] currentDocument = content;

            foreach (var signature in orderedSignatures)
            {
                Console.WriteLine($"\n=== VALIDANDO ASSINATURA DA PÁGINA {signature.PageNumber} ===");

                // 2.1. Validar esta assinatura
                var result = await ValidateSingleSignature(signature, currentDocument);
                validationResults.Add(result);

                // 2.2. Remover a página de metadados desta assinatura para validar a próxima
                currentDocument = RemoveSignaturePage(currentDocument, signature.PageNumber);
            }

            // 3. Reordenar resultados (primeira assinatura primeiro na apresentação)
            validationResults.Reverse();

            // 4. Compilar resultado final
            bool allValid = validationResults.All(r => r.IsValid);

            return new ValidationResult
            {
                IsValid = allValid,
                Message = allValid
                    ? $"✓ Documento válido com {signatures.Count} assinatura(s)."
                    : "✗ Algumas assinaturas são inválidas.",
                SignatureResults = validationResults,
                TotalSignatures = signatures.Count
            };
        }

        private async Task<SignatureValidationResult> ValidateSingleSignature(
    ExtractedSignatureData signature,
    byte[] documentContent)
        {
            // 1. Buscar documento no banco
            var storedDoc = await _repository.GetByCpfAndTimestamp(
                signature.SignerCpf,
                signature.SignedAt);

            // 2. Verificar se encontrou o documento
            if (storedDoc == null)
            {
                return new SignatureValidationResult
                {
                    SignerName = signature.SignerName,
                    SignerCpf = signature.SignerCpf,
                    SignerEmail = signature.SignerEmail,
                    Algorithm = signature.Algorithm,
                    SignedAt = signature.SignedAt,
                    PageNumber = signature.PageNumber,
                    IsValid = false,
                    ValidationMessage = "Assinatura não encontrada no sistema"
                };
            }

            try
            {
                // 3. Remover a página de metadados desta assinatura
                Console.WriteLine($"\n📄 Removendo página de metadados {signature.PageNumber}...");
                var documentWithoutMetadata = RemoveSignaturePage(documentContent, signature.PageNumber);
                Console.WriteLine($"Documento original: {documentContent.Length} bytes");
                Console.WriteLine($"Documento sem metadata: {documentWithoutMetadata.Length} bytes");

                // 4. Re-assinar o documento sem a página de metadados
                Console.WriteLine($"\n🔐 Re-assinando documento...");
                var newSignatureResult = await _signUseCase.Execute(documentWithoutMetadata);

                if (!newSignatureResult.Success)
                {
                    return new SignatureValidationResult
                    {
                        SignerName = signature.SignerName,
                        SignerCpf = signature.SignerCpf,
                        SignerEmail = signature.SignerEmail,
                        Algorithm = signature.Algorithm,
                        SignedAt = signature.SignedAt,
                        PageNumber = signature.PageNumber,
                        IsValid = false,
                        ValidationMessage = $"Erro ao re-assinar documento: {newSignatureResult.ErrorMessage}"
                    };
                }

                // 5. Normalizar e comparar as assinaturas
                var extractedHash = NormalizeBase64(signature.SignatureHash);
                var storedHash = NormalizeBase64(storedDoc.AssinaturaDigital);
                var recalculatedHash = NormalizeBase64(Convert.ToBase64String(newSignatureResult.Signature));

                Console.WriteLine($"\n🔍 COMPARAÇÃO DE ASSINATURAS:");
                Console.WriteLine($"Hash extraído do PDF:  {extractedHash.Substring(0, Math.Min(60, extractedHash.Length))}...");
                Console.WriteLine($"Hash armazenado no BD: {storedHash.Substring(0, Math.Min(60, storedHash.Length))}...");
                Console.WriteLine($"Hash recalculado:      {recalculatedHash.Substring(0, Math.Min(60, recalculatedHash.Length))}...");
                Console.WriteLine($"Tamanho extraído: {extractedHash.Length} chars");
                Console.WriteLine($"Tamanho armazenado: {storedHash.Length} chars");
                Console.WriteLine($"Tamanho recalculado: {recalculatedHash.Length} chars");

                // 6. Verificar se as assinaturas conferem
                bool extractedMatchesStored = extractedHash == storedHash;
                bool recalculatedMatchesStored = recalculatedHash == storedHash;
                bool allMatch = extractedMatchesStored && recalculatedMatchesStored;

                Console.WriteLine($"\n✓ Extraído == Armazenado? {extractedMatchesStored}");
                Console.WriteLine($"✓ Recalculado == Armazenado? {recalculatedMatchesStored}");
                Console.WriteLine($"✓ Validação final: {allMatch}");

                if (!allMatch)
                {
                    Console.WriteLine("\n⚠️ ASSINATURAS DIFERENTES!");
                    if (!extractedMatchesStored)
                        Console.WriteLine("  - Hash do PDF não bate com o armazenado (possível adulteração no PDF)");
                    if (!recalculatedMatchesStored)
                        Console.WriteLine("  - Hash recalculado não bate (documento foi modificado)");
                }

                // 7. Retornar resultado
                return new SignatureValidationResult
                {
                    SignerName = signature.SignerName,
                    SignerCpf = signature.SignerCpf,
                    SignerEmail = signature.SignerEmail,
                    Algorithm = signature.Algorithm,
                    SignedAt = signature.SignedAt,
                    PageNumber = signature.PageNumber,
                    IsValid = allMatch,
                    ValidationMessage = allMatch
                        ? "✓ Assinatura válida - documento íntegro e autêntico"
                        : "✗ ATENÇÃO: Assinatura inválida - documento foi adulterado ou corrompido"
                };
            }
            catch (Exception ex)
            {
                Console.WriteLine($"ERRO na validação: {ex.Message}");
                Console.WriteLine($"Stack trace: {ex.StackTrace}");
                return new SignatureValidationResult
                {
                    SignerName = signature.SignerName,
                    SignerCpf = signature.SignerCpf,
                    SignerEmail = signature.SignerEmail,
                    Algorithm = signature.Algorithm,
                    SignedAt = signature.SignedAt,
                    PageNumber = signature.PageNumber,
                    IsValid = false,
                    ValidationMessage = $"Erro na validação: {ex.Message}"
                };
            }
        }
        /// <summary>
        /// Normaliza string Base64 removendo espaços, quebras de linha e caracteres invisíveis
        /// </summary>
        private string NormalizeBase64(string base64String)
        {
            if (string.IsNullOrEmpty(base64String))
                return string.Empty;

            // Remove todos os espaços em branco (espaços, tabs, quebras de linha, etc)
            return base64String
                .Replace(" ", "")
                .Replace("\n", "")
                .Replace("\r", "")
                .Replace("\t", "")
                .Trim();
        }

        /// <summary>
        /// Remove uma página específica do PDF
        /// </summary>
        private byte[] RemoveSignaturePage(byte[] pdfContent, int pageNumberToRemove)
        {
            using var inputMs = new MemoryStream(pdfContent);
            using var reader = new PdfReader(inputMs);
            using var inputDoc = new PdfDocument(reader);

            using var outputMs = new MemoryStream();
            using var writer = new PdfWriter(outputMs);
            using var outputDoc = new PdfDocument(writer);

            int totalPages = inputDoc.GetNumberOfPages();
            Console.WriteLine($"Removendo página {pageNumberToRemove} de {totalPages} páginas totais");

            // Copiar todas as páginas EXCETO a página de assinatura
            for (int i = 1; i <= totalPages; i++)
            {
                if (i != pageNumberToRemove)
                {
                    inputDoc.CopyPagesTo(i, i, outputDoc);
                }
            }

            outputDoc.Close();
            return outputMs.ToArray();
        }

        // Classes de resultado
        public class ValidationResult
        {
            public bool IsValid { get; set; }
            public string Message { get; set; }
            public List<SignatureValidationResult> SignatureResults { get; set; }
            public int TotalSignatures { get; set; }
        }

        public class SignatureValidationResult
        {
            public string SignerName { get; set; }
            public string SignerCpf { get; set; }
            public string SignerEmail { get; set; }
            public string Algorithm { get; set; }
            public DateTime SignedAt { get; set; }
            public int PageNumber { get; set; }
            public bool IsValid { get; set; }
            public string ValidationMessage { get; set; }
        }
    }
}
*/