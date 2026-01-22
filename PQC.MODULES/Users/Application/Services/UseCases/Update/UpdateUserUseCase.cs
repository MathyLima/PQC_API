using PQC.COMMUNICATION.Requests.Users.Update;
using PQC.COMMUNICATION.Responses.Users;
using PQC.EXCEPTIONS.ExceptionsBase;
using PQC.MODULES.Users.Domain.Entities;
using PQC.MODULES.Users.Infraestructure.Repositories;
using PQC.MODULES.Users.Validators;

namespace PQC.MODULES.Users.Application.Services.UseCases.Update
{
    public class UpdateUserUseCase
    {
        private readonly IUserRepository _repository;

        public UpdateUserUseCase(IUserRepository repository)
        {
            _repository = repository;
        }
    

        public async Task<UserResponseJson> Execute(Guid id, UpdateUserRequestJson request)
        {
            Validate(request);

            // Busca o usuário
            var user =await _repository.GetByIdAsync(id.ToString());

            if (user == null)
            {
                throw new NotFoundException("User not found");
            }

            user.Nome = request.Name ?? user.Nome;
            user.Email = request.Email ?? user.Email;
            user.Telefone = request.Telefone ?? user.Telefone;
            user.Login = request.Login ?? user.Login;
            user.Senha = request.Password ?? user.Senha;
            
            await _repository.UpdateAsync(user);

            await _repository.SaveChangesAsync();


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