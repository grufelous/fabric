using Microsoft.AspNetCore.SignalR;

namespace fabric_core;

public class HostHub: Hub
{
    public async Task SEND_MESSAGE(string message)
    {
        //Clients.Caller.
        //string caller = Clients.Caller.ToString();
        Console.WriteLine($"Called by {Clients.Caller?.ToString()}");
        Console.WriteLine($"Called by {Context.User?.ToString()}");
        await Clients.All.SendAsync("RECEIVE_MESSAGE", message);
        
    }

    public override Task OnConnectedAsync()
    {
        string? userName = Context.User?.Identity?.Name;
        string connectionId = Context.ConnectionId;
        Console.WriteLine($"Connected to user: {userName} ({connectionId})");
        return base.OnConnectedAsync();
    }

    public override Task OnDisconnectedAsync(Exception? exception)
    {
        string? userName = Context.User?.Identity?.Name;
        string? authType = Context.User?.Identity?.AuthenticationType;
        string connectionId = Context.ConnectionId;
        Console.WriteLine($"Disconnected from user: {userName} via {authType} -- ({connectionId})");
        return base.OnDisconnectedAsync(exception);
    }
}
