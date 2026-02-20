using PQC.MODULES.Documents.Application.DTOs;

namespace PQC.MODULES.Documents.Validators
{
    public class DocumentUploadValidator
    {
        private const long MaxFileSizeInBytes = 10 * 1024 * 1024; // 10MB
        private static readonly string[] AllowedExtensions = { ".pdf" };
        private static readonly string[] AllowedContentTypes = { "application/pdf" };

        public List<string> Validate(DocumentUploadRequest request)
        {
            var errors = new List<string>();
            var file = request.Content;
            var contentType = request.ContentType;
            if (file == null || file.Length == 0)
            {
                errors.Add("O arquivo é obrigatório.");
                return errors;
            }

            // Valida tamanho
            if (file.Length > MaxFileSizeInBytes)
            {
                errors.Add($"O arquivo deve ter tamanho menor que {MaxFileSizeInBytes / 1024 / 1024}MB.");
            }

            // Valida extensão
            var extension = Path.GetExtension(request.FileName).ToLowerInvariant();
            if (!AllowedExtensions.Contains(extension))
            {
                errors.Add($"Apenas arquivos PDF são permitidos. Extensão recebida: {extension}");
            }

            // Valida content type
            if (!AllowedContentTypes.Contains(contentType.ToLowerInvariant()))
            {
                errors.Add($"Tipo de arquivo inválido. Esperado: application/pdf. Recebido: {contentType}");
            }

            return errors;
        }
    }
}
