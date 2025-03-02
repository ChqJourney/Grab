using DocumentFormat.OpenXml.Packaging;
using DocumentFormat.OpenXml.Spreadsheet;
using Grab.Core.Models;
using Microsoft.Extensions.Logging;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.RegularExpressions;
using System.Threading.Tasks;

namespace Grab.Infrastructure.Services.DocumentParsers
{
    public class XlsxParser : IDocumentParser
    {
        private readonly ILogger _logger;

        public XlsxParser(ILogger logger)
        {
            _logger = logger;
        }

        public FileType SupportedFileType => FileType.Xlsx;

        public async Task<IDictionary<string, string>> ParseDocumentAsync(string filePath, IEnumerable<DataExtractRule> rules)
        {
            _logger.LogInformation("开始解析XLSX文档: {Path}", filePath);
            var results = new Dictionary<string, string>();

            try
            {
                using (SpreadsheetDocument spreadsheet = SpreadsheetDocument.Open(filePath, false))
                {
                    WorkbookPart? workbookPart = spreadsheet.WorkbookPart;
                    if (workbookPart == null)
                    {
                        _logger.LogWarning("XLSX文档结构无效: {Path}", filePath);
                        return results;
                    }

                    SharedStringTablePart? sharedStringTablePart = workbookPart.SharedStringTablePart;
                    SharedStringTable? sharedStringTable = sharedStringTablePart?.SharedStringTable;

                    foreach (var rule in rules)
                    {
                        if (rule.FileType != FileType.All && rule.FileType != FileType.Xlsx)
                            continue;

                        string value = await ExtractValueFromXlsxAsync(spreadsheet, workbookPart, sharedStringTable, rule);
                        results[rule.FieldName] = value;
                        _logger.LogDebug("从XLSX文档提取数据: {Field}={Value}", rule.FieldName, value);
                    }
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "解析XLSX文档时出错: {Path}", filePath);
            }

            return results;
        }

        private async Task<string> ExtractValueFromXlsxAsync(
            SpreadsheetDocument spreadsheet, 
            WorkbookPart workbookPart, 
            SharedStringTable? sharedStringTable, 
            DataExtractRule rule)
        {
            try
            {
                // 提取位置格式: "cell:Sheet1!A1" 或 "sheet:1,A1" 或 "sheet:1,2,3" 或 "regex:pattern" 等
                if (string.IsNullOrEmpty(rule.Location))
                    return string.Empty;

                string[] locationParts = rule.Location.Split(':');
                if (locationParts.Length != 2)
                    return string.Empty;

                string locationType = locationParts[0].ToLowerInvariant();
                string locationValue = locationParts[1];

                return locationType switch
                {
                    "cell" => ExtractFromCell(workbookPart, sharedStringTable, locationValue),
                    "sheet" => ExtractFromSheet(workbookPart, sharedStringTable, locationValue),
                    "range" => ExtractFromRange(workbookPart, sharedStringTable, locationValue),
                    "property" => ExtractFromDocumentProperty(spreadsheet, locationValue),
                    "regex" => ExtractWithRegex(workbookPart, locationValue),
                    _ => string.Empty
                };
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "从XLSX提取值时出错，规则: {Rule}", rule.FieldName);
                return string.Empty;
            }
        }

        private string ExtractFromCell(WorkbookPart workbookPart, SharedStringTable? sharedStringTable, string locationValue)
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
            Sheet? sheet;
            if (string.IsNullOrEmpty(sheetName))
            {
                // 获取第一个工作表
                sheet = workbookPart.Workbook.Descendants<Sheet>().FirstOrDefault();
            }
            else
            {
                // 根据名称获取工作表
                sheet = workbookPart.Workbook.Descendants<Sheet>()
                    .FirstOrDefault(s => s.Name?.Value == sheetName);
            }

            if (sheet == null)
                return string.Empty;

            return GetCellValue(workbookPart, sharedStringTable, sheet, cellReference);
        }

        private string ExtractFromSheet(WorkbookPart workbookPart, SharedStringTable? sharedStringTable, string locationValue)
        {
            // 格式: "1,A1" 表示第1个工作表的A1单元格
            // 或者 "1,2,3" 表示第1个工作表的第2行第3列
            string[] parts = locationValue.Split(',');
            
            if (parts.Length < 2)
                return string.Empty;

            if (!int.TryParse(parts[0], out int sheetIndex) || sheetIndex <= 0)
                return string.Empty;

            // 获取指定工作表
            var sheets = workbookPart.Workbook.Descendants<Sheet>().ToList();
            if (sheetIndex > sheets.Count)
                return string.Empty;

            var sheet = sheets[sheetIndex - 1]; // 索引从1开始

            if (parts.Length == 2)
            {
                // 使用单元格引用格式 (例如 A1)
                return GetCellValue(workbookPart, sharedStringTable, sheet, parts[1]);
            }
            else if (parts.Length >= 3 && int.TryParse(parts[1], out int rowIndex) && 
                     int.TryParse(parts[2], out int colIndex) && rowIndex > 0 && colIndex > 0)
            {
                // 使用行列索引格式
                string columnName = GetExcelColumnName(colIndex);
                string cellReference = $"{columnName}{rowIndex}";
                return GetCellValue(workbookPart, sharedStringTable, sheet, cellReference);
            }

            return string.Empty;
        }

        private string ExtractFromRange(WorkbookPart workbookPart, SharedStringTable? sharedStringTable, string locationValue)
        {
            // 格式: "Sheet1!A1:B3" 或 "1,A1:B3"
            string sheetId;
            string rangeReference;

            if (locationValue.Contains('!'))
            {
                // 使用工作表名称
                string[] parts = locationValue.Split('!');
                sheetId = parts[0];
                rangeReference = parts[1];

                // 根据名称查找工作表
                var sheet = workbookPart.Workbook.Descendants<Sheet>()
                    .FirstOrDefault(s => s.Name?.Value == sheetId);
                
                if (sheet == null)
                    return string.Empty;
                
                sheetId = sheet.Id!.Value;
            }
            else if (locationValue.Contains(','))
            {
                // 使用工作表索引
                string[] parts = locationValue.Split(',', 2);
                
                if (!int.TryParse(parts[0], out int sheetIndex) || sheetIndex <= 0)
                    return string.Empty;
                
                rangeReference = parts[1];
                
                // 获取对应索引的工作表
                var sheets = workbookPart.Workbook.Descendants<Sheet>().ToList();
                if (sheetIndex > sheets.Count)
                    return string.Empty;
                
                var sheet = sheets[sheetIndex - 1]; // 索引从1开始
                sheetId = sheet.Id!.Value;
            }
            else
            {
                return string.Empty;
            }

            // 解析单元格范围
            if (!ParseCellRange(rangeReference, out string startCell, out string endCell))
                return string.Empty;

            // 获取工作表部分
            WorksheetPart worksheetPart = (WorksheetPart)workbookPart.GetPartById(sheetId);
            Worksheet worksheet = worksheetPart.Worksheet;
            SheetData sheetData = worksheet.GetFirstChild<SheetData>()!;

            // 提取单元格范围内的所有值
            var values = new List<string>();
            
            foreach (var row in sheetData.Elements<Row>())
            {
                foreach (var cell in row.Elements<Cell>())
                {
                    if (cell.CellReference != null && 
                        IsCellInRange(cell.CellReference.Value, startCell, endCell))
                    {
                        string value = GetCellValueInternal(cell, sharedStringTable);
                        if (!string.IsNullOrEmpty(value))
                        {
                            values.Add(value);
                        }
                    }
                }
            }

            // 合并为一个字符串返回
            return string.Join(", ", values);
        }

        private string ExtractFromDocumentProperty(SpreadsheetDocument spreadsheet, string propertyName)
        {
            var props = spreadsheet.PackageProperties;
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

        private string ExtractWithRegex(WorkbookPart workbookPart, string pattern)
        {
            try
            {
                // 获取所有工作表的文本
                var allText = new List<string>();
                
                foreach (var sheet in workbookPart.Workbook.Descendants<Sheet>())
                {
                    WorksheetPart worksheetPart = (WorksheetPart)workbookPart.GetPartById(sheet.Id!);
                    Worksheet worksheet = worksheetPart.Worksheet;
                    SheetData sheetData = worksheet.GetFirstChild<SheetData>()!;
                    
                    foreach (var row in sheetData.Elements<Row>())
                    {
                        foreach (var cell in row.Elements<Cell>())
                        {
                            if (cell.CellValue != null)
                            {
                                allText.Add(cell.CellValue.Text);
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

        private string GetCellValue(
            WorkbookPart workbookPart, 
            SharedStringTable? sharedStringTable, 
            Sheet sheet, 
            string cellReference)
        {
            // 获取工作表部分
            WorksheetPart worksheetPart = (WorksheetPart)workbookPart.GetPartById(sheet.Id!);
            Worksheet worksheet = worksheetPart.Worksheet;
            SheetData sheetData = worksheet.GetFirstChild<SheetData>()!;

            // 查找单元格
            foreach (var row in sheetData.Elements<Row>())
            {
                var cell = row.Elements<Cell>()
                    .FirstOrDefault(c => c.CellReference?.Value == cellReference);
                
                if (cell != null)
                {
                    return GetCellValueInternal(cell, sharedStringTable);
                }
            }

            return string.Empty;
        }

        private string GetCellValueInternal(Cell cell, SharedStringTable? sharedStringTable)
        {
            if (cell.CellValue == null)
                return string.Empty;

            string value = cell.CellValue.Text;

            // 如果单元格包含共享字符串引用
            if (cell.DataType != null && cell.DataType.Value == CellValues.SharedString && 
                sharedStringTable != null && int.TryParse(value, out int ssid) && 
                ssid >= 0 && ssid < sharedStringTable.ChildElements.Count)
            {
                value = sharedStringTable.ChildElements[ssid].InnerText;
            }

            return value;
        }

        private bool ParseCellRange(string range, out string startCell, out string endCell)
        {
            startCell = endCell = string.Empty;
            
            string[] parts = range.Split(':');
            if (parts.Length != 2)
                return false;
            
            startCell = parts[0];
            endCell = parts[1];
            
            return !string.IsNullOrEmpty(startCell) && !string.IsNullOrEmpty(endCell);
        }

        private bool IsCellInRange(string cellReference, string startCell, string endCell)
        {
            // 解析单元格引用中的列和行
            if (!TryParseCellReference(cellReference, out string cellColumn, out int cellRow) ||
                !TryParseCellReference(startCell, out string startColumn, out int startRow) ||
                !TryParseCellReference(endCell, out string endColumn, out int endRow))
            {
                return false;
            }

            // 比较列（转换为数字进行比较）
            int cellColumnIndex = GetColumnIndex(cellColumn);
            int startColumnIndex = GetColumnIndex(startColumn);
            int endColumnIndex = GetColumnIndex(endColumn);

            // 比较行和列
            return cellRow >= startRow && cellRow <= endRow && 
                   cellColumnIndex >= startColumnIndex && cellColumnIndex <= endColumnIndex;
        }

        private bool TryParseCellReference(string cellReference, out string column, out int row)
        {
            column = string.Empty;
            row = 0;

            // 例如：从 "A1" 提取 "A" 和 1
            Match match = Regex.Match(cellReference, @"([A-Z]+)(\d+)");
            if (match.Success && match.Groups.Count >= 3)
            {
                column = match.Groups[1].Value;
                return int.TryParse(match.Groups[2].Value, out row);
            }

            return false;
        }

        private int GetColumnIndex(string column)
        {
            int result = 0;
            
            for (int i = 0; i < column.Length; i++)
            {
                result = result * 26 + (column[i] - 'A' + 1);
            }
            
            return result;
        }

        private string GetExcelColumnName(int columnNumber)
        {
            string columnName = string.Empty;
            
            while (columnNumber > 0)
            {
                int remainder = (columnNumber - 1) % 26;
                columnName = Convert.ToChar('A' + remainder) + columnName;
                columnNumber = (columnNumber - 1) / 26;
            }
            
            return columnName;
        }
    }
}
