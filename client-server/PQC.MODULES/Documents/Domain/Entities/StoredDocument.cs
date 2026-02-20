using PQC.MODULES.Users.Domain.Entities;
using PQC.SHARED.Exceptions.Domain;

namespace PQC.MODULES.Documents.Domain.Entities
{
    public class StoredDocument
    {
        // ===============================
        // Construtor para EF
        // ===============================
        private StoredDocument() { }

        // ===============================
        // Factory principal
        // ===============================
        public static StoredDocument CreateSigned(
            string id,
            string originalPath,          // PDF BASE
            string signedPath,            // PDF ASSINADO
            byte[] originalPdfBytes,      // BYTES NORMALIZADOS
            string originalHash,          // HASH BASE64

            string nome,
            string idUsuario,
            string tipoArquivo,

            string algoritmoAssinatura,
            string assinaturaDigital,
            string chavePublicaUsada,

            DateTime assinadoEm,
            long tamanho
        )
        {
            // ===============================
            // Validações de Domínio
            // ===============================

            if (string.IsNullOrWhiteSpace(id))
                throw new DomainException("Id inválido");

            if (originalPdfBytes == null || originalPdfBytes.Length == 0)
                throw new DomainException("PDF original é obrigatório");

            if (string.IsNullOrWhiteSpace(originalHash))
                throw new DomainException("Hash original é obrigatório");

            if (string.IsNullOrWhiteSpace(assinaturaDigital))
                throw new DomainException("Documento deve ser assinado");

            if (string.IsNullOrWhiteSpace(chavePublicaUsada))
                throw new DomainException("Chave pública é obrigatória");

            if (string.IsNullOrWhiteSpace(nome))
                throw new DomainException("Nome inválido");

            // ===============================
            // Criação
            // ===============================

            return new StoredDocument
            {
                Id = id,

                OriginalPath = originalPath,
                SignedPath = signedPath,

                OriginalPdfBytes = originalPdfBytes,
                OriginalHash = originalHash,

                Nome = nome,
                IdUsuario = idUsuario,
                TipoArquivo = tipoArquivo,

                AlgoritmoAssinatura = algoritmoAssinatura,
                AssinaturaDigital = assinaturaDigital,
                ChavePublicaUsada = chavePublicaUsada,

                AssinadoEm = assinadoEm,
                Tamanho = tamanho
            };
        }

        // ===============================
        // PROPRIEDADES
        // ===============================

        public string Id { get; private set; }

        // Arquivos
        public string OriginalPath { get; private set; }
        public string SignedPath { get; private set; }

        // Criptografia
        public byte[] OriginalPdfBytes { get; private set; }
        public string OriginalHash { get; private set; }

        public string AlgoritmoAssinatura { get; private set; }
        public string AssinaturaDigital { get; private set; }
        public string ChavePublicaUsada { get; private set; }

        public DateTime AssinadoEm { get; private set; }

        // Metadados
        public string Nome { get; private set; }
        public string IdUsuario { get; private set; }
        public string TipoArquivo { get; private set; }

        public long Tamanho { get; private set; }

        // Navigation
        public User Usuario { get; private set; }
    }
}
