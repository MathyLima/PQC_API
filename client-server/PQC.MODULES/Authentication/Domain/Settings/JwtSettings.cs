namespace PQC.MODULES.Authentication.Domain.Settings
{
    public class JwtSettings
    {
       public string? SecretKey { get; set; }
       public string? Issuer { get; set; }
       public string? Audience { get; set; }
       public int ExpirationMinutes { get; set; }
    }
}
