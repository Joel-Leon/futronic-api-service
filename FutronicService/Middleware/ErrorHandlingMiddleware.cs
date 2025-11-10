using System;
using System.Net;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Logging;
using Newtonsoft.Json;
using FutronicService.Models;

namespace FutronicService.Middleware
{
    public class ErrorHandlingMiddleware
    {
 private readonly RequestDelegate _next;
        private readonly ILogger<ErrorHandlingMiddleware> _logger;

     public ErrorHandlingMiddleware(RequestDelegate next, ILogger<ErrorHandlingMiddleware> logger)
   {
      _next = next;
            _logger = logger;
     }

        public async Task InvokeAsync(HttpContext context)
     {
       try
     {
   await _next(context);
        }
       catch (Exception ex)
       {
       _logger.LogError(ex, "Unhandled exception occurred");
 await HandleExceptionAsync(context, ex);
      }
   }

   private Task HandleExceptionAsync(HttpContext context, Exception exception)
 {
       context.Response.ContentType = "application/json";

 HttpStatusCode statusCode;
 string errorCode;

       // Determinar código de estado según tipo de excepción
   if (exception is TimeoutException)
      {
    statusCode = HttpStatusCode.RequestTimeout;
      errorCode = "CAPTURE_TIMEOUT";
            }
    else if (exception is UnauthorizedAccessException)
     {
    statusCode = HttpStatusCode.Forbidden;
    errorCode = "UNAUTHORIZED";
       }
  else if (exception is ArgumentException)
     {
       statusCode = HttpStatusCode.BadRequest;
    errorCode = "INVALID_INPUT";
          }
else
  {
    statusCode = HttpStatusCode.InternalServerError;
     errorCode = "INTERNAL_ERROR";
       }

  context.Response.StatusCode = (int)statusCode;

     var response = new
   {
success = false,
message = "Error interno del servidor",
    error = errorCode,
  data = (object)null
     };

  // No exponer detalles en producción
 #if DEBUG
 response = new
 {
     success = false,
      message = exception.Message,
   error = errorCode,
     data = (object)null
         };
#endif

       var result = JsonConvert.SerializeObject(response);
  return context.Response.WriteAsync(result);
        }
    }
}
