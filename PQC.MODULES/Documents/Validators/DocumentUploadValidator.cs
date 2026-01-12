using Microsoft.AspNetCore.Http;

namespace PQC.MODULES.Documents.Validators
{
    public class DocumentUploadValidator
    {
        private const long MaxFileSizeInBytes = 10 * 1024 * 1024; // 10MB
        private static readonly string[] AllowedExtensions = { ".pdf" };
        private static readonly string[] AllowedContentTypes = { "application/pdf" };

        public List<string> Validate(IFormFile file)
        {
            var errors = new List<string>();

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
            var extension = Path.GetExtension(file.FileName).ToLowerInvariant();
            if (!AllowedExtensions.Contains(extension))
            {
                errors.Add($"Apenas arquivos PDF são permitidos. Extensão recebida: {extension}");
            }

            // Valida content type
            if (!AllowedContentTypes.Contains(file.ContentType.ToLowerInvariant()))
            {
                errors.Add($"Tipo de arquivo inválido. Esperado: application/pdf. Recebido: {file.ContentType}");
            }

            return errors;
        }
    }
}
