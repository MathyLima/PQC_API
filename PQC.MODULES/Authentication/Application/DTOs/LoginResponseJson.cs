namespace PQC.MODULES.Authentication.Application.DTOs
{
    public class LoginResponseJson
    {
        public string Token { get; set; } = string.Empty;
        public string Name { get; set; } = string.Empty;
        public string Email { get; set; } = string.Empty;
    }
}
