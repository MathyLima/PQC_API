namespace PQC.MODULES.Algorithm.Domain.Entities
{
    public class SignatureResult
    {
        public bool Success { get; set; }

        // Assinatura em bytes
        public byte[]? Signature { get; set; }

        // Base64 opcional para envio via JSON
        public string? SignatureBase64 => Signature != null ? Convert.ToBase64String(Signature) : null;

        // Algoritmo usado
        public string? Algorithm { get; set; }

        public string? ErrorMessage { get; set; }
        public int ExitCode { get; set; }
        public string? StdOutput { get; set; }
        public string? StdError { get; set; }
    }
}
