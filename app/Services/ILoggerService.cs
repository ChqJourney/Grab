namespace App.Services
{
    public interface ILoggerService
    {
        Task LogInfoAsync(string message);
        Task LogErrorAsync(string message, Exception ex = null);
        Task LogWarningAsync(string message);
        Task LogProcessStatusAsync(string path, string status, string details = null);
    }
}
