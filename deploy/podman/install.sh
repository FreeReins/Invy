#!/usr/bin/env bash
# Installs or updates Invy as a rootless Podman Quadlet service for the current user. Invy is one container
# that runs its own database, keeping everything in the invy-data volume.
#
#   install.sh           use the published image (ghcr.io), kept up to date by podman auto-update
#   install.sh --build   build the image from this checkout instead (needs the repository, not just this script)
#
# Works straight from the web, without cloning:
#   curl -fsSL https://raw.githubusercontent.com/freereins/invy/main/deploy/podman/install.sh | bash
#
# Run it again to update the services: settings and data are kept.
set -euo pipefail

published_image=ghcr.io/freereins/invy:latest
example_url=https://raw.githubusercontent.com/freereins/invy/main/deploy/podman/invy.env.example
config_home="${XDG_CONFIG_HOME:-$HOME/.config}"
units="$config_home/containers/systemd/invy"
settings="$config_home/invy/invy.env"
here="$(cd "$(dirname "${BASH_SOURCE[0]:-$0}")" 2>/dev/null && pwd || pwd)"

build=false
for argument in "$@"; do
    case "$argument" in
        --build) build=true ;;
        *) sed -n '2,11p' "$0" | sed 's/^# \{0,1\}//'; exit 1 ;;
    esac
done

if ! command -v podman >/dev/null 2>&1; then
    echo "Invy needs Podman 4.4 or later. Install it (for example: sudo apt install podman), then run this again." >&2
    exit 1
fi

if $build; then
    repo="$(cd "$here/../.." 2>/dev/null && pwd)"
    if [[ ! -f "$repo/Dockerfile" ]]; then
        echo "--build needs the Invy repository: run deploy/podman/install.sh --build from a clone." >&2
        exit 1
    fi
    echo "Building the Invy image..."
    podman build --tag localhost/invy:latest "$repo"
    image=localhost/invy:latest
    auto_update=local
else
    echo "Downloading the Invy image..."
    podman pull "$published_image"
    image=$published_image
    auto_update=registry
fi

if [[ ! -f "$settings" ]]; then
    mkdir -p "$(dirname "$settings")"
    if [[ -f "$here/invy.env.example" ]]; then
        cp "$here/invy.env.example" "$settings"
    else
        curl -fsSL "$example_url" -o "$settings"
    fi
    echo "Created $settings. Edit it to change HTTPS options; most settings are in the app."
fi

mkdir -p "$units"
# Left by an install from before Invy was one container.
rm -f "$units"/invy.pod "$units"/invy-db.container "$units"/invy-db.volume \
    "$units"/invy-keys.volume "$units"/invy-backups.volume

cat > "$units/invy-data.volume" <<'EOF'
# Everything Invy keeps: its database, sign-in keys, HTTPS certificate and backups.
[Volume]
VolumeName=invy-data
EOF

cat > "$units/invy.container" <<EOF
# Invy, written by install.sh. Its settings are in ~/.config/invy/invy.env.
[Unit]
Description=Invy

[Container]
ContainerName=invy
Image=$image
# podman auto-update (its timer: systemctl --user enable --now podman-auto-update.timer) replaces the image with a
# newer one and restarts Invy.
AutoUpdate=$auto_update
EnvironmentFile=%h/.config/invy/invy.env
Timezone=local
# HTTP, then HTTPS. To use other ports on this computer, change the left-hand number. If you change the HTTPS one,
# set Https__PublicPort in invy.env to match.
PublishPort=8080:8080
PublishPort=8443:8443
# To keep the data in a folder instead, create it, give it to the app's user with
#   podman unshare chown 1654 ~/invy-data
# and use this line in place of the one below:
#   Volume=%h/invy-data:/data:Z
Volume=invy-data.volume:/data
# Time for Invy to stop its database cleanly.
StopTimeout=30

[Service]
Restart=always

[Install]
# Start with your user session, or at boot once lingering is on (loginctl enable-linger).
WantedBy=default.target
EOF

systemctl --user daemon-reload
# The old pod, if this machine had Invy from before it was one container.
systemctl --user stop invy-pod.service invy-db.service 2>/dev/null || true
systemctl --user restart invy.service

if [[ "$auto_update" == registry ]] && ! systemctl --user is-enabled --quiet podman-auto-update.timer 2>/dev/null; then
    systemctl --user enable --now podman-auto-update.timer >/dev/null 2>&1 \
        && echo "Turned on podman-auto-update.timer: Invy updates itself daily when there's a new release."
fi

echo
echo "Invy is starting at http://localhost:8080 and https://localhost:8443"
echo "Logs: journalctl --user -u invy -f"
if [[ "$(loginctl show-user "$USER" --property=Linger --value 2>/dev/null)" != "yes" ]]; then
    echo "Invy stops when you log out. To keep it running and start it at boot, run:"
    echo "  loginctl enable-linger $USER"
fi
