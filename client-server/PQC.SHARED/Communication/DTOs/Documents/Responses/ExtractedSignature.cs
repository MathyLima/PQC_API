public class ExtractedSignature
{
    public required int Order { get; set; }

    public required string DocumentId { get; set; }

    public required string SignerName { get; set; }

    public required DateTime SignedAt { get; set; }

    public required string Algorithm { get; set; }

    public required string DocumentHash { get; set; }

    public required string SignatureValue { get; set; }

    public int PageNumber { get; set; }

    public required string PublicKey { get; set; }
}
