---
name: add-ai-task
description: "Add a job that Invy asks an AI model to do (read a photo, suggest fields or types), with its prompt, JSON answer schema, model route, fake answers for tests, and docs. USE FOR: new AI feature, new AiTask, change an AI prompt or its answer shape. DO NOT USE FOR: assistants calling Invy (add-mcp-tool), adding AI providers."
---

# Add an AI task

Invy talks to models only through `IAiAccess.AskAsync(AiModel, AiQuestion, ct)`, which returns JSON that matches a
schema. Answers are suggestions a person confirms. Never save them on their own.

1. **Task**: add `<Task>` to `AiTask` (`Managers/Contracts/Messages/AiTask.cs`) with a summary, and its label and
   whether it needs a vision model in `Utilities/AiTasks.cs`. It then shows in Settings → **AI assistants** with its
   own model choice.
2. **Prompt and schema**: `Engines/Transformation/<Task>Prompt.cs` builds the `AiQuestion`: short instructions, the
   space's types and fields from `FieldSchema`, and the JSON schema of the answer. Ask for ids from the lists
   given, never free-text type names.
3. **Reading the answer**: in `TransformationEngine.Ai.cs`, turn the JSON into a contract record, dropping values
   that don't fit their field kind. A bad answer becomes an empty suggestion, not an error.
4. **Manager**: the area's manager picks the route, calls `IAiAccess`, then the engine. Long or background work goes
   on the `ai` queue through a message and handler. When no model is set, throw `AiUnavailableException` with a
   message saying where to set one.
5. **Tests**: add the answer JSON to `FakeAi` (keyed by schema name), then test the engine with good, partial and
   nonsense answers, and the manager with `TestCore`. The first task brings back ChefBot's per-task route
   tests in `AiSettingsTests`.
6. **Docs**: the task in `docs/admin/ai-assistants.md` (what it does, if it needs a vision model) and the AI tasks
   table in `docs/developers/design.md`.

## Example

`ItemsFromPhoto`: `ItemsFromPhotoPrompt.cs`, `TransformationEngine.Ai.cs` (`ReadItemSuggestions`),
`IItemManager.SuggestFromPhotoAsync`, `ItemPhotoTests.cs`. Until phase 3a adds it, follow ChefBot's
`RecipeFromPhoto` in `../chef-bot` (`RecipePhotoPrompt.cs`, `RecipeManager.Photos.cs`).
