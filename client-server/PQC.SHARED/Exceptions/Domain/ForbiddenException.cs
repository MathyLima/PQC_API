using PQC.SHARED.Exceptions.Base;

namespace PQC.SHARED.Exceptions.Domain
{
    /// <summary>
    /// Exceção para operações proibidas.
    /// </summary>
    public class ForbiddenException : BaseException
    {
        public ForbiddenException(string message = "Access forbidden")
            : base(message, "FORBIDDEN", 403)
        {
        }
    }
}
