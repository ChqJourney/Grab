namespace App.Services
{

    public interface IDirectoryScanner
    {
        Task<DirectoryInfo> ScanDirectoryAsync(string path);
        Task<bool> CheckDirectorySignatureAsync(string path);
        Task<IEnumerable<FileInfo>> GetWordFilesAsync(string directoryPath);
    }
}