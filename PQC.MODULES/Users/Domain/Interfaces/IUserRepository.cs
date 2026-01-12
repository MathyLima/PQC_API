using PQC.MODULES.Users.Domain.Entities;

namespace PQC.MODULES.Users.Domain.Interfaces
{
    public interface IUserRepository
    {
        Task<User?> GetByIdAsync(Guid id);
        Task<User?> GetByEmailAsync(string email);
        Task<User?> GetByUsernameAsync(string username);
        Task<List<User>> GetAllAsync(int page = 1, int pageSize = 10);
        Task<int> GetTotalCountAsync();
        Task<User> AddAsync(User user);
        Task<User> UpdateAsync(User user);
        Task<bool> DeleteAsync(Guid id);
        Task<bool> ExistsAsync(Guid id);
    }
}
