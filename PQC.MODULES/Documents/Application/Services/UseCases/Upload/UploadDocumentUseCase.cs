using Microsoft.AspNetCore.Http;
using PQC.COMMUNICATION.Responses.Documents;
using PQC.EXCEPTIONS.ExceptionsBase;
using PQC.MODULES.Documents.Domain.Entities;
using PQC.MODULES.Documents.Infraestructure.InMemory;
using PQC.MODULES.Documents.Validators;

namespace PQC.MODULES.Documents.Application.Services.UseCases.Upload
{
    public class UploadDocumentUseCase
    {
        public async Task<DocumentResponseJson> Execute(IFormFile file, Guid userId)
        {
            var validator = new DocumentUploadValidator();
            var errors = validator.Validate(file);

            if (errors.Any())
            {
                throw new ErrorOnValidationException(errors);
            }

            // Lê o conteúdo do arquivo
            byte[] fileContent;
            using (var memoryStream = new MemoryStream())
            {
                await file.CopyToAsync(memoryStream);
                fileContent = memoryStream.ToArray();
            }
            // Provavelmente executa o algoritmo de assinatura aqui

            var document = new StoredDocument
            {
                Nome = file.FileName,
                IdUsuario = userId.ToString(),
            };

            // Salva no banco em memória
            DocumentInMemoryDatabase.Documents.Add(document);

            return new DocumentResponseJson
            {
                Id = Guid.Parse(document.Id),
                FileName = document.Nome,
                SignedAt = document.Assinado_em
            };
        }
    }
}