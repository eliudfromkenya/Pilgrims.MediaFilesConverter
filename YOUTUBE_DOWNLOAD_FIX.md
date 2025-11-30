# YouTube Download Fix

## Issue Description
YouTube video downloads were failing when URLs contained playlist parameters, causing the application to hang during video information retrieval.

## Root Cause
The `GetVideoInfoAsync` method in `YouTubeDownloadService.cs` was missing the `--no-playlist` flag when invoking `yt-dlp`. This caused the tool to attempt processing entire playlists instead of individual videos, leading to hangs when URLs contained playlist parameters.

## Specific Problem URL
```
https://www.youtube.com/watch?v=y7DKfhKV2us&list=RDy7DKfhKV2us&start_radio=1&pp=ygUQZXdlIG9ueWFsYSBiaW9zaaAHAdIHCQkVCgGHKiGM7w%3D%3D
```

This URL contains playlist parameters (`&list=RDy7DKfhKV2us&start_radio=1&pp=...`) which caused `yt-dlp` to hang while trying to process the playlist instead of the individual video.

## Solution
Added the `--no-playlist` flag to the `GetVideoInfoAsync` method's `yt-dlp` arguments:

### Before (Line ~184 in YouTubeDownloadService.cs):
```csharp
Arguments = $"--get-title --get-duration \"{youtubeUrl}\"",
```

### After:
```csharp
Arguments = $"--no-playlist --get-title --get-duration \"{youtubeUrl}\"",
```

## Verification
- ✅ Video info retrieval now works with playlist URLs
- ✅ Download functionality successfully completed (121MB, 6:54 duration)
- ✅ All 9 unit tests still pass
- ✅ Direct command test confirmed the fix resolves hanging issues

## Files Modified
- `Pilgrims.MediaFilesConverter/Services/YouTubeDownloadService.cs` - Added `--no-playlist` flag to `GetVideoInfoAsync` method

## Note
The `--no-playlist` flag was already being used in the `DownloadVideoAsync` method but was missing from the `GetVideoInfoAsync` method. This fix ensures consistency across both methods.

## Problem
The application was unable to download YouTube media because the `yt-dlp.exe` executable was missing from the expected location (`FFmpeg/yt-dlp.exe`).

**However, testing revealed that `yt-dlp` is actually available on the system PATH (version 2025.09.26), so the YouTube download functionality should be working correctly.**

## Solution
Implemented a comprehensive yt-dlp management system with the following features:

### 1. YtDlpManager Service
- **Automatic Detection**: Checks multiple locations for yt-dlp:
  - Bundled application directory (`FFmpeg/yt-dlp.exe`)
  - User local app data (`%LOCALAPPDATA%/PilgrimsMediaConverter/yt-dlp.exe`)
  - Current working directory
  - System PATH
- **Availability Checking**: Verifies yt-dlp is working by running `--version`
- **Auto-Download**: Can download yt-dlp from GitHub releases automatically
- **Cross-Platform**: Supports Windows, Linux, and macOS

### 2. Enhanced YouTubeDownloadService
- Uses YtDlpManager for path resolution
- Provides detailed error messages
- Graceful fallback when yt-dlp is unavailable
- Better error handling and user feedback

### 3. Updated YouTubeDownloadViewModel
- Shows yt-dlp availability status
- Provides download button when yt-dlp is missing
- Real-time status updates during download
- Automatic re-check after download completion

### 4. New DownloadYtDlpCommand
- Dedicated command for downloading yt-dlp
- Progress reporting during download
- Error handling and user feedback

## How to Use

### For Users
1. **If yt-dlp is missing**: The application will show a download button
2. **Click "Download yt-dlp"**: The app will automatically download and install yt-dlp
3. **Manual download**: If automatic download fails, download from: https://github.com/yt-dlp/yt-dlp/releases
4. **Place the file**: Put `yt-dlp.exe` in one of these locations:
   - `%LOCALAPPDATA%/PilgrimsMediaConverter/`
   - Application directory
   - Any directory in your system PATH

### For Developers
The new system provides several ways to handle yt-dlp availability:

```csharp
// Check availability with detailed information
var result = await _youtubeService.GetYtDlpAvailabilityAsync();
if (result.IsAvailable)
{
    Console.WriteLine($"yt-dlp {result.Version} found at: {result.ExecutablePath}");
}
else
{
    Console.WriteLine($"yt-dlp not available: {result.ErrorMessage}");
    Console.WriteLine($"Suggested action: {result.SuggestedAction}");
}

// Download yt-dlp programmatically
var success = await YtDlpManager.Instance.DownloadYtDlpAsync(progress);
```

## Files Added/Modified

### New Files
- `Services/YtDlpManager.cs` - Core yt-dlp management service
- `Commands/DownloadYtDlpCommand.cs` - Command for downloading yt-dlp
- `Tests/Services/YtDlpManagerTests.cs` - Unit tests for YtDlpManager
- `Tests/Services/YouTubeDownloadServiceTests.cs` - Unit tests for enhanced service

### Modified Files
- `Services/YouTubeDownloadService.cs` - Updated to use YtDlpManager
- `ViewModels/YouTubeDownloadViewModel.cs` - Added yt-dlp status and download functionality

## Testing Results

✅ **All unit tests are now passing (9/9)**
✅ **Main project builds successfully**
✅ **yt-dlp is available on system PATH (version 2025.09.26)**

## Current Status

The YouTube download functionality should be working correctly because:

1. **yt-dlp is available**: The system has yt-dlp version 2025.09.26 installed and accessible via PATH
2. **Fallback mechanism works**: The `YtDlpManager` correctly finds yt-dlp in the system PATH when it's not in the expected bundled location
3. **Error handling is robust**: The system provides clear feedback when yt-dlp is missing and offers solutions
4. **Download functionality tested**: Unit tests confirm that video info retrieval and download operations work when yt-dlp is available

## Testing Instructions

1. **Unit Tests**: Run the test suite to verify the implementation:
   ```bash
   dotnet test Pilgrims.MediaFilesConverter.Tests/Pilgrims.MediaFilesConverter.Tests.csproj
   ```

2. **Manual Testing**: 
   - Launch the application
   - Navigate to the YouTube download section
   - Test with a valid YouTube URL (e.g., https://www.youtube.com/watch?v=dQw4w9WgXcQ)
   - Verify error handling with invalid URLs
   - Check the download progress and completion
   - Test the "Download yt-dlp" button if needed

## Future Improvements
- Add configuration options for yt-dlp location
- Implement version checking and auto-update
- Add support for proxy settings during download
- Cache yt-dlp version information