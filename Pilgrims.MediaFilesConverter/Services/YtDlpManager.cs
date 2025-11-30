using System;
using System.Diagnostics;
using System.IO;
using System.Net.Http;
using System.Reflection;
using System.Runtime.InteropServices;
using System.Threading.Tasks;

namespace Pilgrims.MediaFilesConverter.Services
{
    /// <summary>
    /// Manages yt-dlp executable detection, downloading, and availability
    /// </summary>
    public class YtDlpManager
    {
        private static readonly Lazy<YtDlpManager> _instance = new(() => new YtDlpManager());
        public static YtDlpManager Instance => _instance.Value;

        private readonly HttpClient _httpClient;
        private readonly string _bundledPath;
        private readonly string _userLocalPath;
        private string? _cachedExecutablePath;

        private YtDlpManager()
        {
            _httpClient = new HttpClient();
            
            // Get assembly directory for bundled path
            var assemblyLocation = Assembly.GetExecutingAssembly().Location;
            var assemblyDirectory = Path.GetDirectoryName(assemblyLocation)!;
            _bundledPath = Path.Combine(assemblyDirectory, "FFmpeg", "yt-dlp.exe");
            
            // Get user local app data path
            var userAppData = Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData);
            _userLocalPath = Path.Combine(userAppData, "PilgrimsMediaConverter", "yt-dlp.exe");
        }

        /// <summary>
        /// Gets the path to the yt-dlp executable, checking multiple locations
        /// </summary>
        public string? GetYtDlpPath()
        {
            // Return cached path if available
            if (_cachedExecutablePath != null && File.Exists(_cachedExecutablePath))
            {
                return _cachedExecutablePath;
            }

            // Check locations in order of preference
            var possiblePaths = new[]
            {
                _bundledPath,                    // Bundled with application
                _userLocalPath,                  // User local app data
                "yt-dlp.exe",                    // Current directory
                "yt-dlp",                        // Current directory (Linux/Mac)
                GetSystemPathYtDlp()             // System PATH
            };

            foreach (var path in possiblePaths)
            {
                if (!string.IsNullOrEmpty(path) && File.Exists(path))
                {
                    _cachedExecutablePath = path;
                    return path;
                }
            }

            return null;
        }

        /// <summary>
        /// Checks if yt-dlp is available and working
        /// </summary>
        public async Task<YtDlpAvailabilityResult> CheckAvailabilityAsync()
        {
            var path = GetYtDlpPath();
            if (string.IsNullOrEmpty(path))
            {
                return new YtDlpAvailabilityResult
                {
                    IsAvailable = false,
                    ErrorMessage = "yt-dlp executable not found",
                    SuggestedAction = "Download yt-dlp from https://github.com/yt-dlp/yt-dlp/releases"
                };
            }

            try
            {
                var processInfo = new ProcessStartInfo
                {
                    FileName = path,
                    Arguments = "--version",
                    UseShellExecute = false,
                    RedirectStandardOutput = true,
                    RedirectStandardError = true,
                    CreateNoWindow = true
                };

                using var process = new Process { StartInfo = processInfo };
                process.Start();
                
                var output = await process.StandardOutput.ReadToEndAsync();
                var error = await process.StandardError.ReadToEndAsync();
                await process.WaitForExitAsync();

                if (process.ExitCode == 0)
                {
                    return new YtDlpAvailabilityResult
                    {
                        IsAvailable = true,
                        ExecutablePath = path,
                        Version = output.Trim(),
                        ErrorMessage = null,
                        SuggestedAction = null
                    };
                }
                else
                {
                    return new YtDlpAvailabilityResult
                    {
                        IsAvailable = false,
                        ErrorMessage = $"yt-dlp version check failed: {error}",
                        SuggestedAction = "Check if yt-dlp is corrupted and reinstall if necessary"
                    };
                }
            }
            catch (Exception ex)
            {
                return new YtDlpAvailabilityResult
                {
                    IsAvailable = false,
                    ErrorMessage = $"Error checking yt-dlp: {ex.Message}",
                    SuggestedAction = "Check file permissions and antivirus software"
                };
            }
        }

        /// <summary>
        /// Downloads yt-dlp to the user local directory
        /// </summary>
        public async Task<bool> DownloadYtDlpAsync(IProgress<string>? progress = null)
        {
            try
            {
                progress?.Report("Starting yt-dlp download...");

                // Ensure directory exists
                var directory = Path.GetDirectoryName(_userLocalPath)!;
                Directory.CreateDirectory(directory);

                // Determine the download URL based on OS
                var downloadUrl = GetYtDlpDownloadUrl();
                if (string.IsNullOrEmpty(downloadUrl))
                {
                    progress?.Report("Unsupported operating system for automatic download");
                    return false;
                }

                progress?.Report($"Downloading from {downloadUrl}...");

                // Download the file
                var response = await _httpClient.GetAsync(downloadUrl);
                if (!response.IsSuccessStatusCode)
                {
                    progress?.Report($"Download failed: {response.StatusCode}");
                    return false;
                }

                // Save to temporary file first
                var tempPath = _userLocalPath + ".tmp";
                using (var fileStream = new FileStream(tempPath, FileMode.Create, FileAccess.Write))
                {
                    await response.Content.CopyToAsync(fileStream);
                }

                // On Unix systems, make it executable
                if (!RuntimeInformation.IsOSPlatform(OSPlatform.Windows))
                {
                    try
                    {
                        var chmod = new ProcessStartInfo("chmod", $"+x \"{tempPath}\"")
                        {
                            UseShellExecute = false,
                            CreateNoWindow = true
                        };
                        using var process = Process.Start(chmod);
                        process?.WaitForExit();
                    }
                    catch
                    {
                        // Ignore chmod errors
                    }
                }

                // Move to final location
                File.Move(tempPath, _userLocalPath, true);
                
                // Clear cache
                _cachedExecutablePath = null;

                progress?.Report("yt-dlp downloaded successfully");
                return true;
            }
            catch (Exception ex)
            {
                progress?.Report($"Download error: {ex.Message}");
                return false;
            }
        }

        /// <summary>
        /// Gets the download URL for yt-dlp based on the current OS
        /// </summary>
        private string GetYtDlpDownloadUrl()
        {
            const string baseUrl = "https://github.com/yt-dlp/yt-dlp/releases/latest/download/";

            if (RuntimeInformation.IsOSPlatform(OSPlatform.Windows))
            {
                return baseUrl + "yt-dlp.exe";
            }
            else if (RuntimeInformation.IsOSPlatform(OSPlatform.Linux))
            {
                return baseUrl + "yt-dlp";
            }
            else if (RuntimeInformation.IsOSPlatform(OSPlatform.OSX))
            {
                return baseUrl + "yt-dlp";
            }

            return string.Empty;
        }

        /// <summary>
        /// Checks if yt-dlp is available in the system PATH
        /// </summary>
        private string? GetSystemPathYtDlp()
        {
            try
            {
                var fileName = RuntimeInformation.IsOSPlatform(OSPlatform.Windows) ? "yt-dlp.exe" : "yt-dlp";
                var pathEnv = Environment.GetEnvironmentVariable("PATH");
                
                if (string.IsNullOrEmpty(pathEnv))
                    return null;

                var paths = pathEnv.Split(Path.PathSeparator);
                foreach (var path in paths)
                {
                    var fullPath = Path.Combine(path.Trim(), fileName);
                    if (File.Exists(fullPath))
                    {
                        return fullPath;
                    }
                }
            }
            catch
            {
                // Ignore errors in PATH checking
            }

            return null;
        }

        public void Dispose()
        {
            _httpClient?.Dispose();
        }
    }

    /// <summary>
    /// Result of yt-dlp availability check
    /// </summary>
    public class YtDlpAvailabilityResult
    {
        public bool IsAvailable { get; set; }
        public string? ExecutablePath { get; set; }
        public string? Version { get; set; }
        public string? ErrorMessage { get; set; }
        public string? SuggestedAction { get; set; }
    }
}