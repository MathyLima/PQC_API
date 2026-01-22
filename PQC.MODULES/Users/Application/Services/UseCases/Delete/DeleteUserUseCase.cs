using PQC.EXCEPTIONS.ExceptionsBase;
using PQC.MODULES.Users.Domain.Entities;

namespace PQC.MODULES.Users.Application.Services.UseCases.Delete
{
    public class DeleteUserUseCase
    {
        private readonly IUserRepository _repository;

        public DeleteUserUseCase(IUserRepository repository)
        {
            _repository = repository;
        }

        public async Task Execute(Guid id)
        {
            // Busca o usuário
            var user = await _repository.GetByIdAsync(id.ToString());

            if (user == null)
            {
                throw new NotFoundException("User not found");
            }

            // Remove
            await _repository.DeleteAsync(user);
            await _repository.SaveChangesAsync();
        }
    }
}
