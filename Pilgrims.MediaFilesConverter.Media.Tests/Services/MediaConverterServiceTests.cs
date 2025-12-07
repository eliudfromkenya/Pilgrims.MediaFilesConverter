using System;
using System.Diagnostics;
using System.IO;
using System.Threading.Tasks;
using FFMpegCore;
using Pilgrims.MediaFilesConverter.Models;
using Pilgrims.MediaFilesConverter.Services;
using Xunit;

namespace Pilgrims.MediaFilesConverter.Media.Tests.Services
{
    public class MediaConverterServiceTests
    {
        [Fact]
        public async Task ConvertsMp4ToMp3_ProducesAudioOnlyFile()
        {
            var ffmpegPath = FindFFmpeg();
            if (ffmpegPath == null)
            {
                return;
            }

            GlobalFFOptions.Configure(new FFOptions { BinaryFolder = Path.GetDirectoryName(ffmpegPath)! });

            var tempDir = Path.Combine(Path.GetTempPath(), "MediaConverterServiceTests", Guid.NewGuid().ToString("N"));
            Directory.CreateDirectory(tempDir);

            var inputPath = Path.Combine(tempDir, "input.mp4");
            var outputDir = tempDir;

            try
            {
                var psi = new ProcessStartInfo
                {
                    FileName = ffmpegPath,
                    Arguments = "-hide_banner -loglevel error -y " +
                                "-f lavfi -i color=c=black:s=320x240:d=2 " +
                                "-f lavfi -i sine=frequency=1000:duration=2 " +
                                "-shortest -c:v libx264 -pix_fmt yuv420p -c:a aac " +
                                $"\"{inputPath}\"",
                    UseShellExecute = false,
                    CreateNoWindow = true,
                };

                using (var proc = Process.Start(psi))
                {
                    proc!.WaitForExit();
                    Assert.True(proc.ExitCode == 0, "Failed to generate test input MP4");
                }

                Assert.True(File.Exists(inputPath), "Input MP4 not created");

                var settings = new ConversionSettings
                {
                    OutputFormat = "mp3",
                    OutputDirectory = outputDir,
                    AudioQuality = AudioQuality.Medium
                };

                var svc = new MediaConverterService();
                var mediaFile = new MediaFile(inputPath);

                var result = await svc.ConvertMediaAsync(mediaFile, settings);
                Assert.True(result, "Conversion returned false");

                var outputPath = mediaFile.OutputPath;
                Assert.NotNull(outputPath);
                Assert.True(File.Exists(outputPath!), "Output MP3 file missing");

                var info = await FFProbe.AnalyseAsync(outputPath!);
                Assert.True(info.VideoStreams.Count == 0, "Output should contain no video streams");
                Assert.True(info.AudioStreams.Count >= 1, "Output should contain at least one audio stream");

                var fi = new FileInfo(outputPath!);
                Assert.True(fi.Length > 0, "Output MP3 file size should be greater than zero");
            }
            finally
            {
                try { Directory.Delete(tempDir, true); } catch { }
            }
        }

        private static string? FindFFmpeg()
        {
            var appDir = AppDomain.CurrentDomain.BaseDirectory;
            var bundled = Path.Combine(appDir, "FFmpeg", "ffmpeg.exe");
            if (File.Exists(bundled)) return bundled;

            string[] possible =
            {
                @"C:\\ffmpeg\\bin",
                @"C:\\Program Files\\ffmpeg\\bin",
                @"C:\\Program Files (x86)\\ffmpeg\\bin",
                Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData), "ffmpeg", "bin"),
                Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.ProgramFiles), "ffmpeg", "bin")
            };

            foreach (var path in possible)
            {
                var exe = Path.Combine(path, "ffmpeg.exe");
                if (File.Exists(exe)) return exe;
            }

            return null;
        }
    }
}
