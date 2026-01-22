using PQC.MODULES.Documents.Domain.Entities;

namespace PQC.MODULES.Documents.Infraestructure.Repositories
{
    public interface IDocumentRepository
    {
        Task AddAsync(Document document);
        Task<Document?> GetByIdAsync(string id);
        Task<List<Document>> GetDocumentsByUserId(string userId);
        Task SaveChangesAsync();

    }
}
