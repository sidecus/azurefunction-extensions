namespace zyin.Extensions.AzureFunction.Configuration
{
    using System;
    using Microsoft.Extensions.Hosting;

    /// <summary>
    /// Environment class which supports Environment inheritance
    /// </summary>
    public class HostEnvironment
    {
        /// <summary>
        /// Hosting environment, only read from app settings (local.settings.json for local or App settings in Azure)
        /// https://docs.microsoft.com/en-us/azure/azure-functions/functions-app-settings#azure_functions_environment
        /// </summary>
        public static string Environment => System.Environment.GetEnvironmentVariable("AZURE_FUNCTIONS_ENVIRONMENT") ?? EnvironmentName.Production;

        /// <summary>
        /// Is the current environment a development env
        /// </summary>
        public static bool IsDevelopment => Environment == EnvironmentName.Development;

        /// <summary>
        /// Gets the environment name which is set in IHostEnvironment
        /// </summary>
        public string Name { get; }

        /// <summary>
        /// Gets the parent environment which we want to inherit settings from
        /// </summary>
        public HostEnvironment Parent { get; } = null;

        /// <summary>
        /// Initializes a new Environment object
        /// </summary>
        /// <param name="name">environment name which is set in IHostEnvironment</param>
        /// <param name="parent">Parent environment, can be null and defaults to null</param>
        public HostEnvironment(string name, HostEnvironment parent = null)
        {
            this.Name = name ?? throw new ArgumentNullException(nameof(name));
            this.Parent = parent;
        }
    }
}