# Last seen and audits

This page is for anyone who keeps things in boxes and wants to know they are still there. It explains how Invy
notes when things were last seen, and how to check a box against what Invy says is in it.

## Last seen

Opening a container on **Browse**, or scanning its label, marks it and everything directly in it as seen. Scanning a
single thing's label in **Open** mode marks that thing. An item's page shows **Last seen**.

This needs an editor or owner: viewers can look without changing when things were seen. Opening the same box again
within ten minutes changes nothing, and its timeline gets one **saw** entry, not one for each thing inside.

To find what hasn't been seen for a while, search for `seen>1y` (not seen in more than a year, or never). See [Search](search.md).

## Audit a box

An audit walk checks a container against what Invy says is in it. Use it when you open a box you haven't looked in for
a while, or to check a shelf before a move.

1. Open the container and press **Audit**, or on the **Scan** tab pick **Audit** and scan the container's label.
2. Tick what's there. Scanning a label ticks the thing it's on; something scanned that Invy has somewhere else is
   listed under **Scanned, but Invy has them elsewhere**.
3. Press **Finish the audit**.

Invy then lists the differences:

| List | What happened |
|---|---|
| **Missing** | Things that weren't ticked. They are marked missing, with an entry in their timeline. Lent things are left alone |
| **Found again** | Ticked things that had been marked missing. They are back to normal |
| **Here, but Invy had them elsewhere** | Ticked things Invy had in another place. Press **Move into …** to put them here |

Everything ticked, and the container itself, is marked seen. Search with `is:missing` to see everything still
missing. Ticking a missing thing in a later audit finds it again.

An audit checks up to 2,000 things directly in one container. For a bigger one, audit the containers inside it.

## If something goes wrong

- *"… doesn't hold items."* Only containers can be audited. Open the container it is in instead.
- *"Some of the ticked items no longer exist."* Someone removed something while you were ticking. Close the audit
  and start it again.
- *"Label … isn't on anything yet."* Claim it first: see [Labels and scanning](labels-and-scanning.md).
