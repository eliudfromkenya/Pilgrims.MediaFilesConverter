using System;
using System.IO;
using System.Threading;
using System.Threading.Tasks;
using FFMpegCore;
using FFMpegCore.Enums;
using Pilgrims.MediaFilesConverter.Models;

namespace Pilgrims.MediaFilesConverter.Services
{
    public class MediaConverterService
    {
        public async Task<bool> ConvertMediaAsync(MediaFile mediaFile, ConversionSettings settings, 
            IProgress<double>? progress = null, CancellationToken cancellationToken = default)
        {
            try
            {
                var outputPath = Path.Combine(settings.OutputDirectory, 
                    $"{Path.GetFileNameWithoutExtension(mediaFile.FilePath)}.{settings.OutputFormat}");

                var mediaInfo = await FFProbe.AnalyseAsync(mediaFile.FilePath);
                
                var conversion = FFMpegArguments
                    .FromFileInput(mediaFile.FilePath)
                    .OutputToFile(outputPath, true, options => ConfigureOutput(options, settings, mediaInfo));

                var result = await conversion.ProcessAsynchronously(true);
                
                if (result)
                {
                    mediaFile.OutputPath = outputPath;
                    mediaFile.Status = ConversionStatus.Completed;
                    mediaFile.Progress = 100;
                    progress?.Report(100);
                }
                else
                {
                    mediaFile.Status = ConversionStatus.Failed;
                }

                return result;
            }
            catch (Exception ex)
            {
                var dd = ex.ToString();
                mediaFile.Status = ConversionStatus.Failed;
                return false;
            }
        }

        private void ConfigureOutput(FFMpegArgumentOptions options, ConversionSettings settings, IMediaAnalysis mediaInfo)
        {
            var isAudioOnly = IsAudioOutputFormat(settings.OutputFormat);

            // Set video codec and quality only when output is a video container
            if (!isAudioOnly && mediaInfo.VideoStreams.Count > 0)
            {
                switch (settings.VideoQuality)
                {
                    case Models.VideoQuality.Low:
                        options.WithVideoCodec(VideoCodec.LibX264).WithConstantRateFactor(28);
                        break;
                    case Models.VideoQuality.Medium:
                        options.WithVideoCodec(VideoCodec.LibX264).WithConstantRateFactor(23);
                        break;
                    case Models.VideoQuality.High:
                        options.WithVideoCodec(VideoCodec.LibX264).WithConstantRateFactor(18);
                        break;
                    case Models.VideoQuality.VeryHigh:
                        options.WithVideoCodec(VideoCodec.LibX264).WithConstantRateFactor(15);
                        break;
                }

                // Set resolution if specified
                if (!string.IsNullOrEmpty(settings.Resolution) && settings.Resolution != "Original")
                {
                    var parts = settings.Resolution.Split('x');
                    if (parts.Length == 2 && int.TryParse(parts[0], out int width) && int.TryParse(parts[1], out int height))
                    {
                        options.Resize(width, height);
                    }
                }

                // Apply cropping if enabled
                if (settings.EnableCropping && settings.CropWidth > 0 && settings.CropHeight > 0)
                {
                    options.WithVideoFilters(filterOptions => filterOptions
                        .Scale(settings.CropWidth, settings.CropHeight));
                }
            }

            // Set audio codec and quality
            if (mediaInfo.AudioStreams.Count > 0)
            {
                if (string.Equals(settings.OutputFormat, "mp3", StringComparison.OrdinalIgnoreCase))
                {
                    switch (settings.AudioQuality)
                    {
                        case Models.AudioQuality.Low:
                            options.WithAudioCodec(AudioCodec.LibMp3Lame).WithAudioBitrate(96);
                            break;
                        case Models.AudioQuality.Medium:
                            options.WithAudioCodec(AudioCodec.LibMp3Lame).WithAudioBitrate(128);
                            break;
                        case Models.AudioQuality.High:
                            options.WithAudioCodec(AudioCodec.LibMp3Lame).WithAudioBitrate(192);
                            break;
                        case Models.AudioQuality.VeryHigh:
                            options.WithAudioCodec(AudioCodec.LibMp3Lame).WithAudioBitrate(320);
                            break;
                    }
                }
                else
                {
                    switch (settings.AudioQuality)
                    {
                        case Models.AudioQuality.Low:
                            options.WithAudioCodec(AudioCodec.Aac).WithAudioBitrate(96);
                            break;
                        case Models.AudioQuality.Medium:
                            options.WithAudioCodec(AudioCodec.Aac).WithAudioBitrate(128);
                            break;
                        case Models.AudioQuality.High:
                            options.WithAudioCodec(AudioCodec.Aac).WithAudioBitrate(192);
                            break;
                        case Models.AudioQuality.VeryHigh:
                            options.WithAudioCodec(AudioCodec.Aac).WithAudioBitrate(320);
                            break;
                    }
                }

                if (isAudioOnly)
                {
                    // Ensure video is not included in audio-only outputs
                    options.WithCustomArgument("-vn");
                }
            }

            // Apply trimming if enabled
            if (settings.EnableTrimming && settings.StartTime.HasValue)
            {
                options.Seek(TimeSpan.FromSeconds(settings.StartTime.Value));
                
                if (settings.EndTime.HasValue && settings.EndTime > settings.StartTime)
                {
                    var duration = TimeSpan.FromSeconds(settings.EndTime.Value - settings.StartTime.Value);
                    options.WithDuration(duration);
                }
            }
        }

        private static bool IsAudioOutputFormat(string format)
        {
            if (string.IsNullOrWhiteSpace(format)) return false;
            var audioFormats = ConversionSettings.GetSupportedAudioFormats();
            foreach (var f in audioFormats)
            {
                if (string.Equals(f, format, StringComparison.OrdinalIgnoreCase))
                    return true;
            }
            return false;
        }

        public async Task<bool> SplitMediaAsync(MediaFile mediaFile, double splitTimeSeconds, 
            string outputDirectory, CancellationToken cancellationToken = default)
        {
            try
            {
                var mediaInfo = await FFProbe.AnalyseAsync(mediaFile.FilePath);
                var totalDuration = mediaInfo.Duration;
                var splitTime = TimeSpan.FromSeconds(splitTimeSeconds);

                if (splitTime >= totalDuration)
                    return false;

                var baseName = Path.GetFileNameWithoutExtension(mediaFile.FilePath);
                var extension = Path.GetExtension(mediaFile.FilePath);

                // First part
                var firstPartPath = Path.Combine(outputDirectory, $"{baseName}_part1{extension}");
                var firstPartConversion = FFMpegArguments
                    .FromFileInput(mediaFile.FilePath)
                    .OutputToFile(firstPartPath, true, options => options
                        .WithDuration(splitTime)
                        .CopyChannel());

                // Second part
                var secondPartPath = Path.Combine(outputDirectory, $"{baseName}_part2{extension}");
                var secondPartConversion = FFMpegArguments
                    .FromFileInput(mediaFile.FilePath)
                    .OutputToFile(secondPartPath, true, options => options
                        .Seek(splitTime)
                        .CopyChannel());

                var firstResult = await firstPartConversion.ProcessAsynchronously(true);
                var secondResult = await secondPartConversion.ProcessAsynchronously(true);

                return firstResult && secondResult;
            }
            catch (Exception)
            {
                return false;
            }
        }

        public async Task<bool> JoinMediaAsync(string[] inputFiles, string outputPath, 
            CancellationToken cancellationToken = default)
        {
            try
            {
                var conversion = FFMpegArguments
                    .FromConcatInput(inputFiles)
                    .OutputToFile(outputPath, true, options => options
                        .CopyChannel());

                return await conversion.ProcessAsynchronously(true);
            }
            catch (Exception)
            {
                return false;
            }
        }

        public async Task<IMediaAnalysis?> GetMediaInfoAsync(string filePath, CancellationToken cancellationToken = default)
        {
            try
            {
                return await FFProbe.AnalyseAsync(filePath);
            }
            catch (Exception)
            {
                return null;
            }
        }
    }
}
