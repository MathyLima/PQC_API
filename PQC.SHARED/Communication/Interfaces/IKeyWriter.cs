namespace PQC.SHARED.Communication.Interfaces
{
    /// <summary>
    /// Interface para gerenciamento de chaves (usado por Users)
    /// </summary>
    public interface IKeyWriter
    {
        Task<string> SavePublicKeyAsync(string userId, byte[] publicKey);
        Task<string> SavePrivateKeyAsync(string userId, byte[] privateKey);
        Task DeleteKeysAsync(string userId);
    }
}