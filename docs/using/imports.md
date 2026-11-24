# Importing from a spreadsheet or Homebox

This page is for anyone bringing a list of things into Invy: a spreadsheet you kept before, or an export from
Homebox. It shows how to upload the file, check where each column goes, and follow the import.

Importing is always available to editors and owners, under **Settings → Import**.

## Get a CSV file

- **From a spreadsheet:** save or export it as CSV (comma, semicolon or tab separated, UTF-8), with a heading row
  that names the columns, such as *Name, Quantity, Location*.
- **From Homebox:** use Homebox's **Export to CSV**. Invy recognises its columns, which start with `HB.`.

A file can be up to 20 MB and 100,000 rows.

## Import it

1. Open **Settings → Import**, choose the file under **CSV file**, and press **Upload and check**.
2. Check where each column goes. Invy suggests it from the headings, and shows the first rows:
   - **Name** (one column must be the name), **Marking**, **Amount**, **Unit**, **Tags**, **Notes**.
   - **Location**: the containers it goes in, separated by `/`, such as *Garage / Shelf 2*. Each one is found by
     name, or made.
   - **Type, by name**: a type the space has.
   - **A line in the notes**: adds *Heading: value* to the notes.
   - A **field** of the type picked in **Type for every row**.
   - **Leave out**.
3. Optionally pick **Type for every row**, and press **Choose** next to **Put them in** to import into a container.
4. Press **Import**.

The import runs in the background, 1,000 rows at a time, and carries on if you close the page. The **Imports** list
shows how many rows it has read and how many items it added.

For Homebox, locations become nested containers, labels become tags, the asset ID becomes the marking, and the
description and other details (manufacturer, serial number, purchase details, custom fields) go into the notes.
A Homebox amount of 1 is a single thing, not a counted one.

## Rows it skipped

A row without a name, with an amount that isn't a number, or with a value a field can't hold is skipped, and listed
under **rows were skipped** with the reason. Fix those rows and import just them again.

## If something goes wrong

- **"That file has no heading row".** The first line must name the columns.
- **"Pick exactly one column for the name".** Set one column to **Name**.
- **The import stays at Queued.** Invy may still be busy with another import; imports run one at a time. Look in
  **Settings → Logs** if it doesn't move.
