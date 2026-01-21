using PQC.MODULES.Signatures.Domain.Entities;
using PQC.MODULES.Users.Domain.Entities;

namespace PQC.MODULES.Documents.Domain.Entities
{
    public class Document
    {
        public string Id { get; set; } = Guid.NewGuid().ToString();
        public string Path { get; set; }
        public string? Nome { get; set; }
        public string IdUsuario { get; set; }
        public DateTime UploadEm { get; set; } = DateTime.UtcNow;

        // Navegação
        public User Usuario { get; set; }
        public ICollection<Signature> Assinaturas { get; set; } = new List<Signature>();
    }
}
