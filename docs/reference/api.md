# HTTP API

This page is for developers and for anyone connecting another program to Invy. It lists every endpoint. Invy also
describes them as OpenAPI: open `/docs` on your Invy's address for the interactive reference with every request and
answer shape.

## How to call it

- **Signing in.** The web page uses a sign-in cookie from `POST /api/account/sign-in`. Programs use an API token
  instead: make one under **Settings → AI assistants**, or with `POST /api/tokens`, and send it as
  `Authorization: Bearer <token>`.
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
| POST | `/api/account/sign-in` | anyone | Signs in. Too many wrong passwords in a row lock the account for a while |
| POST | `/api/account/sign-out` | anyone | Signs out |
| GET | `/api/account/link?token=` | anyone | What an invite or password reset link is for |
| POST | `/api/account/accept-invite` | anyone | Creates an account from an invite link and signs it in |
| POST | `/api/account/reset-password` | anyone | Sets a new password from a reset link, unlocks the account and signs it in |
| POST | `/api/account/password` | | Changes your own password; your other devices are signed out |
| GET | `/api/tokens` | | Your API tokens, newest first |
| POST | `/api/tokens` | | Creates an API token that signs in as you; the token is shown only once |
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
