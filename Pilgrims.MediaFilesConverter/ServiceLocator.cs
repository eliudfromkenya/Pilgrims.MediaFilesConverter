using System;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;

namespace Pilgrims.MediaFilesConverter
{
    /// <summary>
    /// Service locator for accessing services throughout the application
    /// </summary>
    public static class ServiceLocator
    {
        private static IServiceProvider? _serviceProvider;
        private static bool _isInitialized = false;

        /// <summary>
        /// Gets the service provider
        /// </summary>
        public static IServiceProvider ServiceProvider => _serviceProvider ?? throw new InvalidOperationException("ServiceLocator has not been initialized");

        /// <summary>
        /// Gets a value indicating whether the service locator has been initialized
        /// </summary>
        public static bool IsInitialized => _isInitialized;

        /// <summary>
        /// Initializes the service locator with the provided service provider
        /// </summary>
        /// <param name="serviceProvider">The service provider</param>
        public static void Initialize(IServiceProvider serviceProvider)
        {
            _serviceProvider = serviceProvider ?? throw new ArgumentNullException(nameof(serviceProvider));
            _isInitialized = true;
        }

        /// <summary>
        /// Gets a service of the specified type
        /// </summary>
        /// <typeparam name="T">The service type</typeparam>
        /// <returns>The service instance</returns>
        public static T GetService<T>() where T : class
        {
            if (!_isInitialized)
                throw new InvalidOperationException("ServiceLocator has not been initialized");

            return _serviceProvider!.GetService<T>() ?? throw new InvalidOperationException($"Service of type {typeof(T).Name} not found");
        }

        /// <summary>
        /// Gets a required service of the specified type
        /// </summary>
        /// <typeparam name="T">The service type</typeparam>
        /// <returns>The service instance</returns>
        public static T GetRequiredService<T>() where T : class
        {
            if (!_isInitialized)
                throw new InvalidOperationException("ServiceLocator has not been initialized");

            return _serviceProvider!.GetRequiredService<T>();
        }

        /// <summary>
        /// Creates a service scope
        /// </summary>
        /// <returns>A service scope</returns>
        public static IServiceScope CreateScope()
        {
            if (!_isInitialized)
                throw new InvalidOperationException("ServiceLocator has not been initialized");

            return _serviceProvider!.CreateScope();
        }

        /// <summary>
        /// Gets the logger
        /// </summary>
        public static ILogger<T> GetLogger<T>() => GetRequiredService<ILogger<T>>();




    }
}