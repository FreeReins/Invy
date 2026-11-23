# AI assistants

AI works with Invy in two ways, both under **Settings → AI assistants**:

- **Invy's AI models** (admins): Invy itself will ask a model you choose to do jobs such as reading what's in a
  photo of something you own.
- **Connect your assistant** (everyone): Invy is an [MCP](https://modelcontextprotocol.io) server, so AI
  assistants such as Claude can connect to it with a token you make.

> [!NOTE]
> This version has no AI tasks and no MCP tools yet. Models and tokens set up now keep working when they arrive.

## AI models Invy uses

This part is for admins. Nothing is sent to a model until someone asks.

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
tasks back to the default. Until Invy has AI tasks, the list is empty.

### Keys and backups

API keys are kept in Invy's database and in [backups](backups-and-restore.md). Keep backups somewhere private.

## Connect an assistant to Invy

### What an assistant can do

Nothing yet: Invy's MCP server answers, but offers no tools in this version. Finding, adding and moving things come
with the inventory itself. Admin settings (people, backups and so on) won't be offered.

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
