using PQC.SHARED.Communication.Interfaces;
using PQC.SHARED.Communication.Interfaces.PQC.SHARED.Interfaces;

namespace PQC.INFRAESTRUCTURE.FilesManagement.Keys
{
    public class KeyManagementService : IKeyReader, IKeyWriter
    {
        private readonly ISecureFileStorage _fileStorage;
        private const string KEYS_BASE_PATH = "keys";

        public KeyManagementService(ISecureFileStorage fileStorage)
        {
            _fileStorage = fileStorage;
        }

        public async Task<string> SavePublicKeyAsync(string userId, byte[] publicKey)
        {
            var path = GetPublicKeyPath(userId);
            var fullPath = await _fileStorage.SaveAsync(path, publicKey);
            Console.WriteLine($"✓ Public key saved: {fullPath}");
            return fullPath;
        }

        public async Task<string> SavePrivateKeyAsync(string userId, byte[] privateKey)
        {
            var path = GetPrivateKeyPath(userId);
            var fullPath = await _fileStorage.SaveAsync(path, privateKey);
            Console.WriteLine($"✓ Private key saved (protected): {fullPath}");
            return fullPath;
        }

        public async Task<byte[]> GetPublicKeyAsync(string userId)
        {
            var path = GetPublicKeyPath(userId);

            if (!await _fileStorage.ExistsAsync(path))
                throw new FileNotFoundException($"Public key not found for user {userId}");

            return await _fileStorage.GetAsync(path);
        }

        public async Task<byte[]> GetPrivateKeyAsync(string userId)
        {
            var path = GetPrivateKeyPath(userId);

            if (!await _fileStorage.ExistsAsync(path))
                throw new FileNotFoundException($"Private key not found for user {userId}");

            return await _fileStorage.GetAsync(path);
        }

        public async Task<bool> HasKeysAsync(string userId)
        {
            var publicKeyPath = GetPublicKeyPath(userId);
            var privateKeyPath = GetPrivateKeyPath(userId);

            var hasPublicKey = await _fileStorage.ExistsAsync(publicKeyPath);
            var hasPrivateKey = await _fileStorage.ExistsAsync(privateKeyPath);

            return hasPublicKey && hasPrivateKey;
        }

        public async Task DeleteKeysAsync(string userId)
        {
            var publicKeyPath = GetPublicKeyPath(userId);
            var privateKeyPath = GetPrivateKeyPath(userId);

            if (await _fileStorage.ExistsAsync(publicKeyPath))
                await _fileStorage.DeleteAsync(publicKeyPath);

            if (await _fileStorage.ExistsAsync(privateKeyPath))
                await _fileStorage.DeleteAsync(privateKeyPath);

            Console.WriteLine($"✓ Keys deleted for user {userId}");
        }

        public Task<string> GetPublicKeyPathAsync(string userId)
        {
            var path = GetPublicKeyPath(userId);
            var fullPath = _fileStorage.GetFullPath(path);
            return Task.FromResult(fullPath);
        }

        public Task<string> GetPrivateKeyPathAsync(string userId)
        {
            var path = GetPrivateKeyPath(userId);
            var fullPath = _fileStorage.GetFullPath(path);
            return Task.FromResult(fullPath);
        }

        // Helpers privados
        private string GetPublicKeyPath(string userId)
        {
            return $"{KEYS_BASE_PATH}/{userId}/public.key";
        }

        private string GetPrivateKeyPath(string userId)
        {
            return $"{KEYS_BASE_PATH}/{userId}/private.key";
        }
    }
}