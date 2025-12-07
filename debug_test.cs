using System;
using System.Diagnostics;
using System.IO;
using System.Threading.Tasks;
using FFMpegCore;
using Pilgrims.MediaFilesConverter.Models;
using Pilgrims.MediaFilesConverter.Services;

class Program
{
    static async Task Main()
    {
        Console.WriteLine("Generating sample MP4 and converting to MP3...");

        var ffmpegExe = FindFFmpeg();
        if (ffmpegExe == null)
        {
            Console.WriteLine("FFmpeg not found on system.");
            return;
        }

        GlobalFFOptions.Configure(new FFOptions { BinaryFolder = Path.GetDirectoryName(ffmpegExe)! });

        var tempDir = Path.Combine(Path.GetTempPath(), "MediaConverterDebug", Guid.NewGuid().ToString("N"));
        Directory.CreateDirectory(tempDir);

        var inputPath = Path.Combine(tempDir, "input.mp4");
        var psi = new ProcessStartInfo
        {
            FileName = ffmpegExe,
            Arguments = "-hide_banner -loglevel error -y " +
                        "-f lavfi -i color=c=black:s=320x240:d=2 " +
                        "-f lavfi -i sine=frequency=1000:duration=2 " +
                        "-shortest -c:v libx264 -pix_fmt yuv420p -c:a aac " +
                        $"\"{inputPath}\"",
            UseShellExecute = false,
            CreateNoWindow = true,
        };

        using (var p = Process.Start(psi))
        {
            p!.WaitForExit();
            if (p.ExitCode != 0)
            {
                Console.WriteLine("Failed to generate input MP4.");
                return;
            }
        }

        var svc = new MediaConverterService();
        var settings = new ConversionSettings
        {
            OutputFormat = "mp3",
            OutputDirectory = tempDir,
            AudioQuality = AudioQuality.Medium
        };

        var mediaFile = new MediaFile { FilePath = inputPath };
        var ok = await svc.ConvertMediaAsync(mediaFile, settings);
        Console.WriteLine($"Conversion result: {ok}");
        Console.WriteLine($"Output path: {mediaFile.OutputPath}");
        try
        {
            var resultFile = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "conversion_result.txt");
            File.WriteAllText(resultFile, (ok ? "OK" : "FAIL") + "\n" + (mediaFile.OutputPath ?? string.Empty));
        }
        catch {}
        if (ok && File.Exists(mediaFile.OutputPath!))
        {
            var info = await FFProbe.AnalyseAsync(mediaFile.OutputPath!);
            Console.WriteLine($"Video streams: {info.VideoStreams.Count}, Audio streams: {info.AudioStreams.Count}");
            try
            {
                var resultFile = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "conversion_result.txt");
                File.AppendAllText(resultFile, "\n" + $"Video:{info.VideoStreams.Count},Audio:{info.AudioStreams.Count}");
            }
            catch {}
        }
    }

    static string? FindFFmpeg()
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
