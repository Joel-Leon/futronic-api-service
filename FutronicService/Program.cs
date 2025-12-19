using System;
using System.IO;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Routing;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
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
                Log.Information("=== FUTRONIC API SERVICE (.NET 8) ===");
                Log.Information("Starting web host...");
                CreateHostBuilder(args).Build().Run();
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

        public static IHostBuilder CreateHostBuilder(string[] args) =>
            Host.CreateDefaultBuilder(args)
                .UseSerilog()
                .ConfigureWebHostDefaults(webBuilder =>
                {
                    webBuilder.UseStartup<Startup>();
                    webBuilder.ConfigureAppConfiguration((hostingContext, config) =>
                    {
                        config.SetBasePath(Directory.GetCurrentDirectory());
                        config.AddJsonFile("appsettings.json", optional: false, reloadOnChange: true);
                        config.AddEnvironmentVariables();
                        config.AddCommandLine(args);
                    });
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
            
            // Registrar servicio de notificaciones de progreso
            services.AddSingleton<IProgressNotificationService, ProgressNotificationService>();
            
            // Registrar servicio de configuración
            services.AddSingleton<IConfigurationService, ConfigurationService>();
            
            // Agregar HttpClient para callbacks HTTP
            services.AddHttpClient();

            // Configurar Controllers con Newtonsoft.Json
            services.AddControllers()
                .AddNewtonsoftJson(options =>
                {
                    // Ignorar valores nulos en respuestas
                    options.SerializerSettings.NullValueHandling = Newtonsoft.Json.NullValueHandling.Ignore;
                    
                    // Formato indentado para legibilidad
                    options.SerializerSettings.Formatting = Newtonsoft.Json.Formatting.Indented;
                    
                    // IMPORTANTE: Permitir case-insensitive para compatibilidad con frontend
                    // Acepta tanto "Dni"/"dni", "Dedo"/"dedo", etc.
                    options.SerializerSettings.ContractResolver = new Newtonsoft.Json.Serialization.DefaultContractResolver
                    {
                        NamingStrategy = new Newtonsoft.Json.Serialization.CamelCaseNamingStrategy()
                    };
                });

            // Configurar opciones de API para case-insensitive model binding
            services.Configure<Microsoft.AspNetCore.Mvc.MvcOptions>(options =>
            {
                options.SuppressImplicitRequiredAttributeForNonNullableReferenceTypes = true;
            });

            // Agregar SignalR para webhooks/notificaciones en tiempo real
            services.AddSignalR();
        }

        public void Configure(IApplicationBuilder app, IWebHostEnvironment env, ILogger<Startup> logger)
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
            app.UseRouting();

            // Endpoints
            app.UseEndpoints(endpoints =>
            {
                endpoints.MapControllers();
                // Mapear SignalR Hub para notificaciones en tiempo real
                endpoints.MapHub<FutronicService.Hubs.FingerprintHub>("/hubs/fingerprint");
            });

            // Log de inicio
            var port = Configuration.GetValue<string>("Kestrel:Endpoints:Http:Url", "http://localhost:5000");
            logger.LogInformation($"? Futronic API Service started successfully on {port}");
            logger.LogInformation("?? Available endpoints:");
            logger.LogInformation("   POST /api/fingerprint/capture");
            logger.LogInformation("   POST /api/fingerprint/register-multi");
            logger.LogInformation("   POST /api/fingerprint/verify-simple");
            logger.LogInformation("   POST /api/fingerprint/identify-live");
            logger.LogInformation("   GET  /api/fingerprint/config");
            logger.LogInformation("   POST /api/fingerprint/config");
            logger.LogInformation("   GET  /health");
            logger.LogInformation("?? SignalR Hub:");
            logger.LogInformation("   WS   /hubs/fingerprint (Real-time notifications)");
        }
    }
}
