using Serilog;
using Serilog.Core;
using ILogger = Serilog.ILogger;

namespace App.Services
{
    public class SerilogService : ILoggerService
    {
        private readonly ILogger _logger;
        public SerilogService()
        {
            _logger = new LoggerConfiguration()
                .WriteTo.Console()
                .WriteTo.File("logs/app-.log", 
                    rollingInterval: RollingInterval.Day,
                    outputTemplate: "{Timestamp:yyyy-MM-dd HH:mm:ss.fff} [{Level:u3}] {Message:lj}{NewLine}{Exception}")
                .CreateLogger();
        }

        public Task LogInfoAsync(string message)
        {
            _logger.Information(message);
            return Task.CompletedTask;
        }

        public Task LogErrorAsync(string message, Exception ex = null)
        {
            if (ex != null)
            {
                _logger.Error(ex, message);
            }
            else
            {
                _logger.Error(message);
            }
            return Task.CompletedTask;
        }

        public Task LogWarningAsync(string message)
        {
            _logger.Warning(message);
            return Task.CompletedTask;
        }

        public Task LogProcessStatusAsync(string path, string status, string details = null)
        {
            var logMessage = $"处理状态 - 路径: {path}, 状态: {status}";
            if (!string.IsNullOrEmpty(details))
            {
                logMessage += $", 详情: {details}";
            }
            _logger.Information(logMessage);
            return Task.CompletedTask;
        }
    }
}