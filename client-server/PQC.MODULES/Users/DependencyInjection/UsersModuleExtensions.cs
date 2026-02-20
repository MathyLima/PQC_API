using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using PQC.MODULES.Users.Application.UseCases.Create;
using PQC.MODULES.Users.Application.UseCases.Delete;
using PQC.MODULES.Users.Application.UseCases.List;
using PQC.MODULES.Users.Application.UseCases.Update;

namespace PQC.MODULES.Users.DependencyInjection
{
    public static class UsersModuleExtensions
    {
        public static IServiceCollection AddUsersModule(
            this IServiceCollection services,
            IConfiguration configuration)
        {
            // Apenas registrar os Use Cases
            // Repositórios, KeyReader, KeyWriter, PasswordHasher, etc.
            // já estão registrados no AddSharedInfrastructure
            AddUseCases(services);

            return services;
        }

        private static void AddUseCases(this IServiceCollection services)
        {
            services.AddScoped<CreateUserUseCase>();
            services.AddScoped<UpdateUserUseCase>();
            services.AddScoped<GetUserByIdUseCase>();
            services.AddScoped<ListUsersUseCase>();
            services.AddScoped<DeleteUserUseCase>();
        }
    }
}