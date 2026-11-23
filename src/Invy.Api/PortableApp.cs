using System.Diagnostics;

namespace Invy.Api;

/// <summary>
/// The portable Windows app: one Invy.exe, built with the Invy.Portable switch (deploy/windows/build.ps1). It
/// unpacks itself to a temporary folder, so it keeps its data in a Invy-data folder next to the exe, and when
/// someone opens it by hand it says where Invy is and opens it in the browser.
/// </summary>
public static class PortableApp
{
    /// <summary>True in the portable build.</summary>
    public static bool IsPortable => AppContext.TryGetSwitch("Invy.Portable", out var portable) && portable;

    /// <summary>Invy-data next to Invy.exe.</summary>
    public static string DataRoot =>
        Path.Combine(Path.GetDirectoryName(Environment.ProcessPath) ?? AppContext.BaseDirectory, "Invy-data");

    /// <summary>Says where Invy is and opens it in the browser, when it is the portable app opened by someone.</summary>
    public static void Announce(IConfiguration configuration, ILogger logger)
    {
        if (!IsPortable || !Environment.UserInteractive)
        {
            return;
        }

        var address = $"http://localhost:{configuration.GetValue("Https:HttpPort", 8080)}";
        logger.LogWarning(
            "Invy is running at {Address}. Its data is in {Folder}. Close this window to stop Invy.",
            address, configuration["Data:Root"]);
        try
        {
            Process.Start(new ProcessStartInfo(address) { UseShellExecute = true })?.Dispose();
        }
        catch (Exception exception) when (exception is System.ComponentModel.Win32Exception or InvalidOperationException)
        {
            logger.LogInformation("Open {Address} in your browser: Invy couldn't open it for you ({Reason}).", address, exception.Message);
        }
    }
}
