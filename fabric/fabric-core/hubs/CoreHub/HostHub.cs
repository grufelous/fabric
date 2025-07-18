using fabric_core.services.EntityMappingService;
using fabric_core.utils;
using fabric_core.utils.Network;
using Microsoft.AspNet.SignalR.Infrastructure;
using Microsoft.AspNetCore.SignalR;

namespace fabric_core.hubs.CoreHub;

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
        Console.WriteLine($"Called by UserId: {Context.UserIdentifier}, Connection Id: {Context.ConnectionId}, User name: {Context.User?.Identity?.Name}");
        await Clients.All.SendAsync("RECEIVE_MESSAGE", message);
    }

    public override Task OnConnectedAsync()
    {
        string? userName = Context.User?.Identity?.Name;
        string connectionId = Context.ConnectionId;

        var cid = Context.ConnectionId;
        var http = Context.GetHttpContext();
        var localIp = http?.Connection.LocalIpAddress;
        var localPort = http?.Connection.LocalPort;
        var remoteIp = http?.Connection.RemoteIpAddress;
        var remotePort = http?.Connection.RemotePort;

        Console.WriteLine($"Local: {localIp}:{localPort}\tRemote: {remoteIp}:{remotePort}");

        if (remotePort.HasValue && localPort.HasValue)
        {
            int? pid = NetTcpConnection.GetOwningProcessId(remoteIp, remotePort.Value, localIp, localPort.Value);
            Console.WriteLine($"Found pid: {pid}");
            if (pid.HasValue)
            {
                Console.WriteLine($"Received pid: {pid}");

                (string? name, string? owner) = Processes.GetProcessInfo(pid.Value);

                Console.WriteLine($"Process name: {name}, owned by: {owner}");
            } else
            {
                Console.WriteLine($"Did not find any pid");
            }
        }
        
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
