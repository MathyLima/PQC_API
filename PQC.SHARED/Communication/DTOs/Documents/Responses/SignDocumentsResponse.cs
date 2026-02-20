namespace PQC.SHARED.Communication.DTOs.Documents.Responses
{
    public class SignDocumentResponse
    {
        
        public Guid DocumentId { get; set; }
        public string DocumentName { get; set; } = string.Empty;
        public byte[] SignedContent { get; set; } = Array.Empty<byte>();
        public string ContentType { get; set; } = string.Empty;
        public string Algorithm { get; set; } = string.Empty;
        public long FileSize { get; set; }
        public DateTime SignedAt { get; set; }
        
    }
}
