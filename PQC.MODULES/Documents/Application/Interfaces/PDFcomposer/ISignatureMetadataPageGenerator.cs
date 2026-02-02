namespace PQC.MODULES.Documents.Application.Interfaces.PDFcomposer
{
    public interface ISignatureMetadataPageGenerator
    {
        Task<byte[]> GenerateMetaDataPageAsync(SignatureMetadata data);
    }
}
