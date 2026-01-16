// PQC.MODULES/Signatures/Domain/Entities/Signature.cs
namespace PQC.MODULES.Signatures.Domain.Entities
{
    public class Signature
    {
        public Guid Id { get; set; }
        public Guid DocumentId { get; set; }
        public Guid SignedByUserId { get; set; }

        // Dados da assinatura
        public byte[] SignatureData { get; set; } = Array.Empty<byte>();

        // Metadados
        public string Algorithm { get; set; } = string.Empty;  // Ex: ML-DSA-65
        public string PublicKeyPath { get; set; } = string.Empty;
        public string PrivateKeyPath { get; set; } = string.Empty;

        // Hashes para integridade
        public string DocumentHashAtSign { get; set; } = string.Empty;  // SHA-256 do documento no momento da assinatura
        public long DocumentSizeAtSign { get; set; }

        // Status
        public bool IsValid { get; set; }  // Última verificação
        public DateTime? LastVerifiedAt { get; set; }

        // Auditoria
        public DateTime CreatedAt { get; set; }
        public bool IsActive { get; set; }

        public Signature()
        {
            Id = Guid.NewGuid();
            CreatedAt = DateTime.UtcNow;
            IsActive = true;
            IsValid = true;
        }
    }
}