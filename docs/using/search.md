# Search

This page is for anyone using Invy. It explains the search box on the **Find** tab and everything you can type in it.

Type in the box on **Find** and the results appear as you type. Each result shows where the thing is, such as
*in Garage › Box F*; open it to see its page. With the box empty, **Find** shows what changed most recently.

## Words

Plain words find items by their name, marking, label code, tags, notes and text fields. A word can be half typed (`base` finds
*Baseball*) or slightly misspelt (`basebll` still finds it): when the words match nothing as typed, Invy looks for
names spelt nearly the same. Every word has to match. Put words in quotes to find them
together: `"box f"`.

## Search terms

| Type | To find |
|---|---|
| `type:tool` | items of a type, by its name |
| `in:shed`, `in:"Box F"` | anything inside a container, at any depth, by its name, marking or label code |
| `brand:dewalt` | an exact field value, using the field's search name from the **Types** tab |
| `price>50`, `price<=20`, `price:10..20` | ranges on number, money and date fields that have **Sort and compare** turned on |
| `bought>2026-01-31` | dates are written year-month-day |
| `qty<5`, `qty:2..10` | counted items by their amount |
| `seen>1y` | not seen in more than a year, or never |
| `added<30d`, `added>2026-01-31` | added in the last 30 days, or after a date. Times ago use d, w, m and y |
| `is:container`, `is:counted`, `is:missing`, `is:lent` | what an item is |
| `is:disposed`, `is:archived` | disposed and archived things, which are otherwise left out |
| `is:low` | counted things at or below their [low-stock level](stock.md) |
| `is:overdue`, `lent:sam` | [loans](lending.md) past their due date, and things lent to someone whose name contains the text |
| `due<30d`, `due:2026-12-01..2026-12-31` | a [reminder date](reminders.md) or loan due back within 30 days (overdue included), or in a range |
| `going:kitchen` | boxes whose [moving](moving.md) destination has that name, marking or label code |
| `project:bookshelf` | what an open [project](projects.md) sets aside |
| `has:barcode` | items with a value in a barcode field |
| `tag:garden` | items with a tag |
| `name:`, `marking:`, `notes:` | text anywhere in that part of the item |

Terms combine: `drill type:tool in:garage price>50` finds tools in the garage, with "drill" in them, that cost more than
50. Put `OR` between alternatives (`drill OR saw`), and `-` before a term to leave its matches out
(`in:garage -is:container`).

## Sort and count

Results come best match first when you typed words, or by name otherwise. The list under the box changes that to
**Name** or **Recently changed**. The line above the results says how many were found; above 10,000 it is an
estimate. Scroll down and more load by themselves.

## Keep a search

Press **Save as a view** above the results to keep a search with a name, show it as a table, cards, a tree, a
board or a timeline, and pin it to **Find**. See [Views](views.md).

## If something goes wrong

The line under the box says what Invy couldn't read and how to write it. For example:

- *"There is no type called …"* The line lists the types there are.
- *"No field has the search name …"* Check the field's search name on the **Types** tab.
- *"… can't be compared."* Turn on **Sort and compare** for that field on the **Types** tab, then edit and save its
  items again so their values are copied for comparing.
- Nothing found? Disposed things are left out unless you add `is:disposed`.
