using Microsoft.Extensions.DependencyInjection;
using PQC.MODULES.Users.Application.Services.UseCases.Create;
using PQC.MODULES.Users.Infraestructure.Repositories;

namespace PQC.MODULES.Users.DependencyInjection
{
    public static class UsersModuleExtensions
    {
        public static IServiceCollection AddUsersModule(this IServiceCollection services)
        {
            services.AddScoped<IUserRepository, UserRepository>();
            services.AddScoped<CreateUserUseCase>();
            return services;
        }
    }
}
