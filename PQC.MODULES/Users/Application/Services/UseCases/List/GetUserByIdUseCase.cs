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
                Id = Guid.Parse(user.Id),
                Name = user.Nome,
                Email = user.Email
            };
        }

        private User? GetUserFromDatabase(Guid id)
        {
            var users = new List<User>
            {
                new User
                {
                    Id = "11111111-1111-1111-1111-111111111111",
                    Nome = "João",
                    Email = "joao@test.com",
                    
                }
            };
            return users.FirstOrDefault(u => u.Id == id.ToString());
        }
    }
}