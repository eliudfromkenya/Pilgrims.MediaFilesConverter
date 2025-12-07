using System;
using System.Threading.Tasks;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Pilgrims.MediaFilesConverter.Services;
using Pilgrims.MediaFilesConverter.Services.UtilityUpgrade;

namespace UtilityUpgradeTest
{
    class Program
    {
        static async Task Main(string[] args)
        {
            Console.WriteLine("Utility Upgrade System Test");
            Console.WriteLine("==========================");

            // Setup DI container
            var services = new ServiceCollection();
            
            // Add logging
            services.AddLogging(builder => builder.AddConsole().SetMinimumLevel(LogLevel.Debug));
            
            // Add configuration
            services.AddSingleton<UtilityConfigurationService>();
            
            // Add services
            services.AddSingleton<FFmpegManager>();
            services.AddSingleton<YtDlpManager>();
            services.AddSingleton<UtilityUpgradeService>();
            services.AddSingleton<IUtilityUpgradeService, UtilityUpgradeService>();
            
            var serviceProvider = services.BuildServiceProvider();

            try
            {
                // Test 1: Get current versions
                Console.WriteLine("\n1. Testing version checking...");
                var upgradeService = serviceProvider.GetRequiredService<IUtilityUpgradeService>();
                
                var ffmpegVersion = await upgradeService.GetFFmpegVersionAsync();
                var ytDlpVersion = await upgradeService.GetYtDlpVersionAsync();
                
                Console.WriteLine($"FFmpeg Version: {ffmpegVersion ?? "Not found"}");
                Console.WriteLine($"yt-dlp Version: {ytDlpVersion ?? "Not found"}");

                // Test 2: Check upgrade status
                Console.WriteLine("\n2. Testing upgrade status...");
                var ffmpegStatus = await upgradeService.GetFFmpegUpgradeStatusAsync();
                var ytDlpStatus = await upgradeService.GetYtDlpUpgradeStatusAsync();
                
                Console.WriteLine($"FFmpeg Status: {ffmpegStatus.Status}");
                Console.WriteLine($"yt-dlp Status: {ytDlpStatus.Status}");

                // Test 3: Check upgrade availability
                Console.WriteLine("\n3. Testing upgrade availability...");
                var ffmpegAvailable = await upgradeService.IsFFmpegUpgradeAvailableAsync();
                var ytDlpAvailable = await upgradeService.IsYtDlpUpgradeAvailableAsync();
                
                Console.WriteLine($"FFmpeg Upgrade Available: {ffmpegAvailable}");
                Console.WriteLine($"yt-dlp Upgrade Available: {ytDlpAvailable}");

                Console.WriteLine("\nAll tests completed successfully!");
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error during testing: {ex.Message}");
                Console.WriteLine($"Stack trace: {ex.StackTrace}");
            }

            Console.WriteLine("\nPress any key to exit...");
            Console.ReadKey();
        }
    }
}