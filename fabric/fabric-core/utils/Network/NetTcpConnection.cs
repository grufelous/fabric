using System.Net;
using System.Runtime.InteropServices;

namespace fabric_core.utils.Network;

internal static class NetTcpConnection
{
    const int AF_INET_IPv4 = 2;
    const int AF_INET_IPv6 = 23;

    private enum TcpTableClass : int
    {
        TCP_TABLE_OWNER_PID_ALL = 5
    }

    [StructLayout(LayoutKind.Sequential)]
    private struct MibTcp4RowOwnerPid
    {
        public uint State;
        public uint LocalAddr;
        public uint LocalPort;
        public uint RemoteAddr;
        public uint RemotePort;
        public uint OwningPid;
    }

    [StructLayout(LayoutKind.Sequential)]
    private struct MibTcp4TableOwnerPid
    {
        public uint NumEntries;
    }

    [StructLayout(LayoutKind.Sequential)]
    private struct MibTcp6RowOwnerPid
    {
        [MarshalAs(UnmanagedType.ByValArray, SizeConst = 16)]
        public byte[] LocalAddr;
        public uint LocalScopeId;
        public uint LocalPort;

        [MarshalAs(UnmanagedType.ByValArray, SizeConst = 16)]
        public byte[] RemoteAddr;
        public uint RemoteScopeId;
        public uint RemotePort;

        public uint State;
        public uint OwningPid;
    }

    [StructLayout(LayoutKind.Sequential)]
    private struct MibTcp6TableOwnerPid
    {
        public uint NumEntries;
    }

    [DllImport("iphlpapi.dll", SetLastError = true)]
    private static extern uint GetExtendedTcpTable(
        IntPtr tcpTable,
        ref int tcpTableLength,
        bool sort,
        int ipVersion,
        TcpTableClass taleClass,
        uint reserved = 0
    );

    public static IEnumerable<(IPEndPoint Local, IPEndPoint Remote, int Pid)> GetAllTcpV4Connections()
    {
        int bufferSize = 0;

        uint result = GetExtendedTcpTable(IntPtr.Zero, ref bufferSize, true, AF_INET_IPv4, TcpTableClass.TCP_TABLE_OWNER_PID_ALL);

        IntPtr buffer = Marshal.AllocHGlobal(bufferSize);

        try
        {
            result = GetExtendedTcpTable(buffer, ref bufferSize, true, AF_INET_IPv4, TcpTableClass.TCP_TABLE_OWNER_PID_ALL);

            if (result != 0)
            {
                yield break;
            }

            var table = Marshal.PtrToStructure<MibTcp4TableOwnerPid>(buffer);
            long rowPtr = buffer.ToInt64() + Marshal.SizeOf<MibTcp4TableOwnerPid>();

            for (int i = 0; i < table.NumEntries; i++)
            {
                var row = Marshal.PtrToStructure<MibTcp4RowOwnerPid>(new nint(rowPtr));
                rowPtr += Marshal.SizeOf<MibTcp4RowOwnerPid>();

                int localPort = ((int)row.LocalPort >> 8) | ((int)row.LocalPort & 0xFF) << 8;
                int remotePort = ((int)row.RemotePort >> 8) | ((int)row.RemotePort & 0xFF) << 8;

                var localIp = new IPAddress(row.LocalAddr);
                var remoteIp = new IPAddress(row.RemoteAddr);

                yield return (
                    new IPEndPoint(localIp, localPort),
                    new IPEndPoint(remoteIp, remotePort),
                    (int)row.OwningPid
                );
            }
        }
        finally
        {
            Marshal.FreeHGlobal(buffer);
        }
    }

    public static IEnumerable<(IPEndPoint Local, IPEndPoint Remote, int Pid)> GetAllTcpV6Connections()
    {
        int bufferSize = 0;

        uint result = GetExtendedTcpTable(IntPtr.Zero, ref bufferSize, true, AF_INET_IPv6, TcpTableClass.TCP_TABLE_OWNER_PID_ALL);

        IntPtr buffer = Marshal.AllocHGlobal(bufferSize);

        try
        {
            result = GetExtendedTcpTable(buffer, ref bufferSize, true, AF_INET_IPv6, TcpTableClass.TCP_TABLE_OWNER_PID_ALL);

            if (result != 0)
            {
                yield break;
            }

            var table = Marshal.PtrToStructure<MibTcp6TableOwnerPid>(buffer);
            long rowPtr = buffer.ToInt64() + Marshal.SizeOf<MibTcp6TableOwnerPid>();

            for (int i = 0; i < table.NumEntries; i++)
            {
                var row = Marshal.PtrToStructure<MibTcp6RowOwnerPid>(new nint(rowPtr));
                rowPtr += Marshal.SizeOf<MibTcp6RowOwnerPid>();

                var localIp = new IPAddress(row.LocalAddr, row.LocalScopeId);
                var remoteIp = new IPAddress(row.RemoteAddr, row.RemoteScopeId);
                Console.WriteLine($"Local\t\tIP: {localIp}\t{row.LocalAddr}\t{row.LocalScopeId}");
                Console.WriteLine($"Remote\t\tIP: {remoteIp}\t{row.RemoteAddr}\t{row.RemoteScopeId}");

                int localPortShort = IPAddress.NetworkToHostOrder((short)(row.LocalPort & 0xFFFF));
                int remotePortShort = IPAddress.NetworkToHostOrder((short)(row.RemotePort & 0xFFFF));

                int localPort = ((int)(row.LocalPort & 0xFF) << 8) | ((int)(row.LocalPort >> 8) & 0xFF);
                int remotePort = ((int)(row.RemotePort & 0xFF) << 8) | ((int)(row.RemotePort >> 8) & 0xFF);

                int localPort2 = (ushort)IPAddress.NetworkToHostOrder((short)row.LocalPort);
                int remotePort2 = (ushort)IPAddress.NetworkToHostOrder((short)row.RemotePort);

                if((localPort != localPort2) || (remotePort != remotePort2))
                {
                    Console.WriteLine($"Caught differences!\t{localPort}\t{localPort2}\t\t{remotePort}\t{remotePort2}");
                }

                var localEndPoint = new IPEndPoint(localIp, localPort);
                var remoteEndPoint = new IPEndPoint(remoteIp, remotePort);
                yield return (
                    localEndPoint,
                    remoteEndPoint,
                    (int)row.OwningPid
                );
            }
        }
        finally
        {
            Marshal.FreeHGlobal(buffer);
        }
    }
    
    public static IEnumerable<(IPEndPoint Local, IPEndPoint Remote, int Pid)> GetAllTcpConnections()
    {
        foreach (var v4connections in GetAllTcpV4Connections())
            yield return v4connections;
        foreach (var v6connections in GetAllTcpV6Connections())
            yield return v6connections;
    }

    public static int? GetOwningProcessId(
        IPAddress remoteIp, int remotePort,
        IPAddress localIp, int localPort
    )
    {
        foreach(var entry in GetAllTcpConnections())
        {
            Console.WriteLine($"Remote (entry local): {entry.Local.Address}:{entry.Local.Port}\tLocal (entry remote): {entry.Remote.Address}:{entry.Remote.Port}\tProcess: {entry.Pid}");
            if(
                (entry.Local.Port == remotePort) &&
                (entry.Local.Address.Equals(remoteIp)) &&
                (entry.Remote.Port == localPort) &&
                (entry.Remote.Address.Equals(localIp))
            )
            {
                return entry.Pid;
            }
        }
        return null;
    }
}
