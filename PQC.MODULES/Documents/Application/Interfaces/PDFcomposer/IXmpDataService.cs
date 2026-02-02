namespace PQC.MODULES.Documents.Application.Interfaces.PDFcomposer
{
    public interface IXmpMetaDataService
    {
        string GenerateXmpMetadata(SignatureMetadata metadata, string existingXmp = null);

    }
}
