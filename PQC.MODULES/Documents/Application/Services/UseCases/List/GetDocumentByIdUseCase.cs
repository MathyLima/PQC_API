using PQC.EXCEPTIONS.ExceptionsBase;
using PQC.MODULES.Documents.Domain.Entities;
using PQC.MODULES.Documents.Infraestructure.InMemory;

namespace PQC.MODULES.Documents.Application.Services.UseCases.List
{
    public class GetDocumentByIdUseCase
    {
        public Document Execute(string id, string userId)
        {
            var document = DocumentInMemoryDatabase.Documents
                .FirstOrDefault(d => d.Id == id && d.IdUsuario == userId );

            if (document == null)
            {
                throw new NotFoundException("Document not found");
            }

            return document;
        }
    }
}