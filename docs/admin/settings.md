# Settings reference

Almost everything is set in Invy's web page, under **Settings**. A few things that have to be known before Invy
starts (the database, HTTPS, ports) are set in the environment instead. This page lists both.

## In the web page

Settings save by themselves as you change them. **Admin** means only admins see or change that section. The sections
are grouped in the sidebar under **You**, **Connections** and **System**.

### Login

| Setting | Default | Who |
|---|---|---|
| **Your password:** change your own. Your other devices are signed out | | Everyone |
| **Wrong passwords in a row before an account is locked** (0 turns lockout off) | 5 | Admin |
| **Minutes an account stays locked** | 15 | Admin |

See [People and accounts](people-and-accounts.md).

### Appearance

Light, dark, or follow the device. Kept in this browser only. The sun and moon button in the header switches too.

### HTTPS

Whether you're on HTTPS, the certificate's expiry date, **Download the certificate**, the names the certificate
covers (admin), and **Always use HTTPS** (admin). See [HTTPS and certificates](https-and-certificates.md).

### AI assistants

For admins, the AI models Invy can use and which model does each task. For everyone, the MCP address and your own
tokens for AI assistants. See [AI assistants](ai-assistants.md).

### Notifications (admin)

| Setting | Default | Meaning |
|---|---|---|
| **Webhook address** | empty | Where notifications are posted. Empty turns notifications off |
| **Send daily reminders at** | 07:30 | When the daily reminders go out |

See [Notifications](notifications.md).

### Backups (admin)

| Setting | Default | Meaning |
|---|---|---|
| **Back up automatically every day** | On | |
| **At** | 03:00 | |
| **Keep this many backups** | 14 | 0 keeps them all |
| **Delete backups older than this many days** | 0 | 0 keeps them whatever their age |

See [Backups and restore](backups-and-restore.md).

### Logs (admin)

The last 1000 log entries since Invy started, with a filter and search. See
[Monitoring and logs](monitoring-and-logs.md).

## Settings in the environment

These are read when Invy starts. Set them in `~/.config/invy/invy.env` with the Podman setup, then run
`systemctl --user restart invy`. With Docker Compose, put them under the Invy service's `environment:` and run
`docker compose up -d`. Names use `__` (two underscores) between their parts.

They can also go in `settings.json` in the data folder, which is the way to set them on Windows
(`C:\ProgramData\Invy\settings.json`, or `Invy-data\settings.json` next to the portable app). It's JSON, with
one level per part of the name; environment variables win over it. Restart Invy after changing it.

```json
{
  "Https": { "Hostnames": "home-server,192.168.1.20" },
  "Accounts": { "RecoverUsername": "" }
}
```

| Setting | Default | Meaning |
|---|---|---|
| `Data__Root` | `/data` in the container; `C:\ProgramData\Invy` for the Windows service; `Invy-data` next to the portable app | The folder that holds everything Invy keeps: `database`, `keys`, `backups` and `settings.json` |
| `Database__Port` | `54339` | The port Invy's own PostgreSQL listens on. It only listens on 127.0.0.1, so nothing outside the computer or container can reach it. Change it if something else on the computer uses it |
| `TZ` | the host's, with Podman | Your time zone, such as `Europe/London`. Reminders and backups follow local time |
| `Https__Mode` | `SelfSigned` | `SelfSigned`, `Certificate` or `Off`. See [HTTPS](https-and-certificates.md) |
| `Https__Hostnames` | empty | Names and addresses the first self-signed certificate covers, separated by commas |
| `Https__PublicPort` | `8443` | The HTTPS port people type, when it differs from 8443 on the host |
| `Https__HttpPort`, `Https__Port` | `8080`, `8443` | The ports Invy listens on inside the container |
| `Https__CertificatePath`, `Https__KeyPath`, `Https__CertificatePassword` | empty | Your own certificate, in `Certificate` mode. See [Your own certificate](own-certificate.md) |
| `Https__TrustedProxies` | empty | Reverse proxies whose forwarded headers are believed. See [Behind a reverse proxy](reverse-proxy.md) |
| `Accounts__RecoverUsername` | empty | Logs a password reset link for this account at startup. See [Lost the admin password](people-and-accounts.md#lost-the-admin-password) |
| `Data__Backups`, `Data__SessionKeys` | `backups`, `keys` | The backup and keys folders, inside `Data__Root` unless a full path is given |
| `Logging__LogLevel__Default` | `Information` | `Debug` for more detail when tracking down a problem, `Warning` for less |
