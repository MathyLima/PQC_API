public class SignatureMetadata
{
    public string DocumentId { get; set; }

    public string DocumentName { get; set; }

    public string SignerName { get; set; }

    public string SignerEmail { get; set; }

    public string SignerId { get; set; }

    public DateTime SignedAt { get; set; }

    public string Algorithm { get; set; }

    public string SignatureValue { get; set; }

    public string PublicKey { get; set; }

    public string HashAlgorithm { get; set; }

    public string DocumentHash { get; set; }
}
