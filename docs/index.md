# Invy documentation

Invy is a self-hosted home inventory: everything you own, where it is, and how to find it again in a moment. It runs
on a computer in your home, keeps your data there, and works for one person, a household or a small business. This is
the place to learn how to install it, keep it running and fix it when something goes wrong.

These pages are plain Markdown in the [`docs/` folder](https://github.com/FreeReins/invy/tree/main/docs)
of the repository. Read them on GitHub, on the [documentation site](https://freereins.github.io/Invy/),
in any text editor, or inside Invy itself: press the **?** button at the top of any screen.

## I want to…

### Try Invy

- [Find out what Invy does](getting-started/what-is-invy.md)
- [Check what I need to run it](getting-started/requirements.md)
- Install it: [with Podman on Linux](getting-started/install-podman.md),
  [with Docker](getting-started/install-docker-compose.md), [on Windows](getting-started/install-windows.md), or
  [on a NAS, macOS or a Raspberry Pi](getting-started/install-other-platforms.md)
- [Set it up the first time](getting-started/first-run.md)

### Use Invy

- [Make a space and share it with people](using/spaces.md)
- [Set up the types of things I keep, and their fields](using/types-and-fields.md)
- [Add things, put them in boxes and move them](using/items.md)
- [Print QR labels and scan things to add and move them](using/labels-and-scanning.md)
- [Check a box against what Invy says is in it](using/audits.md)
- [See where something has been](using/timeline.md)
- [Find anything](using/search.md)
- [Keep a search as a view and pin it to Find](using/views.md)
- [See what it is all worth, and print an insurance inventory](using/reports.md)
- [Get told when something is running low](using/stock.md)
- [Lend things and get reminded when they're due back](using/lending.md)
- [Get reminded of warranties and upkeep, in my calendar too](using/reminders.md)
- [Pack boxes for a move and print a manifest for each](using/moving.md)
- [Set things aside for a project](using/projects.md)
- [Bring in a spreadsheet or a Homebox export](using/imports.md)

### Run Invy

- [Invite people, reset passwords and unlock accounts](admin/people-and-accounts.md)
- [Set up HTTPS](admin/https-and-certificates.md), [use my own certificate](admin/own-certificate.md) or
  [run it behind a reverse proxy](admin/reverse-proxy.md)
- [Reach it away from home over Tailscale](admin/tailscale.md)
- [Look up any setting](admin/settings.md)
- [Send notifications to Home Assistant or anywhere else](admin/notifications.md)
- [Set up AI models and connect an assistant such as Claude](admin/ai-assistants.md)
- [Back up and restore](admin/backups-and-restore.md)
- [Upgrade to a new version](admin/upgrading.md)
- [Check that it's running and read its logs](admin/monitoring-and-logs.md)
- [Uninstall it](admin/uninstall.md)

### Fix a problem

Start at [Troubleshooting](troubleshooting/index.md). It lists common symptoms and links each one to its fix.

### Build on it

- [The design: what Invy does and how it's built](developers/design.md)
