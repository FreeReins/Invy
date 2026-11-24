# Reports

This page is for anyone using Invy. It shows how to see what your things are worth, print an insurance inventory,
list what audits didn't find, and download any of them, or any view, as a spreadsheet.

Reports are at the bottom of the **Views** tab, under **Reports**. Each one shows only when its feature is on for
the space (**Settings**, this space): value totals and the insurance inventory need values, and the missing items
need audits. See [Spaces](spaces.md) for switching features on.

## What it is worth

**What it is worth** shows the value of everything in the space, how many items there are and how many have no
value, then two tables:

- **By place**: each place at the top (a room, a shed) with everything inside it at any depth. **Not in a place** is
  things at the top that aren't in a container.
- **By type**: each type and what its items are worth.

An item's value is its type's first money field times its amount. Currencies aren't converted: a total simply adds
the numbers, so keep one currency per space if you want totals to mean something. Totals update a moment after
something changes.

## Print an insurance inventory

1. On **Views**, under **Reports**, open **Insurance inventory**.
2. Check the list: every thing in the space, by room, with its photo, type, details and value, and a total per
   room. Containers are left out unless they have a value of their own.
3. Press **Print or save as PDF**. Your browser's print dialog opens; choose **Save as PDF** as the printer to keep
   a file, or a real printer for paper. Buttons and tabs are left off the page.

The page lists up to 5,000 things. Above that it says so, and the CSV has everything.

## List the missing items

**Missing items** lists everything an [audit](audits.md) didn't find, or that was marked missing by hand, with where
it belongs and when it was last seen. Ticking a thing in a later audit, or marking it found, takes it off the list.
Press **Print or save as PDF** to print it.

## Download a spreadsheet

- **Download CSV** next to a report, or on its page, downloads every row of it.
- **Export CSV** on an open [view](views.md) downloads everything the view finds right now, in its order.

Every CSV has the same columns: Room, Place, Name, Marking, Type, Amount, Unit, Value, Status, Label, Last seen,
Added, Tags, Photo, then one column per field. Photo is the address of the item's first photo, which opens while
you're signed in to Invy. The file is UTF-8, so Excel, Numbers and LibreOffice keep accents, and a cell that starts
like a formula is written as plain text.

## If something goes wrong

- **Reports isn't on the Views tab.** Values and audits are both off for this space. Switch one on under
  **Settings**.
- **A total looks too low.** Some items have no price: the first line says how many items have no value. Add a
  money field to their type and fill it in.
- **The printed page has a header and page numbers.** Those come from the browser. Switch off **Headers and footers**
  in its print dialog.
