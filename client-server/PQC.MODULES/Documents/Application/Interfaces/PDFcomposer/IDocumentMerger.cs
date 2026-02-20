using System;
using System.Collections.Generic;
using System.Text;

namespace PQC.MODULES.Documents.Application.Interfaces.PDFcomposer
{
    public interface IDocumentMerger
    {
        Task<byte[]> MergeAsync(byte[] originalPdf, byte[] metadataPdf, SignatureMetadata xmpMetaData);
    }

}
