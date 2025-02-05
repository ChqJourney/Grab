namespace App.Services
{
    public interface IDataRepository
    {
        Task<bool> AddDirectoryAsync(string path, string signature);
        Task<bool> UpdateDirectoryStatusAsync(string path, string status);
        Task<bool> AddFileAsync(string path, string directoryPath, long fileSize, double modifiedTime);
        Task<bool> UpdateFileStatusAsync(string path, string status);
        Task<bool> UpdateFileProcessResultAsync(string path, string hash, DateTime processTime);
        Task<IEnumerable<string>> GetPendingFilesAsync();
        Task<IEnumerable<string>> GetPendingDirectoriesAsync();
    }
}