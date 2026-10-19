using Makanak.Infrastructure;
using Makanak.Infrastructure.Hubs;

var builder = WebApplication.CreateBuilder(args);

// Add Controllers
builder.Services.AddControllers();

// Add Clean Architecture Infrastructure (EF Core SqlServer + NetTopologySuite, Identity, SignalR, Repositories, Services)
builder.Services.AddInfrastructure(builder.Configuration);

// Add Swagger Documentation
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();

// Configure CORS for web clients and SignalR
builder.Services.AddCors(options =>
{
    options.AddPolicy("AllowAll", policy =>
    {
        policy.SetIsOriginAllowed(_ => true)
              .AllowAnyMethod()
              .AllowAnyHeader()
              .AllowCredentials();
    });
});

var app = builder.Build();

// Configure the HTTP request pipeline
if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI(c =>
    {
        c.SwaggerEndpoint("/swagger/v1/swagger.json", "Makanak API v1");
        c.RoutePrefix = string.Empty; // Serve Swagger at app root
    });
}

app.UseHttpsRedirection();
app.UseCors("AllowAll");

app.UseAuthentication();
app.UseAuthorization();

// Route Controllers and SignalR Hubs
app.MapControllers();
app.MapHub<NotificationHub>("/hubs/notifications");

app.Run();
