using System;
using System.Threading.Tasks;
using Pilgrims.MediaFilesConverter.Services;

class Program
{
    static async Task Main()
    {
        var service = YouTubeDownloadService.Instance;
        
        Console.WriteLine("Testing GetYtDlpAvailabilityAsync...");
        
        try
        {
            var result = await service.GetYtDlpAvailabilityAsync();
            
            Console.WriteLine($"Result is null: {result == null}");
            if (result != null)
            {
                Console.WriteLine($"IsAvailable: {result.IsAvailable}");
                Console.WriteLine($"ErrorMessage: {result.ErrorMessage}");
                Console.WriteLine($"SuggestedAction: {result.SuggestedAction}");
                Console.WriteLine($"ExecutablePath: {result.ExecutablePath}");
                Console.WriteLine($"Version: {result.Version}");
            }
        }
        catch (Exception ex)
        {
            Console.WriteLine($"Exception: {ex.Message}");
            Console.WriteLine($"Stack trace: {ex.StackTrace}");
        }
    }
}