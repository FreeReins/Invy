# Reach Invy over Tailscale

[Tailscale](https://tailscale.com) lets your phones reach Invy away from home without opening it to the internet.
This page is for whoever runs Invy. It assumes Tailscale is already running on the machine or NAS that runs
Invy and on each phone or computer that should reach it (see
[Tailscale's install guide](https://tailscale.com/kb/1017/install)). It covers what to change in Invy so the
address works without certificate trouble.

There are two ways. Pick one:

| | Address people use | Browser warning |
|---|---|---|
| **Invy's own certificate** | `https://<machine>.<tailnet>.ts.net:8443` | Until each device trusts Invy's certificate |
| **`tailscale serve`** | `https://<machine>.<tailnet>.ts.net` | None: Tailscale gets a real certificate |

`<machine>.<tailnet>.ts.net` is the machine's name in the Tailscale admin console, under **Machines**.

## Use Invy's own certificate

Nothing changes on the Tailscale side. Invy's certificate just has to name the Tailscale address.

1. Find the machine's Tailscale name (`<machine>.<tailnet>.ts.net`) and, if you'll also use it, its `100.x.y.z`
   address.
2. Sign in to Invy as an admin and open **Settings → HTTPS**.
3. Add both to **Names and addresses people use to reach Invy**, after the names already there, separated by
   commas. Press **Create a certificate for these names**.
4. On each device, download and trust the new certificate, as in
   [Trust the certificate on your devices](https-and-certificates.md#trust-the-certificate-on-your-devices).
5. Open `https://<machine>.<tailnet>.ts.net:8443`.

Setting up a new install? Put the names in `Https__Hostnames` before the first start instead; see
[Name the addresses you use](https-and-certificates.md#name-the-addresses-you-use).

## Use `tailscale serve`

`tailscale serve` takes HTTPS on the machine's Tailscale name, with a certificate from Let's Encrypt, and passes it on
to Invy's plain HTTP port. Invy keeps its own HTTPS on port 8443 for your home network.

1. In the Tailscale admin console, turn on **HTTPS Certificates** (under **DNS**). If you skip this, the next step
   asks you to.
2. On the machine that runs Invy, run:

   ```sh
   tailscale serve --bg http://127.0.0.1:8080
   ```

   It keeps running after restarts. `tailscale serve status` shows it; `tailscale serve reset` turns off everything
   `tailscale serve` was doing on that machine.
3. Tell Invy to believe Tailscale's `X-Forwarded-Proto` header, so it knows visitors arrived over HTTPS and doesn't
   send them to port 8443.

   **Docker Compose** (then `docker compose up -d`):

   ```yaml
       environment:
         Https__TrustedProxies: 172.16.0.0/12
   ```

   **Podman** (`~/.config/invy/invy.env`, then `systemctl --user restart invy`):

   ```sh
   Https__TrustedProxies=127.0.0.1
   ```

   Leave `Https__Mode` as it is.
4. Open `https://<machine>.<tailnet>.ts.net`. **Settings → HTTPS** should say "You are using HTTPS."

If the page jumps to `https://<machine>.<tailnet>.ts.net:8443`, or **Settings → HTTPS** says "You are using plain
HTTP", the address in `Https__TrustedProxies` doesn't match where the connection reaches Invy from. With Docker, the connection comes
from Docker's network (inside `172.16.0.0/12`); with rootless Podman, from `127.0.0.1`. See
[Behind a reverse proxy](reverse-proxy.md) for more on `Https__TrustedProxies`.

Links Invy makes, such as invites, use the Tailscale name, because `tailscale serve` passes
it on.

## What Tailscale doesn't reach

Only devices on your tailnet can reach Invy this way. Cloud [AI assistants](ai-assistants.md) can't. Apps on a phone or computer that's on the tailnet can.

## If something goes wrong

See [HTTPS and certificate problems](../troubleshooting/https-and-certificates.md).
