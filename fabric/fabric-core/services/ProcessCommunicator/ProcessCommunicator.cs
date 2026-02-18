using fabric_core.utils.Processes;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO.Pipes;
using System.Linq;
using System.Security.AccessControl;
using System.Security.Principal;
using System.Text;
using System.Threading.Tasks;

namespace fabric_core.services.ProcessCommunicator;

public class ProcessCommunicator
{
    private fabric_shared.Logger.SqliteLogger _logger;
    private ProcessLauncher _processLauncher;

    private readonly String processPath = @"C:\code\dotnet\fabric\fabric\fabric-agent\bin\Release\net8.0\publish\win-x86\fabric-agent.exe";
    private readonly String pipeName = "FABRIC_PIPE";

    private Process? agentProcess;

    private NamedPipeServerStream? pipeServer;
    private StreamWriter? writer;

    private readonly CancellationTokenSource _cts = new CancellationTokenSource();

    public ProcessCommunicator(fabric_shared.Logger.SqliteLogger sqliteLogger, ProcessLauncher processLauncher)
    {
        _logger = sqliteLogger;
        _processLauncher = processLauncher;
        string? envAgentPath = System.Environment.GetEnvironmentVariable("FABRIC_AGENT_PATH");
        processPath = envAgentPath ?? processPath;
        _logger.Info($"Agent process path: {processPath}");
    }

    private void NewAgentProcess_Exited(object? sender, EventArgs e)
    {
        _logger.Info($"Agent process exited. {e.ToString()}");
        Cleanup();
    }

    private void Cleanup()
    {
        try
        {
            writer?.Dispose();
            writer = null;
        }
        catch (Exception ex)
        {
            _logger.Error($"Error during cleanup: {ex.Message}\n{ex.ToString()}");
        }

        try
        {
            pipeServer?.Dispose();
            pipeServer = null;
        }
        catch (Exception ex)
        {
            _logger.Error($"Error during cleanup: {ex.Message}\n{ex.ToString()}");
        }

        _cts.Cancel();
    }

    private async Task<Boolean> ConnectPipe()
    {
        try
        {
            PipeSecurity worldAllowed = new PipeSecurity();
            worldAllowed.AddAccessRule(new PipeAccessRule(
                new SecurityIdentifier(WellKnownSidType.WorldSid, null),
                PipeAccessRights.ReadWrite,
                AccessControlType.Allow
            ));

            NamedPipeServerStream newPipeServer = NamedPipeServerStreamAcl.Create(
                pipeName,
                PipeDirection.InOut,
                1,
                PipeTransmissionMode.Message,
                PipeOptions.Asynchronous,
                4096,
                4096,
                worldAllowed
            );

            _logger.Info($"Waiting for pipe client to connect on {pipeName}...");

            await newPipeServer.WaitForConnectionAsync(_cts.Token);
            pipeServer = newPipeServer;

            _logger.Info($"Pipe client connected! Setting up writer...");

            writer = new StreamWriter(pipeServer)
            {
                AutoFlush = true
            };

            _logger.Info($"Writer created.");

            return true;
        }
        catch (Exception ex)
        {
            _logger.Error($"Error setting up server pipe: {ex.Message}\n{ex.ToString()}");
        }
        return false;
    }

    //private async Task<Boolean> ConnectPipeForUser(String username)
    //{
    //    try
    //    {

    //    }
    //}

    //private static PipeSecurity CreatePipeSecurityForUser(String username)
    //{
    //    PipeSecurity security = new PipeSecurity();
    //    security.AddAccessRule(new PipeAccessRule(
    //        new SecurityIdentifier(WellKnownSidType.BuiltinAdministratorsSid, null),
    //        PipeAccessRights.FullControl,
    //        AccessControlType.Allow
    //        ));
    //    security.AddAccessRule(new PipeAccessRule(
    //        new SecurityIdentifier(WellKnownSidType.LocalSystemSid, null),
    //        PipeAccessRights.FullControl,
    //        AccessControlType.Allow
    //        ));
    //}

    public async Task<Boolean> CreateAndLaunchProcess()
    {
        try
        {
            Process newAgentProcess = new Process();
            newAgentProcess.StartInfo.FileName = processPath;
            newAgentProcess.StartInfo.CreateNoWindow = false;
            newAgentProcess.StartInfo.UseShellExecute = true;
            newAgentProcess.EnableRaisingEvents = true;

            newAgentProcess.Exited += NewAgentProcess_Exited;

            newAgentProcess.Start();

            agentProcess = newAgentProcess;
            _logger.Info($"Started agent process: {agentProcess.Id}");

            await ConnectPipe();

            return true;
        }
        catch (Exception ex)
        {
            _logger.Error($"Error launching agent process: {ex.Message}\n{ex.StackTrace}");
        }
        return false;
    }

    public async Task<Boolean> CreateAndLaunchProcess(String userName)
    {
        try
        {
            Process? newAgentProcess = _processLauncher.Launch(processPath, userName, _logger);
            if (newAgentProcess != null)
            {
                _logger.Info($"Process launched: {newAgentProcess.ProcessName} at {newAgentProcess.Id}");

                newAgentProcess.Exited += NewAgentProcess_Exited;

                agentProcess = newAgentProcess;
                await ConnectPipe();

                return true;
            }
            else
            {

                _logger.Info($"No process launched?");
            }

        }
        catch (Exception ex)
        {
            _logger.Error($"Error launching agent process for user '{userName}': {ex.Message}\n{ex.StackTrace}");
        }
        return false;
    }

    public async Task<Boolean> SendMessageAsync(string message)
    {
        if(writer == null || pipeServer == null || !pipeServer.IsConnected)
        {
            _logger.Error($"Server pipe not connected!");
            return false;
        }
        _logger.Info($"Sending to agent: {message}");
        await writer.WriteLineAsync(message);
        return true;
    }

    public Boolean TerminateProcess()
    {
        try
        {
            if (agentProcess == null)
            {
                _logger.Warn($"No agent process running.");
                return false;
            }
            if (agentProcess.HasExited)
            {
                _logger.Warn($"Process already exited. Exit code: {agentProcess.ExitCode}, at {agentProcess.ExitTime}");
                return false;
            }

            Cleanup();
            agentProcess.Dispose();
            agentProcess.Kill();
            agentProcess = null;
            _cts.Dispose();
            return true;
        }
        catch (Exception ex)
        {
            _logger.Error($"Failed to terminate process: {ex.Message}\n{ex.StackTrace}");
        }
        return false;
    }   
}
