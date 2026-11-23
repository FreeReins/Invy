# Notifications and integrations

## Notifications don't arrive

1. Press **Send a test** under **Settings → Notifications**. Invy says whether the webhook answered.
2. **"The webhook couldn't be reached"**: Invy's machine can't reach that address. With containers, `localhost`
   means the Invy container itself, not your computer: use the other machine's network address or name.
3. **"The webhook answered HTTP 404"**: the address is wrong. For Home Assistant, it's
   `http://<home assistant>:8123/api/webhook/<webhook_id>`, and the automation must be saved and on.

## Home Assistant gets the notification but nothing happens

The automation receives `trigger.json`. Check its trace in Home Assistant (**Automations → the automation →
Traces**). `local_only: true` means the request must come from your network.

## AI assistants

- **401 Unauthorized:** the token is wrong, revoked, or its account was removed. Tokens aren't kept in backups, so
  make a new one after a restore.
- **Can't connect:** the assistant must reach Invy at the MCP address. Cloud assistants can't reach an Invy that is only
  on your home network.
- **Certificate errors:** the machine running the assistant must trust Invy's certificate. See
  [Trust the certificate](../admin/https-and-certificates.md#trust-the-certificate-on-your-devices).
- **The `/api` endpoints say 401 with a token:** tokens only work on `/mcp`. Scripts sign in with a password and use the
  sign-in cookie. The API reference is at `/docs` on Invy's address.
