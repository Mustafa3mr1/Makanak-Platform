using System.Text.Json;
using Microsoft.AspNetCore.Mvc;

namespace Makanak.API.Middleware;

public static class ProblemDetailsWriter
{
    public static Task WriteAsync(HttpContext context, int status, string title, string? detail = null, IDictionary<string, string[]>? errors = null)
    {
        context.Response.StatusCode = status;
        context.Response.ContentType = "application/problem+json";

        ProblemDetails problem = errors is null
            ? new ProblemDetails()
            : new ValidationProblemDetails(errors);

        problem.Status = status;
        problem.Title = title;
        problem.Detail = detail;
        problem.Instance = context.Request.Path;
        problem.Extensions["traceId"] = context.TraceIdentifier;

        return context.Response.WriteAsync(JsonSerializer.Serialize(problem, problem.GetType(), new JsonSerializerOptions(JsonSerializerDefaults.Web)));
    }
}
