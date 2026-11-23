# Notifications

Invy can post notifications to a **webhook**: an address that something else, such as Home Assistant, turns into
phone notifications or announcements. This page is for the admin who sets it up.

> [!NOTE]
> This version only sends the test notification. Reminders for warranties, upkeep, lent items and low stock come in a
> later version, and will use the webhook set up here.

## Set it up

1. Make a webhook in the app that will receive the notifications. For Home Assistant, see the example below.
2. In Invy, an admin opens **Settings → Notifications** and enters the **Webhook address**.
3. Choose **Send daily reminders at**: the time the daily reminders will go out.
4. Press **Send a test**, and check that it arrives.

**Send today's reminders now** sends the daily reminders straight away. Until there are reminders, it says nothing
needed sending.

## What Invy sends

Every notification is a JSON `POST`:

```json
{
  "type": "test",
  "title": "Invy test",
  "message": "Notifications from Invy are working.",
  "sentAt": "2026-11-22T13:30:00+00:00",
  "data": { "sentBy": "Settings" }
}
```

`title` and `message` are ready to show as they are. `type` and `data` let an automation word its own notification,
or do something else entirely.

## Home Assistant

This automation forwards every notification to your phones. Add it in Home Assistant (**Settings → Automations →
Create → Edit in YAML**):

```yaml
alias: "Invy notifications"
triggers:
  - trigger: webhook
    webhook_id: invy
    allowed_methods: [POST]
    local_only: true
actions:
  - action: notify.notify
    data:
      title: "{{ trigger.json.title }}"
      message: "{{ trigger.json.message }}"
```

The webhook address for this example is `http://<your Home Assistant>:8123/api/webhook/invy`.

## Other apps

Anything that accepts a JSON webhook works: Node-RED, n8n, ntfy (through a small bridge), Gotify, Discord or Slack
through an automation tool. Use `title` and `message` for the text.

## Good to know

- Notifications go one way. Invy posts to the webhook.
- Webhook addresses can be on your home network.
- A webhook that's down is tried again over the next half hour. A notification that still can't be delivered is
  logged under **Settings → Logs**.

## If something goes wrong

See [Notifications and integrations](../troubleshooting/notifications-and-integrations.md).
