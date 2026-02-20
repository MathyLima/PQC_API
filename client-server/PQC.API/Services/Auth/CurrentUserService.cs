using PQC.MODULES.Documents.Domain.Interfaces;
using System.Security.Claims;

public class CurrentUserService : ICurrentUserService
{
    private readonly IHttpContextAccessor _httpContextAccessor;

    public CurrentUserService(IHttpContextAccessor httpContextAccessor)
    {
        _httpContextAccessor = httpContextAccessor;
    }

    public Guid GetUserId()
    {
        // Tenta primeiro com "sub", depois com NameIdentifier
        var userId = _httpContextAccessor.HttpContext?
            .User?
            .FindFirst("sub")?.Value
            ?? _httpContextAccessor.HttpContext?
            .User?
            .FindFirst(ClaimTypes.NameIdentifier)?.Value;

        if (string.IsNullOrEmpty(userId))
        {
            // DEBUG: Veja quais claims existem
            var claims = _httpContextAccessor.HttpContext?.User?.Claims
                .Select(c => $"{c.Type}: {c.Value}");
            Console.WriteLine("Claims disponíveis: " + string.Join(", ", claims ?? Array.Empty<string>()));

            throw new UnauthorizedAccessException("Usuário não autenticado");
        }

        return Guid.Parse(userId);
    }
    public string? GetIpAddress()
    {
        return _httpContextAccessor.HttpContext?
            .Connection?
            .RemoteIpAddress?
            .ToString();
    }

    public string? GetUserAgent()
    {
        return _httpContextAccessor.HttpContext?
            .Request?
            .Headers["User-Agent"]
            .ToString();
    }
}
