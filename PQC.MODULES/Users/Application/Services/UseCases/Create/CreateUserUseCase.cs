using PQC.COMMUNICATION.Requests.Users.Create;
using PQC.COMMUNICATION.Responses.Users;
using PQC.EXCEPTIONS.ExceptionsBase;
using PQC.MODULES.Auth.Application.Services.Security;
using PQC.MODULES.Users.Domain.Entities;
using PQC.MODULES.Users.Infraestructure.Repositories;
using PQC.MODULES.Users.Validators;

namespace PQC.MODULES.Users.Application.Services.UseCases.Create
{
    public class CreateUserUseCase
    {
        private readonly IUserRepository _repository;

        public CreateUserUseCase(IUserRepository repository)
        {
            _repository = repository;
        }
        public async Task<UserResponseJson> Execute(CreateUserRequestJson request)
        {
            Validate(request);

            var passwordHash = PasswordHasher.HashPassword(request.Password!);

            var entity = new User
            {
                Id = Guid.NewGuid().ToString(),
                Nome = request.Name!,
                Email = request.Email!,
                Senha = passwordHash!,
                Cpf = request.Cpf!,
                Telefone = request.Telefone!,
                Login = request.Login!,
            };

            await _repository.AddAsync(entity);
            await _repository.SaveChangesAsync();

            return new UserResponseJson
            {
                Id = Guid.Parse(entity.Id),
                Name = entity.Nome,
                Email =  entity.Email,
            };
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
