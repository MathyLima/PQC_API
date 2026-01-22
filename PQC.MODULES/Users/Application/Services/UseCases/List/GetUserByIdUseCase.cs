using PQC.COMMUNICATION.Responses.Users;
using PQC.EXCEPTIONS.ExceptionsBase;
using PQC.MODULES.Users.Domain.Entities;
using PQC.MODULES.Users.Infraestructure.Repositories;

namespace PQC.MODULES.Users.Application.Services.UseCases.List
{
    public class GetUserByIdUseCase
    {
        private readonly IUserRepository _repository;
        public GetUserByIdUseCase(IUserRepository repository)
        {
            _repository = repository;
        }

        public async Task<UserResponseJson> Execute(Guid id)
        {
            var user = await _repository.GetByIdAsync(id.ToString());

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

    }
}