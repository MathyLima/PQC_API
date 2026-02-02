using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Filters;
using PQC.SHARED.Exceptions.Base;
using PQC.SHARED.Communication.DTOs.Responses;

namespace PQC.API.Filters
{
    public class ExceptionFilter : IExceptionFilter
    {
        private readonly IWebHostEnvironment _environment;

        public ExceptionFilter(IWebHostEnvironment environment)
        {
            _environment = environment;
        }

        public void OnException(ExceptionContext context)
        {
            if (context.Exception is BaseException baseException)
            {
                HandleBaseException(context, baseException);
            }
            else
            {
                ThrowUnknownError(context);
            }
        }

        private void HandleBaseException(ExceptionContext context, BaseException exception)
        {
            context.HttpContext.Response.StatusCode = exception.StatusCode;

            context.Result = new ObjectResult(new ResponseErrorMessagesJson
            {
                ErrorCode = exception.Code,
                Message = exception.Message,
                Errors = exception.Errors?.ToList() ?? new List<string>()
            });
        }

        private void ThrowUnknownError(ExceptionContext context)
        {
            var exception = context.Exception;
            context.HttpContext.Response.StatusCode = StatusCodes.Status500InternalServerError;

            // Em desenvolvimento, mostra detalhes completos
            if (_environment.IsDevelopment())
            {
                context.Result = new ObjectResult(new
                {
                    errorCode = "INTERNAL_SERVER_ERROR",
                    message = exception.Message,
                    type = exception.GetType().FullName,
                    stackTrace = exception.StackTrace,
                    innerException = exception.InnerException?.Message
                });
            }
            // Em produção, mostra apenas mensagem genérica
            else
            {
                context.Result = new ObjectResult(new ResponseErrorMessagesJson
                {
                    ErrorCode = "INTERNAL_SERVER_ERROR",
                    Message = "Ocorreu um erro inesperado. Por favor, tente novamente mais tarde."
                });
            }
        }
    }
}