using Microsoft.AspNetCore.Routing.Constraints;
using Microsoft.OpenApi.Models;
using App.Services;

var builder = WebApplication.CreateBuilder(args);

// Add services to the container.
builder.Services.Configure<RouteOptions>(options => 
{
    options.SetParameterPolicy<RegexInlineRouteConstraint>("regex");
});

builder.Services.AddSingleton<ILoggerService, SerilogService>();
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen(c =>
{
    c.SwaggerDoc("v1", new OpenApiInfo { Title = "Mini API", Version = "v1" });
});

var app = builder.Build();

// Enable static files
app.UseDefaultFiles();
app.UseStaticFiles();

app.UseSwagger();
app.UseSwaggerUI();

app.MapGet("/test", (ILoggerService logger) =>
{
    logger.LogInformation("Test endpoint was called");
    return "Hello World!";
})
.WithName("Test_String")
.WithOpenApi();

// Add a new endpoint to test different log levels
app.MapGet("/testlog", (ILoggerService logger) =>
{
    logger.LogDebug("This is a debug message");
    logger.LogInformation("This is an information message");
    logger.LogWarning("This is a warning message");
    try
    {
        throw new Exception("Test exception");
    }
    catch (Exception ex)
    {
        logger.LogError("This is an error message", ex);
    }
    return "Logs have been written. Check the console and logs folder.";
})
.WithName("Test_Logging")
.WithOpenApi();

app.Run();
