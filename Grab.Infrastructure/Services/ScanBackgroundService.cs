using Grab.Core.Interfaces;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;

namespace Grab.Infrastructure.Services
{
    public class ScanBackgroundService : BackgroundService
    {
        private readonly IServiceProvider _serviceProvider;
        private readonly IConfiguration _configuration;
        private readonly ILogger<ScanBackgroundService> _logger;
        private readonly TimeSpan _scanInterval;

        public ScanBackgroundService(
            IServiceProvider serviceProvider,
            IConfiguration configuration,
            ILogger<ScanBackgroundService> logger)
        {
            _serviceProvider = serviceProvider;
            _configuration = configuration;
            _logger = logger;

            // 获取配置的扫描间隔，默认为1小时
            int intervalSeconds = _configuration.GetValue<int>("ScanSettings:ScanInterval", 3600);
            _scanInterval = TimeSpan.FromSeconds(intervalSeconds);
        }

        protected override async Task ExecuteAsync(CancellationToken stoppingToken)
        {
            _logger.LogInformation("Scan Background Service is starting.");

            while (!stoppingToken.IsCancellationRequested)
            {
                _logger.LogInformation("Scan Background Service is running scan at: {Time}", DateTimeOffset.Now);

                try
                {
                    await DoScanAsync(stoppingToken);
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "An error occurred during background scan");
                }

                _logger.LogInformation("Scan completed. Waiting for next scan interval.");

                await Task.Delay(_scanInterval, stoppingToken);
            }

            _logger.LogInformation("Scan Background Service is stopping.");
        }

        private async Task DoScanAsync(CancellationToken stoppingToken)
        {
            // 获取根路径
            string rootPath = _configuration.GetValue<string>("ScanSettings:RootPath") ?? "";

            if (string.IsNullOrEmpty(rootPath) || !Directory.Exists(rootPath))
            {
                _logger.LogWarning("Invalid or missing root path configuration. Scan skipped.");
                return;
            }

            using (var scope = _serviceProvider.CreateScope())
            {
                var scanService = scope.ServiceProvider.GetRequiredService<IScanService>();
                var taskService = scope.ServiceProvider.GetRequiredService<ITaskService>();

                // 获取所有启用的任务
                var tasks = await taskService.GetEnabledTasksAsync();

                if (!tasks.Any())
                {
                    _logger.LogInformation("No enabled tasks found. Scan skipped.");
                    return;
                }

                foreach (var task in tasks)
                {
                    if (stoppingToken.IsCancellationRequested)
                        break;

                    string taskPath = string.IsNullOrEmpty(task.SourcePath) ? rootPath : task.SourcePath;

                    if (!Directory.Exists(taskPath))
                    {
                        _logger.LogWarning("Task {TaskId} has invalid path: {Path}. Skipping this task.", task.Id, taskPath);
                        continue;
                    }

                    _logger.LogInformation("Starting scan for task {TaskId} at path: {Path}", task.Id, taskPath);

                    try
                    {
                        await scanService.ScanDirectoriesAsync(taskPath);
                    }
                    catch (Exception ex)
                    {
                        _logger.LogError(ex, "Error scanning path {Path} for task {TaskId}", taskPath, task.Id);
                    }
                }
            }
        }
    }
}
