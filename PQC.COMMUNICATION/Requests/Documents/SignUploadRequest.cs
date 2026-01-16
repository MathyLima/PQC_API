using Microsoft.AspNetCore.Http;

public class SignUploadRequest
{
    public IFormFile File { get; set; } = default!;
    public string? PrivateKeyPath { get; set; }
}
