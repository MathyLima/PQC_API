using Microsoft.EntityFrameworkCore;
using PQC.MODULES.Infraestructure.Data;
using PQC.MODULES.Users.Domain.Entities;

namespace PQC.MODULES.Users.Infraestructure.Repositories
{
    public class UserRepository: IUserRepository
    {
        private readonly AppDbContext _context;
        public UserRepository(AppDbContext context)
        {
            _context = context;
        }
        public void Add(User user)
        {
            _context.Usuarios.Add(user);
        }
        public async Task AddAsync(User user)
        {
            await _context.Usuarios.AddAsync(user);
        }
        public void SaveChanges()
        {
            _context.SaveChanges();
        }
        public async Task SaveChangesAsync()
        {
            await _context.SaveChangesAsync();
        }
        public User? GetById(string id)
        {
            return _context.Usuarios.Find(id);
        }
        public User? GetByEmail(string email)
        {
            return _context.Usuarios.FirstOrDefault(u => u.Email == email);
        }
        public User? GetByCpf(string cpf)
        {
            return _context.Usuarios.FirstOrDefault(u => u.Cpf == cpf);
        }

        public async Task<User?> GetByLoginAsync(string login)
        {
            return await _context.Usuarios
                .FirstOrDefaultAsync(u => u.Login == login);
        }

        public void Update(User user)
        {
            _context.Usuarios.Update(user);
        }

        public Task UpdateAsync(User user)
        {
            _context.Usuarios.Update(user);
            return Task.CompletedTask;
        }
    }
}
