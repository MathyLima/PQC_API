using PQC.SHARED.Exceptions.Base;
using System;
using System.Collections.Generic;
using System.Text;

namespace PQC.SHARED.Exceptions.Domain
{
    /// <summary>
    /// Exceção para erros de validação.
    /// </summary>
    public class ValidationException : BaseException
    {
        public IDictionary<string, string[]> Errors { get; }

        public ValidationException(IDictionary<string, string[]> errors)
            : base("One or more validation errors occurred", "VALIDATION_ERROR", 400)
        {
            Errors = errors;
        }

        public ValidationException(string propertyName, string errorMessage)
            : base($"Validation failed for '{propertyName}': {errorMessage}", "VALIDATION_ERROR", 400)
        {
            Errors = new Dictionary<string, string[]>
        {
            { propertyName, new[] { errorMessage } }
        };
        }
    }
}
