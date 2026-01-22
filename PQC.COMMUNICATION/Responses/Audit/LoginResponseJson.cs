// PQC.COMMUNICATION/Responses/Auth/LoginResponseJson.cs
namespace PQC.COMMUNICATION.Responses.Audit
{
    public class LoginResponseJson
    {
        public string Token { get; set; } = string.Empty;
        public string UserId { get; set; }
        public string Name { get; set; } = string.Empty;
        public string Email { get; set; } = string.Empty;
    }
}