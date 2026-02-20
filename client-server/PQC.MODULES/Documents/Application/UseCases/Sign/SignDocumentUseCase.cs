namespace PQC.MODULES.Documents.Application.UseCases.Sign
{
    using iText.Kernel.Pdf;
    using Microsoft.Extensions.Logging;
    using PQC.MODULES.Documents.Application.DTOs;
    using PQC.MODULES.Documents.Application.Interfaces.PDFcomposer;
    using PQC.MODULES.Documents.Application.Interfaces.PQCsigner;
    using PQC.MODULES.Documents.Domain.Entities;
    using PQC.MODULES.Documents.Domain.Interfaces;
    using PQC.MODULES.Documents.Infraestructure.DocumentProcessing;
    using PQC.MODULES.Documents.Infraestructure.Repositories;
    using PQC.MODULES.Users.Domain.Entities;
    using PQC.SHARED.Communication.DTOs.Documents.Responses;
    using PQC.SHARED.Communication.Interfaces;
    using PQC.SHARED.Exceptions.Domain;
    using PQC.SHARED.Time;
    using System.Security.Cryptography;

    public class SignDocumentUseCase
    {
        private readonly IDocumentRepository _documentRepository;
        private readonly INativePostQuantumSigner _signatureService;
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
            _logger = logger;
            _userRepository = userRepository;
            _keyReader = keyReader;
        }
        /// <summary>
        /// Prepara os metadados incluindo a chave pública
        /// </summary>
        public async Task<object> PrepareMetadata(string userId, string fileName, string documentId)
        {
            var user = await AuthenticateUser(userId);

            // Recuperar chave pública
            var publicKeyPemBytes = await _keyReader.GetPublicKeyAsync(user.Id.ToString());
            var publicKeyBase64 = Convert.ToBase64String(publicKeyPemBytes);

            return new
            {
                documentId = documentId,
                documentName = fileName,
                signedAt = DateTime.UtcNow.ToString("yyyy.MM.dd HH:mm:ss 'UTC'"),
                signerName = $"{user.Nome}:{user.Cpf}",
                signerEmail = user.Email,
                algorithm = "ml-dsa",
                parameters = "ml-dsa-44",
                hashAlgorithm = "SHA-256",
                publicKey = publicKeyBase64, // ✅ Chave pública aqui
                signatureValue = "" // Será preenchido depois
            };
        }

        public async Task<SignDocumentResponse> Execute(
     SignDocumentRequest request,
     string userId,
     string documentId,
     string originalFilePath)
        {
            _logger.LogInformation("🚀 Starting document signing process");
            _logger.LogInformation($"📋 DocumentId: {documentId}");
            _logger.LogInformation($"📋 UserId: {userId}");

            // ========================================
            // 1. Autenticar usuário
            // ========================================
            var user = await AuthenticateUser(userId);
            _logger.LogInformation($"👤 User authenticated: {user.Nome} ({user.Email})");

            // ========================================
            // 2. Validar nome do arquivo
            // ========================================
            if (string.IsNullOrWhiteSpace(request.FileName))
            {
                _logger.LogError("❌ Nome do arquivo inválido ou vazio");
                throw new ArgumentException("Nome do arquivo é obrigatório", nameof(request.FileName));
            }

            _logger.LogInformation($"📄 FileName: {request.FileName}");

            // ========================================
            // 3. Recuperar chaves
            // ========================================
            _logger.LogInformation("🔑 Recuperando chaves do usuário...");

            var privateKey = await _keyReader.GetPrivateKeyAsync(user.Id.ToString());
            var publicKeyPemBytes = await _keyReader.GetPublicKeyAsync(user.Id.ToString());

            _logger.LogInformation($"🔑 Chave privada: {privateKey.Length} bytes");
            _logger.LogInformation($"🔑 Chave pública: {publicKeyPemBytes.Length} bytes");

            // ========================================
            // 4. Assinar os bytes do ByteRange (ML-DSA faz o hash internamente)
            // ========================================
            _logger.LogInformation("✍️ Assinando Conteudo do ByteRange...");
            _logger.LogInformation($"📊 Conteudo size: {request.DataToSign.Length} bytes");
            _logger.LogInformation($"📊 Conteudo Base64: {Convert.ToBase64String(request.DataToSign).Substring(0, Math.Min(20, Convert.ToBase64String(request.DataToSign).Length))}...");

            var signatureResult = await _signatureService.SignAsync(request.DataToSign, privateKey);

            _logger.LogInformation($"✍️ Assinatura gerada: {signatureResult.Signature.Length} bytes");
            _logger.LogInformation($"✍️ Algoritmo: {signatureResult.Algorithm}");

            // ========================================
            // 5. Preparar dados para persistência
            // ========================================
            var hashBase64 = Convert.ToBase64String(request.DataToSign);
            var signatureBase64 = Convert.ToBase64String(signatureResult.Signature);
            var publicKeyBase64 = Convert.ToBase64String(publicKeyPemBytes);

            _logger.LogInformation("📦 Preparando dados para o banco...");
            _logger.LogInformation($"📦 Hash (Base64): {hashBase64.Substring(0, Math.Min(30, hashBase64.Length))}...");
            _logger.LogInformation($"📦 Signature (Base64): {signatureBase64.Substring(0, Math.Min(30, signatureBase64.Length))}...");
            _logger.LogInformation($"📦 PublicKey (Base64): {publicKeyBase64.Substring(0, Math.Min(30, publicKeyBase64.Length))}...");

            // ========================================
            // 6. Calcular hash do arquivo original
            // ========================================
            byte[] originalPdfBytes = null;
            string originalFileHash = null;

            if (File.Exists(originalFilePath))
            {
                originalPdfBytes = await File.ReadAllBytesAsync(originalFilePath);
                var originalHashBytes = SHA256.HashData(originalPdfBytes);
                originalFileHash = Convert.ToBase64String(originalHashBytes);

                _logger.LogInformation($"📄 PDF original: {originalPdfBytes.Length} bytes");
                _logger.LogInformation($"📄 Hash do arquivo original: {originalFileHash.Substring(0, Math.Min(30, originalFileHash.Length))}...");
            }
            else
            {
                _logger.LogWarning($"⚠️ Arquivo original não encontrado: {originalFilePath}");
                originalPdfBytes = null;
                originalFileHash = null;
            }


            // ========================================
            // 7. Criar entidade do documento
            // ========================================
            _logger.LogInformation("💾 Criando entidade do documento...");

            try
            {
                var document = StoredDocument.CreateSigned(
                    documentId,
                    originalFilePath,                    // Caminho do PDF original
                    request.PreparedFilePath,            // Caminho do PDF preparado (será o signed)
                    originalPdfBytes,                    // Conteúdo do PDF original
                    originalFileHash,                    // Hash do arquivo original
                    request.FileName,                    // ✅ Nome do arquivo (já validado)
                    user.Id.ToString(),
                    "application/pdf",
                    signatureResult.Algorithm,
                    signatureBase64,
                    publicKeyBase64,
                    DateTime.UtcNow,
                    originalPdfBytes?.Length ?? 0
                );

                // ========================================
                // 8. Persistir no banco
                // ========================================
                _logger.LogInformation("💾 Salvando no banco de dados...");

                await _documentRepository.AddAsync(document);
                await _documentRepository.SaveChangesAsync();

                _logger.LogInformation("✅ Documento salvo com sucesso no banco!");
                _logger.LogInformation($"✅ DocumentId: {documentId}");
                _logger.LogInformation($"✅ OriginalPath: {originalFilePath}");
                _logger.LogInformation($"✅ SignedPath: {request.PreparedFilePath}");
                _logger.LogInformation($"✅ FileName: {request.FileName}");
            }
            catch (DomainException ex)
            {
                _logger.LogError($"❌ Erro de validação de domínio: {ex.Message}");
                throw;
            }
            catch (Exception ex)
            {
                _logger.LogError($"❌ Erro ao salvar documento: {ex.Message}");
                _logger.LogError($"Stack trace: {ex.StackTrace}");
                throw;
            }

            // ========================================
            // 9. Retornar resposta
            // ========================================
            _logger.LogInformation("🎉 Processo de assinatura concluído!");

            return new SignDocumentResponse
            {
                DocumentId = Guid.Parse(documentId),
                DocumentName = request.FileName,
                SignedContent = signatureResult.Signature,
                ContentType = "application/pdf",
                Algorithm = signatureResult.Algorithm,
                FileSize = originalPdfBytes?.Length ?? 0,
                SignedAt = DateTime.UtcNow
            };
        }

        private async Task<User> AuthenticateUser(string userId)
        {
            var user = await _userRepository.GetByIdAsync(userId.ToString());
            if (user == null)
            {
                _logger.LogError($"❌ Usuário não encontrado: {userId}");
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