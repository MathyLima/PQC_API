namespace PQC.SHARED.Communication.Interfaces
{
    /// <summary>
    /// Interface para leitura de chaves (usado por módulos que assinam)
    /// </summary>
    public interface IKeyReader
    {
        Task<byte[]> GetPublicKeyAsync(string userId);
        Task<byte[]> GetPrivateKeyAsync(string userId);
        Task<bool> HasKeysAsync(string userId);
    }
}