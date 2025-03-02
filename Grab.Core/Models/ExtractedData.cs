using System;

namespace Grab.Core.Models
{
    public class ExtractedData
    {
        public int Id { get; set; }
        public string FilePath { get; set; } = string.Empty;
        public int TaskId { get; set; }
        public int RuleId { get; set; }
        public string FieldName { get; set; } = string.Empty;
        public string Value { get; set; } = string.Empty;
        public DateTime ExtractedTime { get; set; }
        public bool IsValid { get; set; }
        public string? ValidationMessage { get; set; }
    }
}
