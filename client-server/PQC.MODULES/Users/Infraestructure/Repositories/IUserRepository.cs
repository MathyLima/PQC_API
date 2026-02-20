using PQC.MODULES.Users.Domain.Entities;

public interface IUserRepository
{
    Task AddAsync(User user);
    Task UpdateAsync(User user);
    Task DeleteAsync(User user);
    Task<List<User>> GetAllAsync();
    Task<User?> GetByIdAsync(string id);

    Task<User?> GetByLoginAsync(string login);
    Task<User?> GetByEmailAsync(string email);
    Task<User?> GetByCpfAsync(string cpf);

    Task SaveChangesAsync();
}
