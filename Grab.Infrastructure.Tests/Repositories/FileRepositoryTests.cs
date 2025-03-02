using Grab.Core.Models;
using Grab.Infrastructure.Data;
using Grab.Infrastructure.Repositories;
using Microsoft.EntityFrameworkCore;
using System;
using System.Linq;
using System.Threading.Tasks;
using Xunit;

namespace Grab.Infrastructure.Tests.Repositories
{
    public class FileRepositoryTests
    {
        private readonly DbContextOptions<GrabDbContext> _options;

        public FileRepositoryTests()
        {
            // 为每个测试创建新的内存数据库
            _options = new DbContextOptionsBuilder<GrabDbContext>()
                .UseInMemoryDatabase(databaseName: Guid.NewGuid().ToString())
                .Options;

            // 准备测试数据
            using var context = new GrabDbContext(_options);
            SeedDatabase(context);
        }

        private void SeedDatabase(GrabDbContext context)
        {
            // 首先添加测试目录
            var directories = new[]
            {
                new Directory
                {
                    Path = "/test/dir1",
                    Status = DirectoryStatus.Completed
                },
                new Directory
                {
                    Path = "/test/dir2",
                    Status = DirectoryStatus.Pending
                }
            };

            context.Directories.AddRange(directories);
            context.SaveChanges();

            // 然后添加测试文件
            var files = new[]
            {
                new FileItem
                {
                    Path = "/test/dir1/file1.docx",
                    DirectoryPath = "/test/dir1",
                    FileSize = 12345,
                    ModifiedTime = DateTimeOffset.UtcNow.AddDays(-2).ToUnixTimeSeconds(),
                    ProcessTime = DateTime.UtcNow.AddDays(-1),
                    Status = FileStatus.Processed,
                    Hash = "hash1"
                },
                new FileItem
                {
                    Path = "/test/dir1/file2.doc",
                    DirectoryPath = "/test/dir1",
                    FileSize = 54321,
                    ModifiedTime = DateTimeOffset.UtcNow.AddDays(-1).ToUnixTimeSeconds(),
                    ProcessTime = null,
                    Status = FileStatus.Pending,
                    Hash = "hash2"
                },
                new FileItem
                {
                    Path = "/test/dir2/file3.xlsx",
                    DirectoryPath = "/test/dir2",
                    FileSize = 65432,
                    ModifiedTime = DateTimeOffset.UtcNow.AddHours(-5).ToUnixTimeSeconds(),
                    ProcessTime = null,
                    Status = FileStatus.Pending,
                    Hash = "hash3"
                }
            };

            context.Files.AddRange(files);
            context.SaveChanges();
        }

        [Fact]
        public async Task GetByPathAsync_ExistingPath_ReturnsFile()
        {
            // 准备
            using var context = new GrabDbContext(_options);
            var repository = new FileRepository(context);
            string path = "/test/dir1/file1.docx";

            // 执行
            var result = await repository.GetByPathAsync(path);

            // 验证
            Assert.NotNull(result);
            Assert.Equal(path, result.Path);
            Assert.Equal("/test/dir1", result.DirectoryPath);
            Assert.Equal(12345, result.FileSize);
            Assert.Equal(FileStatus.Processed, result.Status);
            Assert.Equal("hash1", result.Hash);
        }

        [Fact]
        public async Task GetByPathAsync_NonExistingPath_ReturnsNull()
        {
            // 准备
            using var context = new GrabDbContext(_options);
            var repository = new FileRepository(context);
            string path = "/non/existing/file.txt";

            // 执行
            var result = await repository.GetByPathAsync(path);

            // 验证
            Assert.Null(result);
        }

        [Fact]
        public async Task GetByDirectoryPathAsync_ExistingPath_ReturnsMatchingFiles()
        {
            // 准备
            using var context = new GrabDbContext(_options);
            var repository = new FileRepository(context);
            string directoryPath = "/test/dir1";

            // 执行
            var result = await repository.GetByDirectoryPathAsync(directoryPath);

            // 验证
            Assert.NotNull(result);
            Assert.Equal(2, result.Count());
            Assert.Contains(result, f => f.Path == "/test/dir1/file1.docx");
            Assert.Contains(result, f => f.Path == "/test/dir1/file2.doc");
        }

        [Fact]
        public async Task GetByDirectoryPathAsync_NonExistingPath_ReturnsEmptyCollection()
        {
            // 准备
            using var context = new GrabDbContext(_options);
            var repository = new FileRepository(context);
            string directoryPath = "/non/existing/dir";

            // 执行
            var result = await repository.GetByDirectoryPathAsync(directoryPath);

            // 验证
            Assert.NotNull(result);
            Assert.Empty(result);
        }

        [Fact]
        public async Task GetByStatusAsync_ExistingStatus_ReturnsMatchingFiles()
        {
            // 准备
            using var context = new GrabDbContext(_options);
            var repository = new FileRepository(context);
            var status = FileStatus.Pending;

            // 执行
            var result = await repository.GetByStatusAsync(status);

            // 验证
            Assert.NotNull(result);
            Assert.Equal(2, result.Count());
            Assert.Contains(result, f => f.Path == "/test/dir1/file2.doc");
            Assert.Contains(result, f => f.Path == "/test/dir2/file3.xlsx");
        }

        [Fact]
        public async Task GetByStatusAsync_NonExistingStatus_ReturnsEmptyCollection()
        {
            // 准备
            using var context = new GrabDbContext(_options);
            var repository = new FileRepository(context);
            var status = FileStatus.Failed;

            // 执行
            var result = await repository.GetByStatusAsync(status);

            // 验证
            Assert.NotNull(result);
            Assert.Empty(result);
        }

        [Fact]
        public async Task AddAsync_ValidFile_ReturnsTrue()
        {
            // 准备
            using var context = new GrabDbContext(_options);
            var repository = new FileRepository(context);
            var newFile = new FileItem
            {
                Path = "/test/dir2/file4.xls",
                DirectoryPath = "/test/dir2",
                FileSize = 87654,
                ModifiedTime = DateTimeOffset.UtcNow.ToUnixTimeSeconds(),
                Status = FileStatus.Pending,
                Hash = "hash4"
            };

            // 执行
            var result = await repository.AddAsync(newFile);

            // 验证
            Assert.True(result);
            var savedFile = await context.Files.FindAsync("/test/dir2/file4.xls");
            Assert.NotNull(savedFile);
            Assert.Equal(87654, savedFile.FileSize);
            Assert.Equal("hash4", savedFile.Hash);
        }

        [Fact]
        public async Task UpdateAsync_ExistingFile_UpdatesAndReturnsTrue()
        {
            // 准备
            using var context = new GrabDbContext(_options);
            var repository = new FileRepository(context);
            
            // 获取现有文件并修改
            var existingFile = await context.Files.FindAsync("/test/dir1/file1.docx");
            existingFile.Status = FileStatus.Failed;
            existingFile.Hash = "updated-hash";

            // 执行
            var result = await repository.UpdateAsync(existingFile);

            // 验证
            Assert.True(result);
            
            // 从新上下文获取更新后的文件，以确保更改被保存
            using var newContext = new GrabDbContext(_options);
            var updatedFile = await newContext.Files.FindAsync("/test/dir1/file1.docx");
            Assert.NotNull(updatedFile);
            Assert.Equal(FileStatus.Failed, updatedFile.Status);
            Assert.Equal("updated-hash", updatedFile.Hash);
        }

        [Fact]
        public async Task DeleteAsync_ExistingFile_RemovesAndReturnsTrue()
        {
            // 准备
            using var context = new GrabDbContext(_options);
            var repository = new FileRepository(context);
            string path = "/test/dir1/file1.docx";

            // 执行
            var result = await repository.DeleteAsync(path);

            // 验证
            Assert.True(result);
            var deletedFile = await context.Files.FindAsync(path);
            Assert.Null(deletedFile);
        }

        [Fact]
        public async Task DeleteAsync_NonExistingFile_ReturnsFalse()
        {
            // 准备
            using var context = new GrabDbContext(_options);
            var repository = new FileRepository(context);
            string path = "/non/existing/file.txt";

            // 执行
            var result = await repository.DeleteAsync(path);

            // 验证
            Assert.False(result);
        }
    }
}
