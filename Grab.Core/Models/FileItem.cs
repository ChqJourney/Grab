using System;

namespace Grab.Core.Models
{
    public class FileItem
    {
        public string Path { get; set; } = string.Empty;
        public string DirectoryPath { get; set; } = string.Empty;
        public long FileSize { get; set; }
        public long ModifiedTime { get; set; }
        public DateTime? ProcessTime { get; set; }
        public FileStatus Status { get; set; } = FileStatus.Pending;
        public string Hash { get; set; } = string.Empty;
    }

    public enum FileStatus
    {
        Pending,
        Processing,
        Processed,
        Failed,
        Deleted
    }
}
