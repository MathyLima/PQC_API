using PQC.SHARED.Time;

public class SignatureMetadata
{
    public string DocumentId { get; set; }
    public string DocumentName { get; set; }
    public string SignerName { get; set; }
    public string SignerEmail { get; set; }
    public string SignerCpf { get; set; }
    public DateTime SignedAt = RecifeTimeProvider.Now();
    public string Algorithm { get; set; }
    public string SignatureHash { get; set; }
}
public interface ISignatureMetadata
{
    Task<byte[]> GenerateMetadataPageAsync(SignatureMetadata metadata);
}
