namespace PQC.MODULES.Documents.Application.Interfaces.PDFcomposer
{
    public interface IXmpMetaDataService
    {
        // Pré-assinatura (sem hash real)
      
        // Pós-assinatura (com hash real)
        string GenerateXmpMetadata(
            SignatureMetadata metadata,
            string signatureBase64,
            string existingXmp
        );

        void ValidateExistingXmp(string existingXmp);
    }

}
