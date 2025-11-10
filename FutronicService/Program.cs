using System;
using System.IO;
using Microsoft.AspNetCore;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using FutronicService.Middleware;
using FutronicService.Services;
using Serilog;

namespace FutronicService
{
public class Program
  {
        public static void Main(string[] args)
        {
  // Configurar Serilog
            Log.Logger = new LoggerConfiguration()
   .MinimumLevel.Information()
     .WriteTo.Console()
           .CreateLogger();

   try
    {
     Log.Information("=== FUTRONIC API SERVICE ===");
       Log.Information("Starting web host...");
  CreateWebHostBuilder(args).Build().Run();
        }
   catch (Exception ex)
       {
    Log.Fatal(ex, "Host terminated unexpectedly");
     }
            finally
            {
       Log.CloseAndFlush();
            }
 }

        public static IWebHostBuilder CreateWebHostBuilder(string[] args) =>
            WebHost.CreateDefaultBuilder(args)
   .UseStartup<Startup>()
  .UseSerilog()
 .ConfigureAppConfiguration((hostingContext, config) =>
     {
   config.SetBasePath(Directory.GetCurrentDirectory());
               config.AddJsonFile("appsettings.json", optional: false, reloadOnChange: true);
 config.AddEnvironmentVariables();
   config.AddCommandLine(args);
     });
    }

    public class Startup
    {
        public Startup(IConfiguration configuration)
        {
  Configuration = configuration;
}

        public IConfiguration Configuration { get; }

      public void ConfigureServices(IServiceCollection services)
        {
  // Configurar CORS
            var allowedOrigins = Configuration.GetSection("Cors:AllowedOrigins").Get<string[]>();
            services.AddCors(options =>
        {
      options.AddPolicy("CorsPolicy", builder =>
{
   builder.WithOrigins(allowedOrigins ?? new[] { "http://localhost:3000", "http://localhost:3001" })
 .AllowAnyMethod()
           .AllowAnyHeader()
     .AllowCredentials();
   });
 });

         // Registrar servicios
  services.AddSingleton<IFingerprintService, FutronicFingerprintService>();

    // Configurar MVC
    services.AddMvc()
 .AddJsonOptions(options =>
       {
    options.SerializerSettings.NullValueHandling = Newtonsoft.Json.NullValueHandling.Ignore;
         options.SerializerSettings.Formatting = Newtonsoft.Json.Formatting.Indented;
      });
        }

        public void Configure(IApplicationBuilder app, IHostingEnvironment env, ILogger<Startup> logger)
        {
          if (env.IsDevelopment())
            {
          app.UseDeveloperExceptionPage();
 }

            // Middleware de manejo de errores
 app.UseMiddleware<ErrorHandlingMiddleware>();

         // CORS
   app.UseCors("CorsPolicy");

            // Routing
    app.UseMvc();

            // Log de inicio
        var port = Configuration.GetValue<string>("Kestrel:Endpoints:Http:Url", "http://localhost:5000");
       logger.LogInformation($"? Futronic API Service started successfully on {port}");
            logger.LogInformation("?? Available endpoints:");
       logger.LogInformation("   POST /api/fingerprint/capture");
            logger.LogInformation("   POST /api/fingerprint/register");
            logger.LogInformation("   POST /api/fingerprint/verify");
      logger.LogInformation(" POST /api/fingerprint/identify");
   logger.LogInformation("   GET  /api/fingerprint/config");
            logger.LogInformation("   POST /api/fingerprint/config");
       logger.LogInformation("GET  /health");
}
    }
}
