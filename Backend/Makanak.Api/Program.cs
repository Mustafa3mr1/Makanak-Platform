using Microsoft.AspNetCore.Diagnostics;
using Microsoft.OpenApi;
using Makanak.Domain.Exceptions;
using Makanak.Infrastructure;

var builder = WebApplication.CreateBuilder(args);

// Add Controllers
builder.Services.AddControllers();

// Add Clean Architecture Infrastructure (EF Core SqlServer, Identity, JWT, Repositories, Services)
builder.Services.AddInfrastructure(builder.Configuration);

// Add Swagger Documentation with Bearer Token Support
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

// Configure CORS
builder.Services.AddCors(options =>
{
    options.AddPolicy("AllowAll", policy =>
    {
        policy.SetIsOriginAllowed(_ => true)
              .AllowAnyMethod()
              .AllowAnyHeader();
    });
});

var app = builder.Build();

// Global RFC 7807 Exception Handler Middleware
app.UseExceptionHandler(errorApp =>
{
    errorApp.Run(async context =>
    {
        var exceptionFeature = context.Features.Get<IExceptionHandlerFeature>();
        var exception = exceptionFeature?.Error;

        var (statusCode, title) = exception switch
        {
            AuthException => (StatusCodes.Status401Unauthorized, "Authentication Failed"),
            PropertyUnavailableException => (StatusCodes.Status400BadRequest, "Property Unavailable"),
            NegotiationLimitExceededException => (StatusCodes.Status400BadRequest, "Negotiation Limit Exceeded"),
            InvalidOfferOperationException => (StatusCodes.Status400BadRequest, "Invalid Offer Operation"),
            InvalidPropertyOperationException => (StatusCodes.Status400BadRequest, "Invalid Property Operation"),
            DomainException => (StatusCodes.Status400BadRequest, "Domain Business Rule Violation"),
            UnauthorizedAccessException => (StatusCodes.Status401Unauthorized, "Unauthorized Access"),
            KeyNotFoundException => (StatusCodes.Status404NotFound, "Resource Not Found"),
            _ => (StatusCodes.Status500InternalServerError, "An unexpected server error occurred")
        };

        context.Response.StatusCode = statusCode;
        context.Response.ContentType = "application/problem+json";

        var problemDetails = new
        {
            type = $"https://httpstatuses.io/{statusCode}",
            title,
            status = statusCode,
            detail = exception?.Message,
            instance = context.Request.Path.Value
        };

        await context.Response.WriteAsJsonAsync(problemDetails);
    });
});

// Configure HTTP request pipeline
if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI(c =>
    {
        c.SwaggerEndpoint("/swagger/v1/swagger.json", "Makanak API v1");
        c.RoutePrefix = string.Empty; // Swagger UI served at app root
    });
}

app.UseHttpsRedirection();
app.UseCors("AllowAll");

app.UseAuthentication();
app.UseAuthorization();

app.MapControllers();

app.Run();
