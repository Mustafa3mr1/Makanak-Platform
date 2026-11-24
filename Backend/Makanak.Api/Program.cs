using System.Text.Json;
using System.Text.Json.Serialization;
using FluentValidation;
using Microsoft.AspNetCore.Diagnostics;
using Microsoft.EntityFrameworkCore;
using Microsoft.OpenApi;
using Makanak.Application.Validators;
using Makanak.Domain.Exceptions;
using Makanak.Infrastructure;

var builder = WebApplication.CreateBuilder(args);

// Add Controllers with camelCase & String Enum serialization
builder.Services.AddControllers()
    .AddJsonOptions(options =>
    {
        options.JsonSerializerOptions.PropertyNamingPolicy = JsonNamingPolicy.CamelCase;
        options.JsonSerializerOptions.Converters.Add(new JsonStringEnumConverter());
    });

// Register FluentValidation from Application layer
builder.Services.AddValidatorsFromAssemblyContaining<RegisterRequestValidator>();

// Add Clean Architecture Infrastructure (EF Core SqlServer, Identity, JWT, Repositories, Services)
builder.Services.AddInfrastructure(builder.Configuration);

// Add Swagger Documentation with Bearer Token Support & String Enums
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen(options =>
{
    options.SwaggerDoc("v1", new OpenApiInfo
    {
        Title = "Makanak API",
        Version = "v1",
        Description = "Makanak Real Estate & Offer Negotiation Platform API"
    });

    options.AddSecurityDefinition("Bearer", new OpenApiSecurityScheme
    {
        Description = "JWT Authorization header using the Bearer scheme. Example: \"Bearer {token}\"",
        Name = "Authorization",
        In = ParameterLocation.Header,
        Type = SecuritySchemeType.Http,
        Scheme = "Bearer",
        BearerFormat = "JWT"
    });

    options.AddSecurityRequirement(_ => new OpenApiSecurityRequirement
    {
        {
            new OpenApiSecuritySchemeReference("Bearer"),
            new List<string>()
        }
    });
});

// Configure CORS: named "Frontend" policy from config with fallback defaults
var allowedOrigins = builder.Configuration.GetSection("Cors:AllowedOrigins").Get<string[]>()
    ?? new[] { "http://localhost:5173", "http://localhost:3000" };

builder.Services.AddCors(options =>
{
    options.AddPolicy("Frontend", policy =>
    {
        policy.WithOrigins(allowedOrigins)
              .AllowAnyMethod()
              .AllowAnyHeader()
              .AllowCredentials();
    });
});

var app = builder.Build();

// Global RFC 7807 Exception Handler Middleware with machine-readable 'code'
app.UseExceptionHandler(errorApp =>
{
    errorApp.Run(async context =>
    {
        var exceptionFeature = context.Features.Get<IExceptionHandlerFeature>();
        var exception = exceptionFeature?.Error;

        var (statusCode, title, code) = exception switch
        {
            ValidationException => (StatusCodes.Status400BadRequest, "Validation Failed", "validation_failed"),
            NotFoundException => (StatusCodes.Status404NotFound, "Resource Not Found", "not_found"),
            KeyNotFoundException => (StatusCodes.Status404NotFound, "Resource Not Found", "not_found"),
            PropertyUnavailableException => (StatusCodes.Status400BadRequest, "Property Unavailable", "property_not_available"),
            NegotiationLimitExceededException => (StatusCodes.Status400BadRequest, "Negotiation Limit Exceeded", "negotiation_limit_exceeded"),
            InvalidStateTransitionException => (StatusCodes.Status400BadRequest, "Invalid State Transition", "invalid_state_transition"),
            DuplicateActiveOfferException => (StatusCodes.Status400BadRequest, "Duplicate Active Offer", "duplicate_active_offer"),
            OwnPropertyOfferException => (StatusCodes.Status400BadRequest, "Own Property Offer", "own_property_offer"),
            ForbiddenActionException => (StatusCodes.Status400BadRequest, "Forbidden Action", "forbidden_action"),
            DbUpdateConcurrencyException => (StatusCodes.Status409Conflict, "Concurrency Conflict", "concurrency_conflict"),
            AuthException => (StatusCodes.Status401Unauthorized, "Authentication Failed", "unauthorized"),
            UnauthorizedAccessException => (StatusCodes.Status401Unauthorized, "Unauthorized Access", "unauthorized"),
            DomainException de => (StatusCodes.Status400BadRequest, "Domain Business Rule Violation", de.Code),
            _ => (StatusCodes.Status500InternalServerError, "An unexpected server error occurred", "server_error")
        };

        context.Response.StatusCode = statusCode;
        context.Response.ContentType = "application/problem+json";

        object problemResponse;

        if (exception is ValidationException ve)
        {
            var errors = ve.Errors
                .GroupBy(e => JsonNamingPolicy.CamelCase.ConvertName(e.PropertyName))
                .ToDictionary(
                    g => g.Key,
                    g => g.Select(e => e.ErrorMessage).ToArray()
                );

            problemResponse = new
            {
                type = $"https://httpstatuses.io/{statusCode}",
                title,
                status = statusCode,
                detail = "One or more validation errors occurred.",
                instance = context.Request.Path.Value,
                code,
                errors
            };
        }
        else
        {
            problemResponse = new
            {
                type = $"https://httpstatuses.io/{statusCode}",
                title,
                status = statusCode,
                detail = exception?.Message,
                instance = context.Request.Path.Value,
                code
            };
        }

        await context.Response.WriteAsJsonAsync(problemResponse);
    });
});

// Configure HTTP request pipeline
if (app.Environment.IsDevelopment() || app.Environment.IsEnvironment("Testing"))
{
    app.UseSwagger();
    app.UseSwaggerUI(c =>
    {
        c.SwaggerEndpoint("/swagger/v1/swagger.json", "Makanak API v1");
        c.RoutePrefix = string.Empty; // Swagger UI served at app root
    });

    if (app.Configuration.GetValue<bool>("Seed:Enabled"))
    {
        using var scope = app.Services.CreateScope();
        var dbContext = scope.ServiceProvider.GetRequiredService<Makanak.Infrastructure.Data.ApplicationDbContext>();
        var userManager = scope.ServiceProvider.GetRequiredService<Microsoft.AspNetCore.Identity.UserManager<Makanak.Domain.Entities.ApplicationUser>>();
        await Makanak.Infrastructure.Data.DevDataSeeder.SeedAsync(dbContext, userManager);
    }
}

app.UseHttpsRedirection();
app.UseStaticFiles();
app.UseCors("Frontend");

app.UseAuthentication();
app.UseAuthorization();

app.MapControllers();

app.Run();


// Expose Program class for WebApplicationFactory in integration tests
public partial class Program { }
