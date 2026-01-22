using Microsoft.Extensions.DependencyInjection;
using PQC.MODULES.Users.Application.Services.UseCases.Create;
using PQC.MODULES.Users.Application.Services.UseCases.Delete;
using PQC.MODULES.Users.Application.Services.UseCases.List;
using PQC.MODULES.Users.Application.Services.UseCases.Update;
using PQC.MODULES.Users.Infraestructure.Repositories;

namespace PQC.MODULES.Users.DependencyInjection
{
    public static class UsersModuleExtensions
    {
        public static IServiceCollection AddUsersModule(this IServiceCollection services)
        {
            AddRepositories(services);
            AddUseCases(services);
            return services;
        }

        public static void AddRepositories(this IServiceCollection services)
        {
            services.AddScoped<IUserRepository, UserRepository>();
        }

        public static void AddUseCases(this IServiceCollection services)
        {
            services.AddScoped<CreateUserUseCase>();
            services.AddScoped<UpdateUserUseCase>();
            services.AddScoped<GetUserByIdUseCase>();
            services.AddScoped<ListUsersUseCase>();
            services.AddScoped<DeleteUserUseCase>();
        }
    }
}
