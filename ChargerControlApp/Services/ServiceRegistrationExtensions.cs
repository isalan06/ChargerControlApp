using ChargerControlApp.Hardware;
using Microsoft.Extensions.DependencyInjection;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ChargerControlApp.Services
{
    public static class ServiceRegistrationExtensions
    {
        public static IServiceCollection RegisterChargingServices(this IServiceCollection services)
        {
            // 註冊所有狀態
            services.AddSingleton<InitializationState>();
            services.AddSingleton<IdleState>();
            services.AddSingleton<ReservedState>();
            services.AddSingleton<ReservationTimeoutState>();
            services.AddSingleton<OccupiedState>();
            services.AddSingleton<ChargingStateClass>();
            services.AddSingleton<ErrorState>();

            // 註冊狀態機與硬體控制
            services.AddSingleton<ChargingStationStateMachine>();
            services.AddSingleton<HardwareManager>();

            return services;
        }
    }
}
