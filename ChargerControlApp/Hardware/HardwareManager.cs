using ChargerControlApp.DataAccess;
using ChargerControlApp.Services;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Linq.Expressions;
using System.Text;
using System.Threading.Tasks;
using Microsoft.Extensions.DependencyInjection;
using ChargerControlApp.DataAccess.CANBus.Linux;
using System.Runtime.InteropServices;
using ChargerControlApp.DataAccess.Modbus.Services;
using ChargerControlApp.DataAccess.Modbus.Interfaces;

namespace ChargerControlApp.Hardware
{
    public class HardwareManager
    {
        public const int NPB450ControllerInstnaceNumber = 1; // NP-B450 控制器實例數量
        public static IServiceProvider? Services { get; private set; }
        public NPB450Controller[] Charger { get; private set; }
        private SocketCANBusService canBusService { get; set; }
        //private SocketCanBusServiceNoAsync canBusService2 { get; set; }
        //public UartService UartService { get; private set; }
        //LED CONTROLLER
        //MOTOR CONTROLLER 未來給換電站
        public RobotController Robot { get; private set; }
        public ModbusRTUService modbusRTUService { get; private set; }


        public HardwareManager(IServiceProvider serviceProvider)
        {
            Services = serviceProvider;
            //var grpcClientService = Services.GetRequiredService<GrpcClientService>();
            //var chargingStationStateMachine = Services.GetRequiredService<ChargingStationStateMachine>();

            //// 初始化硬體
            //canBusService2 = new DataAccess.SocketCanBusService2("can0", 250000);
            //Charger = new NPB1700Controller(Services, canBusService2);

            // 初始化硬體
            if (RuntimeInformation.IsOSPlatform(OSPlatform.Linux))
            {
                //canBusService = new DataAccess.CANBus.Linux.SocketCANBusService();
                canBusService = Services.GetService<SocketCANBusService>();
            }
            else if (RuntimeInformation.IsOSPlatform(OSPlatform.Windows))
            {
                // throw new PlatformNotSupportedException("Unsupported OS platform");
                canBusService = Services.GetService<SocketCANBusService>();
            }
            else
            {
                throw new PlatformNotSupportedException("Unsupported OS platform");
            }
            //Charger = new NPB1700Controller(Services);//, canBusService);
            //Charger = new NPB450Controller(Services.GetService<ChargingStationStateMachine>(), canBusService);//, canBusService);
            //string portName = "COM1"; // 替換為你的實際 COM 埠名稱


            //#if RELEASE
            //        portName = "/dev/ttyS0";
            //#endif
            //modbusRTUService = Services.GetService<ModbusRTUService>();
            //Robot = Services.GetService<RobotController>();

            // 取得狀態機
            var stateMachine = serviceProvider.GetRequiredService<ChargingStationStateMachine>();

            // 取得 Charger
            //for (int i = 0; i < NPB450ControllerInstnaceNumber; i++)
                //Charger[i] = new NPB450Controller(stateMachine, canBusService, i);
            Charger = serviceProvider.GetRequiredService<NPB450Controller[]>();

            // 取得 ModbusRTUService
            modbusRTUService = serviceProvider.GetRequiredService<ModbusRTUService>();

            // 取得 RobotController
            Robot = serviceProvider.GetRequiredService<RobotController>();
        }
    }
}
