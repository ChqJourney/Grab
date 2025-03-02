using Grab.Core.Models;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace Grab.Core.Interfaces
{
    public interface IFileRepository
    {
        Task<FileItem?> GetByPathAsync(string path);
        Task<IEnumerable<FileItem>> GetByDirectoryPathAsync(string directoryPath);
        Task<IEnumerable<FileItem>> GetByStatusAsync(FileStatus status);
        Task<bool> AddAsync(FileItem file);
        Task<bool> UpdateAsync(FileItem file);
        Task<bool> DeleteAsync(string path);
    }
}
