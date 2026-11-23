# Phase 2: capture

**Goal:** most adding and moving happens by scanning and taking photos, not typing.

**Design sections:** [Capture with less typing](../developers/design.md#capture-with-less-typing),
[Data model](../developers/design.md#data-model) (labels, attachments).

**Out of scope:** reading photos with AI (phase 3). This phase stores photos only.

## 2a Labels and scanning

- [ ] `Label` entity and `LabelManager`: make a batch of unclaimed codes, claim on scan, attach a code to an existing item. Codes are 4 to 6 characters from an alphabet without look-alike characters.
- [ ] Printable label sheets as PDF or print CSS, in common sticker layouts (Avery-style 30 and 65 per sheet), with the QR, code and marking. Route `/l/<code>` opens or claims the label.
- [ ] Web **Scan** tab: `BarcodeDetector` with a bundled `zxing-wasm` fallback. Modes: open, move (scan the item, then where it's going), claim and add.
- [ ] Sticky place ("Adding to Box F") across capture screens.

## 2b Seen, audits and attachments

- [ ] Last seen: opening or scanning a container marks it and its direct contents (`Seen` events, `last_seen_at`).
- [ ] Audit walk: scan a container, tick what's there, mark the rest `Missing`, and list the differences.
- [ ] `Attachment` entity with files under `Data:Root/files/<space>/`, thumbnails on the `images` queue, and backup of the files with the database backup.
- [ ] Barcode lookup accessor (off until turned on in Settings, through `PublicWeb.Handler`), filling in name, brand and image.
- [ ] Docs: `docs/using/labels-and-scanning.md`, `docs/using/audits.md`, and the barcode lookup in `docs/admin/settings.md`.

**Done when:** the build, the tests and the web tests pass. On a phone, scanning an unclaimed label creates a box, and
scanning box then shelf moves it.

## Session log
