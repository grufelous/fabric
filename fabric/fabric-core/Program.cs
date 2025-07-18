﻿using fabric_core.services.auth_hub;
using fabric_core.services.core_hub;
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
                services.AddHostedService<SignalRAuthWorker>();
            })
            .Build();
        host.Run();
    }

    static void Main(string[] args)
    {
        Console.WriteLine("Hello, World!");
        CreateAndRunHost(args);
    }
}
