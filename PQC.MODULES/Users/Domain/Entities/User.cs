using PQC.MODULES.Documents.Domain.Entities;

namespace PQC.MODULES.Users.Domain.Entities
{
    public class User
    {
        public string Id { get; set; } = Guid.NewGuid().ToString();
        public string Nome { get; set; }
        public string Cpf { get; set; }
        public string Email { get; set; }
        public string? Telefone { get; set; }
        public string Login { get; set; }
        public string Senha { get; set; }
        public string CodigoAlgoritmo = "BCrypt";

        // Navegação
        public ICollection<Document> Documentos { get; set; } = new List<Document>();
    }
}
