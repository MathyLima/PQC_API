using PQC.MODULES.Documents.Domain.Entities;

namespace PQC.MODULES.Documents.Infraestructure.Repositories
{
    public interface IDocumentRepository
    {
        Task AddAsync(StoredDocument document);
        Task<StoredDocument?> GetByIdAsync(string id);
        Task<List<StoredDocument>> GetDocumentsByUserId(string userId);
        Task SaveChangesAsync();

    }
}
