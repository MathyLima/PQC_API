public interface IFileStorageService
{
    Task<string> SaveFileAsync(byte[] content, string fileName, string contentType,string UserId);
    Task<byte[]> GetFileAsync(string filePath);
    Task DeleteFileAsync(string filePath);
}
