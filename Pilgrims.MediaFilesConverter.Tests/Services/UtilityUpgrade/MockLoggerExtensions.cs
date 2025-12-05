using System;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;
using Moq;
using Xunit;

namespace Pilgrims.MediaFilesConverter.Tests.Services.UtilityUpgrade
{
    /// <summary>
    /// Test extensions for logging verification
    /// </summary>
    public static class MockLoggerExtensions
    {
        public static void VerifyLogErrorContains(this Mock<ILogger> mockLogger, string expectedMessage)
        {
            mockLogger.Verify(
                x => x.Log(
                    LogLevel.Error,
                    It.IsAny<EventId>(),
                    It.Is<It.IsAnyType>((v, t) => v.ToString().Contains(expectedMessage)),
                    It.IsAny<Exception>(),
                    It.IsAny<Func<It.IsAnyType, Exception, string>>()!),
                Times.AtLeastOnce);
        }

        public static void VerifyLogWarningContains(this Mock<ILogger> mockLogger, string expectedMessage)
        {
            mockLogger.Verify(
                x => x.Log(
                    LogLevel.Warning,
                    It.IsAny<EventId>(),
                    It.Is<It.IsAnyType>((v, t) => v.ToString().Contains(expectedMessage)),
                    It.IsAny<Exception>(),
                    It.IsAny<Func<It.IsAnyType, Exception, string>>()!),
                Times.AtLeastOnce);
        }

        public static void VerifyLogInformationContains(this Mock<ILogger> mockLogger, string expectedMessage)
        {
            mockLogger.Verify(
                x => x.Log(
                    LogLevel.Information,
                    It.IsAny<EventId>(),
                    It.Is<It.IsAnyType>((v, t) => v.ToString().Contains(expectedMessage)),
                    It.IsAny<Exception>(),
                    It.IsAny<Func<It.IsAnyType, Exception, string>>()!),
                Times.AtLeastOnce);
        }
    }

    /// <summary>
    /// Generic test extensions for typed loggers
    /// </summary>
    public static class MockLoggerExtensions<T>
    {
        public static void VerifyLogErrorContains(this Mock<ILogger<T>> mockLogger, string expectedMessage)
        {
            mockLogger.Verify(
                x => x.Log(
                    LogLevel.Error,
                    It.IsAny<EventId>(),
                    It.Is<It.IsAnyType>((v, t) => v.ToString().Contains(expectedMessage)),
                    It.IsAny<Exception>(),
                    It.IsAny<Func<It.IsAnyType, Exception, string>>()!),
                Times.AtLeastOnce);
        }

        public static void VerifyLogWarningContains(this Mock<ILogger<T>> mockLogger, string expectedMessage)
        {
            mockLogger.Verify(
                x => x.Log(
                    LogLevel.Warning,
                    It.IsAny<EventId>(),
                    It.Is<It.IsAnyType>((v, t) => v.ToString().Contains(expectedMessage)),
                    It.IsAny<Exception>(),
                    It.IsAny<Func<It.IsAnyType, Exception, string>>()!),
                Times.AtLeastOnce);
        }

        public static void VerifyLogInformationContains(this Mock<ILogger<T>> mockLogger, string expectedMessage)
        {
            mockLogger.Verify(
                x => x.Log(
                    LogLevel.Information,
                    It.IsAny<EventId>(),
                    It.Is<It.IsAnyType>((v, t) => v.ToString().Contains(expectedMessage)),
                    It.IsAny<Exception>(),
                    It.IsAny<Func<It.IsAnyType, Exception, string>>()!),
                Times.AtLeastOnce);
        }
    }
}