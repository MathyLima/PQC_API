using System.Net;

namespace PQC.EXCEPTIONS.ExceptionsBase
{
    public abstract class PQCExceptions:SystemException
    {
        public PQCExceptions(string ErrorMessage) : base(ErrorMessage) { }
        public abstract List<string> GetErrors();
        public abstract HttpStatusCode GetHttpStatusCode();
    }
}
