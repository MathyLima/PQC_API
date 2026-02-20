namespace PQC.MODULES.Documents.Application.Interfaces.PQCsigner
{
    public interface INativePostQuantumSigner
    {
        /// <summary>
        /// Assina dados usando chave privada em bytes
        /// </summary>
        Task<SignatureResult> SignAsync(byte[] data, byte[] privateKey);

        /// <summary>
        /// Verifica assinatura usando chave pública em bytes
        /// </summary>
        Task<bool> VerifyAsync(byte[] data, byte[] signature, byte[] publicKey);
        Task<(byte[] publicKey, byte[] privateKey)> GenerateKeyPairAsync(string algorithm);
    }

    public class SignatureResult
    {
        public byte[] Signature { get; set; }
        public string Algorithm { get; set; }
    }
}