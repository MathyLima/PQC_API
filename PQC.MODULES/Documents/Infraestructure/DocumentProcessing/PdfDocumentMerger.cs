using PdfSharp.Pdf;
using PdfSharp.Pdf.IO;
using PdfSharp.Pdf.Advanced;
using System.Text;
using PQC.MODULES.Documents.Application.Interfaces.PDFcomposer;

namespace PQC.MODULES.Documents.Infraestructure.DocumentProcessing
{
    public class PdfDocumentMerger : IDocumentMerger
    {
        private readonly IXmpMetaDataService _xmpService;

        public PdfDocumentMerger(IXmpMetaDataService xmpService)
        {
            _xmpService = xmpService;
        }

        public async Task<byte[]> MergeAsync(
            byte[] originalPdf,
            byte[] metadataPdf,
            SignatureMetadata signatureMetadata)
        {
            if (originalPdf == null || originalPdf.Length == 0)
                throw new ArgumentException("Original PDF is empty");
            if (metadataPdf == null || metadataPdf.Length == 0)
                throw new ArgumentException("Metadata PDF is empty");

            using var outputMs = new MemoryStream();
            var outputDoc = new PdfDocument();

            // Documento original
            using var originalMs = new MemoryStream(originalPdf);
            var originalDoc = PdfReader.Open(originalMs, PdfDocumentOpenMode.Import);

            // EXTRAIR XMP EXISTENTE AQUI MESMO
            string existingXmp = GetXmpMetadata(originalDoc);

            // Documento de metadata
            using var metadataMs = new MemoryStream(metadataPdf);
            var metadataDoc = PdfReader.Open(metadataMs, PdfDocumentOpenMode.Import);

            // Copiar todas as páginas
            foreach (PdfPage page in originalDoc.Pages)
                outputDoc.AddPage(page);

            foreach (PdfPage page in metadataDoc.Pages)
                outputDoc.AddPage(page);

            // GERAR XMP COMPLETO (novo ou append)
            string finalXmp = _xmpService.GenerateXmpMetadata(signatureMetadata, existingXmp);

            // SETAR XMP NO PDF FINAL
            SetXmpMetadata(outputDoc, finalXmp);

            outputDoc.Save(outputMs);
            return outputMs.ToArray();
        }

        private string GetXmpMetadata(PdfDocument doc)
        {
            try
            {
                var catalog = doc.Internals.Catalog;
                if (catalog.Elements.ContainsKey("/Metadata"))
                {
                    var metadataRef = catalog.Elements["/Metadata"] as PdfReference;
                    if (metadataRef != null)
                    {
                        var metadataObj = metadataRef.Value as PdfDictionary;
                        if (metadataObj?.Stream != null)
                        {
                            return Encoding.UTF8.GetString(metadataObj.Stream.Value);
                        }
                    }
                }
            }
            catch { }

            return string.Empty;
        }

        private void SetXmpMetadata(PdfDocument doc, string xmpXml)
        {
            var xmpStream = new PdfDictionary(doc);
            xmpStream.Elements["/Type"] = new PdfName("/Metadata");
            xmpStream.Elements["/Subtype"] = new PdfName("/XML");

            byte[] xmpBytes = Encoding.UTF8.GetBytes(xmpXml);
            xmpStream.CreateStream(xmpBytes);

            doc.Internals.Catalog.Elements["/Metadata"] = xmpStream;
        }
    }
}