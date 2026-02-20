using System;
using System.Collections.Generic;
using System.Text;

namespace PQC.MODULES.Documents.Application.DTOs
{
    public class SignDocumentRequest
    {
        public byte[] DataToSign { get; set; }
        public string PreparedFilePath { get; set; } 
        public string FileName { get; set; }
    }
}
