namespace App.Services{

public interface IFileProcessor
{
    Task<bool> ProcessFileAsync(string filePath);
    Task<string> CalculateFileHashAsync(string filePath);
    Task<bool> ValidateFileAsync(string filePath);
    Task<Dictionary<string, object>> ExtractInformationAsync(string filePath);
}
}