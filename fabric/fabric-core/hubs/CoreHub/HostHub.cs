using fabric_core.services.EntityMappingService;
using fabric_core.services.ProcessCommunicator;
using fabric_core.utils.Processes;
using fabric_core.utils.Network;
using Microsoft.AspNet.SignalR.Infrastructure;
using Microsoft.AspNetCore.SignalR;
using fabric_core.utils.User;

namespace fabric_core.hubs.CoreHub;

public class HostHub: Hub
{
    private EntityMappings _entityMappings;
    private fabric_shared.Logger.SqliteLogger _logger;
    private ProcessCommunicator _communicator;
    private ProcessLauncher _processLauncher;

    public HostHub(EntityMappings entityMappings, fabric_shared.Logger.SqliteLogger sqliteLogger, ProcessCommunicator communicator, ProcessLauncher processLauncher)
    {
        _entityMappings = entityMappings;
        _logger = sqliteLogger;
        _communicator = communicator;
        _processLauncher = processLauncher;
    }

    public async Task SEND_MESSAGE(string message)
    {
        _logger.Info($"SEND_MESSAGE received from {Context.ConnectionId}");
        await Clients.All.SendAsync("RECEIVE_MESSAGE", message);
    }

    public async Task StartAgent()
    {
        String connectionId = Context.ConnectionId;
        bool hasValue = _entityMappings.ConnectionIdToWindowsUser.TryGetValue(connectionId, out String? userName);
        if (userName != null)
        {
            _logger.Info($"Received command to start agent process for {userName}...");
            await _communicator.CreateAndLaunchProcess(userName);
        }
        else
        {
            _logger.Warn($"No user associated to connectionId {connectionId}. Ignoring start command.");
        }

    }

    public void StopAgent()
    {
        _logger.Info("Received command to stop agent process...");
        _communicator.TerminateProcess();
    }

    public async Task SendMessageToAgent(string message)
    {
        _logger.Info("Received command to message agent process...");
        await _communicator.SendMessageAsync(message);
    }

    public override Task OnConnectedAsync()
    {
        string? userName = Context.User?.Identity?.Name;
        string? user = "";
        string connectionId = Context.ConnectionId;

        var cid = Context.ConnectionId;
        var http = Context.GetHttpContext();
        var localIp = http?.Connection.LocalIpAddress;
        var localPort = http?.Connection.LocalPort;
        var remoteIp = http?.Connection.RemoteIpAddress;
        var remotePort = http?.Connection.RemotePort;

        _logger.Info($"Connected to {connectionId}\tOn local {localIp}:{localPort} from remote {remoteIp}:{remotePort}");

        if (remotePort.HasValue && localPort.HasValue)
        {
            int? pid = NetTcpConnection.GetOwningProcessId(remoteIp, remotePort.Value, localIp, localPort.Value);
            _logger.Info($"Found pid for browser: {pid}");
            if (pid.HasValue)
            {
                _logger.Info($"Processing this pid: {pid}");

                (String? name, String? owner) = ProcessQueries.GetProcessInfo(pid.Value);

                _logger.Info($"Found process name: '{name}', owned by: '{owner}'");

                if (owner != null)
                {
                    IntPtr userToken = UserQueries.GetUserTokenByName(owner, _logger);
                    _logger.Info($"Received userToken: ''");
                }

                user = owner;
            } else
            {
                _logger.Error($"Did not find any pid corresponding to remote {remoteIp}:{remotePort}");
            }
        }

        string addedConnection = _entityMappings.ConnectionIdToWindowsUser.AddOrUpdate(connectionId, user ?? userName ?? $"Unknown_{connectionId}", (key, oldValue) => userName ?? $"UnknownUpdated_{connectionId}");

        _logger.Info($"Added connection {connectionId}: {addedConnection}");
        return base.OnConnectedAsync();
    }

    public override Task OnDisconnectedAsync(Exception? exception)
    {
        string connectionId = Context.ConnectionId;

        _entityMappings.ConnectionIdToWindowsUser.TryGetValue(connectionId, out string? user);
        _logger.Info($"Disconnected from user: {user ?? "NA"} at connectionId ({connectionId})");

        bool wasRemoved = _entityMappings.ConnectionIdToWindowsUser.Remove(connectionId, out string? remVal);

        _logger.Info($"Removed mapping for connectionId {connectionId}: value was: {remVal}");
        return base.OnDisconnectedAsync(exception);
    }
}
