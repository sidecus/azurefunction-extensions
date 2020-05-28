namespace zyin.Extensions.AzureFunction.Configuration.Sample
{
    using System;
    using Microsoft.AspNetCore.Mvc;
    using Microsoft.Azure.WebJobs;
    using Microsoft.Azure.WebJobs.Extensions.Http;
    using Microsoft.AspNetCore.Http;
    using Microsoft.Extensions.Logging;
    using Microsoft.Extensions.Options;
    using Microsoft.Extensions.Configuration;

    /// <summary>
    /// class demoing HttpTrigger with user secrets and keyvault support
    /// </summary>
    public class MyHttpTrigger
    {
        /// <summary>
        /// Reference to the ICofiguratoin object
        /// </summary>
        private readonly IConfiguration configuration;

        /// <summary>
        /// Reference to app config
        /// </summary>
        private readonly AppConfig appConfig;

        /// <summary>
        /// Initializes a new MyHttpTriggerFunction class.
        /// It takes IAzureKeyVaultService from the DI container.
        /// </summary>
        /// <param name="configuration">configuration</param>
        /// <param name="appConfigOptions">app config options</param>
        public MyHttpTrigger(IConfiguration configuration, IOptions<AppConfig> appConfigOptions)
        {
            // Usually you only need to access your settings via IOptions pattern.
            // I am referencing configuration here for demo purpose.
            this.configuration = configuration ?? throw new ArgumentNullException(nameof(configuration));
            this.appConfig = appConfigOptions?.Value ?? throw new ArgumentNullException(nameof(appConfigOptions));
        }

        /// <summary>
        /// An Azure function endpoint to dump settings - setting can be from app settings, appsettings*.json, or KeyVault.
        /// This is for demo purpose, don't do this in production since settings can contain secrets.
        /// 1. We use ActionResult<T> instead of IActionResult for better return type checking.
        /// </summary>
        /// <param name="req">http request</param>
        /// <param name="settingName">settting name</param>
        /// <returns>message contains the setting value</returns>
        [FunctionName("ShowSetting")]
        public ActionResult<string> ShowSetting(
            [HttpTrigger(AuthorizationLevel.Function, "get", Route = "settings/{settingName}")] HttpRequest req,
            string settingName,
            ILogger log)
        {
            // !!!This is just for demo purpose!!!
            // DO NOT do this in prod - you should never dump secrets.
            var secretValue = this.configuration.GetValue<string>(settingName);
            var message = secretValue != null ? $"{settingName}: {secretValue}" : $"Setting {settingName} doesn't exist.";
            return message;
        }

        /// <summary>
        /// An Azure function endpoint to render AppConfig
        /// </summary>
        /// <param name="req">http request</param>
        /// <returns>AppConfig</returns>
        [FunctionName("ShowAppConfig")]
        public ActionResult<AppConfig> ShowAppConfig(
            [HttpTrigger(AuthorizationLevel.Function, "get", Route = "appconfig")] HttpRequest req,
            ILogger log)
        {
            // !!!This is just for demo purpose!!!
            // DO NOT do this in prod - you should never blindly dump config settings since it can contain secrets.
            return this.appConfig;
        }
    }
}
