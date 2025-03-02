using Grab.Core.Models;

namespace Grab.API.DTOs
{
    public class TaskDto
    {
        public int Id { get; set; }
        public string Name { get; set; } = string.Empty;
        public string Description { get; set; } = string.Empty;
        public string SourcePath { get; set; } = string.Empty;
        public bool Enabled { get; set; } = true;
        public FileType TargetFileType { get; set; }
        public List<DataExtractRuleDto> ExtractRules { get; set; } = new List<DataExtractRuleDto>();
    }

    public class CreateTaskDto
    {
        public string Name { get; set; } = string.Empty;
        public string Description { get; set; } = string.Empty;
        public string SourcePath { get; set; } = string.Empty;
        public bool Enabled { get; set; } = true;
        public FileType TargetFileType { get; set; }
    }

    public class UpdateTaskDto
    {
        public string? Name { get; set; }
        public string? Description { get; set; }
        public string? SourcePath { get; set; }
        public bool? Enabled { get; set; }
        public FileType? TargetFileType { get; set; }
    }

    public class DataExtractRuleDto
    {
        public int Id { get; set; }
        public string FieldName { get; set; } = string.Empty;
        public FileType FileType { get; set; }
        public string Location { get; set; } = string.Empty;
        public string ValidationRule { get; set; } = string.Empty;
        public int TaskId { get; set; }
    }

    public class CreateDataExtractRuleDto
    {
        public string FieldName { get; set; } = string.Empty;
        public FileType FileType { get; set; }
        public string Location { get; set; } = string.Empty;
        public string ValidationRule { get; set; } = string.Empty;
        public int TaskId { get; set; }
    }

    public class UpdateDataExtractRuleDto
    {
        public string? FieldName { get; set; }
        public FileType? FileType { get; set; }
        public string? Location { get; set; }
        public string? ValidationRule { get; set; }
    }
}
