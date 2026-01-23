using PQC.COMMUNICATION.Requests.Documents.Create;
using PQC.EXCEPTIONS.ExceptionsBase;
using PQC.MODULES.Documents.Domain.Entities;
using PQC.MODULES.Documents.Infraestructure.Repositories;
using PQC.MODULES.Documents.Infraestructure.SignAlgorithm.Application.Services.UseCases;
using PQC.MODULES.Users.Infraestructure.Repositories;

public class CreateDocumentUseCase
{
    private readonly IDocumentRepository _repository;
    private readonly IUserRepository _userRepository;
    private readonly SignUploadedDocumentUseCase _signUseCase;
    private readonly IFileStorageService _storageService;

    private readonly ISignatureMetadata _metadataPageGenerator;
    private readonly IDocumentMerger _documentMerger;

    public CreateDocumentUseCase(
        IDocumentRepository repository,
        IUserRepository userRepository,
        SignUploadedDocumentUseCase signUseCase,
        IFileStorageService storageService,
        ISignatureMetadata metadataPageGenerator,
        IDocumentMerger documentMerger)
    {
        _repository = repository;
        _userRepository = userRepository;
        _signUseCase = signUseCase;
        _storageService = storageService;
        _metadataPageGenerator = metadataPageGenerator;
        _documentMerger = documentMerger;
    }

    public async Task<string> Execute(CreateDocumentContentRequest request)
    {
        // 1. OBTER dados do usuário
        var user = await _userRepository.GetByIdAsync(request.UserId!.ToString());

        if (user is null)
            throw new NotFoundException("User not found");
        // 2. ASSINAR documento original
        var signatureResult = await _signUseCase.Execute(request.Content);

        if (!signatureResult.Success)
        {
            throw new Exception($"Signature failed: {signatureResult.ErrorMessage}");
        }

        // 3. GERAR página de metadados
        var metadata = new SignatureMetadata
        {
            DocumentName = request.FileName!,
            SignerName = user.Nome,
            SignerEmail = user.Email,
            SignerCpf = user.Cpf,
            SignedAt = GetRecifeTime(),
            Algorithm = signatureResult.Algorithm,
            SignatureHash = Convert.ToBase64String(signatureResult.Signature)
        };

        var metadataPage = await _metadataPageGenerator.GenerateMetadataPageAsync(metadata);

        // 4. JUNTAR documento + página de metadados
        var finalDocument = await _documentMerger.MergeDocumentsAsync(
            request.Content,
            metadataPage
        );

        // 5. SALVAR documento completo (original + metadados)
        var filePath = await _storageService.SaveFileAsync(
            finalDocument,  // ← Salva o documento COM a página de metadados
            request.FileName,
            request.ContentType,
            request.UserId
        );

        // 6. PERSISTIR no banco
        var document = new StoredDocument
        {
            Id = Guid.NewGuid().ToString(),
            Nome = request.FileName,
            Path = filePath,
            TipoArquivo = request.ContentType,
            Tamanho = finalDocument.Length, // ← Tamanho do documento final
            IdUsuario = request.UserId,
            AssinaturaDigital = Convert.ToBase64String(signatureResult.Signature),
            AlgoritmoAssinatura = signatureResult.Algorithm,
            Assinado_em = metadata.SignedAt,
        };

        await _repository.AddAsync(document);
        await _repository.SaveChangesAsync();

        return document.Id;
    }

    private DateTime GetRecifeTime()
    {
        try
        {
            // Tenta Windows primeiro
            var tzInfo = TimeZoneInfo.FindSystemTimeZoneById("E. South America Standard Time");
            return TimeZoneInfo.ConvertTimeFromUtc(DateTime.UtcNow, tzInfo);
        }
        catch (TimeZoneNotFoundException)
        {
            try
            {
                // Se falhar, tenta Linux/Mac
                var tzInfo = TimeZoneInfo.FindSystemTimeZoneById("America/Recife");
                return TimeZoneInfo.ConvertTimeFromUtc(DateTime.UtcNow, tzInfo);
            }
            catch
            {
                // Fallback: UTC-3 fixo
                return DateTime.UtcNow.AddHours(-3);
            }
        }
    }
}