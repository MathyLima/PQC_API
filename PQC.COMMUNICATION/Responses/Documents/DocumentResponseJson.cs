namespace PQC.COMMUNICATION.Responses.Documents
{
    public class DocumentResponseJson
    {
        public Guid Id { get; set; }
        public string? FileName { get; set; }
        
        public DateTime SignedAt { get; set; }
    }
}
