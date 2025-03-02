using Grab.Core.Models;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace Grab.Core.Interfaces
{
    public interface IExtractedDataRepository
    {
        Task<ExtractedData?> GetByIdAsync(int id);
        Task<IEnumerable<ExtractedData>> GetByFilePathAsync(string filePath);
        Task<IEnumerable<ExtractedData>> GetByTaskIdAsync(int taskId);
        Task<bool> AddAsync(ExtractedData data);
        Task<bool> UpdateAsync(ExtractedData data);
        Task<bool> DeleteAsync(int id);
    }
}
