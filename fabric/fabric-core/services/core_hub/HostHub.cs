﻿using Microsoft.AspNet.SignalR.Infrastructure;
using Microsoft.AspNetCore.SignalR;

namespace fabric_core.services.core_hub;

public class HostHub: Hub
{
    private EntityMappings _entityMappings;
    private fabric_shared.Logger.SqliteLogger _logger;

    public HostHub(EntityMappings entityMappings, fabric_shared.Logger.SqliteLogger sqliteLogger)
    {
        _entityMappings = entityMappings;
        _logger = sqliteLogger;
    }

    public async Task SEND_MESSAGE(string message)
    {
        _logger.SendLogAsync($"Sending {message}", "DEFAULT");
        //Clients.Caller.
        //string caller = Clients.Caller.ToString();
        Console.WriteLine($"Called by UserId: {Context.UserIdentifier}, Connection Id: {Context.ConnectionId}, User name: {Context.User?.Identity?.Name}");
        await Clients.All.SendAsync("RECEIVE_MESSAGE", message);
        _logger.SendLogAsync($"Sent {message}", "DEFAULT");
    }

    public override Task OnConnectedAsync()
    {
        string? userName = Context.User?.Identity?.Name;
        string connectionId = Context.ConnectionId;
        Console.WriteLine($"Connected to user: {userName} ({connectionId})");

        string addedValue = _entityMappings.ConnectionIdToWindowsUser.AddOrUpdate(connectionId, userName ?? $"Unknown_{connectionId}", (key, oldValue) => userName ?? $"UnknownUpdated_{connectionId}");

        _logger.SendLogAsync($"Added connection {connectionId}", "DEFAULT");
        Console.WriteLine($"Able to add {connectionId}: {addedValue}");
        return base.OnConnectedAsync();
    }

    public override Task OnDisconnectedAsync(Exception? exception)
    {
        string? userName = Context.User?.Identity?.Name;
        string? authType = Context.User?.Identity?.AuthenticationType;
        string connectionId = Context.ConnectionId;
        Console.WriteLine($"Disconnected from user: {userName} via {authType} -- ({connectionId})");

        bool wasRemoved = _entityMappings.ConnectionIdToWindowsUser.Remove(connectionId, out string? remVal);

        _logger.SendLogAsync($"Removed connection {connectionId}", "DEFAULT");
        Console.WriteLine($"Able to remove {connectionId}: {wasRemoved}, value was: {remVal}");
        return base.OnDisconnectedAsync(exception);
    }
}
