// PQC.INFRAESTRUCTURE/DependencyInjection.cs
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using PQC.INFRAESTRUCTURE.Data;
using PQC.INFRAESTRUCTURE.FilesManagement.FileStorage.Local;
using PQC.INFRAESTRUCTURE.FilesManagement.Keys;
using PQC.INFRAESTRUCTURE.PostQuantumSigner.Service.Wrapper;
using PQC.INFRAESTRUCTURE.Security.Hashing;
using PQC.MODULES.Documents.Application.Interfaces.PasswordHaser.PQC.INFRAESTRUCTURE.Security.Hashing.Interfaces;
using PQC.MODULES.Documents.Application.Interfaces.PQCsigner;
using PQC.MODULES.Documents.Domain.Interfaces;
using PQC.MODULES.Documents.Domain.Interfaces.Persistence;
using PQC.MODULES.Documents.Infraestructure.Repositories;
using PQC.MODULES.Users.Domain.Interfaces.Persistence;
using PQC.MODULES.Users.Infraestructure.Repositories;
using PQC.SHARED.Communication.Interfaces;
using PQC.SHARED.Communication.Interfaces.PQC.SHARED.Interfaces;

namespace PQC.INFRAESTRUCTURE.DependencyInjection
{
    public static class DependencyInjection
    {
        public static IServiceCollection AddSharedInfrastructure(
            this IServiceCollection services,
            IConfiguration configuration)
        {

            // ========== DATA BASE ============
            services.AddDbContext<AppDbContext>(options =>
            options.UseMySql(
                configuration.GetConnectionString("DefaultConnection"),  // ← Usa esta config
                ServerVersion.AutoDetect(configuration.GetConnectionString("DefaultConnection"))
            ));

            // Registrar interfaces do DbContext para DI
            services.AddScoped<IDocumentsDbContext>(sp => sp.GetRequiredService<AppDbContext>());
            services.AddScoped<IUsersDbContext>(sp => sp.GetRequiredService<AppDbContext>());

            // ========= REPOSITORIES ==========
            services.AddScoped<IDocumentRepository, DocumentRepository>();
            services.AddScoped<IUserRepository, UserRepository>();

            // ========== FILE STORAGE ==========
            services.AddScoped<IFileStorageService, FileSystemSecureStorage>();
            services.AddScoped<ISecureFileStorage, FileSystemSecureStorage>();

            // ========== POST-QUANTUM CRYPTOGRAPHY ==========
            var pqcCliPath = configuration["PQC:CliPath"]
                ?? throw new InvalidOperationException("PQC:CliPath não configurado");

            services.AddScoped<INativePostQuantumSigner>(sp =>
                new NativePostQuantumSigner(
                    pqcCliPath,
                    sp.GetRequiredService<ISecureFileStorage>()));

            // ========== KEY MANAGEMENT ==========
            // KeyManagementService implementa tanto IKeyReader quanto IKeyWriter
            services.AddScoped<KeyManagementService>();
            services.AddScoped<IKeyReader>(sp => sp.GetRequiredService<KeyManagementService>());
            services.AddScoped<IKeyWriter>(sp => sp.GetRequiredService<KeyManagementService>());

            // ========== SECURITY ==========
            services.AddSingleton<IPasswordHasher, BcryptPasswordHasher>();


            return services;
        }
    }
}