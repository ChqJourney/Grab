using Serilog;
using ILogger = Serilog.ILogger;

namespace App.Services;

public class SerilogService : ILoggerService
{
    private readonly ILogger _logger;

    public SerilogService()
    {
        _logger = new LoggerConfiguration()
            .MinimumLevel.Debug()
            .WriteTo.Console()
            .WriteTo.File("logs/app-.txt", 
                rollingInterval: RollingInterval.Day,
                rollOnFileSizeLimit: true)
            .CreateLogger();
    }

    public void LogDebug(string message)
    {
        _logger.Debug(message);
    }

    public void LogError(string message, Exception? exception = null)
    {
        if (exception != null)
            _logger.Error(exception, message);
        else
            _logger.Error(message);
    }

    public void LogInformation(string message)
    {
        _logger.Information(message);
    }

    public void LogWarning(string message)
    {
        _logger.Warning(message);
    }
}
