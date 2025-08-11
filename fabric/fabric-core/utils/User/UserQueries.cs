using fabric_shared.Logger;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.InteropServices;
using System.Text;
using System.Threading.Tasks;

namespace fabric_core.utils.User;

internal class UserQueries
{
    [DllImport("wtsapi32.dll", SetLastError = true)]
    static extern bool WTSEnumerateSessions(
        IntPtr hServer,
        int Reserved,
        int Version,
        out IntPtr ppSessionInfo,
        out int pCount
    );

    [DllImport("wtsapi32.dll")]
    static extern void WTSFreeMemory(IntPtr memory);

    [DllImport("wtsapi32.dll", SetLastError = true)]
    static extern bool WTSQueryUserToken(uint sessionId, out IntPtr token);

    [DllImport("wtsapi32.dll", SetLastError = true)]
    static extern bool WTSQuerySessionInformation(
        IntPtr hServer,
        int sessionId,
        WTS_INFO_CLASS infoClass,
        out IntPtr ppBuffer,
        out uint pBytesReturned
    );

    enum WTS_INFO_CLASS
    {
        WTSUserName = 5,
        WTSDomainName = 7
    }

    [StructLayout(LayoutKind.Sequential)]
    struct WTS_SESSION_INFO
    {
        public int SessionId;
        public string pWinStationName;
        public WTS_CONNECTSTATE_CLASS State;
    }

    enum WTS_CONNECTSTATE_CLASS
    {
        WTSActive,
        WTSConnected,
        WTSConnectQuery,
        WTSShadow,
        WTSDisconnected,
        WTSIdle,
        WTSListen,
        WTSReset,
        WTSDown,
        WTSInit
    }

    public static IntPtr GetUserTokenByName(String targetUserName, SqliteLogger logger)
    {
        IntPtr serverHandle = IntPtr.Zero;
        IntPtr sessionInfoPtr = IntPtr.Zero;
        IntPtr token = IntPtr.Zero;

        try
        {
            if (!WTSEnumerateSessions(serverHandle, 0, 1, out sessionInfoPtr, out int count))
            {
                throw new Exception("Failed to enumerate sessions");
            }

            int dataSize = Marshal.SizeOf(typeof(WTS_SESSION_INFO));

            for (int i = 0; i < count; i++)
            {
                IntPtr current = IntPtr.Add(sessionInfoPtr, i * dataSize);
                WTS_SESSION_INFO session = Marshal.PtrToStructure<WTS_SESSION_INFO>(current);
                WTSQuerySessionInformation(serverHandle, session.SessionId, WTS_INFO_CLASS.WTSUserName, out var userPtr, out _);
                WTSQuerySessionInformation(serverHandle, session.SessionId, WTS_INFO_CLASS.WTSDomainName, out var domainPtr, out _);

                String userName = Marshal.PtrToStringAnsi(userPtr) ?? "";

                String domain = Marshal.PtrToStringAnsi(domainPtr) ?? "";

                logger.Info($"Read username: '{userName}' and domain: '{domain}'");

                String completeName = $"{domain}\\{userName}";

                logger.Info($"Comparing '{completeName}' and '{targetUserName}'");

                if (completeName.Equals(targetUserName, StringComparison.OrdinalIgnoreCase))
                {
                    logger.Info($"Calculated username: '{completeName}' and target username: '{targetUserName}' are equal!");
                    if (WTSQueryUserToken((uint)session.SessionId, out token))
                    {
                        logger.Info($"Found token for username '{userName}': {token}");
                        return token;
                    }
                    else
                    {
                        logger.Info($"Did not find token for username '{userName}': {token}");
                    }
                }
                else
                {
                    logger.Info($"Calculated username: '{completeName}' and target username: '{targetUserName}' are not equal!");
                }
            }
        } finally
        {
            if (sessionInfoPtr != IntPtr.Zero)
            {
                WTSFreeMemory(sessionInfoPtr);
            }
        }

        logger.Info($"Did not find token for username '{targetUserName}'");
        return IntPtr.Zero;
    }
}
