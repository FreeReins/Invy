# Behind a reverse proxy

When Caddy, Traefik, nginx or another reverse proxy handles HTTPS, let it talk to Invy over plain HTTP. This is
also the safe way to reach Invy from outside your home: the proxy gets a real certificate, and Invy stays on
your network.

## Set Invy up

Turn Invy's own HTTPS off, and tell it which proxy to believe:

**Docker Compose:**

```yaml
    environment:
      Https__Mode: "Off"
      Https__TrustedProxies: 172.16.0.0/12   # the proxy's address or network
```

**Podman** (`~/.config/invy/invy.env`, then `systemctl --user restart invy`):

```sh
Https__Mode=Off
Https__TrustedProxies=10.0.0.5
```

- **`Https__TrustedProxies`** lists the proxies whose `X-Forwarded-For` and `X-Forwarded-Proto` headers Invy
  believes: addresses (`10.0.0.5`) or networks (`172.16.0.0/12`), separated by commas. With it, Invy knows
  visitors arrived over HTTPS, marks sign-in cookies secure, and **Settings → HTTPS** says HTTPS is handled by a
  reverse proxy.
- **Leave it empty when nothing sits in front of Invy.** Otherwise anyone could claim to be on HTTPS.
- **Point the proxy at port 8080.** Port 8443 isn't used in this mode.
- **"Always use HTTPS" stays off.** Redirecting to HTTPS, and sending `Strict-Transport-Security`, is the proxy's job.

Which address to list: with Docker Compose, the proxy's container network (Docker's default networks are inside
`172.16.0.0/12`). With the proxy on the same machine outside containers, the address the container sees it come
from. If **Settings → HTTPS** says "HTTPS is switched off" rather than "handled by a reverse proxy" while you're on
HTTPS, the listed address doesn't match the proxy.

## What the proxy must do

- **Pass the original `Host` header**, so links Invy makes (such as invites) use your public name.
- **Send `X-Forwarded-For` and `X-Forwarded-Proto`.**
- **Don't buffer `/api/live`.** It's a live stream that tells open pages when something changes on another device.
  Invy sends `X-Accel-Buffering: no` for nginx and a ping every 25 seconds to keep it open. Without it, pages
  still work, but changes from other devices only show after a reload.

Caddy and Traefik do all of this by themselves. nginx needs the `Host`, forwarded-header and upload-size lines below.

## Caddy

```caddy
invy.example.com {
    reverse_proxy invy:8080
}
```

## nginx

```nginx
server {
    listen 443 ssl;
    server_name invy.example.com;
    # ssl_certificate and ssl_certificate_key as usual

    client_max_body_size 200m;

    location / {
        proxy_pass http://invy:8080;
        proxy_set_header Host $host;
        proxy_set_header X-Forwarded-For $proxy_add_x_forwarded_for;
        proxy_set_header X-Forwarded-Proto $scheme;
    }
}
```

## Traefik

Labels on the Invy service in compose:

```yaml
    labels:
      - traefik.enable=true
      - traefik.http.routers.invy.rule=Host(`invy.example.com`)
      - traefik.http.routers.invy.entrypoints=websecure
      - traefik.http.routers.invy.tls.certresolver=letsencrypt
      - traefik.http.services.invy.loadbalancer.server.port=8080
```

## Before you open it to the internet

Invy is built for a home network. It has password hashing, lockout and HTTPS, but no two-step sign-in. Before you
expose it:

- use a strong password for every account;
- keep the [lockout](people-and-accounts.md#lockout) on;
- consider a VPN (WireGuard, [Tailscale](tailscale.md)) or your proxy's own sign-in in front of it instead.

Exposing it does let cloud AI assistants reach [MCP](ai-assistants.md).
