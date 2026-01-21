// PQC.MODULES.Auth/DependencyInjection/AuthModuleExtensions.cs
using Microsoft.Extensions.DependencyInjection;
using PQC.MODULES.Auth.Application.Services.Security;
using PQC.MODULES.Auth.Application.Services.UseCases.Login;

namespace PQC.MODULES.Auth.DependencyInjection
{
    public static class AuthModuleExtensions
    {
        public static IServiceCollection AddAuthModule(this IServiceCollection services)
        {
            // Serviços
            services.AddScoped<JwtTokenService>();

            // Use Cases
            services.AddScoped<LoginUseCase>();

            return services;
        }
    }
}