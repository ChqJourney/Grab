using Grab.Core.Models;
using Microsoft.Extensions.Logging;
using NPOI.HSSF.UserModel;
using NPOI.SS.UserModel;
using System;
using System.Collections.Generic;
using System.IO;
using System.Text.RegularExpressions;
using System.Threading.Tasks;

namespace Grab.Infrastructure.Services.DocumentParsers
{
    public class XlsParser : IDocumentParser
    {
        private readonly ILogger _logger;

        public XlsParser(ILogger logger)
        {
            _logger = logger;
        }

        public FileType SupportedFileType => FileType.Xls;

        public async Task<IDictionary<string, string>> ParseDocumentAsync(string filePath, IEnumerable<DataExtractRule> rules)
        {
            _logger.LogInformation("开始解析XLS文档: {Path}", filePath);
            var results = new Dictionary<string, string>();

            try
            {
                using (FileStream fs = new FileStream(filePath, FileMode.Open, FileAccess.Read))
                {
                    HSSFWorkbook workbook = new HSSFWorkbook(fs);

                    foreach (var rule in rules)
                    {
                        if (rule.FileType != FileType.All && rule.FileType != FileType.Xls)
                            continue;

                        string value = await ExtractValueFromXlsAsync(workbook, rule);
                        results[rule.FieldName] = value;
                        _logger.LogDebug("从XLS文档提取数据: {Field}={Value}", rule.FieldName, value);
                    }
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "解析XLS文档时出错: {Path}", filePath);
            }

            return results;
        }

        private async Task<string> ExtractValueFromXlsAsync(HSSFWorkbook workbook, DataExtractRule rule)
        {
            try
            {
                // 提取位置格式: "cell:Sheet1!A1" 或 "sheet:1,A1" 或 "sheet:1,2,3" 或 "range:Sheet1!A1:B5" 等
                if (string.IsNullOrEmpty(rule.Location))
                    return string.Empty;

                string[] locationParts = rule.Location.Split(':');
                if (locationParts.Length != 2)
                    return string.Empty;

                string locationType = locationParts[0].ToLowerInvariant();
                string locationValue = locationParts[1];

                return locationType switch
                {
                    "cell" => ExtractFromCell(workbook, locationValue),
                    "sheet" => ExtractFromSheet(workbook, locationValue),
                    "range" => ExtractFromRange(workbook, locationValue),
                    "property" => ExtractFromDocumentProperty(workbook, locationValue),
                    "formula" => ExtractFromFormula(workbook, locationValue),
                    "regex" => ExtractWithRegex(workbook, locationValue),
                    _ => string.Empty
                };
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "从XLS提取值时出错，规则: {Rule}", rule.FieldName);
                return string.Empty;
            }
        }

        private string ExtractFromCell(HSSFWorkbook workbook, string locationValue)
        {
            // 格式: "Sheet1!A1" 或 "A1"（默认第一个工作表）
            string sheetName = string.Empty;
            string cellReference = locationValue;

            // 如果包含工作表名
            if (locationValue.Contains('!'))
            {
                string[] parts = locationValue.Split('!');
                sheetName = parts[0];
                cellReference = parts[1];
            }

            // 获取工作表
            ISheet sheet;
            if (string.IsNullOrEmpty(sheetName))
            {
                // 获取第一个工作表
                sheet = workbook.GetSheetAt(0);
            }
            else
            {
                // 根据名称获取工作表
                sheet = workbook.GetSheet(sheetName);
            }

            if (sheet == null)
                return string.Empty;

            return GetCellValue(sheet, cellReference);
        }

        private string ExtractFromSheet(HSSFWorkbook workbook, string locationValue)
        {
            // 格式: "1,A1" 表示第1个工作表的A1单元格
            // 或者 "1,2,3" 表示第1个工作表的第2行第3列
            string[] parts = locationValue.Split(',');
            
            if (parts.Length < 2)
                return string.Empty;

            if (!int.TryParse(parts[0], out int sheetIndex) || sheetIndex <= 0)
                return string.Empty;

            // 转为0索引并确保索引有效
            sheetIndex--;
            if (sheetIndex < 0 || sheetIndex >= workbook.NumberOfSheets)
                return string.Empty;

            // 获取指定工作表
            ISheet sheet = workbook.GetSheetAt(sheetIndex);

            if (parts.Length == 2)
            {
                // 使用单元格引用格式 (例如 A1)
                return GetCellValue(sheet, parts[1]);
            }
            else if (parts.Length >= 3 && int.TryParse(parts[1], out int rowIndex) && 
                     int.TryParse(parts[2], out int colIndex) && rowIndex > 0 && colIndex > 0)
            {
                // 使用行列索引格式（转为0索引）
                rowIndex--;
                colIndex--;
                
                IRow? row = sheet.GetRow(rowIndex);
                if (row == null)
                    return string.Empty;
                
                ICell? cell = row.GetCell(colIndex);
                if (cell == null)
                    return string.Empty;
                
                return GetCellValueInternal(cell);
            }

            return string.Empty;
        }

        private string ExtractFromRange(HSSFWorkbook workbook, string locationValue)
        {
            // 格式: "Sheet1!A1:B3" 或 "1,A1:B3"
            string sheetName = string.Empty;
            string rangeReference = string.Empty;
            int sheetIndex = -1;

            if (locationValue.Contains('!'))
            {
                // 使用工作表名称
                string[] parts = locationValue.Split('!');
                sheetName = parts[0];
                rangeReference = parts[1];
            }
            else if (locationValue.Contains(','))
            {
                // 使用工作表索引
                string[] parts = locationValue.Split(',', 2);
                
                if (!int.TryParse(parts[0], out sheetIndex) || sheetIndex <= 0)
                    return string.Empty;
                
                // 转为0索引
                sheetIndex--;
                rangeReference = parts[1];
            }
            else
            {
                return string.Empty;
            }

            // 获取工作表
            ISheet sheet;
            if (!string.IsNullOrEmpty(sheetName))
            {
                sheet = workbook.GetSheet(sheetName);
            }
            else if (sheetIndex >= 0 && sheetIndex < workbook.NumberOfSheets)
            {
                sheet = workbook.GetSheetAt(sheetIndex);
            }
            else
            {
                return string.Empty;
            }

            // 解析单元格范围
            if (!ParseCellRange(rangeReference, out CellRangeAddress range))
                return string.Empty;

            // 提取单元格范围内的所有值
            var values = new List<string>();
            
            for (int rowIndex = range.FirstRow; rowIndex <= range.LastRow; rowIndex++)
            {
                IRow? row = sheet.GetRow(rowIndex);
                if (row == null) continue;
                
                for (int colIndex = range.FirstColumn; colIndex <= range.LastColumn; colIndex++)
                {
                    ICell? cell = row.GetCell(colIndex);
                    if (cell == null) continue;
                    
                    string value = GetCellValueInternal(cell);
                    if (!string.IsNullOrEmpty(value))
                    {
                        values.Add(value);
                    }
                }
            }

            // 合并为一个字符串返回
            return string.Join(", ", values);
        }

        private string ExtractFromDocumentProperty(HSSFWorkbook workbook, string propertyName)
        {
            var summaryInformation = workbook.SummaryInformation;
            if (summaryInformation == null)
                return string.Empty;

            return propertyName.ToLowerInvariant() switch
            {
                "title" => summaryInformation.Title ?? string.Empty,
                "subject" => summaryInformation.Subject ?? string.Empty,
                "author" => summaryInformation.Author ?? string.Empty,
                "keywords" => summaryInformation.Keywords ?? string.Empty,
                "comments" => summaryInformation.Comments ?? string.Empty,
                "template" => summaryInformation.Template ?? string.Empty,
                "lastauthor" => summaryInformation.LastAuthor ?? string.Empty,
                "revision" => summaryInformation.RevNumber ?? string.Empty,
                "applicationname" => summaryInformation.ApplicationName ?? string.Empty,
                "createtime" => summaryInformation.CreateDateTime?.ToString() ?? string.Empty,
                "lastsaved" => summaryInformation.LastSaveDateTime?.ToString() ?? string.Empty,
                "totaleditingtime" => summaryInformation.EditTime.ToString() ?? string.Empty,
                "security" => summaryInformation.Security.ToString() ?? string.Empty,
                _ => string.Empty
            };
        }

        private string ExtractFromFormula(HSSFWorkbook workbook, string locationValue)
        {
            // 格式: "Sheet1!A1" 或 "1,A1"
            return ExtractFromCell(workbook, locationValue);
        }

        private string ExtractWithRegex(HSSFWorkbook workbook, string pattern)
        {
            try
            {
                // 获取所有工作表的文本
                var allText = new List<string>();
                
                for (int i = 0; i < workbook.NumberOfSheets; i++)
                {
                    ISheet sheet = workbook.GetSheetAt(i);
                    
                    for (int rowIndex = 0; rowIndex <= sheet.LastRowNum; rowIndex++)
                    {
                        IRow? row = sheet.GetRow(rowIndex);
                        if (row == null) continue;
                        
                        for (int colIndex = 0; colIndex < row.LastCellNum; colIndex++)
                        {
                            ICell? cell = row.GetCell(colIndex);
                            if (cell == null) continue;
                            
                            string cellValue = GetCellValueInternal(cell);
                            if (!string.IsNullOrEmpty(cellValue))
                            {
                                allText.Add(cellValue);
                            }
                        }
                    }
                }

                string text = string.Join(" ", allText);
                
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

        private string GetCellValue(ISheet sheet, string cellReference)
        {
            if (!TryParseCellReference(cellReference, out int rowIndex, out int colIndex))
                return string.Empty;
            
            // 获取行
            IRow? row = sheet.GetRow(rowIndex);
            if (row == null)
                return string.Empty;
            
            // 获取单元格
            ICell? cell = row.GetCell(colIndex);
            if (cell == null)
                return string.Empty;
            
            return GetCellValueInternal(cell);
        }

        private string GetCellValueInternal(ICell cell)
        {
            return cell.CellType switch
            {
                CellType.String => cell.StringCellValue,
                CellType.Numeric => cell.DateCellValue.ToString(),
                CellType.Boolean => cell.BooleanCellValue.ToString(),
                CellType.Formula => GetFormulaValue(cell),
                _ => string.Empty
            };
        }

        private string GetFormulaValue(ICell cell)
        {
            try
            {
                return cell.CachedFormulaResultType switch
                {
                    CellType.String => cell.StringCellValue,
                    CellType.Numeric => cell.NumericCellValue.ToString(),
                    CellType.Boolean => cell.BooleanCellValue.ToString(),
                    _ => string.Empty
                };
            }
            catch
            {
                return cell.CellFormula;
            }
        }

        private bool TryParseCellReference(string cellReference, out int rowIndex, out int colIndex)
        {
            rowIndex = -1;
            colIndex = -1;

            // 例如：从 "A1" 提取 "A" 和 1
            Match match = Regex.Match(cellReference, @"([A-Z]+)(\d+)");
            if (!match.Success || match.Groups.Count < 3)
                return false;
            
            string columnName = match.Groups[1].Value;
            
            if (!int.TryParse(match.Groups[2].Value, out int row))
                return false;
            
            // 行是1索引的，需要转为0索引
            rowIndex = row - 1;
            
            // 将列名转为列索引
            colIndex = 0;
            for (int i = 0; i < columnName.Length; i++)
            {
                colIndex = colIndex * 26 + (columnName[i] - 'A' + 1);
            }
            colIndex--; // 转为0索引
            
            return rowIndex >= 0 && colIndex >= 0;
        }

        private bool ParseCellRange(string rangeReference, out CellRangeAddress cellRange)
        {
            cellRange = null!;
            
            // 分割范围引用，例如 "A1:B3"
            string[] rangeParts = rangeReference.Split(':');
            if (rangeParts.Length != 2)
                return false;
            
            // 解析开始单元格
            if (!TryParseCellReference(rangeParts[0], out int startRow, out int startCol))
                return false;
            
            // 解析结束单元格
            if (!TryParseCellReference(rangeParts[1], out int endRow, out int endCol))
                return false;
            
            cellRange = new CellRangeAddress(startRow, endRow, startCol, endCol);
            return true;
        }
    }
}
