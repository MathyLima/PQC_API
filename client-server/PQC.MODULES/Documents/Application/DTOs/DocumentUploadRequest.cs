namespace PQC.MODULES.Documents.Application.DTOs
{
    public class DocumentUploadRequest
    {   
        public required string UserId { get; set; }
        public required byte[] Content { get; set; }
        public required string FileName { get; set; }
        public required string ContentType { get; set; }
    }
}
