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
                Id = "11111111-1111-1111-1111-111111111111",
                Nome = "João Silva",
                Email = "joao@test.com",
                
            },
            new User
            {
                Id = "22222222-2222-2222-2222-222222222222",
                Nome = "Maria Santos",
                Email = "maria@test.com",
            }
        };

        public void Execute(Guid id)
        {
            // Busca o usuário
            var user = _users.FirstOrDefault(u => u.Id == id.ToString());

            if (user == null)
            {
                throw new NotFoundException("User not found");
            }

            // Remove o usuário
            _users.Remove(user);
        }
    }
}