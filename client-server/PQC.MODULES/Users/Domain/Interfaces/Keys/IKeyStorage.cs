namespace PQC.MODULES.Users.Domain.Interfaces.Keys
{ 
    /// <summary>
  /// Abstração para armazenamento de chaves
  /// </summary>
    public interface IKeyStorage
    {
        Task<string> SavePublicKeyAsync(string userId, byte[] publicKey);
        Task<string> SavePrivateKeyAsync(string userId, byte[] privateKey);
        Task<byte[]> GetPublicKeyAsync(string userId);
        Task<byte[]> GetPrivateKeyAsync(string userId);
        Task DeleteKeysAsync(string userId);
    }
}
