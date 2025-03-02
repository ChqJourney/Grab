using System.Collections.Generic;

namespace Grab.Core.Models
{
    public class Task
    {
        public int Id { get; set; }
        public string Name { get; set; } = string.Empty;
        public string Description { get; set; } = string.Empty;
        public string SourcePath { get; set; } = string.Empty;
        public bool Enabled { get; set; } = true;
        public FileType TargetFileType { get; set; }
        public List<DataExtractRule> ExtractRules { get; set; } = new List<DataExtractRule>();
    }

    public enum FileType
    {
        Doc,
        Docx,
        Xls,
        Xlsx,
        All
    }

    public class DataExtractRule
    {
        public int Id { get; set; }
        public string FieldName { get; set; } = string.Empty;
        public FileType FileType { get; set; }
        public string Location { get; set; } = string.Empty;
        public string ValidationRule { get; set; } = string.Empty;
        public int TaskId { get; set; }
        public Task? Task { get; set; }
    }
}
