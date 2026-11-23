# Install on Windows

This guide installs Invy on a Windows PC with no Docker or containers: one installer that runs Invy in the
background, starting with Windows, with its database inside. It takes a minute. There's also a portable app for trying
Invy without installing anything.

You need **Windows 10 or 11, 64-bit** (x86, not ARM), and an administrator account to install.

## Install

1. Download `Invy-<version>-win-x64.msi` from the
   [latest release](https://github.com/FreeReins/invy/releases/latest).
2. Open it. If Windows says **Windows protected your PC**, press **More info**, check that the file name is the one
   you downloaded, then press **Run anyway**. Windows shows this for programs it hasn't seen often.
3. Accept the licence and press **Install**, then **Yes** when Windows asks for permission.
4. Leave **Open Invy in your browser** ticked and press **Finish**.

Invy opens at <http://localhost:8080> and sends you on to `https://localhost:8443`. The browser warns about the
certificate the first time: choose to continue. [HTTPS and certificates](../admin/https-and-certificates.md) explains
why, and how to make the warning go away for good.

The setup wizard starts. Continue with [First run](first-run.md).

## What the installer does

1. Installs Invy in `C:\Program Files\Invy`.
2. Sets up the **Invy** service, which starts with Windows (shortly after the desktop appears), restarts if it
   stops, and runs without anyone signed in.
3. Keeps everything Invy saves in `C:\ProgramData\Invy`: the database, sign-in keys, HTTPS certificate and
   backups.
4. Lets phones and other computers reach ports 8080 and 8443 through the Windows firewall on **private** networks.
5. Adds **Invy** and **Invy data folder** to the Start menu.

## Use it from your phone

Open `http://<this PC's name>:8080` on the phone, such as `http://home-pc:8080`, or use the PC's address, which
`ipconfig` shows as **IPv4 Address**. The phone must be on the same network.

If it doesn't open, Windows may count your home network as **Public**, where the firewall blocks Invy. Open
**Settings → Network & internet**, choose your connection, and set **Network profile type** to **Private network**.

The PC must be on, and not asleep, for Invy to answer. A PC that sleeps after a few minutes isn't a good home for
Invy; change it under **Settings → System → Power**.

## Settings

Most settings are in Invy's web page. The few read at startup, such as the names in the HTTPS certificate, go in
`C:\ProgramData\Invy\settings.json` (create it in Notepad as an administrator). See the
[settings reference](../admin/settings.md#settings-in-the-environment). Then restart the service in the **Services**
app, or in PowerShell as an administrator:

```powershell
Restart-Service Invy
```

## Day-to-day

| To | Do |
|---|---|
| Check it's running | **Services** app, **Invy**; or `Get-Service Invy` in PowerShell |
| Stop or start it | **Services** app; or `Stop-Service Invy` / `Start-Service Invy` as an administrator |
| Read the log | Invy's **Settings → Logs**, or **Event Viewer → Windows Logs → Application**, source **Invy** |
| Update | Run the new version's `.msi`. See [Upgrading](../admin/upgrading.md) |
| Uninstall | See [Uninstall](../admin/uninstall.md#windows) |

## The portable app

`Invy-<version>-win-x64-portable.exe`, from the same release, is Invy in one file. Put it in a folder of its
own and open it: a window opens and Invy opens in the browser. It keeps its data in a `Invy-data` folder next to
the file, and runs until you close the window.

It doesn't start with Windows or open the firewall, so it's best for trying Invy out. To move to the installed
version, make a backup under **Settings → Backups**, install, and restore it there.

## If something goes wrong

See [Install and startup problems](../troubleshooting/install-and-startup.md). The PostgreSQL log, in
`C:\ProgramData\Invy\database\postgres.log`, says more when the database won't start.

## Next

[First run](first-run.md): create your account and plan your first week.
