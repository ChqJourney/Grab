using System;

namespace Grab.Core.Models
{
    public class Directory
    {
        public string Path { get; set; } = string.Empty;
        public string LastSignature { get; set; } = string.Empty;
        public DateTime? LastCheckTime { get; set; }
        public DateTime? LastProcessTime { get; set; }
        public DirectoryStatus Status { get; set; } = DirectoryStatus.Pending;
    }

    public enum DirectoryStatus
    {
        Pending,
        Processing,
        Completed,
        NeedRecheck
    }
}
