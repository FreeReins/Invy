#!/usr/bin/env bash
# Stops Invy and removes the Quadlet service that install.sh set up for the current user.
#
#   deploy/podman/uninstall.sh                 stop Invy and remove its service; keep its data
#   deploy/podman/uninstall.sh --delete-data   also delete the invy-data volume (database, sign-in keys,
#                                              backups), the settings file and the image (asks first)
#   deploy/podman/uninstall.sh --delete-data --yes   the same, without asking
#
# Kept data means running install.sh again brings Invy back exactly as it was.
set -euo pipefail

config_home="${XDG_CONFIG_HOME:-$HOME/.config}"
units="$config_home/containers/systemd/invy"
settings="$config_home/invy/invy.env"
volume=invy-data
images=(ghcr.io/freereins/invy:latest localhost/invy:latest)
# Quadlet makes a service from each unit, including the volume. Stopping that doesn't delete the volume.
services=(invy.service "$volume-volume.service")

delete_data=false
assume_yes=false
for argument in "$@"; do
    case "$argument" in
        --delete-data) delete_data=true ;;
        --yes) assume_yes=true ;;
        *) sed -n '2,9p' "$0" | sed 's/^# \{0,1\}//'; exit 1 ;;
    esac
done

if $delete_data && ! $assume_yes; then
    echo "This deletes Invy's database (items, spaces, accounts), its backups, sign-in keys,"
    echo "settings ($settings) and image. It can't be undone."
    echo "To keep a backup, download one from Settings > Backups first."
    read -r -p "Type delete to go ahead: " answer
    if [[ "$answer" != delete ]]; then
        echo "Nothing was changed."
        exit 1
    fi
fi

echo "Stopping Invy..."
systemctl --user stop "${services[@]}" 2>/dev/null || true

if [[ -d "$units" ]]; then
    rm -rf "$units"
    echo "Removed the service from $units."
fi
systemctl --user daemon-reload
systemctl --user reset-failed "${services[@]}" 2>/dev/null || true

if podman container exists invy; then
    podman rm --force --time 30 invy >/dev/null
    echo "Removed the invy container."
fi

if ! $delete_data; then
    echo
    echo "Invy is uninstalled. Its data is kept: the $volume volume, $settings and the image."
    echo "Run install.sh to bring it back, or uninstall.sh --delete-data to remove those too."
    exit 0
fi

if podman volume exists "$volume"; then
    podman volume rm "$volume" >/dev/null
    echo "Deleted the $volume volume."
fi
if [[ -f "$settings" ]]; then
    rm "$settings"
    rmdir "$(dirname "$settings")" 2>/dev/null || true
    echo "Deleted $settings."
fi
for image in "${images[@]}"; do
    if podman image exists "$image"; then
        podman rmi "$image" >/dev/null
        echo "Deleted the $image image."
    fi
done
echo
echo "Invy and its data are gone. Run install.sh to start again from the setup wizard."
