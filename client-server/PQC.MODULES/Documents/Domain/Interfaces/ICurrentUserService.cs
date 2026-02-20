namespace PQC.MODULES.Documents.Domain.Interfaces
{

    public interface ICurrentUserService
    {
        Guid GetUserId();
        string? GetIpAddress();
        string? GetUserAgent();
    }
}
