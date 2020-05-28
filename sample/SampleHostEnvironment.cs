namespace zyin.Extensions.AzureFunction.Configuration.Sample
{
    using System.Collections.Generic;
    using Microsoft.Extensions.Hosting;
    using zyin.Extensions.AzureFunction.Configuration;
    
    /// <summary>
    /// Environments definition for our app
    /// </summary>
    public static class SampleHostEnvironment
    {
        /// <summary>
        /// Environment list
        /// </summary>
        public static readonly List<HostEnvironment> HostEnvironments;

        /// <summary>
        /// Initializes the app environments
        /// </summary>
        static SampleHostEnvironment()
        {
            var prod = new HostEnvironment(EnvironmentName.Production);
            var ppe = new HostEnvironment("PPE");
            var dev = new HostEnvironment(EnvironmentName.Development, parent: ppe);

            HostEnvironments = new List<HostEnvironment>() { prod, dev, ppe };
        }
    }
}
