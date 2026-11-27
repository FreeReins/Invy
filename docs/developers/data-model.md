# Database changes

This page is for developers and for admins who want to know what an upgrade does to the database. The tables
themselves are described in [the design](design.md#data-model).

Invy's schema comes from EF Core migrations in
[`src/Invy.Core/ResourceAccess/Persistence/Migrations/`](https://github.com/FreeReins/invy/tree/main/src/Invy.Core/ResourceAccess/Persistence/Migrations),
applied when Invy starts. A released migration is never changed; each change is a new one.
[Upgrading](../admin/upgrading.md) says what an admin does, which is usually nothing.

## Migrations

| Migration | What it changed |
|---|---|
| `Baseline` | Accounts, invite and reset links, API tokens, AI models and routes, the inbox |
| `AddSpace` | `Spaces` and `SpaceMembers` |
| `AddItemType` | `ItemTypes`, with their fields as JSON |
| `AddItems` | `Items`, `ItemEvents` and `ItemValues` |
| `AddSearch` | Search, and each container's `ContentCount` and `ContentValue` |
| `AddLabels` | `Labels`, and `Items.LabelCode` |
| `AddAttachments` | `Attachments` |
| `AddBarcodeLookup` | The admin's barcode lookup switch |
| `AddSuggestedTypes` | `Items.SuggestedTypeId` and `TypeSuggestedAt`, for the Inbox type sweep |
| `AddSpaceFeatures` | `Spaces.Features`, `Items.IsExample` (with the partial index `IX_Items_Examples`) and `SpaceMembers.GettingStartedDismissed` |
| `AddAudienceFeatures` | `Items.LowStockAt`, `Reserved`, `LentTo`, `LentAt`, `DueOn` and `DestinationId` (each list with a partial index), `Spaces.CalendarToken`, and the `Projects`, `ProjectItems` and `Imports` tables |

## AddSpaceFeatures

Spaces gained [kinds and features](design.md#tailored-spaces). As it runs, the migration switches on each existing
space's kind's features, so a home made before has *values*, *barcodes*, *lending* and *reminders* on. A space made
with no starter pack gets none. No existing item is an example, and nobody's getting-started card starts dismissed.

## AddAudienceFeatures

The phase 6 features ([design](design.md#audience-features-phase-6)). Every new item column is empty, so nothing is
low, lent, reserved or going anywhere until someone sets it, and no space's calendar feed is on. Projects are backed
up with what each sets aside inside them. Imports aren't backed up: an import is short-lived work, and what it added
is in the items.
