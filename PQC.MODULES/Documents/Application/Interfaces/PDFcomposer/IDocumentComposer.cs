namespace PQC.MODULES.Documents.Application.Interfaces.PDFcomposer
{
    public interface IDocumentComposer
    {
       
        Task<byte[]> AddMetadataPageAsync(
            byte[] currentPdf,
            SignatureMetadata metadata);
        Task<byte[]> AddXmpSignatureAsync(
             byte[] pdfWithMetadataPage,
             byte[] signature,
             SignatureMetadata metadata);

        /// <summary>
        /// ✅ Normaliza o PDF passando pelo iText7 uma vez
        /// Isso garante que o PDF que será assinado já foi "tocado" pelo iText7
        /// e na validação, quando o iText7 toca novamente, os bytes não mudam
        /// </summary>
        Task<byte[]> NormalizePdfAsync(byte[] pdfContent);
       
    }

}
