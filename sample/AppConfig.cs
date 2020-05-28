namespace zyin.Extensions.AzureFunction.Configuration.Sample
{

    /// <summary>
    /// Sample app config
    /// </summary>
    public class AppConfig
    {
        /// <summary>
        /// Gets or sets an integer config
        /// </summary>
        public int IntConfig { get; set; }

        /// <summary>
        /// Gets or sets a string config
        /// </summary>
        public string StringConfig { get; set; }

        /// <summary>
        /// Secret value in config bound from user secrets or Azure KeyVault
        /// </summary>
        public string AppSecret { get; set; }
    }
}