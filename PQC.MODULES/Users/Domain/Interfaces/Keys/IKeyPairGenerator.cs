namespace PQC.MODULES.Users.Domain.Interfaces.Keys
{
    /// <summary>
    /// Abstração para geração de par de chaves
    /// </summary>
    public interface IKeyPairGenerator
    {
        Task<(byte[] publicKey, byte[] privateKey)> GenerateKeyPairAsync();
    }
}
