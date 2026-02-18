using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.InteropServices;
using System.ServiceProcess;
using System.Text;
using System.Threading.Tasks;

namespace fabric_core.services.SessionMonitoringService;

public class SessionMonitoringService: ServiceBase
{
    //public SessionMonitoringService()
    //{
    //    ServiceName = "SessionMonitor";
    //    CanHandleSessionChangeEvent = true;
    //}

    //protected override void OnStart(string[] args)
    //{
    //    Console.WriteLine($"Service started!");
    //}

    //protected override void OnSessionChange(SessionChangeDescription changeDescription)
    //{
    //    base.OnSessionChange(changeDescription);

    //    switch(changeDescription.Reason)
    //    {
    //        case SessionChangeReason.SessionLogon:
    //            {
    //                OnLogin(changeDescription);
    //                break;
    //            }
    //        case SessionChangeReason.SessionLogoff:
    //            {
    //                OnLogout(changeDescription);
    //                break;
    //            }
    //        case SessionChangeReason.SessionLock:
    //            {
    //                int sessionId = changeDescription.SessionId;
    //                Console.WriteLine($"SessionID: {sessionId} has been locked");
    //                break;
    //            }
    //        case SessionChangeReason.SessionUnlock:
    //            {
    //                int sessionId = changeDescription.SessionId;
    //                Console.WriteLine($"SessionID: {sessionId} has been unlocked");
    //                break;
    //            }
    //        default:
    //            {
    //                Console.WriteLine($"Event: {changeDescription.Reason.ToString()}");
    //                break;
    //            }
    //    }
    //}

    //private void OnLogin(SessionChangeDescription description)
    //{
    //    int sessionId = description.SessionId;
    //    string? userName = QueryUserName(sessionId);
    //}

    //private void OnLogout(SessionChangeDescription description)
    //{
    //    int sessionId = description.SessionId;
    //}

    //// P/Invoke to translate SessionId → UserName
    //[DllImport("wtsapi32.dll", SetLastError = true)]
    //static extern bool WTSQuerySessionInformation(
    //  nint hServer, int sessionId,
    //  WTS_INFO_CLASS infoClass,
    //  out nint ppBuffer, out uint pBytesReturned);

    //[DllImport("wtsapi32.dll")]
    //static extern void WTSFreeMemory(nint pointer);

    //enum WTS_INFO_CLASS { WTSUserName = 5 }

    //private string? QueryUserName(int sessionId)
    //{
    //    if (WTSQuerySessionInformation(
    //          nint.Zero, sessionId,
    //          WTS_INFO_CLASS.WTSUserName,
    //          out var buffer, out _))
    //    {
    //        var user = Marshal.PtrToStringAnsi(buffer);
    //        WTSFreeMemory(buffer);
    //        return user;
    //    }
    //    return null;
    //}

}
