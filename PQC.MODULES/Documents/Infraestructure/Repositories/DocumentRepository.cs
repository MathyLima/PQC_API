using Microsoft.EntityFrameworkCore;
using PQC.MODULES.Documents.Domain.Entities;
using PQC.MODULES.Infraestructure.Data;

namespace PQC.MODULES.Documents.Infraestructure.Repositories
{
    public class DocumentRepository: IDocumentRepository
    {
        private readonly AppDbContext _context;
        public DocumentRepository(AppDbContext context)
        {
            _context = context;
        }
        public async Task<Document?> GetByIdAsync(string id)
        {
            return await _context.Documentos.FindAsync(id);
        }
        public async Task<List<Document>> GetDocumentsByUserId(string userId)
        {
            return await _context.Documentos
                .Where(d => d.IdUsuario == userId)
                .ToListAsync();
        }
        public async Task SaveChangesAsync()
        {
            await _context.SaveChangesAsync();
        }
        public async Task AddAsync(Document document)
        {
            await _context.Documentos.AddAsync(document);
        }

    }
}
