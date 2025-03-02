namespace Grab.API.DTOs
{
    public class ExtractedDataDto
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

    public class ExtractedDataFilterDto
    {
        public int? TaskId { get; set; }
        public int? RuleId { get; set; }
        public string? FieldName { get; set; }
        public DateTime? FromDate { get; set; }
        public DateTime? ToDate { get; set; }
        public bool? IsValid { get; set; }
    }
}
