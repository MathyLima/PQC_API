namespace PQC.MODULES.Documents.Domain.Entities
{
    public class Document
    {
        public Guid Id { get; set; }
        public string? FileName { get; set; }
        public string? ContentType { get; set; }
        public long SizeInBytes { get; set; }
        public byte[]? Content { get; set; }
        public Guid UploadedByUserId { get; set; }
        public DateTime UploadedAt { get; set; }
        public bool IsActive { get; set; }

        public Document()
        {
            Id = Guid.NewGuid();
            UploadedAt = DateTime.UtcNow;
            IsActive = true;
        }
    }
}
