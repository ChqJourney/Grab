using Grab.Core.Models;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace Grab.Core.Interfaces
{
    public interface IDocumentProcessorService
    {
        Task<bool> ProcessDocumentAsync(string filePath, Models.Task task);
        Task<IDictionary<string, string>> ExtractDataFromDocumentAsync(string filePath, IEnumerable<DataExtractRule> rules);
        Task<bool> ValidateDataAsync(string data, string validationRule);
    }
}
