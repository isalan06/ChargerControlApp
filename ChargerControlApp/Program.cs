using ChargerControlApp.DataAccess.CANBus.Interfaces;
using ChargerControlApp.DataAccess.CANBus.Linux;
using ChargerControlApp.DataAccess.Modbus.Interfaces;
using ChargerControlApp.DataAccess.Modbus.Models;
using ChargerControlApp.DataAccess.Modbus.Services;
using ChargerControlApp.DataAccess.Slot.Models;
using ChargerControlApp.DataAccess.Slot.Services;
using ChargerControlApp.Hardware;
using ChargerControlApp.Services;
using ChargerControlApp.Utilities;
using Grpc.Net.Client;
using Microsoft.Extensions.Logging;
using RJCP.IO.Ports;
using Smart.Modbus;
using System.Collections;
using System.Data;
using TacDynamics.Kernel.DeviceService.Protos;
public class Program
{
    public static void Main(string[] args)
    {
        var builder = WebApplication.CreateBuilder(args);

        // 讀取 appsettings.json
        builder.Configuration.AddJsonFile("appsettings.json", optional: false, reloadOnChange: true);
        var configuration = builder.Configuration;

        // 綁定設定
        var settings = new AppSettings();
        configuration.GetSection("AppSettings").Bind(settings);
        builder.Services.Configure<AppSettings>(configuration.GetSection("AppSettings"));
        builder.Services.AddSingleton(settings); // 讓 DI 容器可以取得 AppSettings


        // ModbusRTUService
        builder.Services.AddSingleton<ModbusRTUService>(sp =>
        {
            // 這裡可根據 appsettings.json 參數建立
            var settings = sp.GetRequiredService<AppSettings>();
            return new ModbusRTUService(settings.PortName);
        });

        // 註冊 CANBUS 服務
        builder.Services.AddSingleton<ICANBusService, SocketCANBusService>();
        //builder.Services.AddSingleton<SocketCANBusService>(sp =>
        //{
        //    return new SocketCANBusService();
        //});

        // 註冊 SlotStateMachine
        builder.Services.AddSingleton<SlotStateMachine[]>(sp => {
            var arr = new SlotStateMachine[NPB450Controller.NPB450ControllerInstnaceMaxNumber];

            for (int i = 0; i < NPB450Controller.NPB450ControllerInstnaceMaxNumber; i++)
            {
                arr[i] = new SlotStateMachine(); 
               
            }

            return arr;
        });
        builder.Services.AddSingleton<SlotInfo[]>(sp =>
        {
            var arr = new SlotInfo[NPB450Controller.NPB450ControllerInstnaceMaxNumber];
            for (int i = 0; i < NPB450Controller.NPB450ControllerInstnaceMaxNumber; i++)
            {
                arr[i] = new SlotInfo();
                arr[i].Id = i + 1; // SlotId 從 1 開始
                arr[i].Name = $"Slot {i + 1}";
                if (i < HardwareManager.NPB450ControllerInstnaceNumber)
                    arr[i].IsEnabled = true;
            }
            return arr;
        });

        builder.Services.AddSingleton<SlotServices>();

        // RobotController
        builder.Services.AddSingleton<RobotController>(sp =>
            new RobotController(sp.GetRequiredService<ModbusRTUService>()));

        builder.Services.AddSingleton<NPB450Controller[]>(sp =>
        {
            var stateMachine = sp.GetRequiredService<ChargingStationStateMachine>();
            var canBusService = sp.GetRequiredService<ICANBusService>();
            var logger = sp.GetRequiredService<ILogger<NPB450Controller>>();
            var arr = new NPB450Controller[NPB450Controller.NPB450ControllerInstnaceMaxNumber];
            for (int i = 0; i < NPB450Controller.NPB450ControllerInstnaceMaxNumber; i++)
            {
                arr[i] = new NPB450Controller(stateMachine, canBusService, i, logger); // id 可依需求調整
            }
            return arr;
        });

        

        // 註冊 Grpc 相關服務
        ConfigLoader.Load();
        AppSettings appSettings = ConfigLoader.GetSettings();
        builder.Services.AddSingleton(GrpcChannel.ForAddress(appSettings.ServerIp));
        builder.Services.RegisterChargingServices(); // 共用服務註冊
        builder.Services.AddSingleton<SwappingStationService>();

        // 註冊其他服務
        builder.Services.AddSingleton<HardwareManager>();
        builder.Services.AddSingleton<ChargingStationStateMachine>();
        builder.Services.AddSingleton<IServiceProvider>(provider => provider);

#if RELEASE
        // 監聽所有網卡 (0.0.0.0)，可自訂 port
        builder.WebHost.UseUrls("http://0.0.0.0:5000", "https://0.0.0.0:5001");
#endif


        // ✅ 註冊 BackgroundService
        builder.Services.AddHostedService<CanBusPollingService>();
        builder.Services.AddHostedService<GrpcBackgroundService>();
        builder.Services.AddHostedService<ModbusPollingService>();


        Console.WriteLine("Starting Web Application...");

        // Add services to the container.
        builder.Services.AddControllersWithViews();

        Console.WriteLine("Building Web Application...");

        var app = builder.Build();



        // 註冊 ApplicationStopping 事件
        var lifetime = app.Services.GetRequiredService<IHostApplicationLifetime>();
        lifetime.ApplicationStopping.Register(() =>
        {
            Console.WriteLine("Application stopping, closing Services...");
        });


        Console.WriteLine("Configuring Web Application...");

        // Configure the HTTP request pipeline.
        if (!app.Environment.IsDevelopment())
        {
            app.UseExceptionHandler("/Home/Error");
            // The default HSTS value is 30 days. You may want to change this for production scenarios, see https://aka.ms/aspnetcore-hsts.
            app.UseHsts();
        }

        Console.WriteLine("Starting Web Application Server...");

        app.UseHttpsRedirection();
        app.UseStaticFiles();

        app.UseRouting();

        app.UseAuthorization();

        app.MapControllerRoute(
            name: "default",
            pattern: "{controller=Home}/{action=Index}/{id?}");

        app.Run();

    }
}

