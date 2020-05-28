namespace zyin.Extensions.AzureFunction.Configuration
{
    using System;
    using System.Collections.Generic;
    using System.IO;
    using System.Linq;
    using Microsoft.Azure.Functions.Extensions.DependencyInjection;
    using Microsoft.Azure.KeyVault;
    using Microsoft.Azure.Services.AppAuthentication;
    using Microsoft.Extensions.Configuration;
    using Microsoft.Extensions.Configuration.AzureKeyVault;
    using Microsoft.Extensions.DependencyInjection;
    using Microsoft.Extensions.DependencyInjection.Extensions;

    /// <summary>
    /// Extension class for IFunctionsHostBuilder to enable user secrets and KeyVault.
    /// Use app id and app secret for local development and managed identity for prod.
    /// </summary>
    public static class AzureFunctionHostBuilderExtensions
    {
        /// <summary>
        /// Key vault name settings key. It can be set in:
        /// 1. local.settings.json for local development
        /// 2. appsettings.{env}.json for local development if you are using appsettings.json
        /// 3. App settings for real Azure environments (production/staging etc.)
        /// </summary>
        private static readonly string KeyVaultName = "KeyVaultName";

        /// <summary>
        /// Key vault app id - development only, should be set in user secrets.
        /// </summary>
        private static readonly string KeyVaultAppId = "KeyVaultAppId";

        /// <summary>
        /// Key vault app secret - development only, should be set in user secrets.
        /// </summary>
        private static readonly string KeyVaultAppSecret = "KeyVaultAppSecret";

        /// <summary>
        /// Add App settings, user secret and Azure key vault to FunctionHostBuilder's configuration builder.
        /// 1.appsettings.json and a chain of appsettings.{environment}.json will be added to configuration. The chain is defined by environments parameter if provider.
        /// 2.For development environment, asp.net core user secrets will be enabled.
        /// 3.Azure key vault will be added if KeyVaultName is part of the settings (either app settings or appsettings.json)
        ///   For key vault, use app id and app secret from user secrets for local development; and managed identity for Azure environments.
        /// </summary>
        /// <typeparam name="T">Startup class</typeparam>
        /// <param name="hostBuilder">host builder</param>
        /// <param name="environments">optional environment list. If not defined only appsettings.json and a chain of appsettings.{environment}.json will be added.
        /// If this paremeter is not null and it defines environment inheritance relationship, then appsettings.{parentenvironment}.json files are also added</param>
        /// <returns>host builder</returns>
        public static IFunctionsHostBuilder TryAddAppSettingsAndSecrets<T>(this IFunctionsHostBuilder hostBuilder, List<HostEnvironment> environments = null)
            where T: FunctionsStartup
        {
            if (hostBuilder == null)
            {
                throw new ArgumentNullException(nameof(hostBuilder));
            }

            ValidateEnvironments(environments);

            // Get a config builder wrapping the predefined Azure Function config
            var defaultConfig = GetDefaultAzureFunctionConfig(hostBuilder);
            var configBuilder = new ConfigurationBuilder().AddConfiguration(defaultConfig);

            // Add appsettings.*.json based on provided environment list
            configBuilder.AddAppSettings<T>(environments);

            // Add user secrets and azure keyvault
            if (HostEnvironment.IsDevelopment)
            {
                configBuilder.AddUserSecrets<T>();
            }

            configBuilder.TryAddAzureKeyVault();

            // Replace the configuration in DI container - this is a hack right now since
            // FunctionHostBuilder doesn't provide a way to customize config builder
            var newConfig = configBuilder.Build();
            hostBuilder.Services.Replace(ServiceDescriptor.Singleton(typeof(IConfiguration), newConfig));

            return hostBuilder;
        }

        /// Add appsettings.json and appsettings.{environment}.json will be added to configuration chain. They are optional.
        /// if environments parameter is provided, we'll use it for setting inheritance.
        /// </summary>
        /// <typeparam name="T">Startup class</typeparam>
        /// <param name="configBuilder">config builder</param>
        /// <param name="environments">environment list where you can define appsettings json inheritance</param>
        /// <returns>host builder</returns>
        private static IConfigurationBuilder AddAppSettings<T>(this IConfigurationBuilder configBuilder, List<HostEnvironment> environments)
            where T: FunctionsStartup
        {
            // Get assembly directory containing the startup class
            var startupDirectory = Path.GetDirectoryName(typeof(T).Assembly.Location);

            // Assembly sits in the bin folder (e.g. home/site/wwwroot/bin). Setting files sits it's parent folder.
            var settingsDirectory = Directory.GetParent(startupDirectory).FullName;

            // add json files based on environment inheritence
            configBuilder.SetBasePath(settingsDirectory).AddJsonFiles(environments);

            return configBuilder;
        }

        /// <summary>
        /// Add json file settings based on current environment and the specified
        /// environment inheritance layers.
        /// If environments param is null, only appsettings.json will be added.
        /// </summary>
        /// <param name="configBuilder">config builder</param>
        /// <param name="environments">array of defined environments</param>
        /// <returns>config builder</returns>
        private static IConfigurationBuilder AddJsonFiles(this IConfigurationBuilder configBuilder, IEnumerable<HostEnvironment> environments)
        {
            var currentEnvName = HostEnvironment.Environment;

            // Try to look the current environment up from the given environment list
            var env = environments?.FirstOrDefault(e => string.Equals(e.Name, currentEnvName, StringComparison.OrdinalIgnoreCase));
            if (env == null)
            {
                // If the current environment cannot be found from the environments list, we fall back to
                // the .net core behavior by creating a temp environment using current environment's name.
                env = new HostEnvironment(currentEnvName);
            }

            // Find the layering hierarchy (sigle direction list from children to parent to grand parent), then reverse it (parent to children).
            var layers = new List<HostEnvironment>();
            while (env != null)
            {
                layers.Add(env);
                env = env.Parent;
            }
            layers.Reverse();

            // Add json files based on parent to child chains. Note appsettings.json is always included first.
            configBuilder.AddJsonFile("appsettings.json", optional: true, reloadOnChange: false);
            foreach (var environment in layers)
            {
                configBuilder.AddJsonFile($"appsettings.{environment.Name}.json", optional: true, reloadOnChange: false);
            }

            return configBuilder;
        }

        /// <summary>
        /// Add Azure KeyVault using Managed identity.
        /// </summary>
        /// <param name="configBuilder">config builder</param>
        /// <returns>config builder</returns>
        private static IConfigurationBuilder TryAddAzureKeyVault(this IConfigurationBuilder configBuilder)
        {
            if (configBuilder == null)
            {
                throw new ArgumentNullException(nameof(configBuilder));
            }

            var tempConfig = configBuilder.Build();
            var keyVaultName = tempConfig[KeyVaultName];
            bool useKeyVault = !string.IsNullOrWhiteSpace(keyVaultName);

            if (useKeyVault)
            {
                var keyVaultUrl = $"https://{keyVaultName}.vault.azure.net/";
                if (HostEnvironment.IsDevelopment)
                {
                    // Add Azure keyvault with app id and app secret from user secrets
                    var clientId = tempConfig[KeyVaultAppId];
                    var clientSecret = tempConfig[KeyVaultAppSecret];
                    configBuilder.AddAzureKeyVault(keyVaultUrl, clientId, clientSecret);
                }
                else
                {
                    // Non-development environment. Add keyvault from managed identity
                    var azureServiceTokenProvider = new AzureServiceTokenProvider();
                    var keyVaultClient = new KeyVaultClient(new KeyVaultClient.AuthenticationCallback(azureServiceTokenProvider.KeyVaultTokenCallback));
                    configBuilder.AddAzureKeyVault(keyVaultUrl, keyVaultClient, new DefaultKeyVaultSecretManager());
                }
            }

            return configBuilder;
        }

        /// <summary>
        /// Get base configuration builder for Function app, by adding Function app original IConfiguration as config root.
        /// This is a hack since Azure function host builder doesn't expose a way to customize ConfigurationBuilder.
        /// </summary>
        /// <param name="builder"host builder></param>
        /// <returns>configuration builder</returns>
        private static IConfiguration GetDefaultAzureFunctionConfig(IFunctionsHostBuilder builder)
        {
            return builder.Services.BuildServiceProvider().GetService<IConfiguration>();
        }

        /// <summary>
        /// Validate the environments array to make sure environments are distinct
        /// </summary>
        /// <param name="environments">environment array</param>
        private static void ValidateEnvironments(IEnumerable<HostEnvironment> environments)
        {
            if (environments == null || environments.Any())
            {
                return;
            }

            var envDict = new Dictionary<string, HostEnvironment>(StringComparer.OrdinalIgnoreCase);
            foreach (var env in environments)
            {
                if (envDict.ContainsKey(env.Name))
                {
                    throw new InvalidOperationException("Environment name is not unique in the given environments array");
                }

                envDict.Add(env.Name, env);
            }
        }
    }
}