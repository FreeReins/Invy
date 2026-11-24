# AI assistants

AI works with Invy in two ways, both under **Settings → AI assistants**:

- **Invy's AI models** (admins): Invy itself will ask a model you choose to do jobs such as reading what's in a
  photo of something you own.
- **Connect your assistant** (everyone): Invy is an [MCP](https://modelcontextprotocol.io) server, so AI
  assistants such as Claude can connect to it with a token you make.

## AI models Invy uses

This part is for admins. Nothing is sent to a model until someone asks, except the Inbox sweep below, which runs
once a model is set up.

### What Invy asks models to do

Everything a model answers is a suggestion that someone confirms. Invy never saves it on its own.

| Task | What it does | Where people use it | Needs a model that can see images |
|---|---|---|---|
| **Listing things in photos** | Lists what is in a photo of an open box, with a type from the space and field values | **Add from a photo** on a container's page, and in **Scan**'s **Add** mode | Yes |
| **Reading labels and nameplates** | Reads an item's fields, such as brand, model and serial, from a photo of its label | **Read a label** on an item's page | Yes |
| **Suggesting fields for types** | Suggests fields from a type's name, such as set number and pieces for Lego sets | **Suggest fields** in the **Types** editor | No |
| **Suggesting types for the Inbox** | Suggests a type for things in the Inbox, a couple of minutes after they arrive, for someone to **Accept** or **Ignore** | The **Inbox** on **Browse** | No |

Only editors and owners of a space can use them. The model sees the photos, the names, fields and choices of the
space's types, and for the Inbox, the names, markings, tags and notes of the things it sorts.


**At a glance** lists each task and the model it uses, marked **Default** when it falls back to the default model,
or **Not set up** when there is no model yet. Under **Models**, each model is one row showing its service, model
name, whether it's the default, whether its API key is saved, and the tasks it does. Select a row to open it and
change its fields, **Test** it, or **Remove** it. A row marked **Not saved** has changes waiting for **Save models**.

### Set up a model

1. Open **Settings → AI assistants**.
2. Under **Start from**, pick one, and press **Add a model**. Each choice only fills in the fields:

   | Start from | Talks to | Fill in |
   |---|---|---|
   | **Claude (Anthropic)** | Anthropic's API | An **API key** from the Claude Console, under API keys. The model starts as `claude-opus-5-5` |
   | **OpenAI** | `https://api.openai.com/v1` | An **API key** and the **Model** |
   | **Ollama on your network** | `http://ollama.local:11434/v1` | The **Address** of your Ollama server, ending in `/v1`, and the **Model**. No key |
   | **Another OpenAI-compatible service** | Any address | The **Address**, ending in `/v1`, the **Model**, and a key if the service needs one |

   LM Studio, OpenRouter, vLLM and other services that answer OpenAI's chat completions API work as
   **OpenAI-compatible**. An address on your own network is fine here.
3. Choose the **Default model** in the new model's row. Every task uses it unless you choose another.
4. Press **Save models**, then open the model's row and press **Test**. It asks a short question and says whether the model answered.

API keys are never shown again after saving. Leave the key field empty to keep the saved one.

> [!NOTE]
> Reading photos needs a model that can see images. Claude and OpenAI's current models can. With Ollama, pull a
> vision model such as `qwen2.5vl` or `llama3.2-vision`.

### Use a different model for a task

With two or more models, **Which model does each task** lists each task. Pick a model for one, or leave it on **The
default model**. For example, read photos with a local model and use Claude for the rest. Removing a model sends its
tasks back to the default.

### Keys and backups

API keys are kept in Invy's database and in [backups](backups-and-restore.md). Keep backups somewhere private.

## Connect an assistant to Invy

### What an assistant can do

Once connected, you can ask things like *"Where is my baseball?"*, *"Put the glove in box F"* or *"I used 3 AA
batteries"*. The assistant uses these tools:

| Tool | What it does |
|---|---|
| `where_is` | Finds things by name and says which containers each is in, such as *Garage › Box F* |
| `find_items` | Searches with the same words and filters as **Find** (see [Search](../using/search.md)) |
| `get_item` | Everything about one item, and where it is |
| `list_contents` | What is directly inside a container, or the places at the top |
| `item_history` | An item's timeline and the places it has been |
| `add_items` | Adds things, several at once, to a container or the Inbox: `baseball, glove, 2 bats` |
| `update_item` | Changes an item's name, marking, type, notes, tags or field values |
| `move_items` | Puts things into a container, or takes them out to the top |
| `adjust_quantity` | Adds to or takes from a counted item's amount |
| `mark_seen` | Records that something was just seen where Invy says it is |
| `list_types` | The space's types and their fields |
| `define_type` | Adds a type with its fields |
| `list_spaces` | The spaces you're in, and your role in each |

An assistant can do only what its token's owner can: a viewer of a space can find things there but not change
them. If you're in more than one space, the assistant asks which one, or you can name it (*"in Workshop, where is
the drill?"*). Admin settings (people, backups and so on) aren't offered.

### Connect one

Under **Connect your assistant** there are two steps: **1. Invy's address** shows the MCP address to copy, and
**2. Your tokens** lists your tokens and makes new ones.

1. Open **Settings → AI assistants**.
2. Under **2. Your tokens**, in **Name for a new token**, enter the assistant it's for, and press **Make a token**.
3. Copy the token. It's shown only once.
4. Give the assistant the **MCP address**, `https://<Invy's address>:8443/mcp`, and the token, sent as the header
   `Authorization: Bearer <token>`.

#### Claude Code

Invy shows the exact command after making a token. It looks like this:

```sh
claude mcp add --transport http invy https://invy.local:8443/mcp --header "Authorization: Bearer invy_..."
```

#### Other assistants

Any client that supports MCP over Streamable HTTP with a custom header takes the same address and header.

### Tokens

- A token acts as the person who made it. Each person makes and revokes their own.
- It keeps working after a password change, and stops when it's revoked or its account is removed.
- **Revoke** a token on the same page when you stop using an assistant.
- Tokens aren't kept in backups, so after a restore, make new ones.
- Tokens only work on `/mcp`, not on the rest of the API.

### The assistant has to reach Invy

- An assistant running on your computer (such as Claude Code) can reach Invy on your home network.
- An assistant running in the cloud can't, unless Invy is reachable from the internet. See
  [Behind a reverse proxy](reverse-proxy.md).
- With Invy's own certificate, the machine running the assistant has to trust it. See
  [Trust the certificate](https-and-certificates.md#trust-the-certificate-on-your-devices).

## If something goes wrong

For connected assistants, see [Notifications and integrations](../troubleshooting/notifications-and-integrations.md#ai-assistants).
