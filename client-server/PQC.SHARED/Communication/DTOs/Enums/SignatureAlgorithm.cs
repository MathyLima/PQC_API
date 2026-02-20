    using System.Runtime.Serialization;
namespace PQC.SHARED.Communication.DTOs.Enums
{

    public enum SignatureAlgorithm
    {
        [EnumMember(Value = "ML-DSA-44")]
        ML_DSA_44,

        [EnumMember(Value = "ML-DSA-65")]
        ML_DSA_65,

        [EnumMember(Value = "ML-DSA-87")]
        ML_DSA_87,

        [EnumMember(Value = "ML-KEM-512")]
        ML_KEM_512,

        [EnumMember(Value = "ML-KEM-768")]
        ML_KEM_768,

        [EnumMember(Value = "ML-KEM-1024")]
        ML_KEM_1024
    }

}
