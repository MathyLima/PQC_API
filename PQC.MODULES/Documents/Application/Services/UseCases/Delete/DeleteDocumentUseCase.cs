using PQC.EXCEPTIONS.ExceptionsBase;
using PQC.MODULES.Documents.Infraestructure.InMemory;

namespace PQC.MODULES.Documents.Application.Services.UseCases.Delete
{
    public class DeleteDocumentUseCase
    {
        public void Execute(string id, string userId)
        {
            var document = DocumentInMemoryDatabase.Documents
                .FirstOrDefault(d => d.Id == id && d.IdUsuario == userId);

            if (document == null)
            {
                throw new NotFoundException("Document not found");
            }

            //document.IsActive = false;
        }
    }
}