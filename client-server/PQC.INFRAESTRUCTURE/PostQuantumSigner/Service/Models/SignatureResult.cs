using System;
using System.Collections.Generic;
using System.Text;

namespace PQC.INFRAESTRUCTURE.PostQuantumSigner.Service.Models
{
    /// <summary>
    /// Resultado de uma operação de assinatura.
    /// </summary>
    public class SignatureResult
    {
        public bool Success { get; init; }
        public byte[]? Signature { get; init; }
        public string? Error { get; init; }
    }
}
