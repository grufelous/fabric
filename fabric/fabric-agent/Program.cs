using fabric_agent.Services;
using fabric_shared.Logger;

namespace fabric_agent;

internal class Program
{
    static async Task Main(string[] args)
    {
        SqliteLogger logger = new SqliteLogger();
        logger.Info("Hello from the Agent Process!");
        logger.Info($"Running agent for {System.Security.Principal.WindowsIdentity.GetCurrent().Name}");

        ClientCommunicator communicator = new ClientCommunicator(logger);
        await communicator.StartAsync();

        int timeout = 15;
        while (timeout != 0)
        {
            Console.WriteLine($"Auto-exiting in {timeout}s...");
            timeout--;
            Thread.Sleep(1000);
        }
    }
}
