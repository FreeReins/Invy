# What you need

This page lists what Invy needs to run, so you can choose where to install it.

## A machine to run it on

Invy is one container, or one Windows program, with its PostgreSQL database inside. Any always-on computer on
your home network works: a home server, a NAS, a mini PC, a Raspberry Pi 4 or 5 with a 64-bit system, or the
desktop you use every day.

- **Processor:** 64-bit x86 (`amd64`) or 64-bit ARM (`arm64`). 32-bit systems aren't supported.
- **Memory and disk:** small. The app and database fit comfortably on hardware that runs a few other home services.
- **Software**, one of these:
  - Linux with **Podman 5 or later and systemd**, such as Fedora, Bazzite or another Fedora Atomic desktop. This is
    the main supported setup. See [Install with Podman](install-podman.md).
  - **Docker** or Podman, on Linux, Windows, macOS or a NAS. See [Install with Docker](install-docker-compose.md).
  - **Windows 10 or 11** (64-bit x86), with no containers at all. See [Install on Windows](install-windows.md).

## Network

| Port | Used for |
|---|---|
| `8080` | Plain HTTP. Sends visitors on to HTTPS once **Always use HTTPS** is on |
| `8443` | HTTPS, with a certificate Invy makes itself |
| `54339` | The database, on the same computer only (127.0.0.1). Nothing outside can reach it. Change it with `Database__Port` if something else uses it |

You can publish them on other ports on the host. See
[Use other ports](install-docker-compose.md#use-other-ports).

Invy is meant for a home network. To reach it from outside, put it behind a reverse proxy with a real
certificate. See [Behind a reverse proxy](../admin/reverse-proxy.md).

Invy itself only goes out to the internet when you set it up to: the notification webhook, and AI models on another
computer or service.

## Storage that must be kept

Everything Invy keeps is in one folder: `/data` in the container (the `invy-data` volume), or
`C:\ProgramData\Invy` on Windows. Keep it, and keep a copy of the backups somewhere else too.

| In the data folder | Holds | If it's lost |
|---|---|---|
| `database` | Everything: accounts and settings, and the database's own password | All data is gone. Restore a backup |
| `keys` | Sign-in keys and the HTTPS certificate | Everyone is signed out, and every device has to accept or install the new certificate |
| `backups` | Daily backups | Only the backups |

## Browsers and phones

Any current browser: Firefox, Chrome, Edge or Safari, on a computer or a phone. A phone can add Invy to its home
screen when it reaches Invy over HTTPS with a certificate it trusts. See
[HTTPS and certificates](../admin/https-and-certificates.md#trust-the-certificate-on-your-devices).

## Next

- [Install with Podman](install-podman.md) on Linux with systemd
- [Install on Windows](install-windows.md) with the installer or the portable app
- [Install with Docker](install-docker-compose.md) anywhere else
- [Other platforms](install-other-platforms.md): Synology, Unraid, TrueNAS, Portainer, Docker Desktop, macOS
