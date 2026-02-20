using Microsoft.EntityFrameworkCore;
using PQC.MODULES.Documents.Domain.Entities;

namespace PQC.MODULES.Documents.Domain.Interfaces.Persistence
{
    public interface IDocumentsDbContext
    {
        DbSet<StoredDocument> Documentos { get; }
        Task<int> SaveChangesAsync(CancellationToken cancellationToken = default);
    }
}
