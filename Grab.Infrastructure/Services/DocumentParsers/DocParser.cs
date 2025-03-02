using Grab.Core.Models;
using Microsoft.Extensions.Logging;
using NPOI.HWPF;
using NPOI.HWPF.UserModel;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading.Tasks;

namespace Grab.Infrastructure.Services.DocumentParsers
{
    public class DocParser : IDocumentParser
    {
        private readonly ILogger _logger;

        public DocParser(ILogger logger)
        {
            _logger = logger;
        }

        public FileType SupportedFileType => FileType.Doc;

        public async Task<IDictionary<string, string>> ParseDocumentAsync(string filePath, IEnumerable<DataExtractRule> rules)
        {
            _logger.LogInformation("开始解析DOC文档: {Path}", filePath);
            var results = new Dictionary<string, string>();

            try
            {
                using (FileStream fs = new FileStream(filePath, FileMode.Open, FileAccess.Read))
                {
                    HWPFDocument doc = new HWPFDocument(fs);

                    foreach (var rule in rules)
                    {
                        if (rule.FileType != FileType.All && rule.FileType != FileType.Doc)
                            continue;

                        string value = await ExtractValueFromDocAsync(doc, rule);
                        results[rule.FieldName] = value;
                        _logger.LogDebug("从DOC文档提取数据: {Field}={Value}", rule.FieldName, value);
                    }
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "解析DOC文档时出错: {Path}", filePath);
            }

            return results;
        }

        private async Task<string> ExtractValueFromDocAsync(HWPFDocument doc, DataExtractRule rule)
        {
            try
            {
                // 提取位置格式: "paragraph:1" 或 "table:1,2,3" 或 "range:0,100" 或 "regex:pattern" 等
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
                    "range" => ExtractFromRange(doc, locationValue),
                    "all" => GetAllText(doc),
                    "regex" => ExtractWithRegex(doc, locationValue),
                    "section" => ExtractFromSection(doc, locationValue),
                    _ => string.Empty
                };
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "从DOC提取值时出错，规则: {Rule}", rule.FieldName);
                return string.Empty;
            }
        }

        private string ExtractFromParagraph(HWPFDocument doc, string locationValue)
        {
            if (!int.TryParse(locationValue, out int paragraphIndex) || paragraphIndex <= 0)
                return string.Empty;

            Range range = doc.GetRange();
            int numParagraphs = range.NumParagraphs;

            if (paragraphIndex > numParagraphs)
                return string.Empty;

            Paragraph paragraph = range.GetParagraph(paragraphIndex - 1); // 转为0索引
            return paragraph.Text;
        }

        private string ExtractFromTable(HWPFDocument doc, string locationValue)
        {
            // 格式: "1,2,3" 表示第1个表格的第2行第3列
            string[] indices = locationValue.Split(',');
            if (indices.Length != 3 || 
                !int.TryParse(indices[0], out int tableIndex) || 
                !int.TryParse(indices[1], out int rowIndex) || 
                !int.TryParse(indices[2], out int colIndex))
                return string.Empty;

            if (tableIndex <= 0 || rowIndex <= 0 || colIndex <= 0)
                return string.Empty;

            Range range = doc.GetRange();
            int numTables = 0;
            
            for (int i = 0; i < range.NumParagraphs; i++)
            {
                Paragraph p = range.GetParagraph(i);
                if (p.IsInTable())
                {
                    Table table = range.GetTable(p);
                    numTables++;
                    
                    if (numTables == tableIndex)
                    {
                        if (rowIndex <= table.NumRows)
                        {
                            TableRow row = table.GetRow(rowIndex - 1); // 转为0索引
                            
                            if (colIndex <= row.NumCells)
                            {
                                TableCell cell = row.GetCell(colIndex - 1); // 转为0索引
                                return cell.Text;
                            }
                        }
                        
                        break;
                    }
                    
                    // 跳过此表格的所有段落
                    i += table.NumParagraphs - 1;
                }
            }

            return string.Empty;
        }

        private string ExtractFromRange(HWPFDocument doc, string locationValue)
        {
            // 格式: "0,100" 表示从第0个字符到第100个字符
            string[] indices = locationValue.Split(',');
            if (indices.Length != 2 || 
                !int.TryParse(indices[0], out int startIndex) || 
                !int.TryParse(indices[1], out int endIndex))
                return string.Empty;

            if (startIndex < 0 || endIndex < startIndex)
                return string.Empty;

            string text = doc.GetText();
            
            if (startIndex >= text.Length)
                return string.Empty;
                
            endIndex = Math.Min(endIndex, text.Length - 1);
            
            return text.Substring(startIndex, endIndex - startIndex + 1);
        }

        private string GetAllText(HWPFDocument doc)
        {
            return doc.GetText();
        }

        private string ExtractWithRegex(HWPFDocument doc, string pattern)
        {
            string text = doc.GetText();
            
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

        private string ExtractFromSection(HWPFDocument doc, string locationValue)
        {
            if (!int.TryParse(locationValue, out int sectionIndex) || sectionIndex < 0)
                return string.Empty;

            if (sectionIndex >= doc.GetSectionTable().GetSections().Count)
                return string.Empty;

            Range range = doc.GetRange();
            StringBuilder text = new StringBuilder();
            int currentSection = 0;
            
            for (int i = 0; i < range.NumParagraphs; i++)
            {
                Paragraph p = range.GetParagraph(i);
                
                if (p.IsInTable()) 
                {
                    // 跳过表格
                    Table table = range.GetTable(p);
                    i += table.NumParagraphs - 1;
                    continue;
                }
                
                if (p.GetSectionNumber() == sectionIndex)
                {
                    text.Append(p.Text);
                }
                else if (currentSection < sectionIndex && p.GetSectionNumber() > currentSection)
                {
                    currentSection = p.GetSectionNumber();
                }
                else if (p.GetSectionNumber() > sectionIndex)
                {
                    // 已经超过了目标部分
                    break;
                }
            }

            return text.ToString();
        }
    }
}
