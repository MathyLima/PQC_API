using Microsoft.AspNetCore.Http;

namespace PQC.COMMUNICATION.Requests.Documents.Create
{
    public class CreateDocumentRequestJson
    {
        public IFormFile? File { get; set; }
        public String? FileName { get; set; }
        public string? IdUsuario { get; set; }
    }
}
