using PQC.COMMUNICATION.Requests.Users.List;
using PQC.COMMUNICATION.Responses.Users;
using PQC.MODULES.Users.Domain.Entities;

namespace PQC.MODULES.Users.Application.Services.UseCases.List
{
    public class ListUsersUseCase
    {
        private readonly IUserRepository _repository;

        public ListUsersUseCase(IUserRepository repository)
        {
            _repository = repository;
        }

        public async Task<UserListResponseJson> Execute(UserListRequestJson request)
        {
            var users = await _repository.GetAllAsync();

            if (!string.IsNullOrWhiteSpace(request?.SearchTerm))
            {
                users = users.Where(u =>
                    u.Nome.Contains(request.SearchTerm, StringComparison.OrdinalIgnoreCase) ||
                    u.Email.Contains(request.SearchTerm, StringComparison.OrdinalIgnoreCase)
                ).ToList();
            }

            var response = new UserListResponseJson
            {
                Users = users.Select(u => new UserResponseJson
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
