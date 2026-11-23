---
name: add-field-kind
description: "Add a kind of custom field to Invy's item types (such as colour, rating or duration) end to end: the kind enum, validation, the stored and sortable values, search syntax, the web editor and display, tests, docs. USE FOR: new field kind, new field type, new kind of custom field. DO NOT USE FOR: adding a field to a starter pack (edit config/types-*.json), a new item type (that's data, not code)."
---

# Add a field kind

`<Kind>` is the new value, for example `Rating`. Field definitions live in `item_types.fields` (jsonb) and values in
`items.fields`, keyed by field id. Sortable values are copied into `item_values` (num, date or text).

1. **Kind**: add `<Kind>` to `FieldKind` in `src/Invy.Core/Managers/Contracts/Messages/FieldKind.cs`, with a summary
   line. Enums are written by name, so never reorder or rename existing values.
2. **Definition rules**: in `Engines/ValidationEngine.Fields.cs`, the options the kind needs (for example
   `max` for a rating) and the error message when they're wrong.
3. **Value rules**: in `Engines/FieldEngine.cs`, parse and normalise a value of the kind (`TryRead<Kind>`), and say
   which `item_values` column it goes in, or none if it can't be sorted.
4. **Search**: in `Engines/QueryEngine.Fields.cs`, which operators the kind supports (`:` and ranges) and how a typed
   value is read (`rating>3`). The SQL side in `ItemAccess.Search.cs` needs no change unless the kind needs a new
   column.
5. **Web**: an editor in `src/Invy.Web/src/fields/<Kind>Input.svelte` and a display in `<Kind>Value.svelte`, both
   registered in `src/Invy.Web/src/fields/kinds.js`. Labels sit beside the input, and the input works with the
   keyboard.
6. **AI**: if photos can fill in the kind, describe it in `Engines/Transformation/FieldSchema.cs` so AI answers
   use the right JSON type.
7. **Tests**: `FieldKindTests` (valid and invalid definitions and values), a `QueryEngineTests` case for its
   operators, and a stub field of the kind in `src/Invy.Web/tests/data.js`.
8. **Docs**: a row in the field kinds table in `docs/using/types-and-fields.md`, and its search operators in
   `docs/using/search.md`.

## Example

`Money`: an amount plus a currency in the type's options, stored in `item_values.num`, searched with `price>50`.

## Troubleshooting

- `JsonShapeTests` fails on the enum: the new value is missing its `JsonStringEnumConverter` entry.
- Existing items show the value under **Other fields**: the field id changed. Ids must stay the same when a kind
  changes.
