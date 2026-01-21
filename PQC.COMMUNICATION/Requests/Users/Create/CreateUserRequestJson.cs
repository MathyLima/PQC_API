namespace PQC.COMMUNICATION.Requests.Users.Create
{
    public class CreateUserRequestJson
    {
        public string? Name { get; set; }
        public string? Email { get; set; } 
        public string? Password { get; set; }
        public string? Cpf { get; set; }
        public string? Telefone { get; set; }
        public string? Login { get; set; }
        public string CodigoAlgoritmo  ="BCrypt";

    }
}
