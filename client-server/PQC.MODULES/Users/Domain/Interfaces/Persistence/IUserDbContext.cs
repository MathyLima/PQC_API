using Microsoft.EntityFrameworkCore;
using PQC.MODULES.Users.Domain.Entities;
using System;
using System.Collections.Generic;
using System.Text;

namespace PQC.MODULES.Users.Domain.Interfaces.Persistence
{
    public interface IUsersDbContext
    {
        DbSet<User> Usuarios { get; }
        Task<int> SaveChangesAsync(CancellationToken cancellationToken = default);
    }
}
