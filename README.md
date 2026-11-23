<div align="center">

# Invy

**The self-hosted home inventory: everything you own, where it is, found in a moment.**

For what you keep in boxes, sheds, workshops and stockrooms. Runs on your own server with Docker or Podman, or on
Windows. Free and open source.

[![License: GPL v3](https://img.shields.io/badge/license-GPLv3-blue.svg)](LICENSE)
[![Latest release](https://img.shields.io/github/v/release/freereins/invy?sort=semver)](https://github.com/FreeReins/invy/releases)
[![Container image](https://img.shields.io/badge/image-ghcr.io-2496ED?logo=docker&logoColor=white)](https://github.com/FreeReins/invy/pkgs/container/invy)
[![Documentation](https://img.shields.io/badge/docs-read-success)](https://freereins.github.io/invy/)
[![Buy Me a Coffee](https://img.shields.io/badge/support-Buy%20Me%20a%20Coffee-FFDD00?logo=buymeacoffee&logoColor=black)](https://buymeacoffee.com/strifer.btw)

[**Get started**](#get-started-in-two-minutes) · [**What's coming**](#whats-coming) · [**Documentation**](docs/index.md)

<picture>
  <source media="(prefers-color-scheme: dark)" srcset="docs/images/find-desktop-dark.png">
  <img alt="Invy's Find tab, finding a baseball in Garage › Box F" src="docs/images/find-desktop-light.png" width="900">
</picture>

</div>

> [!NOTE]
> Invy is being built. This version installs, signs people in, and has HTTPS, backups, notifications and AI model
> setup in place. Items, search and labels arrive over the next versions: follow [the roadmap](docs/roadmap/README.md).

## Why Invy

**Find it in a moment.** Search runs as you type and shows where each thing is: *Shed › Shelf 2 › Box ABC*.

**Boxes inside boxes.** Places and containers nest as deep as you like. Move a box and everything in it moves too,
and each item remembers where it has been.

**Less typing.** Print labels with QR codes, scan barcodes, or let an AI model read a photo and fill in the details.

**Your things, your server.** No account to make, no subscription, no ads, no tracking. Your list of belongings stays
on your hardware.

## What's coming

| | |
|---|---|
| 🔎 **Find** | Search by name, type, field or place, with saved views |
| 📦 **Places and containers** | Nest as deep as you like; moves keep a timeline |
| 🏷️ **Types and fields** | A tool's serial number, a cable's length, a book's author |
| 📷 **Capture** | Labels, barcodes and AI photo reading |
| 👪 **Spaces** | Home, Workshop, Shop: each with its own members and roles |
| 🤖 **Assistants** | Claude and other MCP assistants can search and update your inventory |

[The design](docs/developers/design.md) has the detail.

## Ready today

| | |
|---|---|
| 💾 **Backs itself up** | Nightly backups with one-click restore. [Backups →](docs/admin/backups-and-restore.md) |
| 🔒 **Secure by default** | HTTPS out of the box, account lockout, and a strict browser security policy. [HTTPS →](docs/admin/https-and-certificates.md) |
| 🔔 **Notifications** | To Home Assistant or any webhook. [Notifications →](docs/admin/notifications.md) |
| 🤖 **AI models** | Claude, OpenAI or a local model such as Ollama, and per-person tokens for MCP assistants. [AI assistants →](docs/admin/ai-assistants.md) |

## Get started in two minutes

Invy is one container (or one Windows program) with its database inside. Nothing else to set up.

**Docker or Podman** (Linux, macOS, NAS):

```sh
docker run -d --name invy --restart unless-stopped -p 8080:8080 -p 8443:8443 \
  -v invy-data:/data -e TZ=America/Chicago ghcr.io/freereins/invy:latest
```

**Podman as a service** (Fedora, Bazzite and other Linux with systemd; starts at boot and updates itself):

```sh
curl -fsSL https://raw.githubusercontent.com/freereins/invy/main/deploy/podman/install.sh | bash
```

**Windows:** download `Invy-….msi` from the [latest release](https://github.com/FreeReins/invy/releases/latest)
and run it, or the portable `Invy-…-portable.exe` to try it without installing.

Then open `http://<your server>:8080` and follow the setup wizard.
[Full install guides →](docs/getting-started/requirements.md)

## Documentation

Everything you need to run Invy is in [the documentation](docs/index.md). It's plain Markdown that you can read
here, on the [documentation site](https://freereins.github.io/invy/), or inside Invy itself by pressing **?**.

| I want to… | Start here |
|---|---|
| Install Invy | [Requirements](docs/getting-started/requirements.md) · [Podman](docs/getting-started/install-podman.md) · [Docker](docs/getting-started/install-docker-compose.md) · [Windows](docs/getting-started/install-windows.md) · [NAS and others](docs/getting-started/install-other-platforms.md) |
| Set it up | [First run](docs/getting-started/first-run.md) · [Settings](docs/admin/settings.md) · [HTTPS](docs/admin/https-and-certificates.md) · [Backups](docs/admin/backups-and-restore.md) · [Upgrading](docs/admin/upgrading.md) |
| Fix a problem | [Troubleshooting](docs/troubleshooting/index.md) |
| Build on it | [The design](docs/developers/design.md) · [The roadmap](docs/roadmap/README.md) · [Contributing](CONTRIBUTING.md) |

## Screenshots

| Find (light) | Find (phone) |
|---|---|
| <img alt="The Find tab in light mode" src="docs/images/find-desktop-light.png" width="420"> | <img alt="The Find tab on a phone" src="docs/images/find-phone.png" width="200"> |

## Your data stays yours

Invy has no cloud service, no telemetry and no accounts outside your server. It only goes out to the internet to
reach the webhook and AI models you set up.

## Support and contributing

- **Questions or problems?** Start with [Troubleshooting](docs/troubleshooting/index.md), then
  [open an issue](https://github.com/FreeReins/invy/issues/new/choose). See [SUPPORT.md](SUPPORT.md).
- **Security issue?** Report it privately, as [SECURITY.md](SECURITY.md) describes.
- **Want to help?** Read [CONTRIBUTING.md](CONTRIBUTING.md) and [the design](docs/developers/design.md).
- **Enjoying Invy?** Star the repository, tell a friend, or [buy me a coffee](https://buymeacoffee.com/strifer.btw).

## License

[GNU General Public License v3.0](LICENSE).
