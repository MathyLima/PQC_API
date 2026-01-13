namespace PQC.MODULES.Algorithm.Domain.Entities
{
    public class SignatureResult
    {
        public bool Success { get; set; }
        public byte[]? Signature { get; set; }
        public string? ErrorMessage { get; set; }
        public int ExitCode { get; set; }
        public string? StdOutput { get; set; }
        public string? StdError { get; set; }
    }
}
