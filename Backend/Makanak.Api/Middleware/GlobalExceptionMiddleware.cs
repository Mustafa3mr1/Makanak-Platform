using FluentValidation;
using Makanak.Application.Common.Exceptions;

namespace Makanak.API.Middleware;

/// <summary>
/// Converts exceptions into consistent RFC 7807 ProblemDetails responses.
/// Unexpected errors are logged and returned as a generic 500 without SQL text, stack traces or secrets.
/// </summary>
public sealed class GlobalExceptionMiddleware
{
    private readonly RequestDelegate _next;
    private readonly ILogger<GlobalExceptionMiddleware> _logger;

    public GlobalExceptionMiddleware(RequestDelegate next, ILogger<GlobalExceptionMiddleware> logger)
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
        catch (Exception ex) when (!context.Response.HasStarted)
        {
            await HandleAsync(context, ex);
        }
    }

    private async Task HandleAsync(HttpContext context, Exception ex)
    {
        switch (ex)
        {
            case ValidationException v:
                var errors = v.Errors
                    .GroupBy(e => string.IsNullOrWhiteSpace(e.PropertyName) ? "request" : e.PropertyName)
                    .ToDictionary(g => g.Key, g => g.Select(e => e.ErrorMessage).Distinct().ToArray());
                await ProblemDetailsWriter.WriteAsync(context, StatusCodes.Status400BadRequest, "Validation failed", "One or more validation errors occurred.", errors);
                break;

            case NotFoundException:
                await ProblemDetailsWriter.WriteAsync(context, StatusCodes.Status404NotFound, "Not found", ex.Message);
                break;

            case ForbiddenException:
                await ProblemDetailsWriter.WriteAsync(context, StatusCodes.Status403Forbidden, "Forbidden", ex.Message);
                break;

            case UnauthorizedException:
                await ProblemDetailsWriter.WriteAsync(context, StatusCodes.Status401Unauthorized, "Unauthorized", ex.Message);
                break;

            case BusinessRuleException or ConflictException:
                await ProblemDetailsWriter.WriteAsync(context, StatusCodes.Status409Conflict, "Conflict", ex.Message);
                break;

            case PayloadTooLargeException:
                await ProblemDetailsWriter.WriteAsync(context, StatusCodes.Status413PayloadTooLarge, "Payload too large", ex.Message);
                break;

            case BadHttpRequestException bad:
                // Raised by Kestrel/MVC, e.g. request body exceeds [RequestSizeLimit] (413) or malformed request (400).
                await ProblemDetailsWriter.WriteAsync(context, bad.StatusCode,
                    bad.StatusCode == StatusCodes.Status413PayloadTooLarge ? "Payload too large" : "Bad request",
                    bad.StatusCode == StatusCodes.Status413PayloadTooLarge ? "The request body is too large." : "The request could not be processed.");
                break;

            case OperationCanceledException when context.RequestAborted.IsCancellationRequested:
                context.Response.StatusCode = 499; // client closed request; nothing to send
                break;

            default:
                _logger.LogError(ex, "Unhandled exception for {Method} {Path} (trace {TraceId})",
                    context.Request.Method, context.Request.Path, context.TraceIdentifier);
                await ProblemDetailsWriter.WriteAsync(context, StatusCodes.Status500InternalServerError,
                    "Internal server error", "An unexpected error occurred. Please try again later.");
                break;
        }
    }
}
