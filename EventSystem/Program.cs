using EventSystem.Data;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;


// Code attribution
// This resource was referenced for educational material
// https://www.freecodecamp.org/
// FreeCodeCamp - Offers free coding tutorials, projects, and certifications for web development and programming

// Code attribution
// This resource was referenced for educational material
// https://developer.mozilla.org/en-US/
// MDN Web Docs - Provides in-depth documentation and tutorials for web technologies like HTML, CSS, and JavaScript

// Code attribution
// This resource was referenced for educational material
// https://www.codecademy.com/
// Codecademy - Features interactive coding lessons for beginners to advanced learners in various programming languages

// Code attribution
// This resource was referenced for educational material
// https://www.geeksforgeeks.org/
// GeeksforGeeks - Delivers tutorials, coding problems, and articles on programming, algorithms, and data structures


namespace EventSystem
{
    public class Program
    {
        public static void Main(string[] args)
        {
            var builder = WebApplication.CreateBuilder(args);

            // Add services to the container.
            builder.Services.AddControllersWithViews();

            builder.Services.AddDbContext<EventSystemDbContext>(options =>
                options.UseSqlServer(builder.Configuration.GetConnectionString("LocalConn")));

            // Add logging services
            builder.Services.AddLogging(logging =>
            {
                logging.AddConsole();
                logging.AddDebug();
            });

            var app = builder.Build();

            // Configure the HTTP request pipeline.
            if (app.Environment.IsDevelopment())
            {
                app.UseDeveloperExceptionPage();
            }
            else
            {
                // Removed UseExceptionHandler("/Home/Error") to avoid needing Error.cshtml
                app.UseHsts();
            }

            app.UseHttpsRedirection();
            app.UseRouting();

            app.UseAuthorization();

            app.MapStaticAssets();
            app.MapControllerRoute(
                name: "default",
                pattern: "{controller=Home}/{action=Index}/{id?}")
                .WithStaticAssets();

            app.Run();
        }
    }
}