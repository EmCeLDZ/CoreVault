using System.Net;
using System.Text.Json;
using CoreKV.Infrastructure.Logging;

namespace CoreKV.Middleware;

public class GlobalExceptionHandlingMiddleware
{
    private readonly RequestDelegate _next;
    private readonly ILogger<GlobalExceptionHandlingMiddleware> _logger;
    private readonly IAuditLogger _auditLogger;

    public GlobalExceptionHandlingMiddleware(
        RequestDelegate next, 
        ILogger<GlobalExceptionHandlingMiddleware> logger,
        IAuditLogger auditLogger)
    {
        _next = next;
        _logger = logger;
        _auditLogger = auditLogger;
    }

    public async Task InvokeAsync(HttpContext context)
    {
        try
        {
            await _next(context);
        }
        catch (Exception ex)
        {
            await HandleExceptionAsync(context, ex);
        }
    }

    private async Task HandleExceptionAsync(HttpContext context, Exception exception)
    {
        var correlationId = context.TraceIdentifier;
        var userId = context.User?.Identity?.Name ?? "Anonymous";
        var path = context.Request.Path;
        var method = context.Request.Method;

        _logger.LogError(exception, 
            "Unhandled exception occurred. CorrelationId: {CorrelationId}, Path: {Path}, Method: {Method}, User: {User}",
            correlationId, path, method, userId);

        await _auditLogger.LogErrorAsync(exception, correlationId, new Dictionary<string, object>
        {
            ["Path"] = path,
            ["Method"] = method,
            ["UserId"] = userId,
            ["UserAgent"] = context.Request.Headers["User-Agent"].ToString(),
            ["RemoteIP"] = context.Connection.RemoteIpAddress?.ToString() ?? "Unknown"
        });

        context.Response.Clear();
        context.Response.StatusCode = (int)GetStatusCode(exception);
        context.Response.ContentType = "application/problem+json";

        var problemDetails = CreateProblemDetails(exception, correlationId, context);
        
        var jsonOptions = new JsonSerializerOptions
        {
            PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
            WriteIndented = true
        };

        var jsonResponse = JsonSerializer.Serialize(problemDetails, jsonOptions);
        await context.Response.WriteAsync(jsonResponse);
    }

    private static HttpStatusCode GetStatusCode(Exception exception)
    {
        return exception switch
        {
            ArgumentException => HttpStatusCode.BadRequest,
            UnauthorizedAccessException => HttpStatusCode.Unauthorized,
            KeyNotFoundException => HttpStatusCode.NotFound,
            InvalidOperationException => HttpStatusCode.BadRequest,
            TimeoutException => HttpStatusCode.RequestTimeout,
            IOException => HttpStatusCode.InternalServerError,
            _ => HttpStatusCode.InternalServerError
        };
    }

    private static Microsoft.AspNetCore.Mvc.ProblemDetails CreateProblemDetails(
        Exception exception, 
        string correlationId, 
        HttpContext context)
    {
        var statusCode = (int)GetStatusCode(exception);
        
        return new Microsoft.AspNetCore.Mvc.ProblemDetails
        {
            Status = statusCode,
            Title = GetTitle(statusCode),
            Detail = exception.Message,
            Instance = context.Request.Path,
            Extensions = 
            {
                ["correlationId"] = correlationId,
                ["timestamp"] = DateTime.UtcNow,
                ["exceptionType"] = exception.GetType().Name
            }
        };
    }

    private static string GetTitle(int statusCode)
    {
        return statusCode switch
        {
            400 => "Bad Request",
            401 => "Unauthorized",
            404 => "Not Found",
            408 => "Request Timeout",
            500 => "Internal Server Error",
            _ => "An error occurred"
        };
    }
}
