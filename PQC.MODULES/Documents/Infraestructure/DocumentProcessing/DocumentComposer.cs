using iText.Kernel.Pdf;
using PQC.MODULES.Documents.Application.Interfaces.PDFcomposer;

namespace PQC.MODULES.Documents.Infraestructure.DocumentProcessing
{
    public class PdfDocumentComposer : IDocumentComposer
    {
        private readonly ISignatureMetadataPageGenerator _generator;
        private readonly IDocumentMerger _merger;
        private readonly IXmpMetaDataService _xmpService;

        public PdfDocumentComposer(
            ISignatureMetadataPageGenerator generator,
            IDocumentMerger merger,
            IXmpMetaDataService xmpService)
        {
            _generator = generator;
            _merger = merger;
            _xmpService = xmpService;
        }

        public async Task<byte[]> AddMetadataPageAsync(
            byte[] currentPdf,
            SignatureMetadata metadata)
        {
            var metadataPagePdf = await _generator.GenerateMetaDataPageAsync(metadata);

            using var outputMs = new MemoryStream();
            using var pdfWriter = new PdfWriter(outputMs);
            using var pdfDoc = new PdfDocument(pdfWriter);

            // ✅ Extrai XMP customizado ANTES de copiar páginas
            string existingXmp = string.Empty;
            using (var currentMs = new MemoryStream(currentPdf))
            using (var currentReader = new PdfReader(currentMs))
            using (var currentDoc = new PdfDocument(currentReader))
            {
                // Preserva o XMP customizado do PDF original
                existingXmp = CustomXmpHandler.ExtractCustomXmp(currentDoc);
                if (!string.IsNullOrEmpty(existingXmp))
                {
                    Console.WriteLine($"📝 AddMetadataPage: XMP customizado extraído do original ({existingXmp.Length} chars)");
                }
                else
                {
                    Console.WriteLine("📝 AddMetadataPage: Sem XMP customizado no original");
                }

                // Copia páginas do original
                currentDoc.CopyPagesTo(1, currentDoc.GetNumberOfPages(), pdfDoc);
            }

            // Copia página de metadata
            using (var metadataMs = new MemoryStream(metadataPagePdf))
            using (var metadataReader = new PdfReader(metadataMs))
            using (var metadataDoc = new PdfDocument(metadataReader))
            {
                metadataDoc.CopyPagesTo(1, metadataDoc.GetNumberOfPages(), pdfDoc);
            }

            // ✅ Re-injeta o XMP customizado no novo PDF (se existia)
            if (!string.IsNullOrEmpty(existingXmp))
            {
                CustomXmpHandler.InjectCustomXmp(pdfDoc, existingXmp);
                Console.WriteLine("📝 AddMetadataPage: XMP customizado re-injetado no novo PDF");
            }

            pdfDoc.Close();

            return PdfCleanupHelper.StabilizePdf(outputMs.ToArray());
        }

        public async Task<byte[]> AddXmpSignatureAsync(
            byte[] pdfWithMetadataPage,
            byte[] signature,
            SignatureMetadata metadata)
        {
            using var inputMs = new MemoryStream(pdfWithMetadataPage);
            using var outputMs = new MemoryStream();
            using var pdfReader = new PdfReader(inputMs);
            pdfReader.SetUnethicalReading(true);
            using var pdfWriter = new PdfWriter(outputMs);
            using var pdfDoc = new PdfDocument(pdfReader, pdfWriter);

            // Extrai XMP antigo
            string existingXmp = CustomXmpHandler.ExtractCustomXmp(pdfDoc);

            if (!string.IsNullOrWhiteSpace(existingXmp))
            {
                Console.WriteLine($"📝 AddXmpSignature: XMP existente encontrado com {existingXmp.Length} chars");
                _xmpService.ValidateExistingXmp(existingXmp);
            }
            else
            {
                Console.WriteLine("📝 AddXmpSignature: Sem XMP existente, vai criar novo");
            }

            // Gera novo XMP com assinatura real
            string newXmp = _xmpService.GenerateXmpMetadata(
                metadata,
                Convert.ToBase64String(signature),
                existingXmp
            );

            CustomXmpHandler.InjectCustomXmp(pdfDoc, newXmp);

            pdfDoc.Close();

            return outputMs.ToArray();
        }

        [Obsolete("Use AddMetadataPageAsync + AddXmpSignatureAsync")]
        public async Task<byte[]> ComposeForSignatureAsync(
            byte[] originalPdf,
            SignatureMetadata metadata)
        {
            var metaPage = await _generator.GenerateMetaDataPageAsync(metadata);
            var finalPdf = await _merger.MergeAsync(originalPdf, metaPage, metadata);
            return finalPdf;
        }

        /// <summary>
        /// ✅ Normaliza usando o helper de limpeza
        /// </summary>
        public async Task<byte[]> NormalizePdfAsync(byte[] pdfContent)
        {
            return PdfCleanupHelper.StabilizePdf(pdfContent);
        }
    }
}