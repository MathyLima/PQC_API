namespace PQC.SHARED.Communication.DTOs.Documents.Responses
{
    public class ValidationResult
    {
        public bool IsValid { get; set; }
        public string Message { get; set; }
        public List<SignatureValidationResult> SignatureResults { get; set; }
        public int TotalSignatures { get; set; }
    }

}
