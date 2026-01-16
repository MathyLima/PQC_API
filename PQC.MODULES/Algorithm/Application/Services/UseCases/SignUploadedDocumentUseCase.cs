using PQC.MODULES.Algorithm.Application.Services;
using PQC.MODULES.Algorithm.Domain.Entities;

namespace PQC.MODULES.Algorithm.Application.Services.UseCases
{
    public class SignUploadedDocumentUseCase
    {
        private readonly AlgorithmExecutor _algorithmExecutor;

        public SignUploadedDocumentUseCase(AlgorithmExecutor algorithmExecutor)
        {
            _algorithmExecutor = algorithmExecutor;
        }

        public async Task<SignatureResult> Execute(
            byte[] documentContent,
            Guid userId,
            string? privateKeyPath = null
        )
        {
            if (documentContent == null || documentContent.Length == 0)
            {
                throw new ArgumentException("Document content is empty");
            }

            // Aqui não existe banco, nem documento persistido
            // Apenas assinatura direta
            var result = await _algorithmExecutor.SignDocumentAsync(
                documentContent
            );

            if (!result.Success)
            {
                throw new Exception(result.ErrorMessage ?? "Failed to sign document");
            }

            return result;
        }
    }
}
