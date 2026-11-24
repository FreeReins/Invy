# AGENTS.md

Invy is a self-hosted .NET 10 inventory built from ChefBot (`../chef-bot`): Wolverine HTTP API, EF Core on a bundled
PostgreSQL, a Svelte 5 page in `src/Invy.Web/`. [CONTRIBUTING.md](CONTRIBUTING.md) is the full guide. Before changing
the web page or `docs/`, read the `AGENTS.md` in that folder.

## Plan and progress

`docs/roadmap/README.md` lists the phases and their status. Do one with `/phase <id>`: it reads only that phase
file and the `docs/developers/design.md` sections it names, never the whole design. A design change goes in the
roadmap's Decisions list and `design.md` in the same commit.

## Check your work

```sh
dotnet build Invy.slnx -v q -nologo
dotnet run --project tests/Invy.Core.Tests
dotnet run --project tests/Invy.Core.Tests -- --treenode-filter "/*/*/ItemMoveTests/*"   # one class
```

Done means no build warnings and passing tests. `./dev.sh`, `./dev.sh test` and `dotnet watch` never exit; don't
run them or `podman build --target test`.

## Checklists

A new table, manager operation, web screen, field kind, MCP tool, AI task, space feature or docs change has a checklist
skill in `.claude/skills/`: follow it.

## Rules the code doesn't make obvious

- `Invy.Core` is layered and calls only go down: endpoints, handlers and MCP tools call one manager
  (`Managers/Contracts/Services/I*Manager`). Managers call engines and accessors. Only `ValidationEngine` among
  engines calls accessors. No manager calls a manager, no engine calls an engine. `ArchitectureTests` enforces it.
- Core is `internal` except `Managers/Contracts/` and `CoreRegistration`. `Managers/Contracts/Messages/` records are
  the HTTP JSON: keep shapes stable, give public types an XML summary.
- Accessors take and return the contracts in `ResourceAccess/Contracts/Messages/`, never EF entities. Managers
  commit once through `IMessageAccess.CommitAsync`.
- Space-owned rows carry `space_id`; every accessor query filters by it, and `ValidationEngine` checks the caller's
  space role. Install admin grants no space access.
- An item's container is `parent_id` plus its `ltree` `path`. A move writes one `Moved` event and updates the
  subtree's paths in one statement. Never write events or rows per descendant.
- `QueryEngine` parses search text into a query tree; `ItemAccess` turns the tree into SQL. Never put query text
  into SQL.
- Managers react to each other only through messages (`IMessageAccess.PublishAsync`), handled in
  `src/Invy.Api/Messaging/<Message>Handler.cs`, which calls the other manager. Each handler names its queue with
  `[StickyHandler(MessageQueues.…)]` and must be safe to run twice and commit once (Wolverine sends only the first commit's messages; for more work, publish the next message). Never retry by hand: `CoreRegistration` policies do. Every message gets `[MessageIdentity("invy.<name>.v1")]`, is listed in
  `ContractJsonContext` and only gains fields. A breaking change is a new `.v2`.
- The schema comes from EF Core migrations, applied at startup. After changing an entity: `dotnet tool restore`, then
  `dotnet ef migrations add <Name> --project src/Invy.Core --output-dir ResourceAccess/Persistence/Migrations`, then
  change the new class to `internal`. Never edit a released migration. A new table also goes in `BackupAccess.Tables`.
- One container (and a Windows app) running its own PostgreSQL: never add a second container or volume. Files go
  under `Data:Root`.
- A setting people should change goes in `deploy/podman/invy.env.example`, an API change in
  `docs/reference/api.md`, a user-visible change in its `docs/` page.
- User-facing messages are plain sentences saying what happened and what to do next.

## Tests

TUnit, in `tests/Invy.Core.Tests/`: engines directly, managers with the `Fake*` accessors through `TestCore`
(`new TestCore().ItemManager()`). Assertions are awaited: `await Assert.That(actual).IsEqualTo(expected);`.
Ids made in the same millisecond have no order, so assert lists sorted by name, never by id or tied ranks.

## Reading the code cheaply

- Skip `README.md`, `docs/` (except the files above), `node_modules/`, `bin/`, `obj/`, `TestResults/`.
- In Core, start from the manager interface in `Managers/Contracts/Services/`, then open only the engines and
  accessors it uses. Big classes are `<Type>.<Area>.cs` partials (under 400 lines each): open only the area needed.
- Web: one component per screen in `src/Invy.Web/src/tabs/`, `screens/` (setup, sign-in), `settings/`.
- `config/*.json` catalogs can be large. Search them; don't read them whole.
- For how ChefBot did something, search `../chef-bot` for the class name; don't browse it.

## Style

File-scoped namespaces, nullable reference types, `var` where the type is obvious. One top-level type per file,
named after it, namespace matching the folder. The build and `.claude/hooks/dotnet-format.sh` report mistakes.
To tune this file, use the `tune-agents-md` skill.
