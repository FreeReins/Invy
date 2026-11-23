<!-- The Scan tab: the camera, in one of four modes. Open goes to what a label is on (or searches for a product barcode);
     Move takes two scans, the thing and then where it's going; Claim turns each new label into a box in the sticky place;
     Add makes the scanned container the sticky place, then quick add fills it. /l/<code> for a label that isn't on
     anything yet lands here, offering to claim it. -->
<script>
  import { onMount } from 'svelte';
  import { app, run, currentSpace, canEdit, stickyPlace, setStickyPlace, labelCode, findLabel, showSpaceOf } from '../lib/app.svelte.js';
  import { go } from '../lib/addresses.js';
  import { api } from '../lib/api.js';
  import { pendingClick } from '../lib/pending.js';
  import { plural } from '../lib/format.js';
  import NoSpace from '../components/NoSpace.svelte';
  import HelpLink from '../components/HelpLink.svelte';
  import Scanner from '../components/Scanner.svelte';
  import StickyPlace from '../items/StickyPlace.svelte';
  import QuickAdd from '../items/QuickAdd.svelte';
  import Tag from '@lucide/svelte/icons/tag';

  const MODES = [
    { key: 'open', label: 'Open', hint: 'Scan a label to open what it is on, or a product barcode to search for it.' },
    { key: 'move', label: 'Move', hint: 'Scan the thing, then scan where it is going.' },
    { key: 'claim', label: 'Claim', hint: 'Scan new labels. Each one becomes a box in the place below.' },
    { key: 'add', label: 'Add', hint: 'Scan a box or shelf to add things to it, then type what goes in.' },
  ];

  let mode = $state('open');
  /** A label code that isn't on anything yet, offered to claim. */
  let unclaimed = $state(null);
  /** The item a Move scan picked up (ItemRef), waiting for where it goes. */
  let moving = $state(null);
  /** What happened, newest first: { id, text, href? }. */
  let log = $state([]);
  let nextId = 0;

  const space = $derived(currentSpace());
  const editor = $derived(canEdit());
  const modes = $derived(editor ? MODES : MODES.slice(0, 1));
  const current = $derived(MODES.find((candidate) => candidate.key === mode));
  const place = $derived(stickyPlace());
  /** Where new things go, in words: "in Box F" or "at the top". */
  const where = $derived(place ? `in ${place.name}` : 'at the top');

  onMount(() => {
    if (app.scanLabel) {
      unclaimed = app.scanLabel;
      app.scanLabel = null;
    }
  });

  function note(text, href = null) {
    log = [{ id: nextId++, text, href }, ...log].slice(0, 8);
  }

  function change(key) {
    mode = key;
    moving = null;
    unclaimed = null;
  }

  /** A label in this space, or an error saying what to scan instead. */
  async function labelHere(text) {
    const code = labelCode(text);
    if (!code) throw new Error(`"${text}" isn't an Invy label. Scan a QR label printed from Invy.`);
    const label = await findLabel(code);
    if (label.spaceId !== app.spaceId) {
      throw new Error(`Label ${code} belongs to another space. Switch to it with the space picker, or scan it in Open mode.`);
    }
    return label;
  }

  async function claim(code) {
    const made = await api('POST', `/api/spaces/${app.spaceId}/labels/${code}/claim`, { parentId: place?.id ?? null, name: null, typeId: null });
    note(`Made ${made.name} ${where}.`, `/browse/${made.id}`);
    unclaimed = null;
    return made;
  }

  const scanned = (text) => run(async () => {
    if (mode === 'open') {
      const code = labelCode(text);
      if (!code) {
        await go(`/find?q=${encodeURIComponent(text)}`);
        return;
      }
      const label = await findLabel(code);
      showSpaceOf(label);
      if (label.item) await go(`/browse/${label.item.id}`);
      else unclaimed = label.code;
      return;
    }

    const label = await labelHere(text);
    if (mode === 'claim') {
      if (label.item) note(`Label ${label.code} is already on ${label.item.name}.`, `/browse/${label.item.id}`);
      else await claim(label.code);
    } else if (mode === 'move') {
      if (!label.item) throw new Error(`Label ${label.code} isn't on anything yet. Claim it first.`);
      if (!moving) {
        moving = label.item;
      } else if (moving.id === label.item.id) {
        throw new Error(`That's ${moving.name} again. Scan where it is going.`);
      } else {
        await api('POST', `/api/spaces/${app.spaceId}/items/${moving.id}/move`, { parentId: label.item.id });
        note(`Moved ${moving.name} into ${label.item.name}.`, `/browse/${moving.id}`);
        moving = null;
      }
    } else if (mode === 'add') {
      const container = label.item ?? await claim(label.code);
      setStickyPlace(container);
      note(`Adding to ${container.name}.`, `/browse/${container.id}`);
    }
  });
</script>

{#if !space}
  <NoSpace area="var(--area-scan)" />
{:else}
  <section class="scan">
    <h2>Scan</h2>
    <fieldset class="modes">
      <legend class="sr-only">What scanning does</legend>
      {#each modes as option (option.key)}
        <label class:on={mode === option.key}>
          <input type="radio" name="scan-mode" value={option.key} checked={mode === option.key} onchange={() => change(option.key)} />
          {option.label}
        </label>
      {/each}
    </fieldset>
    <p class="muted">{current.hint}</p>

    {#if editor && mode !== 'open'}<StickyPlace />{/if}

    {#if unclaimed}
      <div class="card unclaimed" role="status">
        <span class="empty-icon" style="--area: var(--area-scan)" aria-hidden="true"><Tag size={22} /></span>
        <div>
          <h3>Label {unclaimed} isn't on anything yet</h3>
          {#if editor}
            <p class="muted">Make a box for it {place ? `in ${place.name}` : 'at the top of the space'}, then rename it or fill it from its page.</p>
            <div class="actions">
              <button type="button" class="primary" onclick={pendingClick(() => run(() => claim(unclaimed)))}>Make Box {unclaimed} {where}</button>
              <button type="button" onclick={() => { unclaimed = null; }}>Not now</button>
            </div>
          {:else}
            <p class="muted">Ask an editor of {space.name} to claim it.</p>
          {/if}
        </div>
      </div>
    {/if}

    {#if moving}
      <p class="card waiting" role="status">Moving <strong>{moving.name}</strong>. Now scan where it is going.
        <button type="button" class="small" onclick={() => { moving = null; }}>Cancel</button></p>
    {/if}

    <Scanner onscan={scanned} prompt={moving ? `Scan where ${moving.name} is going` : current.hint.replace(/\.$/, '')}
      typeLabel={mode === 'open' ? 'Or type a label code or barcode' : 'Or type the label code'} />

    {#if mode === 'add' && editor && place}
      <div class="card"><QuickAdd spaceId={app.spaceId} parent={place} onadded={(made) => note(`${plural(made.length, 'item')} added to ${place.name}.`, `/browse/${place.id}`)} /></div>
    {/if}

    {#if log.length}
      <h3>Just now</h3>
      <ul class="log plain" aria-live="polite">
        {#each log as entry (entry.id)}
          <li>{#if entry.href}<a href={entry.href}>{entry.text}</a>{:else}{entry.text}{/if}</li>
        {/each}
      </ul>
    {/if}
    <p class="muted"><HelpLink page="using/labels-and-scanning">Labels and scanning</HelpLink></p>
  </section>
{/if}

<style>
  .scan { margin-top: 1.25rem; display: grid; gap: .75rem; }
  h2 { margin: 0; }
  h3 { margin: .5rem 0 0; font-size: 1.05rem; }
  .scan > p { margin: 0; }
  .modes { display: flex; flex-wrap: wrap; gap: .35rem; margin: 0; padding: 0; border: 0; }
  .modes label { display: inline-flex; align-items: center; gap: .35rem; padding: .45rem .9rem; border-radius: 999px;
    border: 1px solid var(--line); background: var(--panel); cursor: pointer; }
  .modes label.on { border-color: var(--area-scan); background: color-mix(in srgb, var(--area-scan) 14%, var(--panel)); font-weight: 600; }
  .modes label:has(input:focus-visible) { outline: 2px solid var(--accent); outline-offset: 2px; }
  .modes input { accent-color: var(--area-scan); margin: 0; }
  .unclaimed { display: flex; gap: .75rem; align-items: flex-start; }
  .unclaimed h3 { margin: 0 0 .2rem; }
  .unclaimed p { margin: 0 0 .6rem; }
  .waiting { display: flex; flex-wrap: wrap; align-items: center; justify-content: space-between; gap: .5rem; margin: 0; }
  .log { display: grid; gap: .3rem; margin: 0; }
  .log li { padding: .4rem .6rem; border-radius: .5rem; background: var(--chip); }
</style>
