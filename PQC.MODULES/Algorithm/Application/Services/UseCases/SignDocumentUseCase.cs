using PQC.EXCEPTIONS.ExceptionsBase;
using PQC.MODULES.Documents.Infraestructure.InMemory;

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
                .FirstOrDefault(d => d.Id == documentId && d.UploadedByUserId == userId && d.IsActive);

            if (document == null)
            {
                throw new NotFoundException("Document not found");
            }

            // Executa o algoritmo de assinatura
            var result = await _algorithmExecutor.SignDocumentAsync(document.Content, privateKeyPath);

            if (!result.Success)
            {
                throw new Exception($"Failed to sign document: {result.ErrorMessage}");
            }

            // Salva a assinatura no documento
            document.Signature = result.Signature;
        }
    }
}
