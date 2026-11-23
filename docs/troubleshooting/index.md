# Troubleshooting

Find your problem below and follow the link. Every page says what causes the problem and how to fix it. Most fixes
need only the web page or one command.

## Find your problem

| What you see | Go to |
|---|---|
| Invy won't start, or the page doesn't open | [Install and startup](install-and-startup.md) |
| "Invy can't write to …" in the log | [Install and startup](install-and-startup.md#invy-cant-write-to-a-folder) |
| "Invy's bundled PostgreSQL didn't start" | [Install and startup](install-and-startup.md#invys-bundled-postgresql-didnt-start) |
| "Your connection is not private" or another certificate warning | [HTTPS and certificates](https-and-certificates.md) |
| A phone won't trust Invy | [HTTPS and certificates](https-and-certificates.md#a-phone-wont-trust-the-certificate) |
| "This account is locked…" | [Signing in](signing-in.md#an-account-is-locked) |
| Forgot a password, or the admin is locked out | [Signing in](signing-in.md) |
| Everyone was signed out | [Signing in](signing-in.md#everyone-was-signed-out) |
| Notifications or an AI assistant don't work | [Notifications and integrations](notifications-and-integrations.md) |
| A backup failed or won't restore | [Backups](backups.md) |

## First checks

These solve a lot of problems:

1. **Is Invy running?** `systemctl --user status invy` (Podman) or `docker compose ps` (Docker).
2. **What does the log say?** **Settings → Logs** in Invy, or `journalctl --user -u invy` /
   `docker compose logs invy`. Invy's messages say what went wrong and what to do.
3. **Reload the page.** After an upgrade, the browser may still have the old page.

## Still stuck?

[Collect the details](../admin/monitoring-and-logs.md#before-asking-for-help) and
[open an issue](https://github.com/FreeReins/invy/issues/new/choose).
