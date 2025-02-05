using Microsoft.AspNetCore.Routing.Constraints;
using Microsoft.OpenApi.Models;
using App.Services;

var builder = WebApplication.CreateBuilder(args);

// Add services to the container.
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen(c =>
{
    c.SwaggerDoc("v1", new OpenApiInfo { Title = "Mini API", Version = "v1" });
});
builder.Services.Configure<RouteOptions>(options => 
{
    options.SetParameterPolicy<RegexInlineRouteConstraint>("regex");
});
builder.Services.AddSingleton<IDataRepository>(sp => 
    new SqliteDataRepository("data.db", sp.GetRequiredService<ILoggerService>()));
builder.Services.AddSingleton<IDirectoryScanner, DirectoryScanner>();
builder.Services.AddSingleton<IFileProcessor, FileProcessor>();
builder.Services.AddSingleton<ILoggerService, SerilogService>();
builder.Services.AddSingleton(typeof(ProcessManager));

var app = builder.Build();

// Enable static files
app.UseDefaultFiles();
app.UseStaticFiles();

app.UseSwagger();
app.UseSwaggerUI();

app.MapGet("/test", async(ILoggerService logger) =>
{
    await logger.LogInfoAsync("Test endpoint was called");
    return "Hello World!";
})
.WithName("Test_String");

// Add a new endpoint to test different log levels
app.MapGet("/testlog", async(ILoggerService logger) =>
{
    await logger.LogInfoAsync("This is an information message");
    await logger.LogWarningAsync("This is a warning message");
    try
    {
        throw new Exception("Test exception");
    }
    catch (Exception ex)
    {
        await logger.LogErrorAsync("This is an error message", ex);
    }
    return "Logs have been written. Check the console and logs folder.";
})
.WithName("Test_Logging");

app.MapPost("/start", async (ILoggerService logger,ProcessManager processManager, HttpResponse response, HttpRequest request) =>
{
    response.Headers.Append("Content-Type", "text/event-stream");
    response.Headers.Append("Cache-Control", "no-cache");
    response.Headers.Append("Connection", "keep-alive");
    
    await response.WriteAsync("Start processing\n\n");
    await response.Body.FlushAsync();
    using var reader = new StreamReader(request.Body);
    var requestBody = await reader.ReadToEndAsync();
    var jsonDoc = System.Text.Json.JsonDocument.Parse(requestBody);
    var dir = jsonDoc.RootElement.GetProperty("dir").GetString();
    if(dir==null||string.IsNullOrEmpty(dir)||!Directory.Exists(dir)){
        await response.WriteAsync($"error: bad request, dir is not valid\n\n");
        await response.Body.FlushAsync();
        return;
    }
    await response.WriteAsync($"dir:{dir}\n\n");
    await response.Body.FlushAsync();
    processManager.OnLogReceived += async (message) =>
    {
        await response.WriteAsync($"data: {message}\n\n");
        await response.Body.FlushAsync();
    };

    try
    {
        await processManager.StartProcessingAsync(dir);
    }
    catch (Exception ex)
    {
        await response.WriteAsync($"data: [ERROR] Processing failed: {ex.Message}\n\n");
    }
    finally{
        await response.BodyWriter.CompleteAsync();
    }
})
.WithName("Start_Processing");

app.MapPost("/cancel", (ProcessManager processManager) =>
{
    processManager.Stop();
    return Results.Ok(new { message = "Processing cancelled" });
})
.WithName("Cancel_Processing");

app.Run();
