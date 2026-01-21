using PQC.COMMUNICATION.Requests.Users.List;
using PQC.COMMUNICATION.Responses.Users;
using PQC.MODULES.Users.Domain.Entities;

namespace PQC.MODULES.Users.Application.Services.UseCases.List
{
    public class ListUsersUseCase
    {
        public UserListResponseJson Execute(UserListRequestJson request)
        {
            var users = GetUsersFromDatabase();
            if (!string.IsNullOrEmpty(request?.SearchTerm))
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
                    Id = Guid.Parse(u.Id),
                    Name = u.Nome,
                    Email = u.Email
                }).ToList()
            };
            return response;

        }


        //Isso daqui vai vir do repository ( repository serve como uma camada de abstracao para chamada de contexto)
        private List<User> GetUsersFromDatabase()
        {
            return new List<User>
            {
             
            };
        }
    }
}
