# Labels and scanning

This page is for anyone using Invy. It explains how to print QR labels before anything is packed, turn a label into a
box with one scan, and move things by scanning them.

A **label** is a sticker with a QR code and a short code under it, such as **7K3Q**. Codes use letters and digits that
can't be mistaken for each other (there is no 0 or O, 1 or I). Each code belongs to one space, and no two labels in
the same Invy share a code, so a scan always finds the right space.

## Print labels

You need to be an editor or owner of the space.

1. Open **Settings**, then **Labels and printing**.
2. Pick your **Sticker sheet**: **Avery 5160, 30 per sheet (US Letter)** or **Avery L7651, 65 per sheet (A4)**. Other
   brands sell the same sizes under their own numbers.
3. Choose how many **Sheets**, then press **Make … labels**. A preview of the sheets appears.
4. Press **Print**. In the print dialog, print at actual size (100%, not "fit to page"), with no margins added.

Lost a sheet, or printed too few? **Print the unclaimed labels again** prints every label in the space that isn't on
anything yet. Viewers can do this too.

## Turn a label into a box

Stick a label on a box. Then either:

- **Scan it with the phone's camera app.** It opens Invy at the label's address. If the label isn't on anything yet,
  Invy offers **Make Box 7K3Q**; one tap makes a box called *Box 7K3Q*, in the place you're adding to (see below).
- **Use the Scan tab** in **Claim** mode, and scan label after label. Each one becomes a box straight away.

Rename the box, give it a type or fill it from its page afterwards. A label that is already on something opens that
thing instead.

To put a label on something that already exists, open it and press **Stick on a label**, then scan or type the code.
If it had a label before, the old one is freed to use on something else.

## Where new things go

The bar that says **Adding to Box F** shows where scanning puts new boxes, and where **Add** mode's quick add puts new
things. It stays the same until you change it, on this device, for each space.

- Press **Change** to pick a container, or **Top** to go back to the top of the space.
- On a container's page, press **Add things here**.
- In **Add** mode, scan a container's label.

## The Scan tab

Press **Start the camera** and point it at a label. If the camera can't read one, type the code in the box under the
camera and press **Go**. The camera needs a secure connection (HTTPS); without one, typing the code still works.

| Mode | What a scan does |
|---|---|
| **Open** | Opens the item a label is on. A product barcode (UPC or EAN) searches for it on **Find** |
| **Move** | Scan the thing, then scan where it's going. It moves there with everything inside it |
| **Claim** | Each new label becomes a box in the place you're adding to |
| **Add** | Makes the scanned container the place you're adding to, then **Quick add** puts things in it |

Viewers see only **Open**.

## Find things by label

Search finds an item by its label code, and `in:7K3Q` finds everything inside the box with that label. See
[Search](search.md).

## If something goes wrong

- **"isn't an Invy label"**: what was scanned isn't a label from this Invy. Labels from Invy have 4 to 6 letters and
  digits.
- **"belongs to another space"**: the label was made in another space. Scan it in **Open** mode to switch to that
  space, or pick it with the space picker.
- **The camera doesn't start**: allow the camera in the browser's site settings, and check Invy is opened over HTTPS.
  See [HTTPS and certificates](../admin/https-and-certificates.md).
