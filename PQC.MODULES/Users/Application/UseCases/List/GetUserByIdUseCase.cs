using PQC.SHARED.Communication.DTOs.Users.Responses;
using PQC.SHARED.Exceptions.Domain;

namespace PQC.MODULES.Users.Application.UseCases.List
{
    public class GetUserByIdUseCase
    {
        private readonly IUserRepository _repository;
        public GetUserByIdUseCase(IUserRepository repository)
        {
            _repository = repository;
        }

        public async Task<ShortUserResponseJson> Execute(Guid id)
        {
            var user = await _repository.GetByIdAsync(id.ToString());

            // ✅ Lança exceção se não encontrar
            if (user == null)
            {
                throw new EntityNotFoundException("Usuário não encontrado");
            }

            return new ShortUserResponseJson
            {
                Id = Guid.Parse(user.Id),
                Name = user.Nome,
                Email = user.Email
            };
        }

    }
}