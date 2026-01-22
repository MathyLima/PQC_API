using System;
using System.IO;
using System.Threading.Tasks;

public class LocalFileStorageService : IFileStorageService
{
    private readonly string _basePath;

    public LocalFileStorageService(string basePath)
    {
        _basePath = basePath;

        if (!Directory.Exists(_basePath))
            Directory.CreateDirectory(_basePath);
    }

    public async Task<string> SaveFileAsync(byte[] content, string fileName, string contentType, string userId)
    {
        var userPath = Path.Combine(_basePath, userId);
        if (!Directory.Exists(userPath))
            Directory.CreateDirectory(userPath);

        var dateFolder = DateTime.UtcNow.AddHours(-3).ToString("yyyy-MM-dd");
        var datePath = Path.Combine(userPath, dateFolder);
        if (!Directory.Exists(datePath))
            Directory.CreateDirectory(datePath);

        // 🔥 Garantir extensão correta
        if (contentType == "application/pdf" && !fileName.EndsWith(".pdf", StringComparison.OrdinalIgnoreCase))
        {
            fileName += ".pdf";
        }

        var uniqueName = $"{Guid.NewGuid()}_{fileName}";
        var fullPath = Path.Combine(datePath, uniqueName);

        await File.WriteAllBytesAsync(fullPath, content);

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
            File.Delete(filePath);

        return Task.CompletedTask;
    }
}
