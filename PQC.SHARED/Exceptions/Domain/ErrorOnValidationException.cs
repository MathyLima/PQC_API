using PQC.SHARED.Exceptions.Base;

namespace PQC.SHARED.Exceptions.Domain
{
    /// <summary>
    /// Exceção lançada quando uma validação falha.
    /// </summary>
    public class ErrorOnValidationException : BaseException
    {
        /// <summary>
        /// Lista de erros de validação.
        /// </summary>
        public IReadOnlyList<string> ValidationErrors { get; }

        // Construtor simples com mensagem única
        public ErrorOnValidationException(string message)
            : base(message, "VALIDATION_ERROR", 400)
        {
            ValidationErrors = new List<string> { message };
        }

        // Construtor para campo específico
        public ErrorOnValidationException(string fieldName, string error)
            : base($"Validation failed on field '{fieldName}': {error}", "VALIDATION_ERROR", 400)
        {
            ValidationErrors = new List<string> { $"Field '{fieldName}': {error}" };
        }

        // Novo construtor: recebe uma lista de erros
        public ErrorOnValidationException(IEnumerable<string> errors)
            : base("Multiple validation errors occurred", "VALIDATION_ERROR", 400)
        {
            ValidationErrors = new List<string>(errors);
        }
    }
}
