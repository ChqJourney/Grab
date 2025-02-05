namespace App.Models{

public static class ProcessingStatus
{
    public static class Directory
    {
        public const string PENDING = "PENDING";
        public const string PROCESSING = "PROCESSING";
        public const string COMPLETED = "COMPLETED";
        public const string NEED_RECHECK = "NEED_RECHECK";
    }

    public static class File
    {
        public const string PENDING = "PENDING";
        public const string PROCESSED = "PROCESSED";
        public const string FAILED = "FAILED";
        public const string DELETED = "DELETED";
    }
}
}