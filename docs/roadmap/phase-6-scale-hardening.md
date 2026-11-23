# Phase 6: scale hardening

**Goal:** prove the design's scale targets and fix what doesn't meet them.

**Design sections:** [Scale targets](../developers/design.md#scale-targets), [Search](../developers/design.md#search),
[Timeline](../developers/design.md#timeline).

## Work

- [ ] A seed tool (`tools/Invy.Seed`, dev only) that makes 5 million items to a realistic depth (up to 8), with field values, and 50 million events, using `COPY`.
- [ ] A benchmark suite against the seeded database: search p95, item timeline, moving a container that holds 100,000 items, and the rollups catching up.
- [ ] `EXPLAIN (ANALYZE, BUFFERS)` review of the slowest queries, then index or query fixes.
- [ ] Streamed and batched backup and restore: memory stays flat as rows grow.
- [ ] Monthly partitioning of `item_events`, if the benchmark shows it's needed.
- [ ] Docs: `docs/reference/limits.md` with the measured numbers.

**Done when:** the benchmark meets the design's targets on a Raspberry Pi 5-class machine, and the numbers are
recorded here.

## Session log
