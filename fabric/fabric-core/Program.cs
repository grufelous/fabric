using fabric_core.hubs.auth_hub;
using fabric_core.services;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;

namespace fabric_core;

internal class Program
{
    public static void CreateAndRunHost(string[] args)
    {
        IHost host = Host.CreateDefaultBuilder(args)
            .UseWindowsService()
            .ConfigureServices((hostContext, services) =>
            {
                services.AddHostedService<SignalRWorker>();
            })
            .Build();
        Console.WriteLine("Built host. Running...");
        host.Run();
    }

    static void Main(string[] args)
    {
        Console.WriteLine("Hello, World!");
        string? loggerUrl = System.Environment.GetEnvironmentVariable("FABRIC_LOGGER_URL");
        Console.WriteLine($"Read logger URL: {loggerUrl}");
        CreateAndRunHost(args);
    }
}
