using PQC.MODULES.Documents.Domain.Entities;
using System.Reflection.Metadata;

namespace PQC.MODULES.Documents.Infraestructure.Repositories
{
    public interface IDocumentRepository
    {
        Task AddAsync(StoredDocument document);
        Task<StoredDocument?> GetByIdAsync(string id);
        Task<List<StoredDocument>> GetDocumentsByUserId(string userId);
        Task<StoredDocument?> GetByCpfAndTimestamp(string cpf, DateTime signedAt); // ← NOVO
        Task<StoredDocument> GetByDocumentIdAsync(string documentId);
        Task SaveChangesAsync();


    }
}
