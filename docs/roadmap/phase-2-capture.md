# Phase 2: capture

**Goal:** most adding and moving happens by scanning and taking photos, not typing.

**Design sections:** [Capture with less typing](../developers/design.md#capture-with-less-typing),
[Data model](../developers/design.md#data-model) (labels, attachments).

**Out of scope:** reading photos with AI (phase 3). This phase stores photos only.

## 2a Labels and scanning

- [x] `Label` entity and `LabelManager`: make a batch of unclaimed codes, claim on scan, attach a code to an existing item. Codes are 4 to 6 characters from an alphabet without look-alike characters.
- [x] Printable label sheets as PDF or print CSS, in common sticker layouts (Avery-style 30 and 65 per sheet), with the QR, code and marking. Route `/l/<code>` opens or claims the label.
- [x] Web **Scan** tab: `BarcodeDetector` with a bundled `zxing-wasm` fallback. Modes: open, move (scan the item, then where it's going), claim and add.
- [x] Sticky place ("Adding to Box F") across capture screens.

## 2b Seen, audits and attachments

- [ ] Last seen: opening or scanning a container marks it and its direct contents (`Seen` events, `last_seen_at`).
- [ ] Audit walk: scan a container, tick what's there, mark the rest `Missing`, and list the differences.
- [ ] `Attachment` entity with files under `Data:Root/files/<space>/`, thumbnails on the `images` queue, and backup of the files with the database backup.
- [ ] Barcode lookup accessor (off until turned on in Settings, through `PublicWeb.Handler`), filling in name, brand and image.
- [ ] Docs: `docs/using/labels-and-scanning.md`, `docs/using/audits.md`, and the barcode lookup in `docs/admin/settings.md`.

**Done when:** the build, the tests and the web tests pass. On a phone, scanning an unclaimed label creates a box, and
scanning box then shelf moves it.

## Session log

- 2026-11-23 (2a): `Labels` table (code is the key, unique across the install, so `/l/<code>` finds its space) and `Items.LabelCode`, added to the generated `Search` column and to `in:`; `LabelManager` makes batches (4 to 6 characters from `LabelEngine.Alphabet`, longer as the install fills), finds a label from a code or a whole scanned address, claims one as a new "Box <code>" container in the sticky place, and attaches one to an existing item (freeing its old label). Web: **Scan** tab (Open, Move, Claim, Add; `BarcodeDetector`, else bundled `zxing-wasm` 3.1.6 with its `.wasm` served by Invy; a typed-code box for no camera or no HTTPS), `/l/<code>` (opens the item, or offers to claim on Scan), Settings **Labels and printing** (Avery 5160 and L7651 sheets in print CSS with named `@page`s, QR drawn from `uqr`), the sticky place bar (per space, kept in the browser) with **Add things here** on containers, **Stick on a label** on the item page. CSP gains `'wasm-unsafe-eval'` for the reader. `docs/using/labels-and-scanning.md` written early for the in-app help links. Checked against PostgreSQL 17 through the API (migration, batch, claim, double claim refused, attach, unclaimed list, search by code, a URL-encoded scanned address) and a printed QR decoded back by the bundled reader; not tried on a real phone camera. `printed_at` is stored but nothing sets it yet. The design's phone bar (Find, Browse, Scan, Views, More) isn't done: phones show every tab. Next: 2b, last seen on opening or scanning a container (`Seen` events, `LastSeenAt`).
