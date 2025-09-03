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

