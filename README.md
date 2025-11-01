# Pilgrims.MediaFilesConverter

A cross-platform media utility focused on converting audio/video files and downloading media from YouTube, delivered with a clean, modern UI. Built with .NET using Avalonia, and targeting Windows Desktop, Mac and Linux, Browser (WebAssembly), Android, and iOS projects in this repository.

## Purpose
Provide an easy-to-use, consistent interface for:
- Converting media files between formats (video/audio)
- Selecting output quality and format options
- Previewing videos before conversion
- Downloading media from YouTube using yt-dlp

## Technologies Used
- .NET / C#
- Avalonia UI (MVVM pattern)
- FFmpeg (conversion and probing)
- yt-dlp (YouTube downloads)
- Multi-target projects: Desktop (Windows, Mac and Linux), Browser, Android, iOS

## Key Features / Functions
- Media conversion service to transcode video/audio using FFmpeg
- YouTube download modal and service to fetch videos via yt-dlp
- Video preview control for quick inspection prior to conversion
- Theming support (Light/Dark) with a dedicated ThemeService
- Quality selectors for audio/video (e.g., bitrate, resolution)
- Intuitive UI for selecting source files and destination folders

## Screenshots (Sample UI)
![Main UI]("Screen Images/Screenshot1.png")
![Conversion/Preview UI]("Screen Images/Screenshot2.png")
