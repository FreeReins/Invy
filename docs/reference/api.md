# HTTP API

This page is for developers and for anyone connecting another program to Invy. It lists every endpoint. Invy also
describes them as OpenAPI: open `/docs` on your Invy's address for the interactive reference with every request and
answer shape.

## How to call it

- **Signing in.** The API uses the sign-in cookie from `POST /api/account/sign-in`, so a script signs in with a
  password and keeps the cookie. API tokens (**Settings → AI assistants**, or `POST /api/tokens`) only work on the MCP
  server at `/mcp`, sent as `Authorization: Bearer <token>`.
- **Changes from browsers.** A `POST`, `PUT`, `PATCH` or `DELETE` that a browser says came from another web address
  (`Sec-Fetch-Site` other than `same-origin`, or an `Origin` for another host) gets 403. Programs outside a browser
  send neither header and aren't affected.
- **Limits.** One network address can call sign-in, setup, accept-invite, reset-password and the link lookup 10
  times a minute; more get 429 with `Retry-After`.
- **JSON.** Requests and answers are JSON, with names in camelCase and enums as strings (`"Owner"`, `"Money"`).
- **Errors.** A refused request answers with a status and a plain sentence in `detail`: 400 (a rule is broken),
  401 (not signed in), 403 (your role isn't enough), 404 (not there, or a space you aren't in), 409 (someone changed it
  since you read it, or it clashes with what is there).
- **Spaces.** Everything under `/api/spaces/{spaceId}/` checks your role in that space. Viewers can read; editors and
  owners can change. Being an admin of the install gives no access to a space.
- **Paging.** Long lists come a page at a time. Pass the answer's `next` back as `after` (or `before` for activity) to
  get the next page. When `next` is null there are no more.

**Who** below: *anyone* needs no sign-in, *admin* needs an admin account, and the rest need any signed-in account
(and, for space routes, the role named).

## Signing in and accounts

| Method | Path | Who | What it does |
|---|---|---|---|
| GET | `/api/session` | anyone | Whether to show the setup wizard, the sign-in form or the app |
| POST | `/api/setup/account` | anyone | Setup, step one: creates the first account as an admin and signs it in |
| POST | `/api/account/sign-in` | anyone | Signs in. Too many wrong passwords in a row lock the account for a while, except on devices that signed in to it before |
| POST | `/api/account/sign-out` | anyone | Signs out |
| GET | `/api/account/link?token=` | anyone | What an invite or password reset link is for |
| POST | `/api/account/accept-invite` | anyone | Creates an account from an invite link and signs it in |
| POST | `/api/account/reset-password` | anyone | Sets a new password from a reset link, unlocks the account, revokes its API tokens and signs it in |
| POST | `/api/account/password` | | Changes your own password; your other devices are signed out. `revokeApiTokens: true` revokes your API tokens too |
| POST | `/api/account/sign-out-everywhere` | | Signs you out on every other device; this one gets a new session. API tokens keep working |
| GET | `/api/tokens` | | Your API tokens, newest first, with `expiresAt`, `lastUsedAt` and `readOnly` |
| POST | `/api/tokens` | | Creates an API token that signs in as you; the token is shown only once. `expiresInDays` (1–3650, or null for until revoked) and `readOnly` (only the MCP tools that change nothing) |
| DELETE | `/api/tokens/{id}` | | Stops one of your API tokens working |
| GET | `/api/inbox` | | Your messages behind the bell, newest first, and how many are unread |
| POST | `/api/inbox` | | Adds a note to your own inbox |
| POST | `/api/inbox/read` | | Marks your whole inbox read |
| DELETE | `/api/inbox` | | Empties your inbox |
| GET | `/api/live` | | Server-sent events naming what changed (`backups`, `inbox`, `inbox-items`, `items`), with a `ping` every 25 seconds |

## People (admin)

| Method | Path | Who | What it does |
|---|---|---|---|
| GET | `/api/users` | admin | Every account, oldest first |
| DELETE | `/api/users/{id}` | admin | Removes an account |
| POST | `/api/users/{id}/unlock` | admin | Ends a lockout early |
| POST | `/api/users/{id}/password-reset` | admin | A single-use reset link: `https://your-invy/?reset={token}` |
| POST | `/api/invites` | admin | A single-use invite: `https://your-invy/?invite={token}` |
| GET | `/api/links` | admin | Invite and reset links not used yet |
| POST | `/api/links/{id}/regenerate` | admin | Replaces an unused link with a new one |
| DELETE | `/api/links/{id}` | admin | Revokes an unused link |

## Spaces and types

| Method | Path | Who | What it does |
|---|---|---|---|
| GET | `/api/spaces` | | The spaces you are in, with your role in each |
| GET | `/api/starter-packs` | | The kinds of space: each starter pack's types, icon, features, suggested places, examples, getting-started steps and hints |
| GET | `/api/space-features` | | The features a space can switch on and off; `available` is false for ones not built yet |
| POST | `/api/spaces` | | Makes a space with you as its owner, from a starter pack (`preset`) or none, with `features` (the kind's when left out), `places` to make at the top and, with `examples: true`, the kind's example items |
| PUT | `/api/spaces/{spaceId}` | owner | Renames a space |
| DELETE | `/api/spaces/{spaceId}` | owner | Deletes a space and everything in it |
| PUT | `/api/spaces/{spaceId}/features` | owner | Sets which features are on: the `features` sent are on, the rest off |
| PUT | `/api/spaces/{spaceId}/kind` | owner | Changes the space's `kind`, which changes its hints and getting-started steps, never its types |
| DELETE | `/api/spaces/{spaceId}/examples` | editor | Removes the example items and their timelines; one still holding your own things is kept |
| GET | `/api/spaces/{spaceId}/getting-started` | viewer | Your getting-started card: the kind's steps, each `done` from what the space has, and `examplesLeft` |
| POST | `/api/spaces/{spaceId}/getting-started/dismiss` | viewer | Puts your getting-started card away; nobody else's changes |
| POST | `/api/spaces/{spaceId}/getting-started/show` | viewer | Shows your getting-started card again |
| GET | `/api/spaces/{spaceId}/members` | viewer | The people in a space, owners first |
| POST | `/api/spaces/{spaceId}/members` | owner | Adds someone with an account here, by username |
| PUT | `/api/spaces/{spaceId}/members/{memberId}` | owner | Changes a member's role; a space keeps at least one owner |
| DELETE | `/api/spaces/{spaceId}/members/{memberId}` | owner, or yourself | Removes someone, or leaves |
| GET | `/api/spaces/{spaceId}/types` | viewer | The space's types and their fields, by name |
| GET | `/api/spaces/{spaceId}/types/{typeId}` | viewer | One type |
| POST | `/api/spaces/{spaceId}/types` | editor | Adds a type |
| PUT | `/api/spaces/{spaceId}/types/{typeId}` | editor | Replaces a type's name, look and fields; send the `version` you read (409 if it changed) |
| DELETE | `/api/spaces/{spaceId}/types/{typeId}` | editor | Removes a type; its items keep their values |
| POST | `/api/spaces/{spaceId}/types/from-pack` | editor | Copies the type called `type` from the starter pack `pack`, with its fields; 409 if the space has a type of that name |
| POST | `/api/spaces/{spaceId}/types/suggest-fields` | editor | Asks the AI model for fields that suit a type called `name`, leaving out `existingKeys`. Nothing is saved |

## Items

| Method | Path | Who | What it does |
|---|---|---|---|
| GET | `/api/spaces/{spaceId}/items?parent=&after=` | viewer | What is directly inside a container, or at the top without `parent`, 100 at a time |
| GET | `/api/spaces/{spaceId}/inbox?after=` | viewer | Items with no place and no type |
| GET | `/api/spaces/{spaceId}/items/{id}` | viewer | An item and the chain of containers it is in |
| POST | `/api/spaces/{spaceId}/items` | editor | Adds an item, in `parentId` or at the top |
| PUT | `/api/spaces/{spaceId}/items/{id}` | editor | Changes an item's details and fields; send the `version` you read (409 if it changed) |
| PATCH | `/api/spaces/{spaceId}/items/{id}` | editor | Changes only what is sent: `name`, `marking`, `typeId`, `notes`, `tags`, and `fields` as text by search name (`{"price": "129.99"}`; `""` clears). No `version` needed |
| POST | `/api/spaces/{spaceId}/items/{id}/move` | editor | Moves an item and everything inside it to `parentId` (null for the top) |
| POST | `/api/spaces/{spaceId}/items/move` | editor | Moves the items in `itemIds` (1 to 100), each with what's inside it, to `parentId`, in one go |
| POST | `/api/spaces/{spaceId}/items/{id}/quantity` | editor | Adds `delta` to a counted item's amount (negative takes away) |
| POST | `/api/spaces/{spaceId}/items/{id}/dispose` | editor | Marks an item disposed of; refused while anything is inside it |
| POST | `/api/spaces/{spaceId}/items/{id}/restore` | editor | Brings a disposed or archived item back |
| POST | `/api/spaces/{spaceId}/quick-add` | editor | Adds several items from one line of `text`, such as `baseball, glove, 2 bats` |
| GET | `/api/spaces/{spaceId}/items/{id}/history` | viewer | An item's timeline and where it has been |
| POST | `/api/spaces/{spaceId}/items/{id}/seen` | editor | Marks an item seen, and when it holds items, what is directly inside. One `Seen` event; again within 10 minutes changes nothing |
| POST | `/api/spaces/{spaceId}/items/{id}/audit` | editor | Finishes an audit of a container: `present` lists the ids ticked. Unticked contents become `Missing`; ticked missing ones are found. Answers with the `missing`, `found` and `elsewhere` lists. Up to 2,000 items |
| GET | `/api/spaces/{spaceId}/barcodes/{code}` | editor | Looks a UPC or EAN barcode up online (once an admin turned that on): `name`, `brand`, `imageUrl`, `source`. 404 when no database knows it |
| POST | `/api/spaces/{spaceId}/items/from-photo` | editor | Asks the AI model what is in `photos` (1 to 4 images, base64) of an open box: `items` with `name`, `typeId`, `quantity`, `unit` and `fields`. Nothing is added |
| POST | `/api/spaces/{spaceId}/items/{id}/details-from-photo` | editor | Asks the AI model to read the item's fields from `photos` of a label or nameplate: `fields` by field id. Nothing is saved |
| POST | `/api/spaces/{spaceId}/inbox/suggest-types` | editor | Asks for a type for every Inbox item without a suggestion, in the background. Answers 202; items' `suggestedTypeId` fills in as answers come |
| POST | `/api/spaces/{spaceId}/items/{id}/suggested-type/accept` | editor | Gives an Inbox item its suggested type, as an edit would |
| POST | `/api/spaces/{spaceId}/items/{id}/suggested-type/ignore` | editor | Drops an Inbox item's suggested type |
| GET | `/api/spaces/{spaceId}/where-is?q=` | viewer | The best 10 matches for `q` (search syntax works), each with `where`: its containers as text, such as `Garage › Box F` |
| GET | `/api/spaces/{spaceId}/activity?before=` | viewer | The space's timeline, newest first, 50 at a time |
| GET | `/api/spaces/{spaceId}/search?q=&sort=&after=` | viewer | Finds items, 50 at a time; `sort` is `relevance`, `name` or `recent`. The first page has a `total`. See [Search](../using/search.md) for what `q` can hold |

The AI calls answer 400 when no AI model is set up and 502 when the model can't be reached. Their answers are
suggestions only, and the model's name comes back as `model`.

An item's `contentCount` and `contentValue` (everything inside it, at any depth) are recounted in the background
shortly after a change, so they can lag a moment behind.

## Attachments

Photos (JPEG, PNG, WebP, GIF) and PDFs attached to items, up to 10 MB each. A photo's thumbnail is made in the
background just after it is attached; `hasThumbnail` says when it is ready.

| Method | Path | Who | What it does |
|---|---|---|---|
| GET | `/api/spaces/{spaceId}/items/{id}/attachments` | viewer | An item's attachments, oldest first |
| POST | `/api/spaces/{spaceId}/items/{id}/attachments` | editor | Attaches `data` (the file's bytes, base64) as `name`. The type comes from the bytes, not the name |
| POST | `/api/spaces/{spaceId}/items/{id}/attachments/from-web` | editor | Downloads `url` from the public internet and attaches it, named `name` or after the address |
| GET | `/api/spaces/{spaceId}/attachments/{id}/file` | viewer | The file. Photos are sent to show; PDFs to download |
| GET | `/api/spaces/{spaceId}/attachments/{id}/thumbnail` | viewer | A photo's JPEG thumbnail, at most 480 pixels across. 404 until it is made |
| DELETE | `/api/spaces/{spaceId}/attachments/{id}` | editor | Removes an attachment |

## Labels

A label code is unique across the whole install, so looking one up needs no space: the answer says which space it
belongs to. Codes are 4 to 6 characters, without look-alikes such as 0 and O.

| Method | Path | Who | What it does |
|---|---|---|---|
| GET | `/api/labels/{code}` | viewer of its space | The label and the item it is on (`item`, null while unclaimed). `code` may be the whole scanned address, URL-encoded |
| GET | `/api/spaces/{spaceId}/labels?unclaimed=&after=` | viewer | The space's labels by code, 100 at a time; only unclaimed ones with `unclaimed=true` |
| POST | `/api/spaces/{spaceId}/labels` | editor | Makes `count` (1 to 500) new unclaimed codes to print |
| POST | `/api/spaces/{spaceId}/labels/{code}/claim` | editor | Makes a new item with the label on it, in `parentId`: named `name`, or Box and the code; of `typeId`, or none (then it holds items) |
| POST | `/api/spaces/{spaceId}/labels/{code}/attach` | editor | Sticks an unclaimed label on the existing item `itemId`. A label it had before becomes unclaimed |

## Stock, lending, reminders and moving

These work whatever the space's feature switches say; a switch only hides the feature in the page. An item's
`lowStockAt`, `reserved`, `isLow`, `lentTo`, `lentAt`, `dueOn` and `destinationId` come with it everywhere, and
`lowStockAt` is part of the item sent to `PUT /items/{id}` (left out clears it).

| Method | Path | Who | What it does |
|---|---|---|---|
| GET | `/api/spaces/{spaceId}/low-stock` | viewer | Counted items at or below their low-stock level, after what projects set aside, by name: `items` and `more` |
| PUT | `/api/spaces/{spaceId}/items/{id}/low-stock` | editor | Sets a counted item's level `at` (null clears it); `version` optional |
| GET | `/api/spaces/{spaceId}/loans?overdue=` | viewer | What's lent out, soonest due first; `overdue=true` for only what's past its date |
| POST | `/api/spaces/{spaceId}/items/{id}/lend` | editor | Lends the item, whole, `to` someone or somewhere, with an optional `dueOn` (yyyy-MM-dd). One `Lent` event |
| POST | `/api/spaces/{spaceId}/items/{id}/return` | editor | Marks a lent item back. One `Returned` event; nothing when it isn't lent |
| GET | `/api/spaces/{spaceId}/reminders?days=` | viewer | Reminder dates within `days` (30 by default, up to 365) and overdue upkeep: each with `item`, `fieldId`, `field`, `on`, `upkeep` and `days` |
| POST | `/api/spaces/{spaceId}/items/{id}/upkeep-done` | editor | Moves the date in `fieldId`, a field that repeats, to today plus its repeat |
| GET | `/api/spaces/{spaceId}/moving` | viewer | Items with a moving destination, by name |
| PUT | `/api/spaces/{spaceId}/items/{id}/destination` | editor | Sets where a box goes, `destinationId` (a container in the space), or clears it with null |
| POST | `/api/spaces/{spaceId}/items/{id}/arrived` | editor | Moves the box into its destination with one `Moved` event, and clears the destination |
| GET | `/api/spaces/{spaceId}/items/{id}/manifest` | viewer | A box's manifest: `box`, `destination`, `destinationPath`, and what's directly inside |
| GET | `/api/spaces/{spaceId}/calendar` | viewer | The calendar feed's `url`, or null while it's off |
| POST | `/api/spaces/{spaceId}/calendar` | owner | Turns the feed on with a new secret address; an older one stops working |
| DELETE | `/api/spaces/{spaceId}/calendar` | owner | Turns the feed off |
| GET | `/api/spaces/{spaceId}/calendar/{secret}.ics` | anyone with the address | The feed as iCalendar: reminder dates and loans' due dates as all-day events. 404 when it's off or the secret is wrong |

## Projects

| Method | Path | Who | What it does |
|---|---|---|---|
| GET | `/api/spaces/{spaceId}/projects?done=` | viewer | Open projects with what each sets aside; `done=true` adds finished ones |
| POST | `/api/spaces/{spaceId}/projects` | editor | Starts a project: `name`, `notes` |
| GET | `/api/spaces/{spaceId}/projects/{projectId}` | viewer | One project |
| PUT | `/api/spaces/{spaceId}/projects/{projectId}` | editor | Changes its `name` and `notes`; send the `version` you read |
| POST | `/api/spaces/{spaceId}/projects/{projectId}/reserve` | editor | Sets how much of `itemId` it sets aside, `amount` (1 for a single thing, 0 for none). Refused beyond what other projects leave free |
| POST | `/api/spaces/{spaceId}/projects/{projectId}/use-up` | editor | Uses `amount` (null for all) of what it set aside of `itemId`: off a counted item's amount, or a single item disposed of |
| POST | `/api/spaces/{spaceId}/projects/{projectId}/finish` | editor | Marks it done and releases what it still sets aside |
| DELETE | `/api/spaces/{spaceId}/projects/{projectId}` | editor | Removes it, releasing what it still sets aside |
| GET | `/api/spaces/{spaceId}/items/{id}/projects` | viewer | The open projects that set aside some of an item |

## Imports

| Method | Path | Who | What it does |
|---|---|---|---|
| GET | `/api/spaces/{spaceId}/imports` | viewer | The space's last 50 imports, newest first, with `status`, `total`, `done`, `added` and `problems` |
| POST | `/api/spaces/{spaceId}/imports` | editor | Uploads a CSV file: `name` and `data` (base64, up to 20 MB). Answers with its `kind` (`Csv` or `Homebox`), `headers`, first rows as `sample`, and a suggested `mapping`. Nothing is added yet |
| GET | `/api/spaces/{spaceId}/imports/{importId}` | viewer | One import |
| POST | `/api/spaces/{spaceId}/imports/{importId}/start` | editor | Starts it with `mapping` (each `column` and its `target`: `name`, `marking`, `quantity`, `unit`, `tags`, `notes`, `note`, `location`, `type`, `field:<id>` or `ignore`), `parentId` and `typeId`. It runs in the background |
| DELETE | `/api/spaces/{spaceId}/imports/{importId}` | editor | Removes an import that isn't running, and its file. What it added stays |

## Views

A view is a saved search (`query`, written as in the search box) plus how to show it: `display` is `Table`, `Cards`,
`Tree`, `Board` or `Timeline`. The search is read again each time the view runs, so `seen>1y` always counts back from
today. A view is the caller's own unless `shared` is true; `pinned` views show on **Find**. Any member can make their
own views; only editors and owners share views or change shared ones.

| Method | Path | Who | What it does |
|---|---|---|---|
| GET | `/api/spaces/{spaceId}/views` | viewer | The space's shared views and the caller's own, pinned first, then by name. `mine` and `canChange` say what the caller can do |
| POST | `/api/spaces/{spaceId}/views` | viewer (editor to share) | Saves a view: `name`, `query`, `display`, `columns` (for a table: name, type, place, quantity, value, seen, added, label or a field's search name; empty for name, type, place), `sort` (relevance, name or recent), `groupBy` (for a board: a select field's search name), `shared`, `pinned` |
| PUT | `/api/spaces/{spaceId}/views/{viewId}` | its maker, or editor for a shared one | Changes a view; same body as POST |
| DELETE | `/api/spaces/{spaceId}/views/{viewId}` | its maker, or editor for a shared one | Removes a view. Its items are untouched |
| GET | `/api/spaces/{spaceId}/views/{viewId}/results?after=` | viewer | Runs the view's search as of now, 50 at a time, shaped like search results. Each result also has `details`: its `fields` (by field id), `tags`, `lastSeenAt`, `createdAt` and `photoId` (an attachment with a thumbnail, or null) |
| GET | `/api/spaces/{spaceId}/views/{viewId}/timeline?before=` | viewer | The events of the items the view finds as of now, newest first, 50 at a time, shaped like the activity feed |
| GET | `/api/spaces/{spaceId}/views/{viewId}/export.csv` | viewer | Everything the view finds as of now, as CSV (the same columns as the reports below) |

## Reports

Reports read the whole space. The page shows each behind its feature (`values`, `audits`), but the API answers
whatever the switches say. A CSV has one row per item: Room (its container at the top), Place, Name, Marking, Type,
Amount, Unit, Value, Status, Label, Last seen, Added, Tags, Photo (the address of its first photo), then one column
per field search name. It is UTF-8 with a byte order mark, and a cell that starts like a formula is written as text.

| Method | Path | Who | What it does |
|---|---|---|---|
| GET | `/api/spaces/{spaceId}/reports/values` | viewer | `total`, `items`, `unvalued`, and totals `byPlace` (each container at the top, from the rollups; `place: null` is things at the top outside one) and `byType`. Currencies aren't converted |
| GET | `/api/spaces/{spaceId}/reports/insurance` | viewer | The insurance inventory: things (and containers with a value) by room, each with `details`, up to 5,000; `truncated` says when there are more |
| GET | `/api/spaces/{spaceId}/reports/insurance.csv` | viewer | The insurance inventory as CSV, every thing in the space |
| GET | `/api/spaces/{spaceId}/reports/missing?after=` | viewer | Items marked missing, by name, 50 at a time, shaped like search results with `details` |
| GET | `/api/spaces/{spaceId}/reports/missing.csv` | viewer | The missing items as CSV |

## Running the server (admin)

| Method | Path | Who | What it does |
|---|---|---|---|
| GET | `/api/system/security` | admin | The sign-in rules, and whether barcode lookup is on |
| PUT | `/api/system/security` | admin | Changes the lockout rules, "always use HTTPS" and `barcodeLookup` (left out, it is turned off) |
| GET | `/api/system/https` | | How HTTPS is set up |
| POST | `/api/system/https/certificate` | admin | Makes a new self-signed certificate for the given names |
| GET | `/api/system/https/certificate` | | Downloads the public certificate, to install on devices |
| GET | `/api/system/logs?after=` | admin | Log entries since startup, oldest first; `after` gives only newer ones |
| GET | `/api/system/notifications` | admin | The webhook and reminder settings |
| PUT | `/api/system/notifications` | admin | Saves them |
| POST | `/api/system/notifications/test` | admin | Sends a sample notification |
| POST | `/api/system/notifications/send-today` | admin | Sends today's reminders now |
| GET | `/api/system/backups` | admin | The backup settings and files |
| PUT | `/api/system/backups` | admin | Saves when backups run and how many are kept |
| POST | `/api/system/backups` | admin | Makes a backup now |
| GET | `/api/system/backups/{name}` | admin | Downloads a backup file |
| GET | `/api/system/backups/{name}/summary` | admin | Counts what a backup holds |
| POST | `/api/system/backups/{name}/restore` | admin | Replaces all data with a backup |
| DELETE | `/api/system/backups/{name}` | admin | Deletes a backup file |
| GET | `/api/ai/settings` | admin | The AI models (without keys), the default, and which does each task |
| PUT | `/api/ai/settings` | admin | Saves them |
| POST | `/api/ai/models/{id}/test` | admin | Asks a saved model a short question |
| GET | `/api/ai/features` | | Which AI features are ready to use |
