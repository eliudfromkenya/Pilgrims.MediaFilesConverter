using System;
using System.IO;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;
using Moq;
using Xunit;

namespace Pilgrims.MediaFilesConverter.Tests.Services.UtilityUpgrade
{
    /// <summary>
    /// Unit tests for ExtractionService
    /// </summary>
    public class ExtractionServiceTests
    {
        private readonly Mock<ILogger<ExtractionService>> _mockLogger;
        private readonly Mock<IArchiveExtractor> _mockZipExtractor;
        private readonly Mock<IArchiveExtractor> _mockTarGzExtractor;
        private readonly ExtractionService _extractionService;

        public ExtractionServiceTests()
        {
            _mockLogger = new Mock<ILogger<ExtractionService>>();
            _mockZipExtractor = new Mock<IArchiveExtractor>();
            _mockTarGzExtractor = new Mock<IArchiveExtractor>();

            var extractors = new[] { _mockZipExtractor.Object, _mockTarGzExtractor.Object };
            _extractionService = new ExtractionService(_mockLogger.Object, extractors);
        }

        [Fact]
        public async Task ExtractArchiveAsync_ShouldExtract_WhenArchiveFormatIsSupported()
        {
            // Arrange
            var archivePath = "test.zip";
            var destinationPath = Path.GetTempPath();
            var progressReporter = new Progress<ExtractionProgress>();
            var expectedProgress = new ExtractionProgress
            {
                TotalFiles = 10,
                ExtractedFiles = 10,
                CurrentFile = "test.txt",
                Percentage = 100,
                IsCompleted = true
            };

            _mockZipExtractor.Setup(x => x.CanExtract(archivePath)).Returns(true);
            _mockZipExtractor.Setup(x => x.ExtractAsync(archivePath, destinationPath, progressReporter, It.IsAny<CancellationToken>()))
                .ReturnsAsync(expectedProgress);

            // Act
            var result = await _extractionService.ExtractArchiveAsync(archivePath, destinationPath, progressReporter);

            // Assert
            Assert.Equal(expectedProgress, result);
        }

        [Fact]
        public async Task ExtractArchiveAsync_ShouldThrowException_WhenArchiveFormatIsNotSupported()
        {
            // Arrange
            var archivePath = "test.unknown";
            var destinationPath = Path.GetTempPath();
            var progressReporter = new Progress<ExtractionProgress>();

            _mockZipExtractor.Setup(x => x.CanExtract(archivePath)).Returns(false);
            _mockTarGzExtractor.Setup(x => x.CanExtract(archivePath)).Returns(false);

            // Act & Assert
            await Assert.ThrowsAsync<NotSupportedException>(
                () => _extractionService.ExtractArchiveAsync(archivePath, destinationPath, progressReporter));
        }

        [Fact]
        public async Task ExtractArchiveAsync_ShouldHandleCancellation_WhenCancelled()
        {
            // Arrange
            var archivePath = "test.zip";
            var destinationPath = Path.GetTempPath();
            var progressReporter = new Progress<ExtractionProgress>();
            var cancellationToken = new CancellationTokenSource();

            _mockZipExtractor.Setup(x => x.CanExtract(archivePath)).Returns(true);
            _mockZipExtractor.Setup(x => x.ExtractAsync(archivePath, destinationPath, progressReporter, It.IsAny<CancellationToken>()))
                .ThrowsAsync(new OperationCanceledException());

            // Act & Assert
            await Assert.ThrowsAsync<OperationCanceledException>(
                () => _extractionService.ExtractArchiveAsync(archivePath, destinationPath, progressReporter, cancellationToken.Token));
        }

        [Fact]
        public void GetSupportedFormats_ShouldReturnSupportedFormats()
        {
            // Arrange
            _mockZipExtractor.Setup(x => x.SupportedFormats).Returns(new[] { ".zip" });
            _mockTarGzExtractor.Setup(x => x.SupportedFormats).Returns(new[] { ".tar.gz", ".tgz" });

            // Act
            var result = _extractionService.GetSupportedFormats();

            // Assert
            Assert.Contains(".zip", result);
            Assert.Contains(".tar.gz", result);
            Assert.Contains(".tgz", result);
        }

        [Fact]
        public void IsSupportedFormat_ShouldReturnTrue_WhenFormatIsSupported()
        {
            // Arrange
            _mockZipExtractor.Setup(x => x.CanExtract("test.zip")).Returns(true);

            // Act
            var result = _extractionService.IsSupportedFormat("test.zip");

            // Assert
            Assert.True(result);
        }

        [Fact]
        public void IsSupportedFormat_ShouldReturnFalse_WhenFormatIsNotSupported()
        {
            // Arrange
            _mockZipExtractor.Setup(x => x.CanExtract("test.unknown")).Returns(false);
            _mockTarGzExtractor.Setup(x => x.CanExtract("test.unknown")).Returns(false);

            // Act
            var result = _extractionService.IsSupportedFormat("test.unknown");

            // Assert
            Assert.False(result);
        }

        [Fact]
        public async Task ExtractArchiveAsync_ShouldLogError_WhenExtractionFails()
        {
            // Arrange
            var archivePath = "test.zip";
            var destinationPath = Path.GetTempPath();
            var progressReporter = new Progress<ExtractionProgress>();
            var exception = new IOException("Extraction failed");

            _mockZipExtractor.Setup(x => x.CanExtract(archivePath)).Returns(true);
            _mockZipExtractor.Setup(x => x.ExtractAsync(archivePath, destinationPath, progressReporter, It.IsAny<CancellationToken>()))
                .ThrowsAsync(exception);

            // Act
            await Assert.ThrowsAsync<IOException>(
                () => _extractionService.ExtractArchiveAsync(archivePath, destinationPath, progressReporter));

            // Assert
            _mockLogger.VerifyLogErrorContains("Extraction failed");
        }
    }
}