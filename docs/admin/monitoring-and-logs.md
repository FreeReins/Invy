# Monitoring and logs

This page shows how to check that Invy is running, where its logs are, and what to collect before asking for help.

## Is it running?

| Setup | Command |
|---|---|
| Podman | `systemctl --user status invy` |
| Docker Compose | `docker compose ps` |
| Windows | `Get-Service Invy` in PowerShell, or the **Services** app |

Invy starts and stops its database itself, so if Invy is running, so is the database.

## Health check

`GET /health` answers `{"status":"ok"}` when Invy is up. It needs no sign-in and is never redirected to HTTPS, so
uptime monitors such as Uptime Kuma or Home Assistant can use it:

```sh
curl http://localhost:8080/health
```

## Logs

**In Invy:** admins open **Settings → Logs** for the last 1000 entries since Invy started. Filter to warnings and
errors, or search.

**Full logs:**

| Setup | Command |
|---|---|
| Podman | `journalctl --user -u invy -f` |
| Docker Compose | `docker compose logs -f invy` |
| Windows service | Warnings and errors are in **Event Viewer → Windows Logs → Application**, source **Invy** |
| Windows portable app | The window it runs in |

Leave out `-f` to read the log so far instead of following it. With Podman, `--since "1 hour ago"` narrows it down.

The database's warnings and failures are copied into Invy's log, starting `PostgreSQL:`. Its full log is
`database/postgres.log` in the data folder: `podman exec invy cat /data/database/postgres.log` (or
`docker compose exec invy …`), or `C:\ProgramData\Invy\database\postgres.log` on Windows.

To look inside the database by hand, in the container:

```sh
podman exec -it invy sh -c 'PGPASSWORD=$(cat /data/database/password) psql -h 127.0.0.1 -p 54339 -U invy invy'
```

For more detail while tracking down a problem, set `Logging__LogLevel__Default=Debug`, restart, reproduce the
problem, then set it back.

## Scheduled jobs

The daily reminders and the daily backup are scheduled jobs, kept in the database. Each run
schedules the next one, so they survive restarts. If Invy was stopped when a job was due, it runs as soon as
Invy starts again: daily reminders and the backup run if their time has passed that day.

A webhook that's down is tried again over the next half hour. Anything that still fails is logged.

## Before asking for help

Collect:

1. The Invy version (the release tag, or `git log -1 --oneline` with Podman).
2. How you installed it (Podman, Docker Compose, Windows, NAS…).
3. The log around the time of the problem, with passwords and tokens removed.
4. What you did, what you expected, and what happened.

Then [open an issue](https://github.com/FreeReins/invy/issues/new/choose).
