using PQC.SHARED.Communication.DTOs.Enums;

namespace PQC.SHARED.Communication.DTOs.Users.Requests
{
    public class CreateUserRequestJson
    {
        public required string Name { get; set; }
        public required string Email { get; set; }
        public required string Password { get; set; }
        public required string Cpf { get; set; }
        public required string Telefone { get; set; }
        public required string Login { get; set; }
        public required SignatureAlgorithm SignatureAlgorithm { get; set; }
    }
}
