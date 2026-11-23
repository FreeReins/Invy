<!-- Settings, Labels and printing: make sheets of blank QR labels for the space shown, before anything exists, or print
     the unclaimed ones again. Scanning one later makes the box it's stuck on (the Scan tab). Admins also turn on looking
     up product barcodes online, which fills in what a scanned product is. -->
<script>
  import { untrack } from 'svelte';
  import { app, run, currentSpace, canEdit, toast, isAdmin, saveSecurity } from '../lib/app.svelte.js';
  import { api } from '../lib/api.js';
  import { pending, pendingClick } from '../lib/pending.js';
  import { plural } from '../lib/format.js';
  import { LAYOUTS, perSheet } from '../lib/labelSheets.js';
  import LabelSheet from '../components/LabelSheet.svelte';
  import Printer from '@lucide/svelte/icons/printer';
  import QrCode from '@lucide/svelte/icons/qr-code';

  /** The most labels made at once (LabelEngine.BatchLimit). */
  const LIMIT = 500;
  const KEY = 'invy-label-layout';

  function rememberedLayout() {
    try { return LAYOUTS.find((layout) => layout.key === localStorage.getItem(KEY)) ?? LAYOUTS[0]; } catch { return LAYOUTS[0]; }
  }

  let layoutKey = $state(rememberedLayout().key);
  let count = $state(1);
  /** The labels to print (LabelResponse[]), and what they are. */
  let labels = $state(null);
  let what = $state('');

  const space = $derived(currentSpace());
  const layout = $derived(LAYOUTS.find((candidate) => candidate.key === layoutKey));
  const maxSheets = $derived(Math.floor(LIMIT / perSheet(layout)));

  // Labels belong to one space: picking another clears the sheets made for the last.
  $effect(() => {
    app.spaceId;
    untrack(() => { labels = null; });
  });

  function chooseLayout(key) {
    layoutKey = key;
    count = Math.min(count, maxSheets);
    try { localStorage.setItem(KEY, key); } catch { /* kept for this visit only */ }
  }

  function make(event) {
    event.preventDefault();
    return pending(event, () => run(async () => {
      const made = await api('POST', `/api/spaces/${app.spaceId}/labels`, { count: Number(count) * perSheet(layout) });
      labels = made;
      what = `${plural(made.length, 'new label')} for ${space.name}`;
      toast(`${plural(made.length, 'label')} made. Print them, then stick them on boxes.`);
    }));
  }

  async function setLookup(event) {
    const box = event.currentTarget;
    await run(() => saveSecurity({ barcodeLookup: box.checked }));
    box.checked = app.security.barcodeLookup; // back to the saved value if saving failed
    if (box.checked) toast('Scanning a product barcode in Add mode now fills in what it is.');
  }

  const reprint = () => run(async () => {
    const found = [];
    let after = null;
    do {
      const page = await api('GET', `/api/spaces/${app.spaceId}/labels?unclaimed=true${after ? `&after=${encodeURIComponent(after)}` : ''}`);
      found.push(...page.labels);
      after = page.next;
    } while (after && found.length < LIMIT);
    if (found.length === 0) {
      toast(`Every label in ${space.name} is on something. Make new ones to print.`);
      labels = null;
      return;
    }
    labels = found.slice(0, LIMIT);
    what = `${plural(labels.length, 'unclaimed label')} in ${space.name}`;
  });
</script>

<h2>Labels and printing</h2>
{#if !space}
  <p class="muted">Make or join a space first. Labels belong to a space.</p>
{:else}
  <div class="card">
    <p class="muted intro">Print QR labels before anything is in a box. Stick one on, scan it, and it becomes that box, in
      the place you're adding to. Each label also shows its code, to type in if the camera can't read it.</p>
    <label>
      <span>Sticker sheet</span>
      <select value={layoutKey} onchange={(event) => chooseLayout(event.currentTarget.value)}>
        {#each LAYOUTS as option (option.key)}<option value={option.key}>{option.label}</option>{/each}
      </select>
    </label>
    {#if canEdit()}
      <form class="make" onsubmit={make}>
        <label>
          <span>Sheets</span>
          <input type="number" bind:value={count} min="1" max={maxSheets} required inputmode="numeric" />
        </label>
        <button type="submit" class="primary with-icon"><QrCode size={16} aria-hidden="true" />Make {plural(Number(count) * perSheet(layout) || 0, 'label')}</button>
      </form>
    {:else}
      <p class="muted">You can only look in {space.name}, so you can print its unclaimed labels but not make new ones.</p>
    {/if}
    <button type="button" class="with-icon" onclick={pendingClick(reprint)}><Printer size={16} aria-hidden="true" />Print the unclaimed labels again</button>
  </div>

  {#if isAdmin() && app.security}
    <h3>Product barcodes</h3>
    <div class="card">
      <label class="check">
        <input id="barcode-lookup" type="checkbox" class="switch" checked={app.security.barcodeLookup} onchange={setLookup}>
        <span>
          Look up product barcodes online
          <span class="muted" style="display:block">Scanning a UPC or EAN barcode in Scan's Add mode fills in the name, brand and photo.
            Invy sends the barcode, and nothing else, to Open Food Facts, then UPCitemdb (about 100 lookups a day).</span>
        </span>
      </label>
    </div>
  {/if}

  {#if labels}
    <div class="ready">
      <p><strong>{what}</strong>, on {plural(Math.ceil(labels.length / perSheet(layout)), 'sheet')}.</p>
      <button type="button" class="primary with-icon" onclick={() => window.print()}><Printer size={16} aria-hidden="true" />Print</button>
    </div>
    <p class="muted">Print at actual size (100%, no "fit to page"), on the sticker sheet chosen above.</p>
    <LabelSheet {labels} {layout} />
  {/if}
{/if}

<style>
  .intro { margin-top: 0; }
  h3 { margin: 1.25rem 0 .4rem; font-size: 1.05rem; }
  .card label.check { display: flex; gap: .6rem; align-items: flex-start; }
  .card { display: grid; gap: .75rem; justify-items: start; }
  .card label { display: grid; gap: .25rem; }
  .make { display: flex; flex-wrap: wrap; gap: .5rem; align-items: end; }
  .make input { width: 6rem; }
  .ready { display: flex; flex-wrap: wrap; align-items: center; justify-content: space-between; gap: .75rem; margin-top: 1.25rem; }
  .ready p { margin: 0; }
</style>
