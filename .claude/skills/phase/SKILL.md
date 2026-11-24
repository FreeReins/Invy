---
name: phase
description: "Work on one phase (or session such as 0b, 1a) of Invy's roadmap in docs/roadmap/, carrying on where the last session stopped, then record progress. USE FOR: /phase <id>, 'start phase 2', 'continue the roadmap', 'what's next'. DO NOT USE FOR: changing the design without a decision (edit design.md only with the user), small fixes outside the roadmap."
---

# Work on a roadmap phase

The argument is a phase or session id (`0b`, `1`, `1c`). Without one, use the first row of
`docs/roadmap/README.md` that isn't **Done**.

## Start

1. Read `docs/roadmap/README.md` and the phase file it links to. Read no other phase files.
2. Read the phase file's **Session log**: carry on from the last "Next:", or from the first unticked box in this
   session's section.
3. Read only the `docs/developers/design.md` sections the phase file links to. Don't read the whole design.
4. Set the phase's status to **In progress** in the README.
5. Say which boxes you'll do: about one session section (`1a`) or five boxes.

## Work

- Each box names the skill to follow (`add-stored-entity`, `add-core-operation`, `add-web-screen`, `add-field-kind`,
  `add-mcp-tool`, `add-ai-task`, `add-space-feature`, `update-docs`). Load the skill when you reach the box, not before.
- To copy something from ChefBot, search `../chef-bot` for the class or file name; don't browse its folders.
- Run the checks in `AGENTS.md` after each box, not only at the end.
- A phase's **Done when** holds only checks an agent runs: the build, TUnit tests, web tests with axe, and the strict
  docs build. Never write a step for a person to do by hand (a real phone, a dev run, a fresh install). Turn one you
  find into an automated test.
- If the design is wrong or missing something, stop and ask the user. Record their answer in the README's
  **Decisions** list and in `design.md`, in the same commit as the code.

## Finish

1. Tick the boxes that are done and pass the checks. A box that is half-done stays unticked; say what's left in
   the log.
2. Add one line to the phase's **Session log**: `- <yyyy-mm-dd> (<id>): <what was done>. <what's half-done>. Next: <first thing next session>.`
3. When every box is ticked and the phase's **Done when** passes, set the status to **Done** in the README.
4. If you learned something a later session would otherwise rediscover (a command, a pitfall), add it to the right
   skill or `AGENTS.md`, within its token budget in `.waza.yaml`.
5. Report what was done, what the checks printed, and what's next.
