using System.Net;

namespace PQC.EXCEPTIONS.ExceptionsBase
{
    public class ErrorOnValidationException(List<string> errorMessages): PQCExceptions(string.Empty)
    {
        private readonly List<string> _errors = errorMessages;
        public override List<string> GetErrors() => _errors;
        public override HttpStatusCode GetHttpStatusCode() => HttpStatusCode.BadRequest;
    }
}
