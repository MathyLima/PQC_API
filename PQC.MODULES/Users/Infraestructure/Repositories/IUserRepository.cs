using PQC.MODULES.Users.Domain.Entities;

namespace PQC.MODULES.Users.Infraestructure.Repositories
{
    public interface IUserRepository
    {
        void Add(User user);
        Task AddAsync(User user);
        void Update(User user);
        Task UpdateAsync(User user);
        void SaveChanges();
        Task SaveChangesAsync();
        User? GetById(string id);
        Task<User?> GetByLoginAsync(string login); // NOVO
        User? GetByEmail(string email);
        User? GetByCpf(string cpf);


    }
}
