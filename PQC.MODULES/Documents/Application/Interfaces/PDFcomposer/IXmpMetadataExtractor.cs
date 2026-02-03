using PQC.SHARED.Communication.DTOs.Documents.Responses;

namespace PQC.MODULES.Documents.Application.Interfaces.PDFcomposer
{
    public interface IXmpMetadataExtractor
    {
        Task<SignatureValidationResult> ExtractSignaturesAsync(byte[] pdfBytes);
        Task<byte[]> RemoveSignatureMetadataAsync(byte[] pdfContent, int signatureOrder);
        Task<byte[]> RemoveAllXmpAsync(byte[] pdfContent);
    }
}