using ChargerControlApp.DataAccess.Slot.Services;
using ChargerControlApp.Hardware;
using ChargerControlApp.Utilities;
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
            services.AddSingleton<InitialState>();
            services.AddSingleton<IdleState>();
            services.AddSingleton<SwappingState>();
            services.AddSingleton<ManualState>();
            services.AddSingleton<UnspecifiedState>();
            services.AddSingleton<ErrorState>();

            // 註冊狀態機與硬體控制
            services.AddSingleton<ChargingStationStateMachine>();
            services.AddSingleton<HardwareManager>();

            return services;
        }

        public static IServiceCollection RegisterSlotService(this IServiceCollection services)
        {
            services.AddSingleton<InitializationSlotState[]>(sp => { 
                var arr = new InitializationSlotState[NPB450Controller.NPB450ControllerInstnaceMaxNumber];

                for(int i=0;i<NPB450Controller.NPB450ControllerInstnaceMaxNumber;i++)
                {
                    arr[i] = new InitializationSlotState();
                }

                return arr;
            });
            services.AddSingleton<NotUsedSlotState[]>(sp => {
                var arr = new NotUsedSlotState[NPB450Controller.NPB450ControllerInstnaceMaxNumber];
                for (int i = 0; i < NPB450Controller.NPB450ControllerInstnaceMaxNumber; i++)
                {
                    arr[i] = new NotUsedSlotState();
                }
                return arr;
            });
            services.AddSingleton<EmptySlotState[]>(sp => {
                var arr = new EmptySlotState[NPB450Controller.NPB450ControllerInstnaceMaxNumber];
                for (int i = 0; i < NPB450Controller.NPB450ControllerInstnaceMaxNumber; i++)
                {
                    arr[i] = new EmptySlotState();
                }
                return arr;
            });
            services.AddSingleton<IdleSlotState[]>(sp => {
                var arr = new IdleSlotState[NPB450Controller.NPB450ControllerInstnaceMaxNumber];
                for (int i = 0; i < NPB450Controller.NPB450ControllerInstnaceMaxNumber; i++)
                {
                    arr[i] = new IdleSlotState();
                }
                return arr;
            });
            services.AddSingleton<ChargingSlotState[]>(sp => {
                var arr = new ChargingSlotState[NPB450Controller.NPB450ControllerInstnaceMaxNumber];
                for (int i = 0; i < NPB450Controller.NPB450ControllerInstnaceMaxNumber; i++)
                {
                    arr[i] = new ChargingSlotState();
                }
                return arr;
            });
            services.AddSingleton<FloatingSlotState[]>(sp => {
                var arr = new FloatingSlotState[NPB450Controller.NPB450ControllerInstnaceMaxNumber];
                for (int i = 0; i < NPB450Controller.NPB450ControllerInstnaceMaxNumber; i++)
                {
                    arr[i] = new FloatingSlotState();
                }
                return arr;
            });
            services.AddSingleton<StopChargeSlotState[]>(sp => {
                var arr = new StopChargeSlotState[NPB450Controller.NPB450ControllerInstnaceMaxNumber];
                for (int i = 0; i < NPB450Controller.NPB450ControllerInstnaceMaxNumber; i++)
                {
                    arr[i] = new StopChargeSlotState();
                }
                return arr;
            });
            services.AddSingleton<SupplyErrorSlotState[]>(sp => {
                var arr = new SupplyErrorSlotState[NPB450Controller.NPB450ControllerInstnaceMaxNumber];
                for (int i = 0; i < NPB450Controller.NPB450ControllerInstnaceMaxNumber; i++)
                {
                    arr[i] = new SupplyErrorSlotState();
                }
                return arr;
            });
            services.AddSingleton<StateErrorSlotState[]>(sp => {
                var arr = new StateErrorSlotState[NPB450Controller.NPB450ControllerInstnaceMaxNumber];
                for (int i = 0; i < NPB450Controller.NPB450ControllerInstnaceMaxNumber; i++)
                {
                    arr[i] = new StateErrorSlotState();
                }
                return arr;
            });


            return services;
        }
    }
}
