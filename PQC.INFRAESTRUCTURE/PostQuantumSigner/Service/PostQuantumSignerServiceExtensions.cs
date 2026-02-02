using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using PQC.INFRAESTRUCTURE.PostQuantumSigner.Service.Wrapper;
using PQC.MODULES.Documents.Application.Interfaces.PQCsigner;

namespace PQC.INFRAESTRUCTURE.PostQuantumSigner.Service;

public static class PostQuantumSignerServiceExtensions
{
    /// <summary>
    /// Adiciona o serviço de assinatura pós-quântica à coleção de serviços.
    /// </summary>
    public static IServiceCollection AddPostQuantumSigner(
        this IServiceCollection services,
        IConfiguration configuration)
    {
        // Registra o serviço nativo como Singleton (validação de integridade só acontece uma vez)
        services.AddSingleton<INativePostQuantumSigner, NativePostQuantumSigner>();

        return services;
    }
}