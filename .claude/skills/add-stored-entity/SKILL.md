---
name: add-stored-entity
description: "Add a new stored table to Invy: entity, contract, mapping, DbContext, backups, accessor, registration, test fake. USE FOR: new table, new entity, persist a log or journal. DO NOT USE FOR: a column on an existing entity, endpoints (add-core-operation), web screens (add-web-screen)."
---

# Add a stored entity

Edit these without reading them first; each step names the line to copy.
`<Name>` is the type, `<Names>` the table and DbSet.

1. `src/Invy.Core/ResourceAccess/Persistence/<Name>Entity.cs`: plain properties, `Guid Id`.
2. `ResourceAccess/Contracts/Messages/<Name>.cs`: `internal sealed class`, `Id = Guid.CreateVersion7()`, XML summary.
   A new enum gets `[JsonConverter(typeof(JsonStringEnumConverter<T>))]` so backups write it by name.
3. `Persistence/InvyDbContext.cs`: a `DbSet` after the others, and `modelBuilder.Entity<...>` with `ToTable`
   and indexes before `StoreEnumsByName`. Enum columns are stored by name automatically.
4. New `Persistence/EntityMapping.<Name>.cs`, copied from `EntityMapping.Space.cs`: `ToContract` and `ToEntity`.
5. `ResourceAccess/BackupAccess.Tables.cs`: a line at the end of `Tables`
   (`new BackupTable<<Name>Entity>("<names>", BackupJson.Default.List<Name>Entity)`, with a comment
   "Empty in backups made before ..."), and `[JsonSerializable(typeof(List<<Name>Entity>))]` on `BackupJson` in
   `BackupAccess.cs`. Children reached through a parent's `Include` need neither.
6. `Contracts/Services/I<Name>Access.cs` and `ResourceAccess/<Name>Access.cs`, shaped like `SpaceAccess`.
   Removing rows added in the same request: `Load` the query, then remove from `db.<Names>.Local`.
7. `CoreRegistration.cs`: `services.AddScoped<I<Name>Access, <Name>Access>();` with the other accessors.
8. `tests/Invy.Core.Tests/Fake<Name>.cs` with a public `Rows` list, a property on `TestCore`, and
   `services.AddSingleton<I<Name>Access>(<Name>)` in `TestCore.Register`.

## Example

`Space`: `SpaceEntity`, `ISpaceAccess`, `SpaceAccess`, `FakeSpaces`. Before phase 1a: ChefBot's `FoodMoment` in `../chef-bot`.

## Finish

Add a migration: `dotnet tool restore`, then `dotnet ef migrations add Add<Name> --project src/Invy.Core
--output-dir ResourceAccess/Persistence/Migrations`. Make the new class `internal` and read its `Up`. Build and test
(AGENTS.md). Add the table to `docs/developers/data-model.md`.

## Troubleshooting

- `MigrationTests` fails: the model changed with no migration; add one (Finish).
- `SchemaTests` fails: the table is missing from `BackupAccess.Tables` (step 5). A table that must not be backed
  up goes in that test's `NotBackedUp` with the reason.
