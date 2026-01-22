using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Configuration;
using PQC.MODULES.Documents.Application.Services.UseCases.List;
using PQC.MODULES.Documents.Infraestructure.Repositories;

namespace PQC.MODULES.Documents.DependencyInjection
{
    public static class DocumentsModuleExtensions
    {
        public static IServiceCollection AddUDocumentsModule(this IServiceCollection services, IConfiguration configuration)
        {
            AddRepositories(services);
            AddUseCases(services);

            // Dentro de appjson, registrar o caminho para armazenamento local
            var basePath = configuration["FileStorage:BasePath"];
            services.AddSingleton<IFileStorageService>(new LocalFileStorageService(basePath!));

            return services;
        }

        public static void AddRepositories(this IServiceCollection services)
        {
            services.AddScoped<IDocumentRepository, DocumentRepository>();
        }
        public static void AddUseCases(this IServiceCollection services)
        {
            services.AddScoped<CreateDocumentUseCase>();
            services.AddScoped<GetDocumentByIdUseCase>();
            services.AddScoped<ListDocumentsByUserIdUseCase>();
        }
    }
}
