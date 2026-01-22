using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Filters;
using PQC.COMMUNICATION.Responses;
using PQC.EXCEPTIONS.ExceptionsBase;

namespace PQC.API.Filters
{
    public class ExceptionFilter : IExceptionFilter
    {
        public void OnException(ExceptionContext context)
        {
            if (context.Exception is PQCExceptions exception)
            {
                context.HttpContext.Response.StatusCode = (int)exception.GetHttpStatusCode();
                context.Result = new ObjectResult(new ResponseErrorMessagesJson(exception.GetErrors()));
            }
            else
            {
                ThrowUnknownError(context);
            }
        }
/*
        private void ThrowUnknownError(ExceptionContext context)
        {
            context.HttpContext.Response.StatusCode = StatusCodes.Status500InternalServerError;
            context.Result = new ObjectResult(new ResponseErrorMessagesJson("Erro Desconhecido"));
        }
*/
        //PARA DEBUG
        private void ThrowUnknownError(ExceptionContext context)
        {
            var exception = context.Exception;

            context.HttpContext.Response.StatusCode = StatusCodes.Status500InternalServerError;
            context.Result = new ObjectResult(new
            {
                message = exception.Message,
                type = exception.GetType().FullName,
                stackTrace = exception.StackTrace
            });
        }
    }
}
