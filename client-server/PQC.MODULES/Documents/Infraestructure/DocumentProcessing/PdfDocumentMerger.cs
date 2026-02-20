using iText.Kernel.Pdf;
using PQC.MODULES.Documents.Application.Interfaces.PDFcomposer;
using System.Text;

namespace PQC.MODULES.Documents.Infraestructure.DocumentProcessing
{
    /// <summary>
    /// Responsável por unir páginas e persistir XMP
    /// </summary>
    public class PdfDocumentMerger : IDocumentMerger
    {
        private readonly IXmpMetaDataService _xmpService;

        public PdfDocumentMerger(IXmpMetaDataService xmpService)
        {
            _xmpService = xmpService;
        }

        public Task<byte[]> MergeAsync(
            byte[] originalPdf,
            byte[] metadataPdf,
            SignatureMetadata signatureMetadata)
        {
            Console.WriteLine("\n🔧 CRIANDO PDF COM XMP CUSTOMIZADO");

            // 1️⃣ Extrair XMP do original (se existir)
            string existingXmp = ExtractXmpFromSource(originalPdf);

            using var outputMs = new MemoryStream();
            using var pdfWriter = new PdfWriter(outputMs);
            using var pdfDoc = new PdfDocument(pdfWriter);

            // 2️⃣ Copiar páginas originais
            CopyPages(originalPdf, pdfDoc);

            // 3️⃣ Copiar páginas de metadata
            CopyPages(metadataPdf, pdfDoc);

            // 4️⃣ Gerar XMP atualizado
            string customXmp = _xmpService.GenerateXmpMetadata(
                signatureMetadata,
                Convert.ToBase64String(Encoding.UTF8.GetBytes(signatureMetadata.SignatureValue)), // ← adiciona a signature
                existingXmp
            );
            // 5️⃣ Injetar XMP final
            CustomXmpHandler.InjectCustomXmp(pdfDoc, customXmp);

            // 6️⃣ Metadata básico do PDF
            ConfigurePdfInfo(pdfDoc, signatureMetadata);

            pdfDoc.Close();

            return Task.FromResult(outputMs.ToArray());
        }

        // =========================
        // Helpers
        // =========================

        private static void CopyPages(byte[] sourcePdf, PdfDocument targetDoc)
        {
            using var ms = new MemoryStream(sourcePdf);
            using var reader = new PdfReader(ms);
            using var sourceDoc = new PdfDocument(reader);

            sourceDoc.CopyPagesTo(
                1,
                sourceDoc.GetNumberOfPages(),
                targetDoc
            );
        }

        private static string ExtractXmpFromSource(byte[] pdfBytes)
        {
            try
            {
                using var ms = new MemoryStream(pdfBytes);
                using var reader = new PdfReader(ms);
                using var doc = new PdfDocument(reader);

                byte[] xmp = doc.GetXmpMetadata();

                if (xmp == null || xmp.Length == 0)
                    return string.Empty;

                return Encoding.UTF8.GetString(xmp);
            }
            catch (Exception ex)
            {
                Console.WriteLine($"⚠️ Falha ao extrair XMP: {ex.Message}");
                return string.Empty;
            }
        }

        private static void ConfigurePdfInfo(
            PdfDocument doc,
            SignatureMetadata metadata)
        {
            var info = doc.GetDocumentInfo();

            info.SetCreator("PQC Signature Engine");
            info.SetProducer("PQC Documents");

            info.SetMoreInfo("DocumentId", metadata.DocumentId);
            info.SetMoreInfo("SignedBy", metadata.SignerName);
            info.SetMoreInfo("SignedAt", metadata.SignedAt.ToString("O"));

            doc.GetCatalog().SetLang(new PdfString("pt-BR"));
        }
    }
}
