using PQC.MODULES.Documents.Application.Interfaces.PDFcomposer;

namespace PQC.MODULES.Documents.Infraestructure.DocumentProcessing;

public class PdfDocumentComposer : IDocumentComposer
{
    private readonly ISignatureMetadataPageGenerator _generator;
    private readonly IDocumentMerger _merger;

    public PdfDocumentComposer(
        ISignatureMetadataPageGenerator generator,
        IDocumentMerger merger)
    {
        _generator = generator;
        _merger = merger;
    }

    public async Task<byte[]> ComposeForSignatureAsync(
        byte[] originalPdf,
        SignatureMetadata metadata)
    {
        var metaPage = await _generator.GenerateMetaDataPageAsync(metadata);


        var finalPdf = await _merger.MergeAsync(originalPdf, metaPage, metadata);

        return finalPdf;
    }
}
