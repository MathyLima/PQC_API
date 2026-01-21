using PQC.COMMUNICATION.Responses.Documents;
using PQC.MODULES.Documents.Infraestructure.InMemory;

namespace PQC.MODULES.Documents.Application.Services.UseCases.List
{
    public class ListDocumentsUseCase
    {
        public DocumentListResponseJson Execute(string userId)
        {
            // Lista documentos do usuário
            var documents = DocumentInMemoryDatabase.Documents
                .Where(d => d.IdUsuario == userId )
                .Select(d => new DocumentResponseJson
                {
                    Id = Guid.Parse(d.Id),
                    FileName = d.Nome,
                    UploadedAt = d.UploadEm
                })
                .ToList();

            return new DocumentListResponseJson
            {
                Documents = documents
            };
        }
    }
}