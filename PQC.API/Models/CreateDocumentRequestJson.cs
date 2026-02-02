namespace PQC.API.Models
{
    public class CreateDocumentRequestJson
    {
        public IFormFile? File { get; set; }
        public string? FileName { get; set; }
        public string? UserId { get; set; }
    }
}
