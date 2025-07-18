using System.Collections.Concurrent;

namespace fabric_core.services.core_hub;

public class EntityMappings
{
    public ConcurrentDictionary<string, string> ConnectionIdToWindowsUser;

    public ConcurrentDictionary<string, string> SessionToWindowsUser;

    public EntityMappings()
    {
        ConnectionIdToWindowsUser = new ConcurrentDictionary<string, string>();
        SessionToWindowsUser = new ConcurrentDictionary<string, string>();
    }
}
