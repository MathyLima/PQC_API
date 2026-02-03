namespace PQC.MODULES.Documents.Application.UseCases.Sign
{
    using iText.Kernel.Pdf;
    using Microsoft.Extensions.Logging;
    using PQC.MODULES.Documents.Application.DTOs;
    using PQC.MODULES.Documents.Application.Interfaces.PDFcomposer;
    using PQC.MODULES.Documents.Application.Interfaces.PQCsigner;
    using PQC.MODULES.Documents.Domain.Entities;
    using PQC.MODULES.Documents.Domain.Interfaces;
    using PQC.MODULES.Documents.Infraestructure.Repositories;
    using PQC.MODULES.Documents.Infraestructure.DocumentProcessing;
    using PQC.MODULES.Users.Domain.Entities;
    using PQC.SHARED.Communication.DTOs.Documents.Responses;
    using PQC.SHARED.Communication.Interfaces;
    using PQC.SHARED.Exceptions.Domain;
    using PQC.SHARED.Time;
    using System.Security.Cryptography;
    using System.Text;

    public class SignDocumentUseCase
    {
        private readonly IDocumentRepository _documentRepository;
        private readonly INativePostQuantumSigner _signatureService;
        private readonly ICurrentUserService _currentUserService;
        private readonly ILogger<SignDocumentUseCase> _logger;
        private readonly IFileStorageService _storageService;
        private readonly IUserRepository _userRepository;
        private readonly IDocumentComposer _documentComposer;
        private readonly IKeyReader _keyReader;

        public SignDocumentUseCase(
            IDocumentRepository documentRepository,
            IFileStorageService storageService,
            IDocumentComposer documentComposer,
            INativePostQuantumSigner signatureService,
            ICurrentUserService currentUserService,
            IUserRepository userRepository,
            IKeyReader keyReader,
            ILogger<SignDocumentUseCase> logger)
        {
            _documentRepository = documentRepository;
            _signatureService = signatureService;
            _storageService = storageService;
            _documentComposer = documentComposer;
            _currentUserService = currentUserService;
            _logger = logger;
            _userRepository = userRepository;
            _keyReader = keyReader;
        }

        public async Task<SignDocumentResponse> Execute(DocumentUploadRequest request)
        {
            _logger.LogInformation("🚀 Starting document signing process");

            var user = await AuthenticateUser(request.UserId);
            var documentId = Guid.NewGuid().ToString();

            // ========================================
            // 1. Extrai XMP ANTES de normalizar
            //    (se o PDF já foi assinado anteriormente, o XMP pode ser perdido na normalização)
            // ========================================
            _logger.LogInformation("📄 Extraindo XMP existente antes de normalizar...");
            string existingXmp = ExtractExistingXmp(request.Content);

            if (!string.IsNullOrEmpty(existingXmp))
            {
                _logger.LogInformation($"📄 XMP existente encontrado ({existingXmp.Length} chars) — será preservado");
            }
            else
            {
                _logger.LogInformation("📄 Sem XMP existente — primeira assinatura");
            }

            // ========================================
            // 2. Normalização
            // ========================================
            _logger.LogInformation("📄 Normalizando PDF...");
            var normalizedPdf = await _documentComposer.NormalizePdfAsync(request.Content);
            _logger.LogInformation($"📄 PDF normalizado: {normalizedPdf.Length} bytes");

            // ========================================
            // 3. Hash do PDF normalizado
            // ========================================
            var hashBytes = SHA256.HashData(normalizedPdf);
            var hashBase64 = Convert.ToBase64String(hashBytes);
            _logger.LogInformation($"📄 Original PDF Hash: {hashBase64}");

            // ========================================
            // 4. SALVA O PDF ORIGINAL (sem sufixo)
            // ========================================
            _logger.LogInformation("💾 Salvando PDF original (sem assinatura)...");
            var originalReference = await _storageService.SaveFileAsync(
                normalizedPdf,
                request.FileName,
                request.ContentType,
                user.Id
            );
            _logger.LogInformation($"💾 PDF original salvo em: {originalReference}");

            // ========================================
            // 5. Recupera chaves
            // ========================================
            var privateKey = await _keyReader.GetPrivateKeyAsync(user.Id.ToString());
            var publicKeyPemBytes = await _keyReader.GetPublicKeyAsync(user.Id.ToString());

            // ========================================
            // 6. Assina o HASH
            // ========================================
            var signatureResult = await _signatureService.SignAsync(hashBytes, privateKey);
            _logger.LogInformation("✍️ Hash signed successfully");

            // ========================================
            // 7. Metadata
            // ========================================
            var metadata = new SignatureMetadata
            {
                DocumentId = documentId,
                DocumentName = request.FileName,
                SignerId = user.Id.ToString(),
                SignerName = user.Nome,
                SignerEmail = user.Email,
                DocumentHash = hashBase64,
                HashAlgorithm = "SHA-256",
                SignatureValue = Convert.ToBase64String(signatureResult.Signature),
                Algorithm = signatureResult.Algorithm,
                PublicKey = Convert.ToBase64String(publicKeyPemBytes),
                SignedAt = RecifeTimeProvider.Now()
            };

            // ========================================
            // 8. Gera PDF assinado (em memória)
            // ========================================
            _logger.LogInformation("📝 Gerando PDF assinado...");

            // Re-injeta o XMP no PDF normalizado ANTES de adicionar a página de metadata
            // Assim o AddMetadataPageAsync vai encontrar o XMP e preservar
            var pdfWithXmp = normalizedPdf;
            if (!string.IsNullOrEmpty(existingXmp))
            {
                pdfWithXmp = ReInjectXmp(normalizedPdf, existingXmp);
                _logger.LogInformation("📝 XMP re-injetado no PDF normalizado");
            }

            var pdfWithMetadata = await _documentComposer.AddMetadataPageAsync(
                pdfWithXmp,
                metadata
            );

            var signedPdf = await _documentComposer.AddXmpSignatureAsync(
                pdfWithMetadata,
                signatureResult.Signature,
                metadata
            );

            _logger.LogInformation($"📝 PDF assinado gerado: {signedPdf.Length} bytes");

            // ========================================
            // 9. SALVA O PDF ASSINADO COM SUFIXO _signed
            // ========================================
            _logger.LogInformation("💾 Salvando PDF assinado...");

            var signedFileName = AddSignedSuffix(request.FileName);

            var signedReference = await _storageService.SaveFileAsync(
                signedPdf,
                signedFileName,
                request.ContentType,
                user.Id
            );

            _logger.LogInformation($"💾 PDF assinado salvo em: {signedReference}");

            // ========================================
            // 10. Persistência no banco
            // ========================================
            var document = StoredDocument.CreateSigned(
                documentId,
                originalReference,
                signedReference,
                normalizedPdf,
                hashBase64,
                request.FileName,
                user.Id.ToString(),
                request.ContentType!,
                metadata.Algorithm,
                metadata.SignatureValue,
                metadata.PublicKey,
                metadata.SignedAt,
                signedPdf.Length
            );

            await _documentRepository.AddAsync(document);
            await _documentRepository.SaveChangesAsync();

            _logger.LogInformation("✅ Document successfully signed");

            return new SignDocumentResponse
            {
                DocumentId = Guid.Parse(documentId),
                DocumentName = request.FileName,
                SignedContent = signedPdf,
                ContentType = request.ContentType!,
                Algorithm = metadata.Algorithm,
                FileSize = signedPdf.Length,
                SignedAt = metadata.SignedAt
            };
        }

        // ========================================
        // HELPERS
        // ========================================

        /// <summary>
        /// Extrai o XMP customizado do PDF sem modificá-lo
        /// </summary>
        private string ExtractExistingXmp(byte[] pdfContent)
        {
            try
            {
                using var ms = new MemoryStream(pdfContent);
                using var reader = new PdfReader(ms);
                using var doc = new PdfDocument(reader);
                return CustomXmpHandler.ExtractCustomXmp(doc);
            }
            catch (Exception ex)
            {
                _logger.LogWarning($"⚠️ Falha ao extrair XMP: {ex.Message}");
                return string.Empty;
            }
        }

        /// <summary>
        /// Re-injeta o XMP customizado em um PDF
        /// </summary>
        private byte[] ReInjectXmp(byte[] pdfContent, string xmpContent)
        {
            using var inputMs = new MemoryStream(pdfContent);
            using var outputMs = new MemoryStream();
            using var reader = new PdfReader(inputMs);
            using var writer = new PdfWriter(outputMs);
            using var doc = new PdfDocument(reader, writer);

            CustomXmpHandler.InjectCustomXmp(doc, xmpContent);
            doc.Close();

            return outputMs.ToArray();
        }

        /// <summary>
        /// Adiciona o sufixo "_signed" antes da extensão do arquivo
        /// </summary>
        private string AddSignedSuffix(string fileName)
        {
            var extension = Path.GetExtension(fileName);
            var nameWithoutExtension = Path.GetFileNameWithoutExtension(fileName);
            return $"{nameWithoutExtension}_signed{extension}";
        }

        private async Task<User> AuthenticateUser(string userId)
        {
            var user = await _userRepository.GetByIdAsync(userId.ToString());
            if (user == null)
            {
                throw new EntityNotFoundException("User not found");
            }

            return new User
            {
                Cpf = user.Cpf,
                Nome = user.Nome,
                Email = user.Email,
                Id = user.Id
            };
        }
    }
}