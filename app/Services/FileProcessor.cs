using DocumentFormat.OpenXml.Packaging;
using DocumentFormat.OpenXml.Wordprocessing;
using System.Security.Cryptography;

namespace App.Services
{
    public class FileProcessor : IFileProcessor
    {
        private readonly ILoggerService _logger;
        private const int MAX_RETRY_COUNT = 3;
        private const int RETRY_DELAY_MS = 1000;

        public FileProcessor(ILoggerService logger)
        {
            _logger = logger;
        }

        public async Task<bool> ProcessFileAsync(string filePath)
        {
            int retryCount = 0;
            while (retryCount < MAX_RETRY_COUNT)
            {
                try
                {
                    if (!File.Exists(filePath))
                    {
                        await _logger.LogWarningAsync($"文件不存在: {filePath}");
                        return false;
                    }

                    if (!await ValidateFileAsync(filePath))
                    {
                        return false;
                    }

                    var info = await ExtractInformationAsync(filePath);
                    // TODO: 处理提取的信息

                    return true;
                }
                catch (IOException ex) when (IsFileLocked(ex))
                {
                    retryCount++;
                    if (retryCount >= MAX_RETRY_COUNT)
                    {
                        await _logger.LogErrorAsync($"文件访问重试次数超限: {filePath}", ex);
                        return false;
                    }
                    await Task.Delay(RETRY_DELAY_MS);
                }
                catch (Exception ex)
                {
                    await _logger.LogErrorAsync($"处理文件失败: {filePath}", ex);
                    return false;
                }
            }
            return false;
        }

        public async Task<string> CalculateFileHashAsync(string filePath)
        {
            using var sha256 = SHA256.Create();
            using var stream = File.OpenRead(filePath);
            var hash = await Task.Run(() => sha256.ComputeHash(stream));
            return Convert.ToBase64String(hash);
        }

        public async Task<bool> ValidateFileAsync(string filePath)
        {
            try
            {
                using var stream = File.OpenRead(filePath);
                using var word = WordprocessingDocument.Open(stream, false);
                return true;
            }
            catch (Exception ex)
            {
                await _logger.LogErrorAsync($"文件验证失败: {filePath}", ex);
                return false;
            }
        }

        public async Task<Dictionary<string, object>> ExtractInformationAsync(string filePath)
        {
            var info = new Dictionary<string, object>();
            try
            {
                using var word = WordprocessingDocument.Open(filePath, false);
                var body = word.MainDocumentPart?.Document.Body;
                if (body != null)
                {
                    // TODO: 根据具体需求提取文档信息
                    // 这里需要根据文档的具体格式来实现
                }
                return info;
            }
            catch (Exception ex)
            {
                await _logger.LogErrorAsync($"提取文件信息失败: {filePath}", ex);
                throw;
            }
        }

        private bool IsFileLocked(IOException ex)
        {
            return ex.Message.Contains("being used by another process") ||
                   ex.Message.Contains("访问被拒绝");
        }
    }
}