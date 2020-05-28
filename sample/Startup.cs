using Microsoft.Azure.Functions.Extensions.DependencyInjection;
[assembly: FunctionsStartup(typeof(zyin.Extensions.AzureFunction.Configuration.Sample.Startup))]

namespace zyin.Extensions.AzureFunction.Configuration.Sample
{
    using Microsoft.Extensions.Configuration;
    using Microsoft.Extensions.DependencyInjection;
    using zyin.Extensions.AzureFunction.Configuration;

    /// <summary>
    /// Startup class
    /// </summary>
    public class Startup : FunctionsStartup
    {
        /// <summary>
        /// Configure services
        /// </summary>
        /// <param name="builder">host builder</param>
        public override void Configure(IFunctionsHostBuilder builder)
        {
            // Add secrets
            builder.TryAddAppSettingsAndSecrets<Startup>(SampleHostEnvironment.HostEnvironments);
            
            // Inject IOptions pattern for AppConfig (which can reference KeyVault secrets)
            builder.Services
                .AddOptions<AppConfig>()
                .Configure<IConfiguration>((appConfig, configuration) =>
                {
                    configuration.GetSection("AppConfig").Bind(appConfig);
                });
        }
    }
}