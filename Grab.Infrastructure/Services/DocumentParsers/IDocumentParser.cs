using Grab.Core.Models;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace Grab.Infrastructure.Services.DocumentParsers
{
    public interface IDocumentParser
    {
        /// <summary>
        /// 解析文档并根据指定规则提取数据
        /// </summary>
        /// <param name="filePath">文件路径</param>
        /// <param name="rules">提取规则</param>
        /// <returns>字段名称和值的字典</returns>
        Task<IDictionary<string, string>> ParseDocumentAsync(string filePath, IEnumerable<DataExtractRule> rules);
        
        /// <summary>
        /// 获取此解析器支持的文件类型
        /// </summary>
        FileType SupportedFileType { get; }
    }
}
