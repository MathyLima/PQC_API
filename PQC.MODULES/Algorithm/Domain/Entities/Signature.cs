// PQC.MODULES/Signatures/Domain/Entities/Signature.cs
using PQC.MODULES.Documents.Domain.Entities;

namespace PQC.MODULES.Signatures.Domain.Entities
{
    public class Signature
    {
        public string Id { get; set; } = Guid.NewGuid().ToString();
        public string IdDocumento { get; set; }
        public string Nome { get; set; }
        public string Cpf { get; set; }
        public string Email { get; set; }
        public string? Telefone { get; set; }
        public string AssinaturaDigital { get; set; }
        public DateTime AssinadoEm { get; set; } = DateTime.UtcNow;

        // Navegação
        public Document Documento { get; set; }
    }
}