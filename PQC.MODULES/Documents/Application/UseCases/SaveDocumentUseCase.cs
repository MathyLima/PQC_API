using PQC.MODULES.Documents.Application.DTOs;

namespace PQC.MODULES.Documents.Application.UseCases
{
    public class SaveDocumentUseCase
    {
        private readonly IFileStorageService _fileStorageService;
        private readonly IUserRepository _userRepository;

        public SaveDocumentUseCase(IFileStorageService fileStorageService, IUserRepository userRepository)
        {
            _fileStorageService = fileStorageService;
            _userRepository = userRepository;
        }

        public async Task<string> Execute(DocumentUploadRequest request)
        {
            // Validate user
            var user = await _userRepository.GetByIdAsync(request.UserId);
            if (user == null)
            {
                throw new Exception("User not found.");
            }
            // Save document using the file storage service
            var documentPath = await _fileStorageService.SaveFileAsync(request.Content,request.FileName, request.ContentType,request.UserId);
            return documentPath;
        }
    }
}
