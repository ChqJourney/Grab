using Grab.Core.Models;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace Grab.Core.Interfaces
{
    public interface IDirectoryRepository
    {
        Task<Directory?> GetByPathAsync(string path);
        Task<IEnumerable<Directory>> GetAllAsync();
        Task<IEnumerable<Directory>> GetByStatusAsync(DirectoryStatus status);
        Task<bool> AddAsync(Directory directory);
        Task<bool> UpdateAsync(Directory directory);
        Task<bool> DeleteAsync(string path);
    }
}
