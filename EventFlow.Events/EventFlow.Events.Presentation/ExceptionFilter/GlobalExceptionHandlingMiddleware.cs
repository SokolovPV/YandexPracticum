using System.ComponentModel.DataAnnotations;
using EventFlow.Events.Domain.Exceptions;
using Microsoft.AspNetCore.Mvc;
namespace EventFlow.Events.Presentation.ExceptionFilter;
/// <summary>
/// Глобальный обработчик для перехвата исключений, если мы их не обработали в коде
/// </summary>
public class GlobalExceptionHandlingMiddleware
{
    private readonly RequestDelegate _next;
    private readonly ILogger<GlobalExceptionHandlingMiddleware> _logger;
    public GlobalExceptionHandlingMiddleware(RequestDelegate next, ILogger<GlobalExceptionHandlingMiddleware> logger)
    {
        _next = next;
        _logger = logger;
    }

    public async Task InvokeAsync(HttpContext httpContext)
    {
        try
        {
            await _next(httpContext);
        }
        catch (Exception ex)
        {
            await HandleException(httpContext, ex);
        }
    }

    private async Task HandleException(HttpContext httpContext, Exception ex)
    {
        _logger.LogError(ex,
            "Необработанное исключение. Метод={Method}, Путь={Path}, RequestId={RequestId}",
            httpContext.Request.Method,
            httpContext.Request.Path,
            httpContext.Request.Headers["x-request-id"]);

        //если заголовки были отправлены клиенту, мы не сможем их поменять
        if (httpContext.Response.HasStarted)
            return;

        var statusCode = MapStatusCode(ex);

        httpContext.Response.StatusCode = statusCode;
        httpContext.Response.ContentType = "application/json";

        var error = new ProblemDetails
        {
            Status = statusCode,
            Detail = ex.Message,
            Title = MapTitle(ex)
        };

        await httpContext.Response.WriteAsJsonAsync(error);

    }

    private static int MapStatusCode(Exception ex)
        => ex switch
        {
            ValidationException ve => StatusCodes.Status400BadRequest,
            KeyNotExistException kne => StatusCodes.Status404NotFound,
             NoAvailableSeatsException nac => StatusCodes.Status409Conflict,
            _ => StatusCodes.Status500InternalServerError
        };

    private static string MapTitle(Exception ex)
      => ex switch
      {
          ValidationException ve => "Validation Failed",
          KeyNotExistException kne => "Invalid Identifier",
          NoAvailableSeatsException nac => "No available seats",
          _ => "Unknown Error"
      };
}