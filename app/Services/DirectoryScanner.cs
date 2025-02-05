using System.Security.Cryptography;
using System.Text;

namespace App.Services
{
    public class DirectoryScanner : IDirectoryScanner
    {
        private readonly ILoggerService _logger;
        private readonly IDataRepository _repository;

        public DirectoryScanner(ILoggerService logger, IDataRepository repository)
        {
            _logger = logger;
            _repository = repository;
        }

        public async Task<DirectoryInfo> ScanDirectoryAsync(string path)
        {
            try
            {
                var directory = new DirectoryInfo(path);
                if (!directory.Exists)
                {
                    await _logger.LogErrorAsync($"目录不存在: {path}");
                    return null;
                }

                // 获取所有子目录并按字母顺序排序
                var subDirectories = directory.GetDirectories("*", SearchOption.TopDirectoryOnly)
                                           .OrderBy(d => d.FullName)
                                           .ToList();

                foreach (var dir in subDirectories)
                {
                    await ScanDirectoryAsync(dir.FullName);
                }

                return directory;
            }
            catch (Exception ex)
            {
                await _logger.LogErrorAsync($"扫描目录时出错: {path}", ex);
                return null;
            }
        }

        public async Task<bool> CheckDirectorySignatureAsync(string path)
        {
            try
            {
                string newSignature = await CalculateDirectorySignatureAsync(path);
                return true;
            }
            catch (Exception ex)
            {
                await _logger.LogErrorAsync($"计算目录签名时出错: {path}", ex);
                return false;
            }
        }

        public async Task<IEnumerable<FileInfo>> GetWordFilesAsync(string directoryPath)
        {
            try
            {
                var directory = new DirectoryInfo(directoryPath);
                return directory.GetFiles("*.doc*", SearchOption.TopDirectoryOnly)
                              .Where(f => f.Extension.Equals(".doc", StringComparison.OrdinalIgnoreCase) ||
                                        f.Extension.Equals(".docx", StringComparison.OrdinalIgnoreCase))
                              .OrderBy(f => f.FullName);
            }
            catch (Exception ex)
            {
                await _logger.LogErrorAsync($"获取Word文件列表时出错: {directoryPath}", ex);
                return Enumerable.Empty<FileInfo>();
            }
        }

        private Task<string> CalculateDirectorySignatureAsync(string path)
        {
            var directory = new DirectoryInfo(path);
            var files = directory.GetFiles("*.*", SearchOption.TopDirectoryOnly)
                                .OrderBy(f => f.FullName);

            using var sha256 = SHA256.Create();
            var signatureBuilder = new StringBuilder();

            foreach (var file in files)
            {
                signatureBuilder.AppendLine($"{file.FullName}|{file.Length}|{file.LastWriteTimeUtc.Ticks}");
            }

            var signatureBytes = Encoding.UTF8.GetBytes(signatureBuilder.ToString());
            var hashBytes = sha256.ComputeHash(signatureBytes);
            return Task.FromResult(Convert.ToBase64String(hashBytes));
        }

        // 移除了 MonitorDirectoryChangesAsync 方法
    }
}

