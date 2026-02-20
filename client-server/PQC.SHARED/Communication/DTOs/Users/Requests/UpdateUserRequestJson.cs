using PQC.SHARED.Communication.DTOs.Enums;

namespace PQC.SHARED.Communication.DTOs.Users.Requests
{
    public class UpdateUserRequestJson
    {
        public string? Nome { get; set; }
        public string? Cpf { get; set; }  
        public string? Email { get; set; } 
        public string? Telefone { get; set; } 
        public string? Login { get; set; } 
        public string? Senha { get; set; } 
        public SignatureAlgorithm? AlgoritmoAssinatura { get; set; }
    }
}
