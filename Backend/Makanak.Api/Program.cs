using System.Reflection;
using System.Text.Json.Serialization;
using Makanak.API.Authentication;
using Makanak.API.Middleware;
using Makanak.Application;
using Makanak.Application.Common.Models;
using Makanak.Infrastructure;
using Makanak.Infrastructure.Persistence;
using Makanak.Infrastructure.Storage;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.Http.Features;
using Microsoft.Extensions.FileProviders;
using Microsoft.Extensions.Options;
using Microsoft.OpenApi.Models;

var builder = WebApplication.CreateBuilder(args);

// ---------------------------------------------------------------- Services
builder.Services.AddApplication();
builder.Services.AddInfrastructure(builder.Configuration, builder.Environment.ContentRootPath);

builder.Services.AddControllers()
    .AddJsonOptions(o =>
    {
        // Enums travel as strings; undefined numeric values are rejected (allowIntegerValues: false).
        o.JsonSerializerOptions.Converters.Add(new JsonStringEnumConverter(namingPolicy: null, allowIntegerValues: false));
    });

builder.Services.Configure<FormOptions>(o => o.MultipartBodyLengthLimit = ImageRules.MaxRequestBytes);

builder.Services.AddAuthentication(JwtBearerDefaults.AuthenticationScheme).AddJwtBearer();
builder.Services.AddSingleton<Microsoft.Extensions.Options.IConfigureOptions<JwtBearerOptions>, ConfigureJwtBearerOptions>();
builder.Services.AddAuthorization();

var allowedOrigins = builder.Configuration.GetSection("Cors:AllowedOrigins").Get<string[]>() ?? Array.Empty<string>();
builder.Services.AddCors(options => options.AddPolicy("Frontend", policy =>
    policy.WithOrigins(allowedOrigins).AllowAnyHeader().AllowAnyMethod()));

builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen(options =>
{
    options.SwaggerDoc("v1", new OpenApiInfo
    {
        Title = "Makanak Real Estate API",
        Version = "v1",
        Description = "Property listings, offers and negotiation. Authenticate via POST /api/auth/login, then click 'Authorize' and paste the token."
    });

    options.AddSecurityDefinition("Bearer", new OpenApiSecurityScheme
    {
        Name = "Authorization",
        Type = SecuritySchemeType.Http,
        Scheme = "bearer",
        BearerFormat = "JWT",
        In = ParameterLocation.Header,
        Description = "Paste the JWT only (the 'Bearer ' prefix is added automatically)."
    });
    options.AddSecurityRequirement(new OpenApiSecurityRequirement
    {
        {
            new OpenApiSecurityScheme { Reference = new OpenApiReference { Type = ReferenceType.SecurityScheme, Id = "Bearer" } },
            Array.Empty<string>()
        }
    });

    var xml = Path.Combine(AppContext.BaseDirectory, $"{Assembly.GetExecutingAssembly().GetName().Name}.xml");
    if (File.Exists(xml)) options.IncludeXmlComments(xml);
});

var app = builder.Build();

// ---------------------------------------------------------- Initialization
if (app.Configuration.GetValue<bool>("Database:InitializeOnStartup"))
{
    await DatabaseInitializer.MigrateAsync(app.Services);
    await DatabaseInitializer.SeedAsync(app.Services, app.Configuration);
}

// ---------------------------------------------------------------- Pipeline
app.UseMiddleware<GlobalExceptionMiddleware>();

if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI(c => c.SwaggerEndpoint("/swagger/v1/swagger.json", "Makanak API v1"));
}

app.UseHttpsRedirection();

var storage = app.Services.GetRequiredService<IOptions<ImageStorageSettings>>().Value;
Directory.CreateDirectory(storage.RootPath);
app.UseStaticFiles(new StaticFileOptions
{
    FileProvider = new PhysicalFileProvider(storage.RootPath),
    RequestPath = "/" + storage.RequestPath.Trim('/'),
    OnPrepareResponse = ctx => ctx.Context.Response.Headers["X-Content-Type-Options"] = "nosniff"
});

app.UseCors("Frontend");
app.UseAuthentication();
app.UseAuthorization();
app.MapControllers();

app.Run();

// Required so integration tests can use WebApplicationFactory<Program>.
public partial class Program { }
