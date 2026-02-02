using PQC.SHARED.Exceptions.Base;

namespace PQC.SHARED.Exceptions.Domain
{

    /// <summary>
    /// Exceção para conflitos (ex: registro duplicado).
    /// </summary>
    public class ConflictException : BaseException
    {
        public ConflictException(string message)
            : base(message, "CONFLICT", 409)
        {
        }
    }
}
