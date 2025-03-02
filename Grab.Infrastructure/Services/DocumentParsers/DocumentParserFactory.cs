using Grab.Core.Models;
using Microsoft.Extensions.Logging;
using System;
using System.IO;

namespace Grab.Infrastructure.Services.DocumentParsers
{
    public static class DocumentParserFactory
    {
        public static IDocumentParser CreateParser(string filePath, ILogger logger)
        {
            string extension = Path.GetExtension(filePath).ToLowerInvariant();
            
            return extension switch
            {
                ".docx" => new DocxParser(logger),
                ".doc" => new DocParser(logger),
                ".xlsx" => new XlsxParser(logger),
                ".xls" => new XlsParser(logger),
                _ => throw new NotSupportedException($"文件类型不支持: {extension}")
            };
        }

        public static FileType GetFileTypeFromExtension(string extension)
        {
            return extension.ToLowerInvariant() switch
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
