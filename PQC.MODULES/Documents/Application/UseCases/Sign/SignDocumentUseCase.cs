namespace PQC.MODULES.Documents.Application.UseCases.Sign
{
    using Microsoft.Extensions.Logging;
    using PQC.MODULES.Documents.Application.DTOs;
    using PQC.MODULES.Documents.Application.Interfaces.PDFcomposer;
    using PQC.MODULES.Documents.Application.Interfaces.PQCsigner;
    using PQC.MODULES.Documents.Domain.Entities;
    using PQC.MODULES.Documents.Domain.Interfaces;
    using PQC.MODULES.Documents.Infraestructure.Repositories;
    using PQC.MODULES.Users.Domain.Entities;
    using PQC.SHARED.Communication.DTOs.Documents.Responses;
    using PQC.SHARED.Communication.Interfaces;
    using PQC.SHARED.Exceptions.Domain;


    /// <summary>
    /// Use Case para assinar um documento usando criptografia pós-quântica.
    /// </summary>
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
            _keyReader = keyReader;  // ← ADICIONE ISSO
        }

        public async Task<SignDocumentResponse> Execute(DocumentUploadRequest request)
        {
            _logger.LogInformation("Starting document signing process");

            // Verifica se o usuário está autenticado
            var user = await AuthenticateUser(request.UserId);

            // Recuperar a chave privada do usuário
            // Mantém a interface retornando bytes
            var privateKey = await _keyReader.GetPrivateKeyAsync(user.Id.ToString());
            _logger.LogInformation("Private key retrieved successfully");
            // NativePostQuantumSigner salva temporariamente e chama a CLI
            var digitalSignature = await _signatureService.SignAsync(
                request.Content,
                privateKey
            );
            _logger.LogInformation("Document signed successfully using post-quantum algorithm");
            var documentId = Guid.NewGuid().ToString();

            // Criar metadados da assinatura
            var metadata = new SignatureMetadata
            {
                DocumentId = documentId,
                DocumentName = request.FileName,
                SignerName = user.Nome,
                SignerEmail = user.Email,
                SignerCpf = user.Cpf,
                Algorithm = digitalSignature.Algorithm,
                SignatureHash = Convert.ToBase64String(digitalSignature.Signature)
            };

            // Mesclar a assinatura no documento PDF
            var signedDocumentContent = await _documentComposer.ComposeForSignatureAsync(
                request.Content,
                metadata);
            _logger.LogInformation("Signature merged into document successfully");

            // Armazenar o documento assinado
            var documentReference = await _storageService.SaveFileAsync(
                signedDocumentContent,
                request.FileName,
                request.ContentType,
                user.Id);

            // Salvar os metadados do documento no repositório
            var documentoAssinado = StoredDocument.CreateSigned(
                documentId,
                documentReference,
                request.FileName,
                user.Id.ToString(),
                request.ContentType!,
                digitalSignature.Algorithm,
                signedDocumentContent.Length,
                metadata.SignatureHash,
                DateTime.UtcNow
            );
            await _documentRepository.AddAsync(documentoAssinado);
            await _documentRepository.SaveChangesAsync();
            _logger.LogInformation("Document signing process completed successfully");

            return new SignDocumentResponse
            {
                DocumentId = Guid.Parse(documentId),
                DocumentName = request.FileName,
                SignedContent = signedDocumentContent,
                ContentType = request.ContentType!,
                Algorithm = digitalSignature.Algorithm,
                FileSize = signedDocumentContent.Length,
                SignedAt = DateTime.UtcNow
            };
        }

        private async Task<User> AuthenticateUser(string userId)
        {
           // var userId = _currentUserService.GetUserId();
            //if (userId == Guid.Empty)
            //{
              //  throw new UnauthorizedAccessException("User not authenticated");
            //}
            //Resgatar usuário
            var user = await _userRepository.GetByIdAsync(userId.ToString());
            if (user == null)
            {
                throw new EntityNotFoundException("User not found");
            }
            return new User {
                Cpf = user.Cpf,
                Nome = user.Nome,
                Email = user.Email,
                Id = user.Id
            };
        }

    }



}
