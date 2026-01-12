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
                errors.Add("File is required");
                return errors;
            }

            // Valida tamanho
            if (file.Length > MaxFileSizeInBytes)
            {
                errors.Add($"File size must be less than {MaxFileSizeInBytes / 1024 / 1024}MB");
            }

            // Valida extensão
            var extension = Path.GetExtension(file.FileName).ToLowerInvariant();
            if (!AllowedExtensions.Contains(extension))
            {
                errors.Add($"Only PDF files are allowed. Received: {extension}");
            }

            // Valida content type
            if (!AllowedContentTypes.Contains(file.ContentType.ToLowerInvariant()))
            {
                errors.Add($"Invalid content type. Expected: application/pdf, Received: {file.ContentType}");
            }

            return errors;
        }
    }
}