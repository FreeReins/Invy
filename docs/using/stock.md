# Low stock

This page is for anyone who keeps counted things, such as screws, filament or stock for sale, and wants to be told
before they run out. It shows how to give a thing a low-stock level and where Invy tells you it's running low.

Low stock shows while the space has the **Stock** feature on (**Settings → Kind and features**). See
[Spaces](spaces.md#features).

## Set a level

1. Open the thing on **Browse**. It must be counted: it has an **Amount**.
2. Press **Edit**.
3. In **Low stock at**, enter the amount at or below which it is running low, such as 5. Leave it empty to stop
   watching it.
4. Press **Save**.

A thing at or below its level shows **Running low** on its page.

## Where you hear about it

- **Right away:** when a change takes a thing from above its level to at or below it, Invy sends one
  notification to the webhook (type `low-stock`). Topping it up and running low again sends another.
- **Every day:** the daily reminders include a list of everything still low (type `low-stock-daily`).
- **On the Views tab:** **Running low** lists everything at or below its level.
- **In search:** `is:low` finds the same things, and combines with other terms (`is:low in:workshop`).

Notifications go to the webhook an admin sets up; see [Notifications](../admin/notifications.md).

## Projects count too

What [projects](projects.md) have set aside counts against the level, even though the amount doesn't change until
it's used up. 20 screws with 16 set aside and a level of 5 are running low: only 4 are free.

## If something goes wrong

- **Low stock at isn't in the form.** The thing isn't counted (tick **Counted** first), or **Stock** is off for
  the space.
- **No notification arrived.** Check the webhook with **Send a test** under **Settings → Notifications**, and see
  [Notifications and integrations](../troubleshooting/notifications-and-integrations.md).
