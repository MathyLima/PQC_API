
using System.Net;


namespace PQC.EXCEPTIONS.ExceptionsBase
{
    public class UnauthorizedException : PQCExceptions
    {
        public UnauthorizedException(string message) : base(message)
        {
        }

        public override List<string> GetErrors()
        {
            return new List<string> { Message };
        }

        public override HttpStatusCode GetHttpStatusCode() => HttpStatusCode.NotFound;

    }
}
