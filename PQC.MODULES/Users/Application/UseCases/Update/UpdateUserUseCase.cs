using PQC.MODULES.Users.Validators;
using PQC.SHARED.Communication.DTOs.Users.Requests;
using PQC.SHARED.Communication.DTOs.Users.Responses;
using PQC.SHARED.Exceptions.Domain;

namespace PQC.MODULES.Users.Application.UseCases.Update
{
    public class UpdateUserUseCase
    {
        private readonly IUserRepository _repository;

        public UpdateUserUseCase(IUserRepository repository)
        {
            _repository = repository;
        }
    

        public async Task<ShortUserResponseJson> Execute(Guid id, UpdateUserRequestJson request)
        {
            Validate(request);

            // Busca o usuário
            var user =await _repository.GetByIdAsync(id.ToString());

            if (user == null)
            {
                throw new EntityNotFoundException("Usuário não encontrado");
            }

            user.Nome = request.Nome ?? user.Nome;
            user.Email = request.Email ?? user.Email;
            user.Telefone = request.Telefone ?? user.Telefone;
            user.Login = request.Login ?? user.Login;
            user.Senha = request.Senha ?? user.Senha;
            user.AlgoritmoAssinatura = request.AlgoritmoAssinatura ?? user.AlgoritmoAssinatura;

            await _repository.UpdateAsync(user);

            await _repository.SaveChangesAsync();


            return new ShortUserResponseJson
            {
                Id = Guid.Parse(user.Id),
                Name = user.Nome,
                Email = user.Email
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