using Microsoft.AspNetCore.SignalR;

namespace fabric_core.hubs.auth_hub;

internal class NameUserIdProvider: IUserIdProvider
{
    public string GetUserId(HubConnectionContext context)
    {
        return context.User?.Identity?.Name ?? "Unknown User";
    }
}
