# People and accounts

This page is for the admin: the person who set Invy up. It covers inviting people, resetting passwords, locking
and unlocking accounts, and getting back in if you lose the admin password.

> [!NOTE]
> The **People** tab is for the people in a space, not for accounts: see
> [Spaces, people and roles](../using/spaces.md). Making accounts is the admin's job, and for now it is in the HTTP
> API only: open `/docs` on Invy's address for
> the API reference. `POST /api/invites` makes an invite link, `POST /api/users/{id}/password-reset` a reset link,
> `POST /api/users/{id}/unlock` unlocks an account and `DELETE /api/users/{id}` removes one.

## Invite someone

An invite link works once and expires after 7 days. Send it to the person by message or email: Invy doesn't send
email itself. They open it and create their own account.

## Passwords

- **Change your own password** under **Settings → Login**. Your other devices are signed out.
- **Someone forgot theirs:** an admin makes a reset link for them and sends it. It works once and expires after 24
  hours.

## Lockout

After 5 wrong passwords in a row, an account is locked for 15 minutes.

- **Unlock someone early** through the API (see the note at the top).
- **Change the rules** under **Settings → Login → Sign-in lockout**: how many wrong passwords in a row (0 turns lockout
  off), and how many minutes an account stays locked.
- Sign-ins sent all at once count too: only the first 5 have their passwords checked.

## Lost the admin password

When no admin can sign in, use the recovery setting:

1. Set `Accounts__RecoverUsername` to the admin's username:
   - **Podman:** add `Accounts__RecoverUsername=yourname` to `~/.config/invy/invy.env`, then run
     `systemctl --user restart invy`.
   - **Docker Compose:** add `Accounts__RecoverUsername: yourname` under the Invy service's `environment:`, then
     run `docker compose up -d`.
2. Find the reset line in the log:
   - **Podman:** `journalctl --user -u invy | grep "Password reset"`
   - **Docker Compose:** `docker compose logs invy | grep "Password reset"`

   It reads like `Password reset for yourname: open Invy and add /?reset=… to its address`.
3. Open Invy's address with `/?reset=…` on the end, such as `https://invy.local:8443/?reset=…`, and choose a
   new password. The link works once and expires after 24 hours.
4. **Remove the setting** and restart again, so a new link isn't logged at every start.

If the log says there's no such account, check the spelling of the username.

## After a restore

Restoring a backup brings back the accounts and passwords as they were when the backup was made, and signs everyone
out, the admin included. See [Backups and restore](backups-and-restore.md).

## If something goes wrong

See [Signing-in problems](../troubleshooting/signing-in.md).
