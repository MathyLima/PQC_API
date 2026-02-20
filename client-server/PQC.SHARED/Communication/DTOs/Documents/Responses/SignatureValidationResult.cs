using System;
using System.Collections.Generic;
using System.Text;

namespace PQC.SHARED.Communication.DTOs.Documents.Responses
{
    public class SignatureValidationResult
    {
        public bool HasSignatures { get; set; }
        public List<ExtractedSignature> Signatures { get; set; } = new();
        public string RawXmp { get; set; }
    }
}
