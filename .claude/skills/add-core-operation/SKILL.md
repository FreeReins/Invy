---
name: add-core-operation
description: "Add a Invy use case end to end: manager method, request/response records, JSON context, endpoint, API docs, TestCore tests. USE FOR: new endpoint, new API call, new manager method, new query or report. DO NOT USE FOR: new tables (add-stored-entity), handlers between managers, web UI (add-web-screen)."
---

# Add a core operation

1. **Contracts** in `src/Invy.Core/Managers/Contracts/Messages/`: one `public sealed record` per file,
   positional, with an XML `<summary>` and `<param>` for each field. Public enums get
   `[JsonConverter(typeof(JsonStringEnumConverter<T>))]`.
2. **JSON**: add the top-level request and response to `Managers/Contracts/ContractJsonContext.cs` under a
   `// <area>` comment. Nested records are picked up automatically.
3. **Interface**: add the method to the area's `Managers/Contracts/Services/I*Manager.cs` with a summary. Choose
   the manager whose area fits; never call another manager.
4. **Work**: put pure logic in an engine (`Engines/`, registered as a singleton in `CoreRegistration.cs`,
   reading nothing). The manager loads through accessors, calls the engine, saves, and calls
   `messages.CommitAsync` once. Use `clock.Today()` for dates.
5. **Constructor change**: `TestCore` builds managers by dependency injection, so only a new accessor or engine
   type needs a line in `TestCore.Register`.
6. **Endpoint** in `src/Invy.Api/Endpoints/<Area>Endpoints.cs`: a static method with
   `[WolverineGet("/api/...")]`, query values as nullable parameters, the manager as `[FromServices]`. Add
   `[Authorize(Policy = Sessions.AdminPolicy)]` only for admin-only operations.
7. **Docs**: add a row to `docs/reference/api.md`, and update the user page in `docs/` the change affects (`docs/AGENTS.md` maps features to pages). Release builds need regenerated Wolverine code
   (CONTRIBUTING.md, "Code made at build time"); Debug needs nothing.
8. **Tests**: engines directly, managers through `new TestCore().<Area>Manager()`. Assertions are awaited.
   Compare ordered lists as `string.Join(", ", ...)`; TUnit's `IsEquivalentTo` won't take `int?`
   collections.

## Example

`GET /api/spaces/{spaceId}/items/{id}/history`: `ItemHistoryResponse`, `TimelineEngine`,
`IItemManager.GetHistoryAsync` in `ItemManager.History.cs`, `Endpoints/ItemEndpoints.cs`, `TimelineTests.cs`.

Every space-owned operation takes `spaceId` from the route and checks the caller's role in `ValidationEngine`
before anything else.

## Troubleshooting

- `JsonShapeTests` fails: a record from step 1 is missing in step 2.
- `ArchitectureTests` fails: something calls sideways. Move the logic down a layer.
- TestCore calls managers directly, so an endpoint can pass every test and still fail at runtime (Wolverine builds
  its code on first call). Check new endpoints once against a real database: start a throwaway
  `postgres:17-alpine` container on a spare port and run Invy.Api with `Database__Mode=External`, `Https__Mode=Off`
  and `ConnectionStrings__Invy` pointing at it; it listens on 8080 (`Https__HttpPort` changes that, not
  `ASPNETCORE_URLS`, when a dev run holds 8080).
- An endpoint that sends a file returns `Task<IResult>` (`Results.File`). Its manager returns a record in
  `Managers/Contracts/` (not `Messages/`, which `JsonShapeTests` checks), such as `AttachmentContent`.
- A download read from the database as it is sent (a CSV) is written inside the endpoint to `HttpContext.Response`
  (`ReportEndpoints.WriteCsvAsync`): database reads in a `Results.Stream` callback run after the request's scope and
  silently return nothing.
