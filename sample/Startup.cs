using Microsoft.Azure.Functions.Extensions.DependencyInjection;
[assembly: FunctionsStartup(typeof(zyin.Extensions.AzureFunction.Configuration.Sample.Startup))]

namespace zyin.Extensions.AzureFunction.Configuration.Sample
{
    using System.Collections.Generic;
    using Microsoft.AspNetCore.Hosting;
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
            // Add settings and secrets
            builder.TryAddAppSettingsAndSecrets<Startup>(this.BuildEnvironments());
            
            // Inject IOptions pattern for AppConfig (which can reference KeyVault secrets)
            builder.Services
                .AddOptions<AppConfig>()
                .Configure<IConfiguration>((appConfig, configuration) =>
                {
                    configuration.GetSection("AppConfig").Bind(appConfig);
                });
        }

        /// <summary>
        /// Build known environments for our app.
        /// In this sample, we have 3 environments: Development (inheriting settings from PPE), PPE (pre-production-env), and Production.
        /// </summary>
        /// <returns>Environment list</returns>
        private IEnumerable<HostEnvironment> BuildEnvironments()
        {
            var prod = new HostEnvironment(EnvironmentName.Production);
            var ppe = new HostEnvironment("PPE");
            var dev = new HostEnvironment(EnvironmentName.Development, parent: ppe);

            return new List<HostEnvironment>() { prod, dev, ppe };
        }
    }
}