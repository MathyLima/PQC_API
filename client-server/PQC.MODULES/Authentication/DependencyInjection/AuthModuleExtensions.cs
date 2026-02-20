using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using PQC.MODULES.Authentication.Application.UseCases.Login;
using PQC.MODULES.Authentication.Domain.Settings;


namespace PQC.MODULES.Authentication.DependencyInjection
{
    public static class AuthModuleExtensions
    {
        public static IServiceCollection AddAuthModule(
            this IServiceCollection services,
            IConfiguration configuration
        )
        {
            // Bind manual do Jwt
            services.Configure<JwtSettings>(options =>
            {
                configuration.GetSection("Jwt").Bind(options);
            });

            // Serviços
            services.AddScoped<Token>();

            // Use Cases
            services.AddScoped<LoginUseCase>();

            return services;
        }
    }
}
