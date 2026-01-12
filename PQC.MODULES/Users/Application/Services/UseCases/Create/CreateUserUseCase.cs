using PQC.COMMUNICATION.Requests.Users.Create;
using PQC.COMMUNICATION.Responses.Users;
using PQC.EXCEPTIONS.ExceptionsBase;
using PQC.MODULES.Auth.Application.Services.Security;
using PQC.MODULES.Users.Domain.Entities;
using PQC.MODULES.Users.Validators;

namespace PQC.MODULES.Users.Application.Services.UseCases.Create
{
    public class CreateUserUseCase
    {
        public UserResponseJson Execute(CreateUserRequestJson request)
        {
            Validate(request);
            var passwordHash = PasswordHasher.HashPassword(request.Password);
            var entity = new User
            {
                Name = request.Name,
                Email = request.Email,
                PasswordHash = passwordHash,
                CreatedAt = DateTime.UtcNow,
            };


            // Salva no banco a entidade criada
            
            //Retorna os dados que quero
            return new UserResponseJson { Id =  entity.Id, Name = entity.Name, CreatedAt = entity.CreatedAt};

        }

        private void Validate(CreateUserRequestJson request)
        {
            var validator = new CreationValidator();
            var result = validator.Validate(request);
            if (!result.IsValid)
            {
                var errors = result.Errors.Select(failure => failure.ErrorMessage).ToList();
                throw new ErrorOnValidationException(errors);
            }

        }
    }
}
