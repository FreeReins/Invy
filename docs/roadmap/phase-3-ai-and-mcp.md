# Phase 3: AI and MCP

**Goal:** a photo of an open box becomes items to confirm, and an assistant can find and move things.

**Design sections:** [AI tasks](../developers/design.md#ai-tasks), [MCP tools](../developers/design.md#mcp-tools).

**Out of scope:** new AI providers. Use ChefBot's Anthropic and OpenAI-compatible providers as they are.

## 3a AI tasks

- [x] `ItemsFromPhoto` and `ItemDetailsFromPhoto`, following `add-ai-task`: the prompt and JSON schema include the space's types and fields. Suggestions are shown for confirming and never saved on their own.
- [x] `SuggestTypeFields` in the **Types** editor, and the `SuggestType` background sweep for the Inbox on the `ai` queue.
- [x] Web: photo capture in **Scan** and on the item page, with a review list (tick, edit, keep).

## 3b MCP

- [x] Every tool in the design's MCP table, one `add-mcp-tool` pass per group. Tools honour space roles. (`list_views` and `run_view` moved to phase 5.)
- [x] Settings → **AI assistants** → **Connect your assistant** shows the `claude mcp add` command for Invy.
- [x] Docs: `docs/admin/ai-assistants.md` (models, tasks, tools).

**Done when:** the build and the tests pass (`FakeAi` answers for each task), including MCP tool tests that find
"baseball" and move an item through the tools, as an assistant would.

## Session log

- 2026-11-24 (3a): four AI tasks, each with a prompt and id-limited JSON schema in `Engines/Transformation/` (`TypeCatalog` describes the space's types, `FieldValues` turns text answers into each field kind's value or drops them). `POST .../items/from-photo` and `.../items/{id}/details-from-photo` (1 to 4 photos, suggestions only), `POST .../types/suggest-fields` (search names made from labels, reserved and taken ones dropped, each checked as the Types editor would). The Inbox sweep: `Items.SuggestedTypeId` and `TypeSuggestedAt` (migration `AddSuggestedTypes`), `SuggestInboxTypes` on the ai queue two minutes after things land in the Inbox and on **Suggest types**, 40 items a question, each asked once; accept and ignore endpoints; `InboxTypesSuggested` sends the `inbox-items` live topic. Web: `PhotoItems.svelte` (review list: tick, edit name, type and amount, add) on a container's page and in Scan's Add mode, `LabelReader.svelte` (**Read a label**) on the item page, suggested types with Accept and Ignore in the Inbox, **Suggest fields** in the type editor, all shown only when `aiReady(task)`. Per-task route tests back in `AiSettingsTests`; `ItemPhotoTests`, `TypeFieldSuggestionTests`, `InboxTypeTests`; a11y rows for each new state. Docs: admin/ai-assistants (task table), items, types-and-fields, labels-and-scanning, api; decision in design.md. Not tried against PostgreSQL or a real model in a dev run: the migration and `ItemAccess.Suggestions` are covered only by the schema and migration tests. Next: 3b, the MCP tools, one `add-mcp-tool` pass per group (Finding first).
- 2026-11-24 (3b): 13 MCP tools in `Api/Mcp/` (`ItemTools`: where_is, find_items, get_item, list_contents, item_history, add_items, update_item, move_items, adjust_quantity, mark_seen; `TypeTools`: list_types, define_type; `SpaceTools`: list_spaces). `McpCaller` reads the token's owner from the request and the `space` argument (name or id; the only space when left out, else a refusal naming them). New manager calls with endpoints: `WhereIsAsync` (`GET .../where-is`, containers as text, "the Inbox" for unsorted things), `ChangeItemAsync` (`PATCH .../items/{id}`, fields as text by search name, read like AI answers), `MoveItemsAsync` (`POST .../items/move`, outermost only, one commit); a type's field with no search name gets one from its label (`Utilities/FieldKeys`). The **Connect your assistant** command was already there from phase 0. The test project now references Invy.Api: `McpToolTests` (find the baseball, move the glove, roles, several spaces, define a type and fill it), `AssistantCallTests`. Also checked by hand against PostgreSQL 17 through `/mcp` with a token (all 13 tools, refusals as plain sentences) and 3a end to end with a stand-in OpenAI-compatible model (the queued Inbox sweep, accept, from-photo, suggest-fields), so the `AddSuggestedTypes` migration and `ItemAccess.Suggestions` SQL ran for real. Docs: admin/ai-assistants (tools table), api, design.md and a decision. Next: phase 4.
