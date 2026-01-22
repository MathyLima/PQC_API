using PQC.MODULES.Documents.Infraestructure.SignAlgorithm.Application.Services;
using PQC.MODULES.Documents.Infraestructure.SignAlgorithm.Domain.Entities;

namespace PQC.MODULES.Documents.Infraestructure.SignAlgorithm.Application.Services.UseCases
{
    public class SignUploadedDocumentUseCase
    {
        private readonly SignDocumentAlgorithmExecutor _algorithmExecutor;

        public SignUploadedDocumentUseCase(SignDocumentAlgorithmExecutor algorithmExecutor)
        {
            _algorithmExecutor = algorithmExecutor;
        }

        /// <summary>
        /// Assina um documento usando o algoritmo PQC
        /// </summary>
        /// <param name="documentContent">Conteúdo do documento em bytes</param>
        /// <returns>Resultado da assinatura contendo algoritmo usado e assinatura digital</returns>
        public async Task<SignatureResult> Execute(byte[] documentContent)
        {
            if (documentContent == null || documentContent.Length == 0)
            {
                throw new ArgumentException("Document content is empty");
            }

            var result = await _algorithmExecutor.SignDocumentAsync(documentContent);

            if (!result.Success)
            {
                throw new Exception($"Failed to sign document: {result.ErrorMessage}");
            }

            return result;
        }
    }
}