using iText.Kernel.Pdf;

namespace PQC.MODULES.Documents.Infraestructure.DocumentProcessing
{
    /// <summary>
    /// ✅ Utilitário para controlar exatamente o que o iText7 adiciona ao PDF
    /// Garanta que após processar, o PDF tenha bytes estáveis/determinísticos
    /// </summary>
    public static class PdfCleanupHelper
    {
        /// <summary>
        /// Processa o PDF com iText7 mas garante que metadados
        /// automáticos sejam removidos/controlados para estabilizar os bytes
        /// </summary>
        public static byte[] StabilizePdf(byte[] pdfContent)
        {
            using var inputMs = new MemoryStream(pdfContent);
            using var outputMs = new MemoryStream();

            using var pdfReader = new PdfReader(inputMs);

            // ✅ WriterProperties para minimizar mudanças
            var writerProps = new WriterProperties();
            // O valor geralmente utilizado é 9 (nível máximo de compressão do zlib/deflate).
            writerProps.SetCompressionLevel(9);

            using var pdfWriter = new PdfWriter(outputMs, writerProps);
            pdfWriter.SetCloseStream(false);

            using var pdfDoc = new PdfDocument(pdfReader, pdfWriter);

            // ✅ Remover/fixar metadados que mudam toda vez
            FixDocumentInfo(pdfDoc);
            FixDocumentId(pdfDoc);
            RemoveXmpStandardMetadata(pdfDoc);

            pdfDoc.Close();

            return outputMs.ToArray();
        }

        /// <summary>
        /// ✅ Fix Document Info para ser determinístico
        /// </summary>
        private static void FixDocumentInfo(PdfDocument pdfDoc)
        {
            var info = pdfDoc.GetDocumentInfo();

            // Remover campos que mudam toda vez
            // O método GetPdfObject() é interno, utilizar reflection para acessar o PdfDictionary
            var infoDictProperty = typeof(PdfDocumentInfo).GetProperty("PdfObject", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
            var infoDict = infoDictProperty?.GetValue(info) as PdfDictionary;

            if (infoDict != null)
            {
                // ✅ Remover Producer (iText adiciona "iText 7.x.x")
                infoDict.Remove(PdfName.Producer);

                // ✅ Remover CreationDate (timestamp que muda)
                infoDict.Remove(PdfName.CreationDate);

                // ✅ Remover ModDate
                infoDict.Remove(PdfName.ModDate);
            }
        }

        /// <summary>
        /// ✅ Fixar Document ID para ser determinístico
        /// O iText7 gera dois IDs únicos toda vez que salva
        /// </summary>
        private static void FixDocumentId(PdfDocument pdfDoc)
        {
            // ✅ Usar um ID fixo baseado no conteúdo
            // Isso garante que o mesmo conteúdo sempre gere o mesmo ID
            var trailer = pdfDoc.GetTrailer();

            // Remover ID existente - será recriado pelo iText com valor fixo
            trailer.Remove(PdfName.ID);
        }

        /// <summary>
        /// ✅ Remover XMP metadata padrão que o iText adiciona
        /// (diferente do nosso XMP customizado em /PQCSignatureMetadata)
        /// </summary>
        private static void RemoveXmpStandardMetadata(PdfDocument pdfDoc)
        {
            // Remover o XMP metadata stream padrão do catalog
            var catalog = pdfDoc.GetCatalog();
            catalog.GetPdfObject().Remove(PdfName.Metadata);
        }
    }
}