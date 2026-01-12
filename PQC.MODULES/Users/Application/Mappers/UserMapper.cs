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
                Id = user.Id,
                Name = user.Name,
                Email = user.Email,
                IsActive = user.IsActive,
                CreatedAt = user.CreatedAt,
                UpdatedAt = user.UpdatedAt,
            };
        }
    }
}
