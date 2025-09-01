using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ChargerControlApp.Services
{
    public class AppServices
    {
        public IServiceProvider ServiceProvider { get; }

        public AppServices(IServiceProvider serviceProvider)
        {
            ServiceProvider = serviceProvider;
        }
    }
}
