using PQC.SHARED.Time;
using System;
using System.Collections.Generic;
using System.Text;

namespace PQC.INFRAESTRUCTURE.PostQuantumSigner.Service.Models
{

    /// <summary>
    /// Request para assinatura.
    /// </summary>
    public class SignatureRequest
    {
        public required byte[] Data { get; init; }
        public required string NomeUsuario { get; init; }
        public required string CpfUsuario { get; init; }
        public required string SignatureAlgorithm { get; init; }
        public required byte[] DigitalSignature { get; init; }
        public string TimeStampAssinatura = RecifeTimeProvider.Now().ToString();
    }
}
