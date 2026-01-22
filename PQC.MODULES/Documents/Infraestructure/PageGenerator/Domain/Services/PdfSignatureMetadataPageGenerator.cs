using iText.Kernel.Pdf;
using iText.Layout;
using iText.Layout.Element;
using iText.Layout.Properties;

namespace PQC.MODULES.Documents.Infraestructure.PageGenerator.Domain.Services
{
    public class PdfSignatureMetadataPageGenerator:ISignatureMetadata
    {
        public async Task<byte[]> GenerateMetadataPageAsync(SignatureMetadata metadata)
        {
            using var ms = new MemoryStream();
            using var writer = new PdfWriter(ms);
            using var pdf = new PdfDocument(writer);
            using var document = new Document(pdf);

            document.Add(new Paragraph("CERTIFICADO DE ASSINATURA DIGITAL")
            .SetTextAlignment(TextAlignment.CENTER)
            .SetFontSize(18)
            .SimulateBold());

            document.Add(new Paragraph("\n"));

            document.Add(new Paragraph($"Documento: {metadata.DocumentName}")
                .SetFontSize(12));
            document.Add(new Paragraph($"Assinado em: {metadata.SignedAt:dd/MM/yyyy HH:mm:ss} UTC")
                .SetFontSize(12));
            document.Add(new Paragraph("\n"));

            // Informações do signatário
            document.Add(new Paragraph("DADOS DO SIGNATÁRIO")
                .SetFontSize(14)
                .SimulateBold());

            document.Add(new Paragraph($"Nome: {metadata.SignerName}")
                .SetFontSize(11));

            document.Add(new Paragraph($"CPF: {metadata.SignerCpf}")
                .SetFontSize(11));

            document.Add(new Paragraph($"Email: {metadata.SignerEmail}")
                .SetFontSize(11));

            document.Add(new Paragraph("\n"));

            // Informações técnicas
            document.Add(new Paragraph("DADOS TÉCNICOS DA ASSINATURA")
                .SetFontSize(14)
                .SimulateBold());

            document.Add(new Paragraph($"Algoritmo: {metadata.Algorithm}")
                .SetFontSize(11));

            document.Add(new Paragraph($"Hash da Assinatura: {metadata.SignatureHash}")
                .SetFontSize(9)
                .SetFontFamily("Courier"));

            document.Close();

            return ms.ToArray();

        }
    }
}
