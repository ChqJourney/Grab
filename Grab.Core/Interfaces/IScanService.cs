using Grab.Core.Models;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace Grab.Core.Interfaces
{
    public interface IScanService
    {
        Task ScanDirectoriesAsync(string rootPath);
        Task ProcessDirectoryAsync(string path);
        Task ProcessFileAsync(string path);
        Task<string> CalculateDirectorySignatureAsync(string path);
        Task<string> CalculateFileHashAsync(string path);
        Task<bool> IsFileLockedAsync(string path);
    }
}
