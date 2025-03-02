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
    public class DirectoryRepositoryTests
    {
        private readonly DbContextOptions<GrabDbContext> _options;

        public DirectoryRepositoryTests()
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
            // 添加测试目录
            var directories = new[]
            {
                new Directory
                {
                    Path = "/test/dir1",
                    LastSignature = "sig1",
                    LastCheckTime = DateTime.UtcNow.AddDays(-1),
                    LastProcessTime = DateTime.UtcNow.AddDays(-1),
                    Status = DirectoryStatus.Completed
                },
                new Directory
                {
                    Path = "/test/dir2",
                    LastSignature = "sig2",
                    LastCheckTime = DateTime.UtcNow.AddHours(-2),
                    LastProcessTime = null,
                    Status = DirectoryStatus.Pending
                },
                new Directory
                {
                    Path = "/test/dir3",
                    LastSignature = "sig3",
                    LastCheckTime = DateTime.UtcNow.AddHours(-1),
                    LastProcessTime = null,
                    Status = DirectoryStatus.Processing
                }
            };

            context.Directories.AddRange(directories);
            context.SaveChanges();
        }

        [Fact]
        public async Task GetByPathAsync_ExistingPath_ReturnsDirectory()
        {
            // 准备
            using var context = new GrabDbContext(_options);
            var repository = new DirectoryRepository(context);
            string path = "/test/dir1";

            // 执行
            var result = await repository.GetByPathAsync(path);

            // 验证
            Assert.NotNull(result);
            Assert.Equal(path, result.Path);
            Assert.Equal("sig1", result.LastSignature);
            Assert.Equal(DirectoryStatus.Completed, result.Status);
        }

        [Fact]
        public async Task GetByPathAsync_NonExistingPath_ReturnsNull()
        {
            // 准备
            using var context = new GrabDbContext(_options);
            var repository = new DirectoryRepository(context);
            string path = "/non/existing/path";

            // 执行
            var result = await repository.GetByPathAsync(path);

            // 验证
            Assert.Null(result);
        }

        [Fact]
        public async Task GetAllAsync_ReturnsAllDirectories()
        {
            // 准备
            using var context = new GrabDbContext(_options);
            var repository = new DirectoryRepository(context);

            // 执行
            var result = await repository.GetAllAsync();

            // 验证
            Assert.NotNull(result);
            Assert.Equal(3, result.Count());
        }

        [Fact]
        public async Task GetByStatusAsync_ExistingStatus_ReturnsMatchingDirectories()
        {
            // 准备
            using var context = new GrabDbContext(_options);
            var repository = new DirectoryRepository(context);

            // 执行
            var result = await repository.GetByStatusAsync(DirectoryStatus.Pending);

            // 验证
            Assert.NotNull(result);
            Assert.Single(result);
            Assert.Equal("/test/dir2", result.First().Path);
        }

        [Fact]
        public async Task AddAsync_ValidDirectory_ReturnsTrue()
        {
            // 准备
            using var context = new GrabDbContext(_options);
            var repository = new DirectoryRepository(context);
            var newDirectory = new Directory
            {
                Path = "/test/dir4",
                LastSignature = "sig4",
                LastCheckTime = DateTime.UtcNow,
                Status = DirectoryStatus.Pending
            };

            // 执行
            var result = await repository.AddAsync(newDirectory);

            // 验证
            Assert.True(result);
            var savedDirectory = await context.Directories.FindAsync("/test/dir4");
            Assert.NotNull(savedDirectory);
            Assert.Equal("sig4", savedDirectory.LastSignature);
        }

        [Fact]
        public async Task UpdateAsync_ExistingDirectory_UpdatesAndReturnsTrue()
        {
            // 准备
            using var context = new GrabDbContext(_options);
            var repository = new DirectoryRepository(context);
            
            // 获取现有目录并修改
            var existingDirectory = await context.Directories.FindAsync("/test/dir1");
            existingDirectory.Status = DirectoryStatus.NeedRecheck;
            existingDirectory.LastSignature = "updated-sig";

            // 执行
            var result = await repository.UpdateAsync(existingDirectory);

            // 验证
            Assert.True(result);
            
            // 从新上下文获取更新后的目录，以确保更改被保存
            using var newContext = new GrabDbContext(_options);
            var updatedDirectory = await newContext.Directories.FindAsync("/test/dir1");
            Assert.NotNull(updatedDirectory);
            Assert.Equal(DirectoryStatus.NeedRecheck, updatedDirectory.Status);
            Assert.Equal("updated-sig", updatedDirectory.LastSignature);
        }

        [Fact]
        public async Task DeleteAsync_ExistingDirectory_RemovesAndReturnsTrue()
        {
            // 准备
            using var context = new GrabDbContext(_options);
            var repository = new DirectoryRepository(context);
            string path = "/test/dir1";

            // 执行
            var result = await repository.DeleteAsync(path);

            // 验证
            Assert.True(result);
            var deletedDirectory = await context.Directories.FindAsync(path);
            Assert.Null(deletedDirectory);
        }

        [Fact]
        public async Task DeleteAsync_NonExistingDirectory_ReturnsFalse()
        {
            // 准备
            using var context = new GrabDbContext(_options);
            var repository = new DirectoryRepository(context);
            string path = "/non/existing/path";

            // 执行
            var result = await repository.DeleteAsync(path);

            // 验证
            Assert.False(result);
        }
    }
}
