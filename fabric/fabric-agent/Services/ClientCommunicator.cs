using System;
using System.Collections.Generic;
using System.IO.Pipes;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace fabric_agent.Services;

public class ClientCommunicator
{
    private readonly string pipeName = "FABRIC_PIPE";
    private fabric_shared.Logger.SqliteLogger logger;
    private StreamReader? reader;

    public ClientCommunicator(fabric_shared.Logger.SqliteLogger _logger)
    {
        logger = _logger;
        logger.Info("Starting client communicator...");
    }

    public async Task StartAsync()
    {
        using var clientStream = new NamedPipeClientStream(
            ".",
            pipeName,
            PipeDirection.InOut,
            PipeOptions.Asynchronous
        );

        await clientStream.ConnectAsync();

        logger.Info($"Connected to {pipeName}");

        reader = new StreamReader(clientStream);

        while (true)
        {
            String? readValue = await reader.ReadLineAsync();

            logger.Info($"Read value: {readValue}");

            if (readValue == null || readValue.Equals("exit", StringComparison.OrdinalIgnoreCase))
            {
                break;
            }
        }
            
    }
}
