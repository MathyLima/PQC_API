using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using PQC.SHARED.Communication.Interfaces.PQC.SHARED.Interfaces;

namespace PQC.INFRAESTRUCTURE.FilesManagement.FileStorage.Local
{
    public class FileSystemSecureStorage : IFileStorageService, ISecureFileStorage
    {
        private readonly string _basePath;
        private readonly ILogger<FileSystemSecureStorage> _logger;

        public FileSystemSecureStorage(IConfiguration configuration, ILogger<FileSystemSecureStorage> logger)
        {
            _basePath = configuration["Storage:BasePath"] ?? Path.Combine(Directory.GetCurrentDirectory(), "secure-storage");
            _logger = logger;

            if (!Directory.Exists(_basePath))
                Directory.CreateDirectory(_basePath);
        }

        // =================== IFileStorageService ===================
        public async Task<string> SaveFileAsync(byte[] content, string fileName, string contentType, string userId)
        {
            var userPath = Path.Combine(_basePath, userId);
            if (!Directory.Exists(userPath))
                Directory.CreateDirectory(userPath);

            var dateFolder = DateTime.UtcNow.AddHours(-3).ToString("yyyy-MM-dd");
            var datePath = Path.Combine(userPath, dateFolder);
            if (!Directory.Exists(datePath))
                Directory.CreateDirectory(datePath);

            if (contentType == "application/pdf" && !fileName.EndsWith(".pdf", StringComparison.OrdinalIgnoreCase))
                fileName += ".pdf";

            var uniqueName = $"{Guid.NewGuid()}_{fileName}";
            var fullPath = Path.Combine(datePath, uniqueName);

            await File.WriteAllBytesAsync(fullPath, content);
            _logger.LogInformation("File saved at {Path}", fullPath);

            return fullPath;
        }

        public async Task<byte[]> GetFileAsync(string filePath)
        {
            if (!File.Exists(filePath))
                throw new FileNotFoundException("File not found", filePath);

            return await File.ReadAllBytesAsync(filePath);
        }

        public Task DeleteFileAsync(string filePath)
        {
            if (File.Exists(filePath))
            {
                File.Delete(filePath);
                _logger.LogInformation("File deleted: {Path}", filePath);
            }

            return Task.CompletedTask;
        }

        // =================== ISecureFileStorage ===================
        public async Task<string> SaveAsync(string path, byte[] data)
        {
            var fullPath = GetFullPath(path);
            var directory = Path.GetDirectoryName(fullPath);

            if (!Directory.Exists(directory!))
                Directory.CreateDirectory(directory!);

            await File.WriteAllBytesAsync(fullPath, data);
            _logger.LogInformation("File saved at {Path}", fullPath);

            return fullPath;
        }

        public async Task<byte[]> GetAsync(string path)
        {
            var fullPath = GetFullPath(path);

            if (!File.Exists(fullPath))
                throw new FileNotFoundException($"File not found: {path}");

            return await File.ReadAllBytesAsync(fullPath);
        }

        public Task DeleteAsync(string path)
        {
            var fullPath = GetFullPath(path);

            if (File.Exists(fullPath))
            {
                File.Delete(fullPath);
                _logger.LogInformation("File deleted: {Path}", fullPath);
            }

            return Task.CompletedTask;
        }

        public Task<bool> ExistsAsync(string path)
        {
            var fullPath = GetFullPath(path);
            return Task.FromResult(File.Exists(fullPath));
        }

        public Task<IEnumerable<string>> ListFilesAsync(string directoryPath)
        {
            var fullPath = GetFullPath(directoryPath);

            if (!Directory.Exists(fullPath))
                return Task.FromResult(Enumerable.Empty<string>());

            var files = Directory.GetFiles(fullPath)
                .Select(f => Path.GetRelativePath(_basePath, f));

            return Task.FromResult(files);
        }

        public string GetFullPath(string relativePath)
        {
            relativePath = relativePath.Replace("..", "").Replace("\\", "/");
            return Path.Combine(_basePath, relativePath);
        }
    }
}
