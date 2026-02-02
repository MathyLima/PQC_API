namespace PQC.MODULES.Documents.Application.Interfaces.PDFcomposer
{
    public interface IDocumentComposer
    {
        Task<byte[]> ComposeForSignatureAsync(
            byte[] originalPdf,
            SignatureMetadata metadata
        );
    }

}
