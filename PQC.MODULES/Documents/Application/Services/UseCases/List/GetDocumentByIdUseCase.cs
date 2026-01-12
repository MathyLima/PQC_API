using PQC.EXCEPTIONS.ExceptionsBase;
using PQC.MODULES.Documents.Domain.Entities;
using PQC.MODULES.Documents.Infraestructure.InMemory;

namespace PQC.MODULES.Documents.Application.Services.UseCases.GetById
{
    public class GetDocumentByIdUseCase
    {
        public Document Execute(Guid id, Guid userId)
        {
            var document = DocumentInMemoryDatabase.Documents
                .FirstOrDefault(d => d.Id == id && d.UploadedByUserId == userId && d.IsActive);

            if (document == null)
            {
                throw new NotFoundException("Document not found");
            }

            return document;
        }
    }
}