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
        };

        public UserResponseJson Execute(Guid id, UpdateUserRequestJson request)
        {
            Validate(request);

            // Busca o usuário
            var user = _users.FirstOrDefault(u => u.Id == id.ToString());

            if (user == null)
            {
                throw new NotFoundException("User not found");
            }

            user.Nome = request.Name;
            user.Email = request.Email;

            return new UserResponseJson
            {
                Id = Guid.Parse(user.Id),
                Name = user.Nome,
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