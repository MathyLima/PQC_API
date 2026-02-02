using PQC.MODULES.Documents.Domain.Entities;
using PQC.SHARED.Communication.DTOs.Enums;

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
        public string? PublicKeyReference { get; set; }   // Pode ser: caminho, ID do vault, etc
        public string? PrivateKeyReference { get; set; }  // Depende da implementação
        public SignatureAlgorithm AlgoritmoAssinatura { get; set; }
        public string CodigoAlgoritmo = "BCrypt";

        // Navegação
        public ICollection<StoredDocument> Documentos { get; set; } = new List<StoredDocument>();
    }
}
