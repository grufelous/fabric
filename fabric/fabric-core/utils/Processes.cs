using Microsoft.AspNetCore.Http;
using System.Diagnostics;
using System.Runtime.InteropServices;
using System.Text;

namespace fabric_core.utils;

internal class Processes
{
    [DllImport("advapi32.dll", SetLastError = true)]
    static extern bool OpenProcessToken(
        IntPtr ProcessHandle,
        uint DesiredAccess,
        out IntPtr TokenHandle
    );

    [DllImport("kernel32.dll", SetLastError = true)]
    static extern IntPtr OpenProcess(
        uint processAccess,
        bool bInheritHandle,
        int processId
    );

    [DllImport("kernel32.dll", SetLastError = true)]
    static extern bool CloseHandle(IntPtr hObject);

    [DllImport("advapi32.dll", SetLastError = true, CharSet = CharSet.Auto)]
    static extern bool LookupAccountSid(
        string? lpSystemName,
        IntPtr Sid,
        StringBuilder Name,
        ref uint cchName,
        StringBuilder ReferencedDomainName,
        ref uint cchReferencedDomainName,
        out SID_NAME_USE peUse
    );

    [DllImport("advapi32.dll", SetLastError = true)]
    static extern bool GetTokenInformation(
        IntPtr TokenHandle,
        TOKEN_INFORMATION_CLASS TokenInformationClass,
        IntPtr TokenInformation,
        uint TokenInformationLength,
        out uint ReturnLength
    );

    enum TOKEN_INFORMATION_CLASS
    {
        TokenUser = 1
    }

    [StructLayout(LayoutKind.Sequential)]
    struct TOKEN_USER
    {
        public _SID_AND_ATTRIBUTES User;
    }

    [StructLayout(LayoutKind.Sequential)]
    struct _SID_AND_ATTRIBUTES
    {
        public IntPtr Sid;
        public uint Attributes;
    }

    enum SID_NAME_USE
    {
        SidTypeUser = 1,
    }

    const uint TOKEN_QUERY = 0x0008;
    const uint PROCESS_QUERY_LIMITED_INFORMATION = 0x1000;

    public static (string? ProcessName, string? Owner) GetProcessInfo(int pid)
    {
        string? name = null;

        try
        {
            name = Process.GetProcessById(pid).ProcessName;
            Process p = Process.GetProcessById(pid);

        }
        catch (Exception e)
        {
            Console.WriteLine(e.ToString());
        }

        IntPtr hProcess = OpenProcess(PROCESS_QUERY_LIMITED_INFORMATION, false, pid);
        if (hProcess == IntPtr.Zero)
        {
            return (name, null);
        }

        if (!OpenProcessToken(hProcess, TOKEN_QUERY, out var hToken))
        {
            CloseHandle(hProcess);
            return (name, null);
        }

        GetTokenInformation(hToken, TOKEN_INFORMATION_CLASS.TokenUser, IntPtr.Zero, 0, out var reqLength);
        IntPtr tokenInfo = Marshal.AllocHGlobal((int)reqLength);

        try
        {
            if (GetTokenInformation(hToken, TOKEN_INFORMATION_CLASS.TokenUser, tokenInfo, reqLength, out _))
            {
                var tokenUser = Marshal.PtrToStructure<TOKEN_USER>(tokenInfo);
                IntPtr pSid = tokenUser.User.Sid;

                var nameSb = new StringBuilder(256);
                var domSb = new StringBuilder(256);
                uint nameLen = (uint)nameSb.Capacity;
                uint domLen = (uint)domSb.Capacity;

                if (LookupAccountSid(
                    null,
                    pSid,
                    nameSb,
                    ref nameLen,
                    domSb,
                    ref domLen,
                    out _
                ))
                {
                    return (name, $"{domSb}\\{nameSb}");
                }
            }
        }
        finally
        {
            Marshal.FreeHGlobal(tokenInfo);
            CloseHandle(hToken);
            CloseHandle(hProcess);
        }

        return (name, null);
    }
}
