using System;
using System.IO;
using System.Threading.Tasks;
using App.Services;
using DocumentFormat.OpenXml.Packaging;
using Moq;
using Xunit;

namespace App.Tests.Services
{
    public class FileProcessorTests
    {
        private readonly Mock<ILoggerService> _loggerMock;
        private readonly FileProcessor _processor;

        public FileProcessorTests()
        {
            _loggerMock = new Mock<ILoggerService>();
            _processor = new FileProcessor(_loggerMock.Object);
        }

        [Fact]
        public async Task CalculateFileHashAsync_ValidFile_ReturnsHash()
        {
            // Arrange
            var tempPath = Path.Combine(Path.GetTempPath(), Guid.NewGuid().ToString());
            File.WriteAllText(tempPath, "test content");

            try
            {
                // Act
                var hash = await _processor.CalculateFileHashAsync(tempPath);

                // Assert
                Assert.NotNull(hash);
                Assert.NotEmpty(hash);
            }
            finally
            {
                File.Delete(tempPath);
            }
        }

        [Fact]
        public async Task CalculateFileHashAsync_NonExistentFile_ThrowsException()
        {
            // Arrange
            var nonExistentPath = Path.Combine(Path.GetTempPath(), Guid.NewGuid().ToString());

            // Act & Assert
            await Assert.ThrowsAsync<FileNotFoundException>(
                () => _processor.CalculateFileHashAsync(nonExistentPath));
        }

        [Fact]
        public async Task ValidateFileAsync_NonExistentFile_ReturnsFalse()
        {
            // Arrange
            var nonExistentPath = Path.Combine(Path.GetTempPath(), Guid.NewGuid().ToString());

            // Act
            var result = await _processor.ValidateFileAsync(nonExistentPath);

            // Assert
            Assert.False(result);
            _loggerMock.Verify(l => l.LogErrorAsync(It.Is<string>(s => s.Contains(nonExistentPath)), It.IsAny<Exception>()), Times.Once);
        }
    }
}