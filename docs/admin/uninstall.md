# Uninstall

## Podman

```sh
deploy/podman/uninstall.sh                # stop Invy and remove its service; keep its data
deploy/podman/uninstall.sh --delete-data  # also delete its data, settings and image (asks first)
```

Without a clone, download the script and run it the same way:
`curl -fsSL https://raw.githubusercontent.com/freereins/invy/main/deploy/podman/uninstall.sh | bash`.

The first form stops Invy and removes its service from `~/.config/containers/systemd/invy/`. It keeps the
`invy-data` volume, `invy.env` and the image, so `install.sh` brings Invy back as it was.

`--delete-data` removes those too, after you type `delete`. Download a backup from **Settings → Backups** first if you
want one.

## Docker Compose

```sh
docker compose down       # stop and remove the container; keep the data
docker compose down -v    # also delete the invy-data volume: all data, keys and backups
```

Download a backup first if you want to keep anything.

## Windows

Uninstall **Invy** from **Settings → Apps → Installed apps**. That removes the program and the Invy service, but
keeps `C:\ProgramData\Invy`, so installing again brings everything back. To remove your data too, delete that
folder afterwards.

The portable app installs nothing: delete `Invy-….exe` and the `Invy-data` folder next to it.
