using Grab.Core.Models;

namespace Grab.API.DTOs
{
    public class DirectoryDto
    {
        public string Path { get; set; } = string.Empty;
        public string LastSignature { get; set; } = string.Empty;
        public DateTime? LastCheckTime { get; set; }
        public DateTime? LastProcessTime { get; set; }
        public DirectoryStatus Status { get; set; }
    }

    public class FileItemDto
    {
        public string Path { get; set; } = string.Empty;
        public string DirectoryPath { get; set; } = string.Empty;
        public long FileSize { get; set; }
        public DateTime ModifiedTime { get; set; }
        public DateTime? ProcessTime { get; set; }
        public FileStatus Status { get; set; }
        public string Hash { get; set; } = string.Empty;
    }

    public class ScanRequestDto
    {
        public string RootPath { get; set; } = string.Empty;
        public bool Recursive { get; set; } = true;
        public int? TaskId { get; set; }
    }

    public class ScanStatusDto
    {
        public string Id { get; set; } = string.Empty;
        public string Status { get; set; } = string.Empty;
        public DateTime StartTime { get; set; }
        public DateTime? EndTime { get; set; }
        public int TotalDirectories { get; set; }
        public int ProcessedDirectories { get; set; }
        public int TotalFiles { get; set; }
        public int ProcessedFiles { get; set; }
        public int SuccessfulFiles { get; set; }
        public int FailedFiles { get; set; }
    }
}
