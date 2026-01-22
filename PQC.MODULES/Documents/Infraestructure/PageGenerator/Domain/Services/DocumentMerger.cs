using iText.Kernel.Pdf;

public interface IDocumentMerger
{
    Task<byte[]> MergeDocumentsAsync(byte[] originalDocument, byte[] metadataPage);
}

public class PdfDocumentMerger : IDocumentMerger
{
    public async Task<byte[]> MergeDocumentsAsync(byte[] originalDocument, byte[] metadataPage)
    {
        using var outputMs = new MemoryStream();
        using var writer = new PdfWriter(outputMs);
        using var mergedPdf = new PdfDocument(writer);

        // Adicionar documento original
        using (var originalMs = new MemoryStream(originalDocument))
        using (var originalPdfDoc = new PdfDocument(new PdfReader(originalMs)))
        {
            originalPdfDoc.CopyPagesTo(1, originalPdfDoc.GetNumberOfPages(), mergedPdf);
        }

        // Adicionar página de metadados
        using (var metadataMs = new MemoryStream(metadataPage))
        using (var metadataPdfDoc = new PdfDocument(new PdfReader(metadataMs)))
        {
            metadataPdfDoc.CopyPagesTo(1, metadataPdfDoc.GetNumberOfPages(), mergedPdf);
        }

        mergedPdf.Close();

        return outputMs.ToArray();
    }
}