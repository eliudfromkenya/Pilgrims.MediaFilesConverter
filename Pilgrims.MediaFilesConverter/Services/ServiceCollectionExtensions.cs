using System;
using System.Net.Http;
using System.Threading.Tasks;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Pilgrims.MediaFilesConverter.Services.Interfaces;
using Pilgrims.MediaFilesConverter.Services.UtilityUpgrade;

namespace Pilgrims.MediaFilesConverter.Services
{
    /// <summary>
    /// Extension methods for configuring utility upgrade services in the dependency injection container
    /// </summary>
    public static class ServiceCollectionExtensions
    {
        /// <summary>
        /// Adds utility upgrade services to the dependency injection container
        /// </summary>
        /// <param name="services">The service collection</param>
        /// <returns>The service collection for chaining</returns>
        public static IServiceCollection AddUtilityUpgradeServices(this IServiceCollection services)
        {
            if (services == null)
                throw new ArgumentNullException(nameof(services));

            // Register HttpClient for version checkers and download service
            services.AddHttpClient();

            // Register version checkers
            services.AddSingleton<FFmpegVersionChecker>();
            services.AddSingleton<YtDlpVersionChecker>();

            // Register core services
            services.AddSingleton<IDownloadService, DownloadService>();
            services.AddSingleton<IExtractionService, ExtractionService>();
            services.AddSingleton<IUtilityConfigurationService, UtilityConfigurationService>();

            // Register upgrade services
            services.AddSingleton<FFmpegUpgradeService>();
            services.AddSingleton<YtDlpUpgradeService>();

            // Register main utility upgrade service
            services.AddSingleton<UtilityUpgradeService>();

            // Register message service for user notifications
            services.AddSingleton<IMessageService, MessageService>();

            return services;
        }

        /// <summary>
        /// Adds utility upgrade services with custom HttpClient configuration
        /// </summary>
        /// <param name="services">The service collection</param>
        /// <param name="configureHttpClient">HttpClient configuration action</param>
        /// <returns>The service collection for chaining</returns>
        public static IServiceCollection AddUtilityUpgradeServices(
            this IServiceCollection services,
            Action<HttpClient> configureHttpClient)
        {
            if (services == null)
                throw new ArgumentNullException(nameof(services));

            if (configureHttpClient == null)
                throw new ArgumentNullException(nameof(configureHttpClient));

            // Register HttpClient with custom configuration
            services.AddHttpClient("UtilityUpgrade", configureHttpClient);

            // Register version checkers
            services.AddSingleton<FFmpegVersionChecker>();
            services.AddSingleton<YtDlpVersionChecker>();

            // Register core services
            services.AddSingleton<IDownloadService, DownloadService>();
            services.AddSingleton<IExtractionService, ExtractionService>();
            services.AddSingleton<IUtilityConfigurationService, UtilityConfigurationService>();

            // Register upgrade services
            services.AddSingleton<FFmpegUpgradeService>();
            services.AddSingleton<YtDlpUpgradeService>();

            // Register main utility upgrade service
            services.AddSingleton<UtilityUpgradeService>();

            return services;
        }

        /// <summary>
        /// Adds utility upgrade services with custom HttpClient factory
        /// </summary>
        /// <param name="services">The service collection</param>
        /// <param name="httpClientFactory">HttpClient factory function</param>
        /// <returns>The service collection for chaining</returns>
        public static IServiceCollection AddUtilityUpgradeServices(
            this IServiceCollection services,
            Func<IServiceProvider, HttpClient> httpClientFactory)
        {
            if (services == null)
                throw new ArgumentNullException(nameof(services));

            if (httpClientFactory == null)
                throw new ArgumentNullException(nameof(httpClientFactory));

            // Register HttpClient with factory
            services.AddSingleton(httpClientFactory);

            // Register version checkers
            services.AddSingleton<FFmpegVersionChecker>();
            services.AddSingleton<YtDlpVersionChecker>();

            // Register core services
            services.AddSingleton<IDownloadService, DownloadService>();
            services.AddSingleton<IExtractionService, ExtractionService>();
            services.AddSingleton<IUtilityConfigurationService, UtilityConfigurationService>();

            // Register upgrade services
            services.AddSingleton<FFmpegUpgradeService>();
            services.AddSingleton<YtDlpUpgradeService>();

            // Register main utility upgrade service
            services.AddSingleton<UtilityUpgradeService>();

            return services;
        }
    }
}