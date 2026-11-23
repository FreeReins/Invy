# Items, containers and moves

This page is for anyone using Invy. It explains how to add things, put them inside each other, move them, and count
them.

Everything in Invy is an **item**: a place (the shed), a container (box F) or a thing (a baseball). An item that
**Holds items** can have others inside it, to any depth: *Shed › Box ABC › Box F › Baseball*.

## Add items

- **One at a time:** on the **Browse** tab, press **New item** for something at the top of the space, or open a
  container and press **Add an item** to put it inside. Give it a **Name**, and if you like a **Marking** (whatever is
  written on it, such as *ABC*), a **Type** and its fields, **Tags** and **Notes**.
- **Several at once:** type them into **Quick add**, separated by commas: `baseball, glove, 2 bats`. A number in
  front makes a counted item (*Bats*, 2), and so does a unit (`3.5 kg filament`) or a count after (`screws x200`).
  In a container they go inside it; on the top of **Browse** they go to the **Inbox**.

## The Inbox

Things with no place and no type wait in the **Inbox**, at the bottom of **Browse**. Open one, then **Edit** it to
give it a type, or **Move** it into a container.

## Look inside

**Browse** shows the places at the top of the space. Open one to see what's inside it, and so on down. The line
above an item's name shows where it is, such as *Top › Garage › Box F*; each part is a link.

A container shows how many things are inside it, at any depth (and, for types with a money field, what they are worth
together). Invy recounts these just after a change, so they can lag a moment behind.

## Move items

1. Open the item and press **Move**.
2. Open containers until you are where it should go. **Top** takes you back to the top of the space.
3. Press **Move to …**.

Everything inside goes with it: moving box F to the garage moves the baseball in it too. Invy won't put an item
inside itself or inside something that is inside it.

## Photos and files

An item's page has **Photos and files**. Press **Add a photo or PDF** to attach pictures (JPEG, PNG, WebP or GIF) or
PDFs, such as a receipt or a manual, up to 10 MB each. On a phone it offers the camera. Photos are made smaller before
they are sent, and show as thumbnails; press one to open the whole file. Adding a photo writes an entry in the item's
timeline.

The files are kept in Invy's data folder and backed up with everything else (see
[Backups and restore](../admin/backups-and-restore.md)). Viewers can look at them; editors and owners add and remove
them.

## Counted items

Turn on **Counted** for many of the same, such as 200 screws or 3.5 kg of filament, and give an **Amount** and an
optional **Unit**. On its page, the **One more** and **One fewer** buttons change the amount. It can't go below zero.

## Dispose and restore

Press **Dispose** when something is thrown away, sold or given away. It leaves its container, but its history is
kept, and **Restore** brings it back. A container has to be empty first.

## If something goes wrong

- *"… doesn't hold items."* Edit that item and turn on **Holds items**, or pick another place.
- *"… is inside …, so … can't go into it."* Move the inner one out first.
- *"Someone else changed … while you were editing it."* Close the form, open the item again, and redo your change.
- *"… still has … inside."* Move what's inside somewhere else before disposing of the container.
- *"Invy keeps photos (JPEG, PNG, WebP or GIF) and PDFs."* Save the file as one of those first. iPhone photos taken
  as HEIC are usually turned into JPEG by the browser; if not, change **Settings → Camera → Formats** to **Most
  Compatible** on the phone.
