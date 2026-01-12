using PQC.MODULES.Users.Domain.Entities;

namespace PQC.MODULES.Users.Infrastructure.InMemory
{
    public static class UserInMemoryDatabase
    {
        public static List<User> Users { get; } = new();
    }
}