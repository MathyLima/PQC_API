namespace PQC.COMMUNICATION.Responses.Documents
{
    public class DocumentResponseJson
    {
        public Guid Id { get; set; }
        public string? FileName { get; set; }
        public string? ContentType { get; set; }
        public long SizeInBytes { get; set; }
        public DateTime UploadedAt { get; set; }
    }
}
