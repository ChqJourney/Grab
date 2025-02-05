using System;
using System.Threading.Tasks;
using App.Services;
using Microsoft.Data.Sqlite;
using Moq;
using Xunit;

namespace App.Tests.Services
{
    public class SqliteDataRepositoryTests : IDisposable
    {
        private readonly Mock<ILoggerService> _loggerMock;
        private readonly SqliteDataRepository _repository;
        private readonly string _connectionString;

        public SqliteDataRepositoryTests()
        {
            _loggerMock = new Mock<ILoggerService>();
            _connectionString = "Data Source=:memory:";
            _repository = new SqliteDataRepository(_connectionString, _loggerMock.Object);
        }

        [Fact]
        public async Task AddDirectoryAsync_ValidDirectory_ReturnsTrue()
        {
            // Arrange
            var path = "/test/directory";
            var signature = "test-signature";

            // Act
            var result = await _repository.AddDirectoryAsync(path, signature);

            // Assert
            Assert.True(result);
        }

        [Fact]
        public async Task UpdateDirectoryStatusAsync_ExistingDirectory_ReturnsTrue()
        {
            // Arrange
            var path = "/test/directory";
            var signature = "test-signature";
            await _repository.AddDirectoryAsync(path, signature);

            // Act
            var result = await _repository.UpdateDirectoryStatusAsync(path, "PROCESSING");

            // Assert
            Assert.True(result);
        }

        [Fact]
        public async Task AddFileAsync_ValidFile_ReturnsTrue()
        {
            // Arrange
            var dirPath = "/test/directory";
            var filePath = "/test/directory/file.doc";
            await _repository.AddDirectoryAsync(dirPath, "test-signature");

            // Act
            var result = await _repository.AddFileAsync(filePath, dirPath, 1024, DateTime.UtcNow.Ticks);

            // Assert
            Assert.True(result);
        }

        [Fact]
        public async Task UpdateFileStatusAsync_ExistingFile_ReturnsTrue()
        {
            // Arrange
            var dirPath = "/test/directory";
            var filePath = "/test/directory/file.doc";
            await _repository.AddDirectoryAsync(dirPath, "test-signature");
            await _repository.AddFileAsync(filePath, dirPath, 1024, DateTime.UtcNow.Ticks);

            // Act
            var result = await _repository.UpdateFileStatusAsync(filePath, "PROCESSED");

            // Assert
            Assert.True(result);
        }

        [Fact]
        public async Task UpdateFileProcessResultAsync_ExistingFile_ReturnsTrue()
        {
            // Arrange
            var dirPath = "/test/directory";
            var filePath = "/test/directory/file.doc";
            await _repository.AddDirectoryAsync(dirPath, "test-signature");
            await _repository.AddFileAsync(filePath, dirPath, 1024, DateTime.UtcNow.Ticks);

            // Act
            var result = await _repository.UpdateFileProcessResultAsync(filePath, "test-hash", DateTime.UtcNow);

            // Assert
            Assert.True(result);
        }

        public void Dispose()
        {
            _repository.Dispose();
        }
    }
}