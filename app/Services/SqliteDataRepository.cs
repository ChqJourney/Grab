using Microsoft.Data.Sqlite;
using Dapper;
using System.Data;

namespace App.Services
{
    public class SqliteDataRepository : IDataRepository, IDisposable
    {
        private readonly string _connectionString;
        private readonly ILoggerService _logger;

        public SqliteDataRepository(string dbPath, ILoggerService logger)
        {
            _connectionString = $"Data Source={dbPath}";
            _logger = logger;
            InitializeDatabase();
        }

        private void InitializeDatabase()
        {
            using var connection = new SqliteConnection(_connectionString);
            connection.Execute(@"
                CREATE TABLE IF NOT EXISTS directories (
                    path TEXT PRIMARY KEY,
                    last_signature TEXT,
                    last_check_time TIMESTAMP,
                    last_process_time TIMESTAMP,
                    status TEXT
                );

                CREATE TABLE IF NOT EXISTS files (
                    path TEXT PRIMARY KEY,
                    directory_path TEXT,
                    file_size INTEGER,
                    modified_time REAL,
                    process_time TIMESTAMP,
                    status TEXT,
                    hash TEXT,
                    FOREIGN KEY (directory_path) REFERENCES directories(path)
                );");
        }

        public async Task<bool> AddDirectoryAsync(string path, string signature)
        {
            try
            {
                using var connection = new SqliteConnection(_connectionString);
                var result = await connection.ExecuteAsync(
                    @"INSERT OR REPLACE INTO directories 
                    (path, last_signature, last_check_time, status) 
                    VALUES (@Path, @Signature, @CheckTime, @Status)",
                    new
                    {
                        Path = path,
                        Signature = signature,
                        CheckTime = DateTime.UtcNow,
                        Status = "PENDING"
                    });
                return result > 0;
            }
            catch (Exception ex)
            {
                await _logger.LogErrorAsync($"添加目录记录失败: {path}", ex);
                return false;
            }
        }

        public async Task<bool> UpdateDirectoryStatusAsync(string path, string status)
        {
            try
            {
                using var connection = new SqliteConnection(_connectionString);
                var result = await connection.ExecuteAsync(
                    @"UPDATE directories 
                    SET status = @Status, 
                        last_check_time = @CheckTime
                    WHERE path = @Path",
                    new
                    {
                        Path = path,
                        Status = status,
                        CheckTime = DateTime.UtcNow
                    });
                return result > 0;
            }
            catch (Exception ex)
            {
                await _logger.LogErrorAsync($"更新目录状态失败: {path}", ex);
                return false;
            }
        }

        public async Task<bool> AddFileAsync(string path, string directoryPath, long fileSize, double modifiedTime)
        {
            try
            {
                using var connection = new SqliteConnection(_connectionString);
                var result = await connection.ExecuteAsync(
                    @"INSERT OR REPLACE INTO files 
                    (path, directory_path, file_size, modified_time, status) 
                    VALUES (@Path, @DirectoryPath, @FileSize, @ModifiedTime, @Status)",
                    new
                    {
                        Path = path,
                        DirectoryPath = directoryPath,
                        FileSize = fileSize,
                        ModifiedTime = modifiedTime,
                        Status = "PENDING"
                    });
                return result > 0;
            }
            catch (Exception ex)
            {
                await _logger.LogErrorAsync($"添加文件记录失败: {path}", ex);
                return false;
            }
        }

        public async Task<bool> UpdateFileStatusAsync(string path, string status)
        {
            try
            {
                using var connection = new SqliteConnection(_connectionString);
                var result = await connection.ExecuteAsync(
                    @"UPDATE files 
                    SET status = @Status 
                    WHERE path = @Path",
                    new { Path = path, Status = status });
                return result > 0;
            }
            catch (Exception ex)
            {
                await _logger.LogErrorAsync($"更新文件状态失败: {path}", ex);
                return false;
            }
        }

        public async Task<bool> UpdateFileProcessResultAsync(string path, string hash, DateTime processTime)
        {
            try
            {
                using var connection = new SqliteConnection(_connectionString);
                var result = await connection.ExecuteAsync(
                    @"UPDATE files 
                    SET hash = @Hash, 
                        process_time = @ProcessTime 
                    WHERE path = @Path",
                    new
                    {
                        Path = path,
                        Hash = hash,
                        ProcessTime = processTime
                    });
                return result > 0;
            }
            catch (Exception ex)
            {
                await _logger.LogErrorAsync($"更新文件处理结果失败: {path}", ex);
                return false;
            }
        }

        public async Task<IEnumerable<string>> GetPendingFilesAsync()
        {
            try
            {
                using var connection = new SqliteConnection(_connectionString);
                return await connection.QueryAsync<string>(
                    "SELECT path FROM files WHERE status = 'PENDING' ORDER BY path");
            }
            catch (Exception ex)
            {
                await _logger.LogErrorAsync("获取待处理文件列表失败", ex);
                return Enumerable.Empty<string>();
            }
        }

        public async Task<IEnumerable<string>> GetPendingDirectoriesAsync()
        {
            try
            {
                using var connection = new SqliteConnection(_connectionString);
                return await connection.QueryAsync<string>(
                    "SELECT path FROM directories WHERE status IN ('PENDING', 'NEED_RECHECK') ORDER BY path");
            }
            catch (Exception ex)
            {
                await _logger.LogErrorAsync("获取待处理目录列表失败", ex);
                return Enumerable.Empty<string>();
            }
        }

        public void Dispose()
        {
            // 清理资源（如果需要）
        }
    }
}