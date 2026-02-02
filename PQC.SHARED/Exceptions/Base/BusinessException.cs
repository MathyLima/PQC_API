namespace PQC.SHARED.Exceptions.Base
{

    /// <summary>
    /// Exceção para regras de negócio violadas.
    /// </summary>
    public class BusinessException : BaseException
    {
        public BusinessException(string message)
            : base(message, "BUSINESS_RULE_VIOLATION", 400)
        {
        }

        public BusinessException(string message, Exception innerException)
            : base(message, "BUSINESS_RULE_VIOLATION", innerException, 400)
        {
        }
    }
}
