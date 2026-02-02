using PQC.SHARED.Exceptions.Base;

namespace PQC.SHARED.Exceptions.Domain
{

    /// <summary>
    /// Exceção para operações não autorizadas.
    /// </summary>
    public class UnauthorizedException : BaseException
    {
        public UnauthorizedException(string message = "Unauthorized access")
            : base(message, "UNAUTHORIZED", 401)
        {
        }
    }
}
