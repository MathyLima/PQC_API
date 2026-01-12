// PQC.EXCEPTIONS/ExceptionsBase/NotFoundException.cs
using System.Net;

namespace PQC.EXCEPTIONS.ExceptionsBase
{
    public class NotFoundException : PQCExceptions
    {
        public NotFoundException(string message) : base(message)
        {
        }

        public override List<string> GetErrors()
        {
            return new List<string> { Message };
        }

        public override HttpStatusCode GetHttpStatusCode() => HttpStatusCode.NotFound;

    }
}