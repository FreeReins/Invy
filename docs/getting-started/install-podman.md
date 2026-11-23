# Install with Podman

This guide installs Invy on Linux as a rootless Podman service that starts by itself, restarts if it stops, logs
to the system journal and updates itself when there's a new release. Invy is one container: it runs its own
database inside it. It takes a minute or two.

You need Linux with **Podman 5 or later** and **systemd**, as on Fedora, Bazzite and other Fedora Atomic desktops.
Nothing else.

## Install

1. Run the install script:

   ```sh
   curl -fsSL https://raw.githubusercontent.com/freereins/invy/main/deploy/podman/install.sh | bash
   ```

   From a clone of the repository, `deploy/podman/install.sh` does the same.

2. Optional but recommended: keep Invy running after you log out, and start it when the computer starts:

   ```sh
   loginctl enable-linger $USER
   ```

3. Open <http://localhost:8080> on the same computer, or `http://<this computer's address>:8080` from another
   device. Invy sends you on to `https://…:8443`. The browser warns about the certificate the first time:
   choose to continue. [HTTPS and certificates](../admin/https-and-certificates.md) explains why, and how to make
   the warning go away for good.

The setup wizard starts. Continue with [First run](first-run.md).

## What the script does

1. Downloads the Invy image from `ghcr.io/freereins/invy`.
2. Copies the settings file to `~/.config/invy/invy.env`, unless it's already there.
3. Writes the service into `~/.config/containers/systemd/invy/` and starts it.
4. Turns on `podman-auto-update.timer`, which checks for a new image once a day and restarts Invy with it.

Run it again whenever you like: your settings and data are kept. See [Upgrading](../admin/upgrading.md).

To run code that isn't released yet, build the image from a clone instead: `deploy/podman/install.sh --build`.

## Day-to-day commands

```sh
systemctl --user status invy          # is it running?
systemctl --user restart invy         # after editing invy.env
journalctl --user -u invy -f          # follow the log
systemctl --user stop invy            # stop it (data is kept)
systemctl --user start invy           # start it again
podman auto-update                       # update now instead of waiting for the timer
```

The most recent log entries are also in Invy, under **Settings → Logs** (admins only).

## Where things are kept

| What | Where |
|---|---|
| Settings file | `~/.config/invy/invy.env` |
| Service | `~/.config/containers/systemd/invy/` |
| Database, sign-in keys, HTTPS certificate and backups | the `invy-data` Podman volume, mounted at `/data`. To keep it in a folder instead, see the comment in `invy.container` |

## Change the ports

Invy listens on 8080 (HTTP) and 8443 (HTTPS). To use other ports on the host, edit the left-hand numbers in
`~/.config/containers/systemd/invy/invy.container`:

```ini
PublishPort=80:8080
PublishPort=443:8443
```

If you change the HTTPS port, also set `Https__PublicPort` to it in `invy.env`, so Invy's links and redirects
use it. Then:

```sh
systemctl --user daemon-reload
systemctl --user restart invy
```

Rootless Podman can't use ports below 1024 unless your system allows it. Running `install.sh` again writes the
service file afresh, so make the change again after it.

## If something goes wrong

Invy doesn't open: see [Install and startup problems](../troubleshooting/install-and-startup.md).

## Next

[First run](first-run.md): create your account and plan your first week.
