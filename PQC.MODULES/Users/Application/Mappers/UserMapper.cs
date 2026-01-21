using PQC.COMMUNICATION.Responses.Users;
using PQC.MODULES.Users.Domain.Entities;

namespace PQC.MODULES.Users.Application.Mappers
{
    public static class UserMapper
    {
        public static UserResponseJson ToResponse(User user)
        {
            return new UserResponseJson
            {
                Id = Guid.Parse(user.Id),
                Name = user.Nome,
                Email = user.Email,
            };
        }
    }
}
