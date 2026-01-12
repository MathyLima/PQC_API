using PQC.EXCEPTIONS.ExceptionsBase;
using PQC.MODULES.Users.Domain.Entities;

namespace PQC.MODULES.Users.Application.Services.UseCases.Delete
{
    public class DeleteUserUseCase
    {
        private static List<User> _users = new()
        {
            new User
            {
                Id = Guid.Parse("11111111-1111-1111-1111-111111111111"),
                Name = "João Silva",
                Email = "joao@test.com",
                CreatedAt = DateTime.UtcNow.AddDays(-10)
            },
            new User
            {
                Id = Guid.Parse("22222222-2222-2222-2222-222222222222"),
                Name = "Maria Santos",
                Email = "maria@test.com",
                CreatedAt = DateTime.UtcNow.AddDays(-5)
            }
        };

        public void Execute(Guid id)
        {
            // Busca o usuário
            var user = _users.FirstOrDefault(u => u.Id == id);

            if (user == null)
            {
                throw new NotFoundException("User not found");
            }

            // Remove o usuário
            _users.Remove(user);
        }
    }
}