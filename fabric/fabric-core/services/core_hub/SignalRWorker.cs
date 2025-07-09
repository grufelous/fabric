using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http.Connections;
using Microsoft.AspNetCore.Authentication.Negotiate;
using Microsoft.Extensions.Logging;


namespace fabric_core.services.core_hub;

internal class SignalRWorker: BackgroundService
{
    //private IHost? _webHost;
    private WebApplication? _webApp;

    protected override async Task ExecuteAsync(CancellationToken cancellationToken) 
    {
        WebApplicationBuilder webApplicationBuilder = WebApplication.CreateBuilder();

        webApplicationBuilder.Logging.AddConsole();
        webApplicationBuilder.Logging.SetMinimumLevel(LogLevel.Debug);

        webApplicationBuilder.WebHost.UseKestrel()
            .UseUrls("http://localhost:5000");

        webApplicationBuilder.Services.AddCors(options =>
        {
            options.AddDefaultPolicy(policy =>
            {
                policy.WithOrigins("http://localhost:3000")
                      .AllowAnyHeader()
                      .AllowAnyMethod()
                      .AllowCredentials();
            });
        });
        webApplicationBuilder.Services.AddAuthentication(NegotiateDefaults.AuthenticationScheme)
            .AddNegotiate();
        webApplicationBuilder.Services.AddAuthorization();

        webApplicationBuilder.Services.AddSignalR(options =>
        {
            options.EnableDetailedErrors = true;
        });

        _webApp = webApplicationBuilder.Build();
        _webApp.UseCors();
        _webApp.UseAuthentication();
        _webApp.UseAuthorization();
        _webApp.MapHub<HostHub>("/fabric_core_hub");

        _ = Task.Run(() => _webApp.RunAsync(cancellationToken));

        while (!cancellationToken.IsCancellationRequested)
        {
            await Task.Delay(1000, cancellationToken);
        }
    }

    public override async Task StopAsync(CancellationToken stoppingToken)
    {
        //if(_webHost != null)
        //{
        //    await _webHost.StopAsync(stoppingToken);
        //}

        if(_webApp != null)
        {
            await _webApp.StopAsync(stoppingToken);
        }
    }
}
