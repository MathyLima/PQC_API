using PQC.EXCEPTIONS.ExceptionsBase;
using PQC.MODULES.Documents.Infraestructure.InMemory;
using PQC.MODULES.Signatures.Domain.Entities;
using PQC.MODULES.Users.Infrastructure.InMemory;

namespace PQC.MODULES.Algorithm.Application.Services.UseCases
{
    public class SignDocumentUseCase
    {
        private readonly AlgorithmExecutor _algorithmExecutor;

        public SignDocumentUseCase(AlgorithmExecutor algorithmExecutor)
        {
            _algorithmExecutor = algorithmExecutor;
        }

        public async Task Execute(Guid documentId, Guid userId, string? privateKeyPath = null)
        {
            // Busca o documento
            var document = DocumentInMemoryDatabase.Documents
                    .FirstOrDefault(d =>
                        d.Id == documentId.ToString() &&
                        d.IdUsuario == userId.ToString()
                       );
            var usuario = UserInMemoryDatabase.Users
            .FirstOrDefault(u => u.Id == userId.ToString());


            if (document == null)
            {
                throw new NotFoundException("Document not found");
            }

            // Executa o algoritmo de assinatura
            var result = await _algorithmExecutor.SignDocumentAsync(document.Path);
            if (!result.Success)
            {
                throw new Exception($"Failed to sign document: {result.ErrorMessage}");
            }

            var signature = new Signature
            {
                Id = Guid.NewGuid().ToString(),
                IdDocumento = document.Id,
                AssinaturaDigital = Convert.ToBase64String(result.Signature),
                AssinadoEm = DateTime.UtcNow,
                Nome = usuario.Nome,
                Cpf = usuario.Cpf,
                Email = usuario.Email
            };


            // Salva a assinatura no documento
            document.Assinaturas.Add(signature);
        }
    }
}
