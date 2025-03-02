using DocumentFormat.OpenXml.Packaging;
using DocumentFormat.OpenXml.Spreadsheet;
using DocumentFormat.OpenXml.Wordprocessing;
using Grab.Core.Interfaces;
using Grab.Core.Models;
using Grab.Infrastructure.Services.DocumentParsers;
using Microsoft.Extensions.Logging;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text.RegularExpressions;
using System.Threading.Tasks;

namespace Grab.Infrastructure.Services
{
    public class DocumentProcessorService : IDocumentProcessorService
    {
        private readonly IExtractedDataRepository _extractedDataRepository;
        private readonly ILogger<DocumentProcessorService> _logger;

        public DocumentProcessorService(
            IExtractedDataRepository extractedDataRepository,
            ILogger<DocumentProcessorService> logger)
        {
            _extractedDataRepository = extractedDataRepository;
            _logger = logger;
        }

        public async Task<bool> ProcessDocumentAsync(string filePath, Core.Models.Task task)
        {
            _logger.LogInformation("Processing document: {Path} with task: {TaskId}", filePath, task.Id);

            try
            {
                // 获取文件类型
                string extension = Path.GetExtension(filePath).ToLowerInvariant();
                FileType fileType = DocumentParserFactory.GetFileTypeFromExtension(extension);

                // 筛选适用于此文件类型的规则
                var rules = task.ExtractRules
                    .Where(r => r.FileType == fileType || r.FileType == FileType.All)
                    .ToList();

                if (!rules.Any())
                {
                    _logger.LogInformation("No applicable rules found for file: {Path}", filePath);
                    return false;
                }

                // 创建适当的文档解析器
                var parser = DocumentParserFactory.CreateParser(filePath, _logger);

                // 提取数据
                var extractedData = await parser.ParseDocumentAsync(filePath, rules);
                bool anyDataProcessed = false;

                // 保存提取的数据
                foreach (var entry in extractedData)
                {
                    string fieldName = entry.Key;
                    string value = entry.Value;

                    // 找到对应的规则
                    var rule = rules.FirstOrDefault(r => r.FieldName == fieldName);
                    if (rule == null)
                    {
                        continue;
                    }

                    // 验证数据
                    bool isValid = await ValidateDataAsync(value, rule.ValidationRule);
                    string? validationMessage = isValid ? null : "Data failed validation";

                    // 创建提取数据记录
                    var data = new ExtractedData
                    {
                        FilePath = filePath,
                        TaskId = task.Id,
                        RuleId = rule.Id,
                        FieldName = fieldName,
                        Value = value,
                        ExtractedTime = DateTime.UtcNow,
                        IsValid = isValid,
                        ValidationMessage = validationMessage
                    };

                    // 保存数据
                    await _extractedDataRepository.AddAsync(data);
                    anyDataProcessed = true;

                    _logger.LogInformation("Extracted data from file: {Path}, Field: {Field}, Valid: {Valid}", 
                        filePath, fieldName, isValid);
                }

                return anyDataProcessed;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error processing document: {Path}", filePath);
                return false;
            }
        }

        public async Task<IDictionary<string, string>> ExtractDataFromDocumentAsync(string filePath, IEnumerable<DataExtractRule> rules)
        {
            _logger.LogInformation("Extracting data from document: {Path}", filePath);

            try
            {
                // 创建适当的文档解析器
                var parser = DocumentParserFactory.CreateParser(filePath, _logger);
                
                // 使用解析器提取数据
                return await parser.ParseDocumentAsync(filePath, rules);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error extracting data from document: {Path}", filePath);
                return new Dictionary<string, string>();
            }
        }

        public async Task<bool> ValidateDataAsync(string data, string validationRule)
        {
            if (string.IsNullOrEmpty(validationRule))
            {
                // 如果没有验证规则，默认为有效
                return true;
            }

            try
            {
                // 假设验证规则是正则表达式
                if (validationRule.StartsWith("regex:"))
                {
                    string pattern = validationRule.Substring(6);
                    return Regex.IsMatch(data, pattern);
                }
                
                // 非空验证
                if (validationRule == "notEmpty")
                {
                    return !string.IsNullOrWhiteSpace(data);
                }
                
                // 数字验证
                if (validationRule == "numeric")
                {
                    return double.TryParse(data, out _);
                }
                
                // 日期验证
                if (validationRule == "date")
                {
                    return DateTime.TryParse(data, out _);
                }

                // 可以扩展更多验证规则...

                _logger.LogWarning("Unknown validation rule: {Rule}", validationRule);
                return false;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error validating data with rule: {Rule}", validationRule);
                return false;
            }
        }
    }
}
