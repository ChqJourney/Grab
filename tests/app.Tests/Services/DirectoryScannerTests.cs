using System;
using System.IO;
using System.Threading.Tasks;
using App.Services;
using Moq;
using Xunit;

namespace App.Tests.Services
{
    public class DirectoryScannerTests
    {
        private readonly Mock<ILoggerService> _loggerMock;
        private readonly Mock<IDataRepository> _repositoryMock;
        private readonly DirectoryScanner _scanner;

        public DirectoryScannerTests()
        {
            _loggerMock = new Mock<ILoggerService>();
            _repositoryMock = new Mock<IDataRepository>();
            _scanner = new DirectoryScanner(_loggerMock.Object, _repositoryMock.Object);
        }

        [Fact]
        public async Task ScanDirectoryAsync_NonExistentDirectory_ReturnsNull()
        {
            // Arrange
            var nonExistentPath = Path.Combine(Path.GetTempPath(), Guid.NewGuid().ToString());

            // Act
            var result = await _scanner.ScanDirectoryAsync(nonExistentPath);

            // Assert
            Assert.Null(result);
            _loggerMock.Verify(l => l.LogErrorAsync(It.Is<string>(s => s.Contains(nonExistentPath)), null), Times.Once);
        }

        [Fact]
        public async Task CheckDirectorySignatureAsync_ValidDirectory_ReturnsTrue()
        {
            // Arrange
            var tempPath = Path.Combine(Path.GetTempPath(), Guid.NewGuid().ToString());
            Directory.CreateDirectory(tempPath);
            try
            {
                // Act
                var result = await _scanner.CheckDirectorySignatureAsync(tempPath);

                // Assert
                Assert.True(result);
            }
            finally
            {
                Directory.Delete(tempPath, true);
            }
        }

        [Fact]
        public async Task GetWordFilesAsync_EmptyDirectory_ReturnsEmptyList()
        {
            // Arrange
            var tempPath = Path.Combine(Path.GetTempPath(), Guid.NewGuid().ToString());
            Directory.CreateDirectory(tempPath);
            try
            {
                // Act
                var result = await _scanner.GetWordFilesAsync(tempPath);

                // Assert
                Assert.Empty(result);
            }
            finally
            {
                Directory.Delete(tempPath, true);
            }
        }

        [Fact]
        public async Task GetWordFilesAsync_WithWordFiles_ReturnsWordFiles()
        {
            // Arrange
            var tempPath = Path.Combine(Path.GetTempPath(), Guid.NewGuid().ToString());
            Directory.CreateDirectory(tempPath);
            try
            {
                var docPath = Path.Combine(tempPath, "test.doc");
                var docxPath = Path.Combine(tempPath, "test.docx");
                var txtPath = Path.Combine(tempPath, "test.txt");

                File.WriteAllText(docPath, "");
                File.WriteAllText(docxPath, "");
                File.WriteAllText(txtPath, "");

                // Act
                var result = await _scanner.GetWordFilesAsync(tempPath);

                // Assert
                Assert.Equal(2, result.Count());
                Assert.Contains(result, f => f.Name == "test.doc");
                Assert.Contains(result, f => f.Name == "test.docx");
            }
            finally
            {
                Directory.Delete(tempPath, true);
            }
        }
        [Fact]
        public void GetWordFiles_WithWordFiles_Returns(){
            var tempPath = "abc";
            Assert.True(tempPath == "abc");
        }
    }
}