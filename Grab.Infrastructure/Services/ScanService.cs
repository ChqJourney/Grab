using Grab.Core.Interfaces;
using Grab.Core.Models;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Security.Cryptography;
using System.Text;
using System.Threading.Tasks;

namespace Grab.Infrastructure.Services
{
    public class ScanService : IScanService
    {
        private readonly IDirectoryRepository _directoryRepository;
        private readonly IFileRepository _fileRepository;
        private readonly ITaskService _taskService;
        private readonly IDocumentProcessorService _documentProcessor;
        private readonly IConfiguration _configuration;
        private readonly ILogger<ScanService> _logger;
        private readonly int _maxConcurrentTasks;
        private readonly int _maxRetryCount;

        public ScanService(
            IDirectoryRepository directoryRepository,
            IFileRepository fileRepository,
            ITaskService taskService,
            IDocumentProcessorService documentProcessor,
            IConfiguration configuration,
            ILogger<ScanService> logger)
        {
            _directoryRepository = directoryRepository;
            _fileRepository = fileRepository;
            _taskService = taskService;
            _documentProcessor = documentProcessor;
            _configuration = configuration;
            _logger = logger;

            _maxConcurrentTasks = _configuration.GetValue<int>("ScanSettings:MaxConcurrentTasks", 5);
            _maxRetryCount = _configuration.GetValue<int>("ScanSettings:MaxRetryCount", 3);
        }

        public async Task ScanDirectoriesAsync(string rootPath)
        {
            try
            {
                _logger.LogInformation("Starting directory scan at: {Path}", rootPath);

                if (!Directory.Exists(rootPath))
                {
                    _logger.LogWarning("Directory does not exist: {Path}", rootPath);
                    return;
                }

                // 获取或创建根目录记录
                var dirRecord = await _directoryRepository.GetByPathAsync(rootPath);
                
                if (dirRecord == null)
                {
                    // 新目录，添加记录
                    dirRecord = new Core.Models.Directory
                    {
                        Path = rootPath,
                        Status = DirectoryStatus.Pending,
                        LastCheckTime = DateTime.UtcNow
                    };
                    
                    await _directoryRepository.AddAsync(dirRecord);
                    
                    _logger.LogInformation("Added new directory record: {Path}", rootPath);
                }

                // 计算目录特征值
                string currentSignature = await CalculateDirectorySignatureAsync(rootPath);

                // 检查目录是否有变化
                if (dirRecord.Status == DirectoryStatus.Completed && 
                    dirRecord.LastSignature == currentSignature)
                {
                    _logger.LogInformation("Directory hasn't changed, skipping: {Path}", rootPath);
                    return;
                }

                // 更新目录状态为处理中
                dirRecord.Status = DirectoryStatus.Processing;
                dirRecord.LastSignature = currentSignature;
                dirRecord.LastCheckTime = DateTime.UtcNow;
                
                await _directoryRepository.UpdateAsync(dirRecord);

                // 处理目录
                await ProcessDirectoryAsync(rootPath);

                // 递归处理子目录
                foreach (var subDir in Directory.GetDirectories(rootPath))
                {
                    await ScanDirectoriesAsync(subDir);
                }

                // 更新目录处理状态为完成
                dirRecord.Status = DirectoryStatus.Completed;
                dirRecord.LastProcessTime = DateTime.UtcNow;
                
                await _directoryRepository.UpdateAsync(dirRecord);
                
                _logger.LogInformation("Completed directory scan: {Path}", rootPath);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error scanning directory: {Path}", rootPath);
                
                // 尝试将目录状态更新为需要重新检查
                try
                {
                    var dirRecord = await _directoryRepository.GetByPathAsync(rootPath);
                    if (dirRecord != null)
                    {
                        dirRecord.Status = DirectoryStatus.NeedRecheck;
                        await _directoryRepository.UpdateAsync(dirRecord);
                    }
                }
                catch (Exception updateEx)
                {
                    _logger.LogError(updateEx, "Failed to update directory status after error: {Path}", rootPath);
                }
                
                throw;
            }
        }

        public async Task ProcessDirectoryAsync(string path)
        {
            _logger.LogInformation("Processing directory: {Path}", path);

            try
            {
                // 获取目录中的所有文件
                string[] files = Directory.GetFiles(path);
                
                _logger.LogInformation("Found {Count} files in directory: {Path}", files.Length, path);

                // 获取所有启用的任务，用于后续文件处理
                var tasks = await _taskService.GetEnabledTasksAsync();
                
                if (!tasks.Any())
                {
                    _logger.LogWarning("No enabled tasks found. Skipping file processing.");
                    return;
                }

                // 为每个文件创建或更新记录
                foreach (string filePath in files)
                {
                    try
                    {
                        if (!File.Exists(filePath))
                        {
                            _logger.LogWarning("File no longer exists: {Path}", filePath);
                            continue;
                        }

                        FileInfo fileInfo = new FileInfo(filePath);
                        
                        // 检查文件是否是支持的类型
                        string extension = Path.GetExtension(filePath).ToLowerInvariant();
                        
                        if (!IsFileExtensionSupported(extension))
                        {
                            _logger.LogDebug("Unsupported file extension: {Extension} for file: {Path}", extension, filePath);
                            continue;
                        }

                        // 获取或创建文件记录
                        var fileRecord = await _fileRepository.GetByPathAsync(filePath);
                        long lastModified = new DateTimeOffset(fileInfo.LastWriteTimeUtc).ToUnixTimeMilliseconds();

                        if (fileRecord == null)
                        {
                            // 新文件，添加记录
                            fileRecord = new FileItem
                            {
                                Path = filePath,
                                DirectoryPath = path,
                                FileSize = fileInfo.Length,
                                ModifiedTime = lastModified,
                                Status = FileStatus.Pending
                            };
                            
                            await _fileRepository.AddAsync(fileRecord);
                            
                            _logger.LogInformation("Added new file record: {Path}", filePath);
                        }
                        else if (fileRecord.ModifiedTime != lastModified || fileRecord.FileSize != fileInfo.Length)
                        {
                            // 文件已修改，更新记录
                            fileRecord.FileSize = fileInfo.Length;
                            fileRecord.ModifiedTime = lastModified;
                            fileRecord.Status = FileStatus.Pending;
                            fileRecord.Hash = string.Empty; // 重置哈希，下次处理时重新计算
                            
                            await _fileRepository.UpdateAsync(fileRecord);
                            
                            _logger.LogInformation("File has been modified, updated record: {Path}", filePath);
                        }
                        else if (fileRecord.Status == FileStatus.Pending || fileRecord.Status == FileStatus.Failed)
                        {
                            // 文件需要处理，已经在合适的状态
                            _logger.LogInformation("File needs processing: {Path}, Status: {Status}", filePath, fileRecord.Status);
                        }
                        else
                        {
                            // 文件没有变化且已处理，跳过
                            _logger.LogDebug("File hasn't changed, skipping: {Path}", filePath);
                            continue;
                        }

                        // 对需要处理的文件进行处理
                        if (fileRecord.Status == FileStatus.Pending || fileRecord.Status == FileStatus.Failed)
                        {
                            await ProcessFileAsync(filePath);
                        }
                    }
                    catch (Exception fileEx)
                    {
                        _logger.LogError(fileEx, "Error processing file: {Path}", filePath);
                    }
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error processing directory: {Path}", path);
                throw;
            }
        }

        public async Task ProcessFileAsync(string path)
        {
            _logger.LogInformation("Processing file: {Path}", path);

            var fileRecord = await _fileRepository.GetByPathAsync(path);
            
            if (fileRecord == null)
            {
                _logger.LogWarning("File record not found for: {Path}", path);
                return;
            }

            // 检查文件是否存在
            if (!File.Exists(path))
            {
                _logger.LogWarning("File no longer exists: {Path}", path);
                fileRecord.Status = FileStatus.Deleted;
                await _fileRepository.UpdateAsync(fileRecord);
                return;
            }

            try
            {
                // 检查文件是否被锁定
                if (await IsFileLockedAsync(path))
                {
                    _logger.LogWarning("File is locked, will retry later: {Path}", path);
                    return;
                }

                // 更新文件状态为处理中
                fileRecord.Status = FileStatus.Processing;
                await _fileRepository.UpdateAsync(fileRecord);

                // 计算文件哈希
                string hash = await CalculateFileHashAsync(path);
                
                // 如果哈希没有变化且状态不是失败，跳过处理
                if (!string.IsNullOrEmpty(fileRecord.Hash) && fileRecord.Hash == hash && fileRecord.Status != FileStatus.Failed)
                {
                    _logger.LogInformation("File content hasn't changed, skipping: {Path}", path);
                    fileRecord.Status = FileStatus.Processed;
                    fileRecord.ProcessTime = DateTime.UtcNow;
                    await _fileRepository.UpdateAsync(fileRecord);
                    return;
                }

                // 获取可用任务
                var tasks = await _taskService.GetEnabledTasksAsync();
                bool anyTaskProcessed = false;

                // 获取文件扩展名
                string extension = Path.GetExtension(path).ToLowerInvariant();
                FileType fileType = GetFileTypeFromExtension(extension);

                // 匹配合适的任务处理文件
                foreach (var task in tasks)
                {
                    // 检查任务是否适用于此文件类型
                    if (task.TargetFileType != FileType.All && task.TargetFileType != fileType)
                    {
                        continue;
                    }

                    bool processed = await _documentProcessor.ProcessDocumentAsync(path, task);
                    
                    if (processed)
                    {
                        anyTaskProcessed = true;
                        _logger.LogInformation("File processed successfully by task {TaskId}: {Path}", task.Id, path);
                    }
                }

                // 更新文件状态和哈希
                fileRecord.Hash = hash;
                fileRecord.ProcessTime = DateTime.UtcNow;
                fileRecord.Status = anyTaskProcessed ? FileStatus.Processed : FileStatus.Failed;
                
                await _fileRepository.UpdateAsync(fileRecord);
                
                _logger.LogInformation("File processing completed: {Path}, Status: {Status}", path, fileRecord.Status);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error processing file: {Path}", path);
                
                // 更新文件状态为失败
                fileRecord.Status = FileStatus.Failed;
                fileRecord.ProcessTime = DateTime.UtcNow;
                
                await _fileRepository.UpdateAsync(fileRecord);
            }
        }

        public async Task<string> CalculateDirectorySignatureAsync(string path)
        {
            try
            {
                var directoryInfo = new DirectoryInfo(path);
                
                if (!directoryInfo.Exists)
                {
                    return string.Empty;
                }

                // 使用目录的元数据生成签名
                StringBuilder sb = new StringBuilder();
                
                sb.Append(directoryInfo.LastWriteTimeUtc.Ticks);
                sb.Append("_");
                
                // 获取子文件夹和文件的数量
                try
                {
                    int fileCount = directoryInfo.GetFiles().Length;
                    int dirCount = directoryInfo.GetDirectories().Length;
                    
                    sb.Append(fileCount);
                    sb.Append("_");
                    sb.Append(dirCount);
                }
                catch (UnauthorizedAccessException)
                {
                    // 权限不足，使用替代方法
                    sb.Append("NA");
                }

                // 计算签名哈希
                using (SHA256 sha = SHA256.Create())
                {
                    byte[] hashBytes = sha.ComputeHash(Encoding.UTF8.GetBytes(sb.ToString()));
                    return BitConverter.ToString(hashBytes).Replace("-", "").ToLowerInvariant();
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error calculating directory signature: {Path}", path);
                return string.Empty;
            }
        }

        public async Task<string> CalculateFileHashAsync(string path)
        {
            try
            {
                if (!File.Exists(path))
                {
                    return string.Empty;
                }

                using (var fileStream = new FileStream(path, FileMode.Open, FileAccess.Read, FileShare.ReadWrite))
                using (SHA256 sha = SHA256.Create())
                {
                    byte[] hashBytes = await sha.ComputeHashAsync(fileStream);
                    return BitConverter.ToString(hashBytes).Replace("-", "").ToLowerInvariant();
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error calculating file hash: {Path}", path);
                return string.Empty;
            }
        }

        public async Task<bool> IsFileLockedAsync(string path)
        {
            try
            {
                using (FileStream stream = File.Open(path, FileMode.Open, FileAccess.ReadWrite, FileShare.None))
                {
                    // 如果能打开文件，则文件未被锁定
                    stream.Close();
                }
                return false;
            }
            catch (IOException)
            {
                // 文件被锁定
                return true;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error checking if file is locked: {Path}", path);
                return true; // 出错时假设文件被锁定
            }
        }

        private bool IsFileExtensionSupported(string extension)
        {
            return extension == ".doc" || extension == ".docx" || extension == ".xls" || extension == ".xlsx";
        }

        private FileType GetFileTypeFromExtension(string extension)
        {
            return extension switch
            {
                ".doc" => FileType.Doc,
                ".docx" => FileType.Docx,
                ".xls" => FileType.Xls,
                ".xlsx" => FileType.Xlsx,
                _ => FileType.All
            };
        }
    }
}
