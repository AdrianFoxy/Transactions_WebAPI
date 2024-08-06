using System.Net;
using System.Text.Json;
using CsvHelper;

namespace Transactions_WebAPI.Middleware
{
    public class ErrorHandlerMiddleware
    {
        private readonly RequestDelegate _next;

        public ErrorHandlerMiddleware(RequestDelegate next)
        {
            _next = next;
        }

        public async Task Invoke(HttpContext context)
        {
            try
            {
                await _next(context);
            }
            catch (TimeZoneNotFoundException ex)
            {
                await HandleExceptionAsync(context, ex, HttpStatusCode.BadRequest, "Invalid time zone");
            }
            catch (CsvHelperException ex)
            {
                await HandleExceptionAsync(context, ex, HttpStatusCode.BadRequest, "CSV processing error");
            }
            catch (Exception ex)
            {
                await HandleExceptionAsync(context, ex, HttpStatusCode.InternalServerError, "Internal server error");
            }
        }

        private static Task HandleExceptionAsync(HttpContext context, Exception exception, HttpStatusCode statusCode, string message)
        {
            var result = JsonSerializer.Serialize(new
            {
                title = message,
                status = (int)statusCode,
                description = exception.Message
            });
            context.Response.ContentType = "application/json";
            context.Response.StatusCode = (int)statusCode;
            return context.Response.WriteAsync(result);
        }
    }
}