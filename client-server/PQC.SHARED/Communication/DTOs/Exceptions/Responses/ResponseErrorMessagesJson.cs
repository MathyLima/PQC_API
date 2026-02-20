namespace PQC.SHARED.Communication.DTOs.Responses
{
    public class ResponseErrorMessagesJson
    {
        public string ErrorCode { get; set; } = string.Empty;
        public string Message { get; set; } = string.Empty;
        public List<string> Errors { get; set; } = new();

        public ResponseErrorMessagesJson() { }

        public ResponseErrorMessagesJson(string message)
        {
            Message = message;
            ErrorCode = "ERROR";
        }

        public ResponseErrorMessagesJson(string errorCode, string message)
        {
            ErrorCode = errorCode;
            Message = message;
        }

        public ResponseErrorMessagesJson(string errorCode, string message, List<string> errors)
        {
            ErrorCode = errorCode;
            Message = message;
            Errors = errors;
        }
    }
}