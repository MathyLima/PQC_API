using PQC.SHARED.Communication.DTOs.Users.Responses;

namespace PQC.MODULES.Users.Application.UseCases.List
{
    public class ListUsersUseCase
    {
        private readonly IUserRepository _repository;

        public ListUsersUseCase(IUserRepository repository)
        {
            _repository = repository;
        }

        public async Task<ShortUsersListResponse> Execute()
        {
            var users = await _repository.GetAllAsync();

          

            var response = new ShortUsersListResponse
            {
                Users = users.Select(u => new ShortUserResponseJson
                {
                    Id = Guid.Parse(u.Id), // assumindo Guid
                    Name = u.Nome,
                    Email = u.Email
                }).ToList()
            };

            return response;
        }
    }
}
