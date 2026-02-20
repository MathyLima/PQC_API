using System;
using System.Collections.Generic;
using System.Text;

namespace PQC.INFRAESTRUCTURE.PostQuantumSigner.Service.Models
{
    /// <summary>
    /// Request para verificação de assinatura.
    /// </summary>
    public class VerificationRequest
    {
        public required byte[] Data { get; init; }
        public required byte[] Signature { get; init; }
        public required string PublicKeyPath { get; init; }
    }
}
