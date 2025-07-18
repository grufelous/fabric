using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text;
using System.Text.Json;
using System.Threading.Channels;
using System.Threading.Tasks;

namespace fabric_shared.Logger;

public class SqliteLogger
{
    private static readonly HttpClient _httpClient = new HttpClient();
    private const string LOG_SERVER_ENDPOINT = "http://localhost:8080/log";
    private readonly Channel<string> _logQueue = Channel.CreateUnbounded<string>();

    const string source = "FabricLogger";
    const string logName = "Application";

    public SqliteLogger()
    {
        Console.WriteLine("Making shared logger");

        //if (!EventLog.SourceExists(source))
        //{
        //    EventLog.CreateEventSource(source, logName);
        //}

        _ = Task.Run(ProcessLogQueueAsync);
    }

    private async Task ProcessLogQueueAsync()
    {
        while(await _logQueue.Reader.WaitToReadAsync())
        {
            while(_logQueue.Reader.TryRead(out var logPayload))
            {
                var content = new StringContent(logPayload, Encoding.UTF8, "application/json");
                try
                {
                    await _httpClient.PostAsync(LOG_SERVER_ENDPOINT, content);
                }
                catch (Exception ex)
                {
                    Console.WriteLine($"Failed to write log to server: {ex.Message}\n{ex.StackTrace}");
                }
            }
        }
    }

    private object CreateLogObject(string message, string level)
    {
        return new
        {
            ts = (new DateTimeOffset(DateTime.UtcNow).ToUnixTimeMilliseconds()).ToString(),
            user = System.Security.Principal.WindowsIdentity.GetCurrent().Name,
            process = System.AppDomain.CurrentDomain.FriendlyName,
            message,
            level,
        };
    }

    private void LogToEventViewer(string message, string level)
    {
        //var entryType = level switch
        //{
        //    "ERROR" => EventLogEntryType.Error,
        //    "WARNING" => EventLogEntryType.Warning,
        //    _ => EventLogEntryType.Information
        //};

        //EventLog.WriteEntry(source, message, entryType);
    }

    public Task AsyncInfo(string message)
    {
        return SendLogAsync(message, "INFO");
    }

    public void Info(string message)
    {
        _ = AsyncInfo(message);
    }

    public Task AsyncWarn(string message)
    {
        return SendLogAsync(message, "WARNING");
    }

    public void Warn(string message)
    {
        _ = AsyncWarn(message);
    }

    public Task AsyncError(string message)
    {
        return SendLogAsync(message, "ERROR");
    }

    public void Error(string message)
    {
        _ = AsyncError(message);
    }

    public async Task SendLogAsync(string message, string level)
    {
        var log = CreateLogObject(message, level);
        string logString = JsonSerializer.Serialize(log);
        LogToEventViewer(message, level);
        await _logQueue.Writer.WriteAsync(logString);
    }
}
