namespace App.Services
{
    internal class LoggerServiceWrapper : ILoggerService
    {
        private readonly ILoggerService _inner;
        private readonly ProcessManager _processManager;

        public LoggerServiceWrapper(ILoggerService inner, ProcessManager processManager)
        {
            _inner = inner;
            _processManager = processManager;
        }

        private async Task NotifyLog(string message)
        {
            if (_processManager.hasHandlerAlready())
            {

                await _processManager.RaiseLogReceivedEvent(message);

            }
        }

        public async Task LogInfoAsync(string message)
        {
            await _inner.LogInfoAsync(message);
            await NotifyLog($"[INFO] {message}");
        }

        public async Task LogErrorAsync(string message, Exception ex = null)
        {
            await _inner.LogErrorAsync(message, ex);
            await NotifyLog($"[ERROR] {message}{(ex != null ? $"\nException: {ex.Message}" : "")}");
        }

        public async Task LogWarningAsync(string message)
        {
            await _inner.LogWarningAsync(message);
            await NotifyLog($"[WARN] {message}");
        }

        public async Task LogProcessStatusAsync(string path, string status, string details = null)
        {
            await _inner.LogProcessStatusAsync(path, status, details);
            await NotifyLog($"[STATUS] {path}: {status}{(details != null ? $" - {details}" : "")}");
        }
    }


    public class ProcessManager
    {
        public event Func<string, Task> OnLogReceived;
        private readonly IDirectoryScanner _directoryScanner;
        private readonly IFileProcessor _fileProcessor;
        private readonly IDataRepository _repository;
        private readonly ILoggerService _logger;
        private readonly CancellationTokenSource _cancellationTokenSource;
        private readonly SemaphoreSlim _processingSemaphore;
        private const int MAX_CONCURRENT_PROCESSES = 4;

        public ProcessManager(
            IDirectoryScanner directoryScanner,
            IFileProcessor fileProcessor,
            IDataRepository repository,
            ILoggerService logger)
        {
            _directoryScanner = directoryScanner;
            _fileProcessor = fileProcessor;
            _repository = repository;
            _logger = new LoggerServiceWrapper(logger, this);
            _cancellationTokenSource = new CancellationTokenSource();
            _processingSemaphore = new SemaphoreSlim(MAX_CONCURRENT_PROCESSES);
        }
        public bool hasHandlerAlready()
        {
            return OnLogReceived != null;
        }
        public async Task RaiseLogReceivedEvent(string message)
        {
            if (OnLogReceived != null)
            {
                await OnLogReceived.Invoke(message);
            }
        }

        public async Task StartProcessingAsync(string rootPath)
        {
            try
            {
                while (!_cancellationTokenSource.Token.IsCancellationRequested)
                {
                    // 处理待处理的目录
                    await ProcessPendingDirectoriesAsync(rootPath);

                    // 处理待处理的文件
                    await ProcessPendingFilesAsync();

                    await Task.Delay(1000, _cancellationTokenSource.Token);
                }
            }
            catch (OperationCanceledException)
            {
                await _logger.LogInfoAsync("处理已停止");
            }
            catch (Exception ex)
            {
                await _logger.LogErrorAsync("处理过程中发生错误", ex);
                throw;
            }
        }

        private async Task ProcessPendingDirectoriesAsync(string rootPath)
        {
            var pendingDirs = await _repository.GetPendingDirectoriesAsync();
            foreach (var dir in pendingDirs)
            {
                if (_cancellationTokenSource.Token.IsCancellationRequested) break;

                try
                {
                    await _repository.UpdateDirectoryStatusAsync(dir, "PROCESSING");
                    var signature = await _directoryScanner.CheckDirectorySignatureAsync(dir);
                    var files = await _directoryScanner.GetWordFilesAsync(dir);

                    foreach (var file in files)
                    {
                        await _repository.AddFileAsync(
                            file.FullName,
                            dir,
                            file.Length,
                            file.LastWriteTimeUtc.Ticks);
                    }

                    await _repository.UpdateDirectoryStatusAsync(dir, "COMPLETED");
                }
                catch (Exception ex)
                {
                    await _logger.LogErrorAsync($"处理目录失败: {dir}", ex);
                    await _repository.UpdateDirectoryStatusAsync(dir, "NEED_RECHECK");
                }
            }
        }

        private async Task ProcessPendingFilesAsync()
        {
            var pendingFiles = await _repository.GetPendingFilesAsync();
            var tasks = new List<Task>();

            foreach (var file in pendingFiles)
            {
                if (_cancellationTokenSource.Token.IsCancellationRequested) break;

                await _processingSemaphore.WaitAsync();
                tasks.Add(ProcessFileWithSemaphoreAsync(file));
            }

            await Task.WhenAll(tasks);
        }

        private async Task ProcessFileWithSemaphoreAsync(string filePath)
        {
            try
            {
                await _repository.UpdateFileStatusAsync(filePath, "PROCESSING");
                var success = await _fileProcessor.ProcessFileAsync(filePath);

                if (success)
                {
                    var hash = await _fileProcessor.CalculateFileHashAsync(filePath);
                    await _repository.UpdateFileProcessResultAsync(filePath, hash, DateTime.UtcNow);
                    await _repository.UpdateFileStatusAsync(filePath, "PROCESSED");
                }
                else
                {
                    await _repository.UpdateFileStatusAsync(filePath, "FAILED");
                }
            }
            catch (Exception ex)
            {
                await _logger.LogErrorAsync($"处理文件失败: {filePath}", ex);
                await _repository.UpdateFileStatusAsync(filePath, "FAILED");
            }
            finally
            {
                _processingSemaphore.Release();
            }
        }

        public void Stop()
        {
            _cancellationTokenSource.Cancel();
        }

        public void Dispose()
        {
            _cancellationTokenSource.Dispose();
            _processingSemaphore.Dispose();
        }
    }
}