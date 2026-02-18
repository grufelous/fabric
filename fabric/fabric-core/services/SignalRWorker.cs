using fabric_core.hubs.CoreHub;
using fabric_core.services.EntityMappingService;
using fabric_core.utils.Processes;
using Microsoft.AspNetCore.Authentication.Negotiate;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using static System.Net.WebRequestMethods;

namespace fabric_core.services;

internal class SignalRWorker: BackgroundService
{
    private WebApplication? _webApp;

    protected override async Task ExecuteAsync(CancellationToken cancellationToken) 
    {
        Console.WriteLine("Starting execute async");
        WebApplicationBuilder webApplicationBuilder = WebApplication.CreateBuilder();

        webApplicationBuilder.Logging.AddConsole();
        webApplicationBuilder.Logging.SetMinimumLevel(LogLevel.Debug);

        webApplicationBuilder.WebHost.UseKestrel()
            .UseUrls("http://localhost:5000");


        webApplicationBuilder.Services.AddSingleton<fabric_shared.Logger.SqliteLogger>();
        webApplicationBuilder.Services.AddSingleton<EntityMappings>();
        webApplicationBuilder.Services.AddSingleton<ProcessLauncher>();
        webApplicationBuilder.Services.AddSingleton<ProcessCommunicator.ProcessCommunicator>();
        

        webApplicationBuilder.Services.AddCors(options =>
        {
            options.AddDefaultPolicy(policy =>
            {
                policy.WithOrigins(["http://localhost:3000", "http://localhost:3030", "http://10.0.2.2:3000", "http://10.0.2.2:3030"])
                      .AllowAnyHeader()
                      .AllowAnyMethod()
                      .AllowCredentials();
            });
        });

        webApplicationBuilder.Services.AddSignalR(options =>
        {
            options.EnableDetailedErrors = true;
        });

        _webApp = webApplicationBuilder.Build();
        _webApp.UseCors();

        _webApp.MapHub<HostHub>("/fabric_core_hub");

        _ = Task.Run(() => _webApp.RunAsync(cancellationToken));

        while (!cancellationToken.IsCancellationRequested)
        {
            await Task.Delay(1000, cancellationToken);
        }
    }

    public override async Task StopAsync(CancellationToken stoppingToken)
    {
        if(_webApp != null)
        {
            await _webApp.StopAsync(stoppingToken);
        }
    }
}
