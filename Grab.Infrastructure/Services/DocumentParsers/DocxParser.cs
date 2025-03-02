using DocumentFormat.OpenXml.Packaging;
using DocumentFormat.OpenXml.Wordprocessing;
using Grab.Core.Models;
using Microsoft.Extensions.Logging;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading.Tasks;

namespace Grab.Infrastructure.Services.DocumentParsers
{
    public class DocxParser : IDocumentParser
    {
        private readonly ILogger _logger;

        public DocxParser(ILogger logger)
        {
            _logger = logger;
        }

        public FileType SupportedFileType => FileType.Docx;

        public async Task<IDictionary<string, string>> ParseDocumentAsync(string filePath, IEnumerable<DataExtractRule> rules)
        {
            _logger.LogInformation("开始解析DOCX文档: {Path}", filePath);
            var results = new Dictionary<string, string>();

            try
            {
                using (WordprocessingDocument doc = WordprocessingDocument.Open(filePath, false))
                {
                    var mainPart = doc.MainDocumentPart;
                    if (mainPart == null || mainPart.Document == null)
                    {
                        _logger.LogWarning("DOCX文档结构无效: {Path}", filePath);
                        return results;
                    }

                    var body = mainPart.Document.Body;
                    if (body == null)
                    {
                        _logger.LogWarning("DOCX文档没有正文: {Path}", filePath);
                        return results;
                    }

                    foreach (var rule in rules)
                    {
                        if (rule.FileType != FileType.All && rule.FileType != FileType.Docx)
                            continue;

                        string value = await ExtractValueFromDocxAsync(doc, rule);
                        results[rule.FieldName] = value;
                        _logger.LogDebug("从DOCX文档提取数据: {Field}={Value}", rule.FieldName, value);
                    }
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "解析DOCX文档时出错: {Path}", filePath);
            }

            return results;
        }

        private async Task<string> ExtractValueFromDocxAsync(WordprocessingDocument doc, DataExtractRule rule)
        {
            try
            {
                // 提取位置格式: "paragraph:1" 或 "table:1,2,3" 或 "regex:pattern" 等
                if (string.IsNullOrEmpty(rule.Location))
                    return string.Empty;

                string[] locationParts = rule.Location.Split(':');
                if (locationParts.Length != 2)
                    return string.Empty;

                string locationType = locationParts[0].ToLowerInvariant();
                string locationValue = locationParts[1];

                return locationType switch
                {
                    "paragraph" => ExtractFromParagraph(doc, locationValue),
                    "table" => ExtractFromTable(doc, locationValue),
                    "header" => ExtractFromHeader(doc, locationValue),
                    "footer" => ExtractFromFooter(doc, locationValue),
                    "regex" => ExtractWithRegex(doc, locationValue),
                    "property" => ExtractFromDocumentProperty(doc, locationValue),
                    "xpath" => ExtractWithXPath(doc, locationValue),
                    _ => string.Empty
                };
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "从DOCX提取值时出错，规则: {Rule}", rule.FieldName);
                return string.Empty;
            }
        }

        private string ExtractFromParagraph(WordprocessingDocument doc, string locationValue)
        {
            if (!int.TryParse(locationValue, out int paragraphIndex) || paragraphIndex <= 0)
                return string.Empty;

            var body = doc.MainDocumentPart?.Document.Body;
            if (body == null) return string.Empty;

            // 获取所有段落
            var paragraphs = body.Elements<Paragraph>().ToList();
            if (paragraphIndex > paragraphs.Count)
                return string.Empty;

            // 提取指定段落的文本
            var paragraph = paragraphs[paragraphIndex - 1]; // 索引从1开始
            return paragraph.InnerText;
        }

        private string ExtractFromTable(WordprocessingDocument doc, string locationValue)
        {
            // 格式: "table:1,2,3" 表示第1个表格的第2行第3列
            string[] indices = locationValue.Split(',');
            if (indices.Length != 3 || 
                !int.TryParse(indices[0], out int tableIndex) || 
                !int.TryParse(indices[1], out int rowIndex) || 
                !int.TryParse(indices[2], out int colIndex))
                return string.Empty;

            if (tableIndex <= 0 || rowIndex <= 0 || colIndex <= 0)
                return string.Empty;

            var body = doc.MainDocumentPart?.Document.Body;
            if (body == null) return string.Empty;

            // 获取所有表格
            var tables = body.Elements<Table>().ToList();
            if (tableIndex > tables.Count)
                return string.Empty;

            var table = tables[tableIndex - 1]; // 索引从1开始

            // 获取指定行
            var rows = table.Elements<TableRow>().ToList();
            if (rowIndex > rows.Count)
                return string.Empty;

            var row = rows[rowIndex - 1]; // 索引从1开始

            // 获取指定列
            var cells = row.Elements<TableCell>().ToList();
            if (colIndex > cells.Count)
                return string.Empty;

            var cell = cells[colIndex - 1]; // 索引从1开始
            return cell.InnerText;
        }

        private string ExtractFromHeader(WordprocessingDocument doc, string locationValue)
        {
            var headers = doc.MainDocumentPart?.HeaderParts;
            if (headers == null || !headers.Any())
                return string.Empty;

            // 简单地获取第一个页眉
            var header = headers.First().Header;
            if (header == null)
                return string.Empty;

            // 如果locationValue指定了段落索引
            if (int.TryParse(locationValue, out int paragraphIndex) && paragraphIndex > 0)
            {
                var paragraphs = header.Elements<Paragraph>().ToList();
                if (paragraphIndex <= paragraphs.Count)
                {
                    return paragraphs[paragraphIndex - 1].InnerText;
                }
                return string.Empty;
            }

            // 否则返回整个页眉内容
            return header.InnerText;
        }

        private string ExtractFromFooter(WordprocessingDocument doc, string locationValue)
        {
            var footers = doc.MainDocumentPart?.FooterParts;
            if (footers == null || !footers.Any())
                return string.Empty;

            // 简单地获取第一个页脚
            var footer = footers.First().Footer;
            if (footer == null)
                return string.Empty;

            // 如果locationValue指定了段落索引
            if (int.TryParse(locationValue, out int paragraphIndex) && paragraphIndex > 0)
            {
                var paragraphs = footer.Elements<Paragraph>().ToList();
                if (paragraphIndex <= paragraphs.Count)
                {
                    return paragraphs[paragraphIndex - 1].InnerText;
                }
                return string.Empty;
            }

            // 否则返回整个页脚内容
            return footer.InnerText;
        }

        private string ExtractWithRegex(WordprocessingDocument doc, string pattern)
        {
            var body = doc.MainDocumentPart?.Document.Body;
            if (body == null) return string.Empty;

            string text = body.InnerText;
            try
            {
                // 使用正则表达式匹配
                Match match = Regex.Match(text, pattern);
                if (match.Success)
                {
                    // 如果存在捕获组，返回第一个捕获组的值，否则返回整个匹配
                    return match.Groups.Count > 1 ? match.Groups[1].Value : match.Value;
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "正则表达式匹配时出错: {Pattern}", pattern);
            }

            return string.Empty;
        }

        private string ExtractFromDocumentProperty(WordprocessingDocument doc, string propertyName)
        {
            var props = doc.PackageProperties;
            if (props == null)
                return string.Empty;

            return propertyName.ToLowerInvariant() switch
            {
                "title" => props.Title ?? string.Empty,
                "subject" => props.Subject ?? string.Empty,
                "creator" => props.Creator ?? string.Empty,
                "keywords" => props.Keywords ?? string.Empty,
                "description" => props.Description ?? string.Empty,
                "category" => props.Category ?? string.Empty,
                "created" => props.Created?.ToString() ?? string.Empty,
                "modified" => props.Modified?.ToString() ?? string.Empty,
                "lastmodifiedby" => props.LastModifiedBy ?? string.Empty,
                "revision" => props.Revision ?? string.Empty,
                "contentstatus" => props.ContentStatus ?? string.Empty,
                _ => string.Empty
            };
        }

        private string ExtractWithXPath(WordprocessingDocument doc, string xpath)
        {
            try
            {
                // 为了保持实现简单，这里提供一个有限的XPath功能子集
                var body = doc.MainDocumentPart?.Document.Body;
                if (body == null) return string.Empty;

                // 简单的XPath解析
                if (xpath == "/document/body/text()")
                {
                    return body.InnerText;
                }
                else if (xpath.StartsWith("/document/body/paragraph[") && xpath.EndsWith("]/text()"))
                {
                    string indexPart = xpath.Substring("/document/body/paragraph[".Length, 
                        xpath.Length - "/document/body/paragraph[".Length - "]/text()".Length);
                    
                    if (int.TryParse(indexPart, out int index) && index > 0)
                    {
                        var paragraphs = body.Elements<Paragraph>().ToList();
                        if (index <= paragraphs.Count)
                        {
                            return paragraphs[index - 1].InnerText;
                        }
                    }
                }
                // 可以扩展更多XPath模式...
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "XPath提取出错: {XPath}", xpath);
            }

            return string.Empty;
        }
    }
}
