using Microsoft.EntityFrameworkCore;
using PQC.MODULES.Infraestructure.Data;
using PQC.MODULES.Users.Domain.Entities;

namespace PQC.MODULES.Users.Infraestructure.Repositories
{
    public class UserRepository : IUserRepository
    {
        // 1. Campos
        private readonly AppDbContext _context;

        // 2. Construtor
        public UserRepository(AppDbContext context)
        {
            _context = context;
        }

        // 3. CREATE
        public async Task AddAsync(User user)
        {
            await _context.Usuarios.AddAsync(user);
        }
        // 4. READ
        public async Task<List<User>> GetAllAsync()
        {
            return await _context.Usuarios.ToListAsync();
        }

        public async Task<User?> GetByIdAsync(string id)
        {
            return await _context.Usuarios.FindAsync(id);
        }

        public async Task<User?> GetByEmailAsync(string email)
        {
            return await _context.Usuarios.FirstOrDefaultAsync(u => u.Email == email);
        }

        public async Task<User?> GetByCpfAsync(string cpf)
        {
            return await _context.Usuarios.FirstOrDefaultAsync(u => u.Cpf == cpf);
        }

        public async Task<User?> GetByLoginAsync(string login)
        {
            return await _context.Usuarios
                .FirstOrDefaultAsync(u => u.Login == login);
        }
        // 5. UPDATE
        public Task UpdateAsync(User user)
        {
            _context.Usuarios.Update(user);
            return Task.CompletedTask;
        }

        // 6. DELETE
        public async Task DeleteAsync(User user)
        {
            _context.Usuarios.Remove(user);
            await Task.CompletedTask;
        }

        // 7. PERSISTÊNCIA
        public async Task SaveChangesAsync()
        {
            await _context.SaveChangesAsync();
        }
    }
}
