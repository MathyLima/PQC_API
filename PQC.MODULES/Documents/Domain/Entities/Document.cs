using PQC.MODULES.Users.Domain.Entities;

namespace PQC.MODULES.Documents.Domain.Entities
{
    public class Document
    {
        public string Id { get; set; } = Guid.NewGuid().ToString();
        public string? Path { get; set; }
        public string? Nome { get; set; }
        public string? IdUsuario { get; set; }
        public string? TipoArquivo { get; set; }
        public string? AlgoritmoAssinatura { get; set; }
        public long Tamanho { get; set; }
        public DateTime Assinado_em { get; set; } = TimeZoneInfo.ConvertTimeBySystemTimeZoneId(
            DateTime.UtcNow,
            "E. South America Standard Time" // fuso de Recife / Brasília
        );
        public string? AssinaturaDigital { get; set; }
        public User Usuario { get; set; } = null!;
    }
}
