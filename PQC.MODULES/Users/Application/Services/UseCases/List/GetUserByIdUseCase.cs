using PQC.COMMUNICATION.Responses.Users;
using PQC.EXCEPTIONS.ExceptionsBase;
using PQC.MODULES.Users.Domain.Entities;

namespace PQC.MODULES.Users.Application.Services.UseCases.List
{
    public class GetUserByIdUseCase
    {
        public UserResponseJson Execute(Guid id)
        {
            var user = GetUserFromDatabase(id);

            // ✅ Lança exceção se não encontrar
            if (user == null)
            {
                throw new NotFoundException("User not found");
            }

            return new UserResponseJson
            {
                Id = user.Id,
                Name = user.Name,
                Email = user.Email,
                CreatedAt = user.CreatedAt,
            };
        }

        private User? GetUserFromDatabase(Guid id)
        {
            var users = new List<User>
            {
                new User
                {
                    Id = Guid.Parse("11111111-1111-1111-1111-111111111111"),
                    Name = "João",
                    Email = "joao@test.com",
                    CreatedAt = DateTime.UtcNow
                }
            };
            return users.FirstOrDefault(u => u.Id == id);
        }
    }
}