using PQC.MODULES.Users.Domain.Entities;
using PQC.SHARED.Exceptions.Domain;
using PQC.SHARED.Time;

namespace PQC.MODULES.Documents.Domain.Entities
{
    public class StoredDocument
    {
        // Construtor só para o EF
        private StoredDocument() { }

        // Factory obrigatória
        public static StoredDocument CreateSigned(
            string id,
            string path,
            string nome,
            string idUsuario,
            string tipoArquivo,
            string algoritmoAssinatura,
            long tamanho,
            string assinaturaDigital,
            DateTime assinadoEm
        )
        {
            if (string.IsNullOrWhiteSpace(assinaturaDigital))
                throw new DomainException("Documento deve ser assinado");

            if (string.IsNullOrWhiteSpace(nome))
                throw new DomainException("Nome do documento inválido");

          

            return new StoredDocument
            {
                Id = id,

                Path = path,
                Nome = nome,
                IdUsuario = idUsuario,
                TipoArquivo = tipoArquivo,

                AlgoritmoAssinatura = algoritmoAssinatura,
                AssinaturaDigital = assinaturaDigital,
                Assinado_em = assinadoEm,

                Tamanho = tamanho
            };
        }

        // ================= PROPRIEDADES =================

        public string Id { get; private set; }

        public string Path { get; private set; }
        public string Nome { get; private set; }

        public string IdUsuario { get; private set; }
        public string TipoArquivo { get; private set; }

        public string AlgoritmoAssinatura { get; private set; }
        public string AssinaturaDigital { get; private set; }

        public DateTime Assinado_em { get; private set; }

        public long Tamanho { get; private set; }

        public User Usuario { get; private set; }
    }
}
