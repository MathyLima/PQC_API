using Microsoft.EntityFrameworkCore;
using PQC.MODULES.Users.Domain.Entities;
using PQC.MODULES.Users.Domain.Interfaces.Persistence;

namespace PQC.MODULES.Users.Infraestructure.Repositories
{
    public class UserRepository : IUserRepository
    {
        private readonly IUsersDbContext _context;

        public UserRepository(IUsersDbContext usersDbContext)
        {
            _context = usersDbContext;
        }

        // ========== CREATE ==========
        public async Task AddAsync(User user)
        {
            await _context.Usuarios.AddAsync(user);
        }

        // ========== READ (OTIMIZADO) ==========

        public async Task<List<User>> GetAllAsync()
        {
            return await _context.Usuarios
                .AsNoTracking()
                .ToListAsync();
        }

        public async Task<User?> GetByIdAsync(string id)
        {
            return await _context.Usuarios.FindAsync(id);
        }

        public async Task<User?> GetByEmailAsync(string email)
        {
            return await _context.Usuarios
                .AsNoTracking()
                .FirstOrDefaultAsync(u => u.Email == email);
        }

        public async Task<User?> GetByCpfAsync(string cpf)
        {
            return await _context.Usuarios
                .AsNoTracking()
                .FirstOrDefaultAsync(u => u.Cpf == cpf);
        }

   
        public async Task<User?> GetByLoginAsync(string login)
        {
          

            return await _context.Usuarios
                .AsNoTracking() 
                .FirstOrDefaultAsync(u => u.Login == login);

        }

        // ========== UPDATE ==========

        public Task UpdateAsync(User user)
        {
            _context.Usuarios.Update(user);
            return Task.CompletedTask;
        }

        // ========== DELETE ==========

        public async Task DeleteAsync(User user)
        {
            _context.Usuarios.Remove(user);
            await Task.CompletedTask;
        }

        // ========== PERSISTÊNCIA ==========

        public async Task SaveChangesAsync()
        {
            await _context.SaveChangesAsync();
        }
    }
}