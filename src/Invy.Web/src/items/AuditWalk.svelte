<!-- An audit walk of one container: tick what is there, by hand or by scanning labels, then finish. Invy marks what
     wasn't ticked missing and lists the differences: what went missing, what turned up again, and what was found here
     though Invy had it somewhere else, with a button to move each of those here. -->
<script>
  import { onMount } from 'svelte';
  import { run, toast, labelCode, findLabel } from '../lib/app.svelte.js';
  import { api } from '../lib/api.js';
  import { pendingClick } from '../lib/pending.js';
  import { label as named, amount } from '../lib/items.js';
  import { plural } from '../lib/format.js';
  import Modal from '../components/Modal.svelte';
  import Scanner from '../components/Scanner.svelte';
  import HelpLink from '../components/HelpLink.svelte';

  /** spaceId; container: ItemResponse; ondone(): after the audit, to reload; onclose(). */
  let { spaceId, container, ondone, onclose } = $props();

  /** ItemSummary[]: everything directly inside, every page. */
  let contents = $state(null);
  /** Ids ticked as there. */
  let ticked = $state(new Set());
  /** ItemRef[]: scanned here, but Invy has them somewhere else. */
  let extras = $state([]);
  /** AuditResponse once finished. */
  let result = $state(null);
  /** Ids moved here from the Elsewhere list. */
  let moved = $state(new Set());

  const base = $derived(`/api/spaces/${spaceId}`);

  onMount(() => run(async () => {
    let page = await api('GET', `${base}/items?parent=${container.id}`);
    const all = [...page.items];
    while (page.next) {
      page = await api('GET', `${base}/items?parent=${container.id}&after=${encodeURIComponent(page.next)}`);
      all.push(...page.items);
    }
    contents = all;
  }));

  function toggle(id, on) {
    const next = new Set(ticked);
    if (on) next.add(id); else next.delete(id);
    ticked = next;
  }

  const scanned = (text) => run(async () => {
    const code = labelCode(text);
    if (!code) throw new Error(`"${text}" isn't an Invy label. Tick things without a label in the list.`);
    const inside = contents?.find((item) => item.labelCode === code);
    if (inside) {
      toggle(inside.id, true);
      toast(`${inside.name} ticked.`);
      return;
    }
    const found = await findLabel(code);
    if (found.spaceId !== spaceId) throw new Error(`Label ${code} belongs to another space, so it can't be in ${container.name}.`);
    if (!found.item) throw new Error(`Label ${code} isn't on anything yet. Claim it from Scan first.`);
    if (found.item.id === container.id) throw new Error(`That's ${container.name} itself. Scan what's inside it.`);
    if (!extras.some((extra) => extra.id === found.item.id)) extras = [...extras, found.item];
    toast(`${found.item.name} ticked. Invy has it somewhere else.`);
  });

  const finish = () => run(async () => {
    result = await api('POST', `${base}/items/${container.id}/audit`, { present: [...ticked, ...extras.map((extra) => extra.id)] });
    ondone();
  });

  const moveHere = (item) => run(async () => {
    await api('POST', `${base}/items/${item.id}/move`, { parentId: container.id });
    moved = new Set([...moved, item.id]);
    toast(`${item.name} moved into ${container.name}.`);
    ondone();
  });

  const tickAll = () => { ticked = new Set(contents.map((item) => item.id)); };
</script>

<Modal title="Audit {container.name}" wide {onclose}>
  {#if result}
    <p role="status">
      {plural(result.seen, 'thing')} seen.
      {#if !result.missing.length && !result.found.length && !result.elsewhere.length}Everything matches.{/if}
    </p>
    {#if result.missing.length}
      <h3>Missing ({result.missing.length})</h3>
      <p class="muted">Not ticked, so marked missing. Ticking one in a later audit, or anywhere else, finds it again.</p>
      <ul class="plain list">{#each result.missing as item (item.id)}<li><a href="/browse/{item.id}">{named(item)}</a></li>{/each}</ul>
    {/if}
    {#if result.found.length}
      <h3>Found again ({result.found.length})</h3>
      <ul class="plain list">{#each result.found as item (item.id)}<li><a href="/browse/{item.id}">{named(item)}</a></li>{/each}</ul>
    {/if}
    {#if result.elsewhere.length}
      <h3>Here, but Invy had them elsewhere ({result.elsewhere.length})</h3>
      <ul class="plain list">
        {#each result.elsewhere as item (item.id)}
          <li class="row">
            <a href="/browse/{item.id}">{named(item)}</a>
            {#if moved.has(item.id)}<span class="muted">Moved here</span>
            {:else}<button type="button" class="small" onclick={pendingClick(() => moveHere(item))}>Move into {container.name}</button>{/if}
          </li>
        {/each}
      </ul>
    {/if}
    <div class="actions end"><button type="button" class="primary" onclick={onclose}>Done</button></div>
  {:else}
    <p class="muted">Tick what's in {container.name}, or scan its labels. Anything left unticked is marked missing when you finish.
      <HelpLink page="using/audits">How audits work</HelpLink></p>
    <Scanner onscan={scanned} prompt="Scan the labels of what's inside" typeLabel="Or type a label code" />

    {#if contents === null}
      <span class="skeleton line"></span><span class="skeleton line"></span>
    {:else}
      <div class="row tally">
        <strong aria-live="polite">{ticked.size} of {contents.length} ticked</strong>
        {#if contents.length}<button type="button" class="small" onclick={tickAll}>Tick all</button>{/if}
      </div>
      {#if contents.length}
        <fieldset class="ticks">
          <legend class="sr-only">What's in {container.name}</legend>
          {#each contents as item (item.id)}
            <label>
              <input type="checkbox" checked={ticked.has(item.id)} onchange={(event) => toggle(item.id, event.currentTarget.checked)} />
              <span>{named(item)}{#if item.quantity != null}<span class="muted"> · {amount(item)}</span>{/if}</span>
              {#if item.status !== 'Active'}<span class="chip warn">{item.status}</span>{/if}
            </label>
          {/each}
        </fieldset>
      {:else}
        <p class="muted">Invy has nothing in {container.name}. Scan anything you find in it.</p>
      {/if}
      {#if extras.length}
        <h3>Scanned, but Invy has them elsewhere</h3>
        <ul class="plain list">{#each extras as extra (extra.id)}<li>{named(extra)}</li>{/each}</ul>
      {/if}
      <div class="actions end">
        <button type="button" onclick={onclose}>Cancel</button>
        <button type="button" class="primary" onclick={pendingClick(finish)}>Finish the audit</button>
      </div>
    {/if}
  {/if}
</Modal>

<style>
  h3 { margin: 1rem 0 .3rem; font-size: 1rem; }
  .tally { align-items: center; margin: 1rem 0 .4rem; }
  .ticks { margin: 0; padding: 0; border: 0; display: grid; gap: .15rem; }
  .ticks label { display: flex; align-items: center; gap: .6rem; padding: .45rem .5rem; border-radius: .4rem; cursor: pointer; }
  .ticks label:hover { background: var(--chip); }
  .ticks input { width: 1.15rem; height: 1.15rem; margin: 0; flex: none; }
  .list { display: grid; gap: .3rem; margin: 0; }
  .list li.row { align-items: center; }
  .end { justify-content: flex-end; margin-top: 1rem; }
</style>
