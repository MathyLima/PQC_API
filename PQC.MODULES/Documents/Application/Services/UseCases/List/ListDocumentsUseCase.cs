using PQC.COMMUNICATION.Responses.Documents;
using PQC.MODULES.Documents.Infraestructure.InMemory;

namespace PQC.MODULES.Documents.Application.Services.UseCases.List
{
    public class ListDocumentsUseCase
    {
        public DocumentListResponseJson Execute(Guid userId)
        {
            // Lista documentos do usuário
            var documents = DocumentInMemoryDatabase.Documents
                .Where(d => d.UploadedByUserId == userId && d.IsActive)
                .Select(d => new DocumentResponseJson
                {
                    Id = d.Id,
                    FileName = d.FileName,
                    ContentType = d.ContentType,
                    SizeInBytes = d.SizeInBytes,
                    UploadedAt = d.UploadedAt
                })
                .ToList();

            return new DocumentListResponseJson
            {
                Documents = documents
            };
        }
    }
}