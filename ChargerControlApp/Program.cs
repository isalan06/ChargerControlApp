using ChargerControlApp.DataAccess.Modbus.Services;
using ChargerControlApp.Hardware;
using Smart.Modbus;
using System.Collections;
using System.Data;
public class Program
{
    public static void Main(string[] args)
    {
        var builder = WebApplication.CreateBuilder(args);

        // 監聽所有網卡 (0.0.0.0)，可自訂 port
        //builder.WebHost.UseUrls("http://0.0.0.0:5000", "https://0.0.0.0:5001");

        ModbusRTUService modbusRTUService = new ModbusRTUService();
        RobotController robotController = new RobotController(modbusRTUService);

        // 註冊 robotController 為 Singleton
        builder.Services.AddSingleton<RobotController>(robotController);

        modbusRTUService.Open();

        robotController.Open();

        robotController.ServerOn(1, true);

        robotController.SetJogMode(1, 2);


        Console.WriteLine("Starting Web Application...");

        // Add services to the container.
        builder.Services.AddControllersWithViews();

        Console.WriteLine("Building Web Application...");

        var app = builder.Build();

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

