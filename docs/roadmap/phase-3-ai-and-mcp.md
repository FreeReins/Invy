# Phase 3: AI and MCP

**Goal:** a photo of an open box becomes items to confirm, and an assistant can find and move things.

**Design sections:** [AI tasks](../developers/design.md#ai-tasks), [MCP tools](../developers/design.md#mcp-tools).

**Out of scope:** new AI providers. Use ChefBot's Anthropic and OpenAI-compatible providers as they are.

## 3a AI tasks

- [ ] `ItemsFromPhoto` and `ItemDetailsFromPhoto`, following `add-ai-task`: the prompt and JSON schema include the space's types and fields. Suggestions are shown for confirming and never saved on their own.
- [ ] `SuggestTypeFields` in the **Types** editor, and the `SuggestType` background sweep for the Inbox on the `ai` queue.
- [ ] Web: photo capture in **Scan** and on the item page, with a review list (tick, edit, keep).

## 3b MCP

- [ ] Every tool in the design's MCP table, one `add-mcp-tool` pass per group. Tools honour space roles.
- [ ] Settings → **AI assistants** → **Connect your assistant** shows the `claude mcp add` command for Invy.
- [ ] Docs: `docs/admin/ai-assistants.md` (models, tasks, tools).

**Done when:** the build and the tests pass (`FakeAi` answers for each task). In a dev run, `claude mcp add` against
`/mcp` answers "where is my baseball?" and moves an item.

## Session log
