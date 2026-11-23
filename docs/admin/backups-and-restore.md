# Backups and restore

Invy backs itself up every night. This page explains what's in a backup, how to keep copies safe, and how to
restore one.

## What Invy does by itself

- Writes a backup every day at **03:00** and keeps the newest **14**.
- An admin changes the time, how many to keep, and a maximum age under **Settings → Backups**.
- Backups are compressed JSON files with all of Invy's own data: accounts, settings, AI
  models with their API keys, spaces and everything in them, and the bell's messages. API tokens for AI assistants
  aren't included.
- Photos and PDFs attached to items aren't inside the backup file. As each backup is made, any attached file not
  copied before is copied into `files` inside the backups folder. Each file is copied once and kept there, even after
  it is removed from Invy or old backups are deleted, so any backup can be restored with its files.

## Make, download and delete backups

Under **Settings → Backups**:

- **Back up now** makes one straight away. Do this before upgrading.
- Each backup in the list can be opened to see what it holds, **downloaded**, **restored** or **deleted**.

## Keep a copy somewhere else

A backup on the same disk doesn't help if that disk fails. Either:

- download a backup now and then, and keep it somewhere safe, along with the `files` folder inside the backups
  folder if you attach photos; or
- keep the backups in a folder that is itself backed up, as below.

Backups contain password hashes and AI API keys, so keep downloaded copies private.

### Keep backups in a folder

Backups are kept in `backups` in Invy's data folder. To keep them somewhere else, mount a folder over it.

**Podman:** create the folder and give it to Invy's user, then add a line after the `/data` one in
`~/.config/containers/systemd/invy/invy.container` (running `install.sh` again writes that file afresh, so add
it again after):

```sh
mkdir ~/invy-backups
podman unshare chown 1654 ~/invy-backups
```

```ini
Volume=%h/invy-backups:/data/backups:Z
```

Then run `systemctl --user daemon-reload && systemctl --user restart invy`.

**Docker Compose:** add `- ./backups:/data/backups` under `volumes:`, after the `/data` line, with the folder made
as in [Use a host folder instead of a volume](../getting-started/install-docker-compose.md#use-a-host-folder-instead-of-a-volume).

**Windows:** set `Data:Backups` to the folder in `settings.json` (see the
[settings reference](settings.md#settings-in-the-environment)). The Invy service must be able to write to it.

## Restore a backup

> [!WARNING]
> Restoring replaces **everything** in Invy with what the backup holds. Make a fresh backup first if you might
> want today's data back.

1. Open **Settings → Backups**.
2. To restore a backup you downloaded, put it back in the backups folder first, keeping its name
   (`invy-backup-….json.gz`). For a volume, copy it in: `podman cp invy-backup-….json.gz invy:/data/backups/`
   or `docker compose cp invy-backup-….json.gz invy:/data/backups/`. On Windows, copy it into
   `C:\ProgramData\Invy\backups`. Then reload the page.
3. Press restore next to the backup and confirm.
4. Sign in again.

After a restore:

- **Everyone signs in again,** the admin who restored included.
- **Passwords go back** to what they were when the backup was made. Sessions from before the restore stop working.
- **AI assistants need new tokens.**
- **Attached files come back** from `files` in the backups folder, when they are missing from Invy's own `files`
  folder.

## Move Invy to another machine

1. On the old machine, **Back up now** and download the backup.
2. Install Invy on the new machine and create a temporary account in the setup wizard.
3. Copy the backup into the new backups folder, and the old backups folder's `files` folder next to it, then
   restore it.
4. Sign in with your old account.

Devices that trusted the old certificate need the new one. Or copy the keys volume across too, to keep it.

## If something goes wrong

See [Backup problems](../troubleshooting/backups.md).
