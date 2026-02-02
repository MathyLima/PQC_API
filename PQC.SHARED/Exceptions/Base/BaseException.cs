namespace PQC.SHARED.Exceptions.Base
{
    /// <summary>
    /// Exceção base para todas as exceções da aplicação.
    /// </summary>
    public abstract class BaseException : Exception
    {
        public string Code { get; }
        public int StatusCode { get; }
        public IEnumerable<string>? Errors { get; protected set; }

        protected BaseException(string message, string code, int statusCode = 500)
            : base(message)
        {
            Code = code;
            StatusCode = statusCode;
        }

        protected BaseException(string message, string code, Exception innerException, int statusCode = 500)
            : base(message, innerException)
        {
            Code = code;
            StatusCode = statusCode;
        }

        // Novo construtor para múltiplos erros
        protected BaseException(string message, string code, IEnumerable<string> errors, int statusCode = 500)
            : base(message)
        {
            Code = code;
            StatusCode = statusCode;
            Errors = errors;
        }
    }
}