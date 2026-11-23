using System.Runtime.Versioning;
using Microsoft.Extensions.Logging.EventLog;

namespace Invy.Api;

/// <summary>The Windows service's warnings and errors go to the Application event log as Invy, the source the installer registers.</summary>
[SupportedOSPlatform("windows")]
public static class WindowsEventLog
{
    public const string Source = "Invy";

    public static void UseInvySource(EventLogSettings eventLog) => eventLog.SourceName = Source;
}
