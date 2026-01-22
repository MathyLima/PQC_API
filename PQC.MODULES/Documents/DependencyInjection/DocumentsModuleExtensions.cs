using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using PQC.MODULES.Documents.Application.Services.UseCases.List;
using PQC.MODULES.Documents.Infraestructure.PageGenerator.Domain.Services;
using PQC.MODULES.Documents.Infraestructure.Repositories;
using PQC.MODULES.Documents.Infraestructure.SignAlgorithm.Application.Services;
using PQC.MODULES.Documents.Infraestructure.SignAlgorithm.Application.Services.UseCases;
using PQC.MODULES.Users.Infraestructure.Repositories;

namespace PQC.MODULES.Documents.DependencyInjection
{
    public static class DocumentsModuleExtensions
    {
        public static IServiceCollection AddUDocumentsModule(this IServiceCollection services, IConfiguration configuration)
        {
            AddRepositories(services);
            AddUseCases(services,configuration);

            // Registrar o caminho para armazenamento local
            var basePath = configuration["FileStorage:BasePath"];
            services.AddSingleton<IFileStorageService>(new LocalFileStorageService(basePath!));

            return services;
        }

        public static void AddRepositories(this IServiceCollection services)
        {
            services.AddScoped<IDocumentRepository, DocumentRepository>();
            // Se você tiver IUserRepository
            services.AddScoped<IUserRepository, UserRepository>();
        }

        public static void AddUseCases(this IServiceCollection services, IConfiguration configuration)
        {
            // Use cases principais
            services.AddScoped<CreateDocumentUseCase>();
            services.AddScoped<GetDocumentByIdUseCase>();
            services.AddScoped<ListDocumentsByUserIdUseCase>();

            // Use case de assinatura
            services.AddScoped<SignUploadedDocumentUseCase>();

            // Executor de assinatura com valores do appsettings.json
            var algorithmSection = configuration.GetSection("Algorithm");
            var keysSection = configuration.GetSection("Keys");

            services.AddScoped<SignDocumentAlgorithmExecutor>(_ =>
            {
                var execPath = algorithmSection["ExecutablePath"];
                var tempDir = algorithmSection["TempDirectory"];
                var privateKeyPath = keysSection["PrivateKeyPath"];
                return new SignDocumentAlgorithmExecutor(execPath!, tempDir!, privateKeyPath!);
            });

            // Serviços auxiliares
            services.AddScoped<IDocumentMerger, PdfDocumentMerger>();
            services.AddScoped<ISignatureMetadata, PdfSignatureMetadataPageGenerator>();
        }




    }
}
