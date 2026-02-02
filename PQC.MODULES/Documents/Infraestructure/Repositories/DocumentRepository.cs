using Microsoft.EntityFrameworkCore;
using PQC.MODULES.Documents.Domain.Entities;
using PQC.MODULES.Documents.Domain.Interfaces.Persistence;

namespace PQC.MODULES.Documents.Infraestructure.Repositories
{
    public class DocumentRepository : IDocumentRepository
    {
        private readonly IDocumentsDbContext _context;

        public DocumentRepository(IDocumentsDbContext context)
        {
            _context = context;
        }

        public async Task<StoredDocument?> GetByIdAsync(string id)
        {
            return await _context.Documentos.FindAsync(id);
        }

        public async Task<List<StoredDocument>> GetDocumentsByUserId(string userId)
        {
            return await _context.Documentos
                .Where(d => d.IdUsuario == userId)
                .ToListAsync();
        }

        public async Task<StoredDocument?> GetByCpfAndTimestamp(string cpf, DateTime signedAt)
        {
            var tolerance = TimeSpan.FromSeconds(2);
            var minTime = signedAt.Add(-tolerance);
            var maxTime = signedAt.Add(tolerance);

            return await _context.Documentos
                .Include(d => d.Usuario) // Aqui depende se Usuario também tem interface
                .FirstOrDefaultAsync(d =>
                    d.Usuario.Cpf == cpf &&
                    d.Assinado_em >= minTime &&
                    d.Assinado_em <= maxTime);
        }

        public async Task SaveChangesAsync()
        {
            await _context.SaveChangesAsync();
        }

        public async Task AddAsync(StoredDocument document)
        {
            await _context.Documentos.AddAsync(document);
        }
    }
}
