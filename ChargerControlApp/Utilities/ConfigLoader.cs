using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

using Microsoft.Extensions.Configuration;
using System.IO;

namespace ChargerControlApp.Utilities
{
    public class ConfigLoader
    {
        private static IConfiguration _configuration;

        public static void Load()
        {
            var builder = new ConfigurationBuilder()
                .SetBasePath(AppContext.BaseDirectory)
                .AddJsonFile("appsettings.json", optional: false, reloadOnChange: true);
            _configuration = builder.Build();
        }

        public static AppSettings GetSettings()
        {
            var settings = new AppSettings();
            _configuration?.GetSection("AppSettings")?.Bind(settings);
            return settings;
        }
    }
}
