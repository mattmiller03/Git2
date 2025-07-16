using Microsoft.Extensions.DependencyInjection;
using System;

namespace UiDesktopApp2.Services
{
    /// <summary>
    /// Service locator for accessing dependency injection services
    /// </summary>
    public static class ServiceLocator
    {
        private static IServiceProvider? _serviceProvider;

        /// <summary>
        /// Initialize the service locator with the service provider
        /// </summary>
        /// <param name="serviceProvider">The service provider from dependency injection</param>
        public static void Initialize(IServiceProvider serviceProvider)
        {
            _serviceProvider = serviceProvider ?? throw new ArgumentNullException(nameof(serviceProvider));
        }

        /// <summary>
        /// Get a required service from the container
        /// </summary>
        /// <typeparam name="T">Type of service to retrieve</typeparam>
        /// <returns>The service instance</returns>
        /// <exception cref="InvalidOperationException">Thrown when ServiceLocator is not initialized</exception>
        public static T GetRequiredService<T>() where T : notnull
        {
            if (_serviceProvider == null)
                throw new InvalidOperationException("ServiceLocator not initialized. Call Initialize() first.");

            return _serviceProvider.GetRequiredService<T>();
        }

        /// <summary>
        /// Get a service from the container (returns null if not found)
        /// </summary>
        /// <typeparam name="T">Type of service to retrieve</typeparam>
        /// <returns>The service instance or null</returns>
        /// <exception cref="InvalidOperationException">Thrown when ServiceLocator is not initialized</exception>
        public static T? GetService<T>() where T : class
        {
            if (_serviceProvider == null)
                throw new InvalidOperationException("ServiceLocator not initialized. Call Initialize() first.");

            return _serviceProvider.GetService<T>();
        }

        /// <summary>
        /// Check if the service locator is initialized
        /// </summary>
        public static bool IsInitialized => _serviceProvider != null;
    }
}
