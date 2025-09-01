using ChargerControlApp.DataAccess.Modbus.Models;
using ChargerControlApp.DataAccess.Modbus.Services;
using ChargerControlApp.DataAccess.Motor.Models;
using ChargerControlApp.DataAccess.Motor.Services;
using ChargerControlApp.Hardware;
using ChargerControlApp.Test.Modbus;

class Program
{
    public static async Task Main(string[] args)
    {
        Console.WriteLine("Starting Modbus Test...V1.0.3");
        var port = new MyModbusTesting();

        var port2 = new ModbusRTUService("COM1", 115200, System.IO.Ports.Parity.Even, 8, System.IO.Ports.StopBits.One);
        port2.Open();

        var command = new ModbusRTUFrame(0x1, 0x3, 0x0, 5, null);
        var data = port2.Act(command);


        SingleMotorService sms = new SingleMotorService(port2, 0x1);
        await sms.ExecuteRouteProcessOnce();

        
        RobotController rc = new RobotController(port2);
        rc.Open();

        rc.ServerOn(0, true);

        /*

        port.Test2();

        port.Open();

        ushort[] data = await port.Read();

        port.Close();

        Console.WriteLine("Close Next Progress");

        if(data != null)
        {
            Console.WriteLine("Data read from Modbus device:");
            for (int i = 0; i < data.Length; i++)
            {
                Console.WriteLine($"Register {i}: {data[i]}");
            }
        }
        else
        {
            Console.WriteLine("Failed to read data from Modbus device.");
        }

        Console.WriteLine("Modbus Test Completed.");

        */

        var builder = WebApplication.CreateBuilder(args);

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

        rc.ServerOn(0, false);

        Thread.Sleep(1000);

        rc.Close();
        port2.Close();
    }
}

