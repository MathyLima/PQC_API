// PQC.COMMUNICATION/Responses/Auth/LoginResponseJson.cs
namespace PQC.COMMUNICATION.Responses.Auth
{
    public class LoginResponseJson
    {
        public string Token { get; set; } = string.Empty;
        public Guid UserId { get; set; }
        public string Name { get; set; } = string.Empty;
        public string Email { get; set; } = string.Empty;
    }
}