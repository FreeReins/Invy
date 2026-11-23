# First run

The first time you open Invy, a short setup wizard creates your account and checks the secure connection. It takes
a minute or two, and you can change everything later under **Settings**.

## The setup wizard

1. **Create your account.** Enter your name, a username and a password of at least 8 characters. This first
   account is the **admin**: it runs the server (HTTPS, backups, AI models and logs) and can invite other people.
2. **Secure connection.** This step is left out when HTTPS is turned off, for example behind a reverse proxy. It
   shows the HTTPS address and lets you:
   - enter the names and addresses people use to reach Invy, such as `invy.local` or `192.168.1.20`, so the
     certificate matches them;
   - download the certificate, to install on your devices so the browser stops warning;
   - choose **Always use HTTPS**, which is on to start with.

   [HTTPS and certificates](../admin/https-and-certificates.md) explains each of these.
3. Press **Finish**. Invy opens on the **Find** tab.

## Next steps

- [Check your backups](../admin/backups-and-restore.md): Invy makes one every night
- [Set up an AI model or connect an assistant](../admin/ai-assistants.md)
- [Send notifications to Home Assistant](../admin/notifications.md)
