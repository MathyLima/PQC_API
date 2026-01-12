using PQC.COMMUNICATION.Requests.Users.Update;
using PQC.COMMUNICATION.Responses.Users;
using PQC.EXCEPTIONS.ExceptionsBase;
using PQC.MODULES.Users.Domain.Entities;
using PQC.MODULES.Users.Validators;

namespace PQC.MODULES.Users.Application.Services.UseCases.Update
{
    public class UpdateUserUseCase
    {
        // Simulação do banco em memória
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

        public UserResponseJson Execute(Guid id, UpdateUserRequestJson request)
        {
            Validate(request);

            // Busca o usuário
            var user = _users.FirstOrDefault(u => u.Id == id);

            if (user == null)
            {
                throw new NotFoundException("User not found");
            }

            user.Name = request.Name;
            user.Email = request.Email;

            return new UserResponseJson
            {
                Id = user.Id,
                Name = user.Name,
                CreatedAt = user.CreatedAt
            };
        }

        private void Validate(UpdateUserRequestJson request)
        {
            var validator = new UpdateValidator();
            var result = validator.Validate(request);

            if (!result.IsValid)
            {
                var errors = result.Errors.Select(failure => failure.ErrorMessage).ToList();
                throw new ErrorOnValidationException(errors);
            }
        }
    }
}