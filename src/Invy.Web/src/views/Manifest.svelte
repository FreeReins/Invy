<!-- A packed box's manifest (/views/manifest?box=<id>), to print and stick on it: its name and label, where it goes,
     and what's inside. Printed from the browser, as label sheets are. -->
<script>
  import { untrack } from 'svelte';
  import { app, run } from '../lib/app.svelte.js';
  import { api } from '../lib/api.js';
  import { amount, label } from '../lib/items.js';
  import { plural } from '../lib/format.js';
  import Printer from '@lucide/svelte/icons/printer';
  import ArrowLeft from '@lucide/svelte/icons/arrow-left';

  let manifest = $state(null);
  const boxId = new URLSearchParams(location.search).get('box');

  $effect(() => {
    void app.spaceId;
    untrack(() => run(async () => {
      manifest = boxId ? await api('GET', `/api/spaces/${app.spaceId}/items/${boxId}/manifest`) : null;
    }));
  });
</script>

<section class="manifest">
  <a class="button back no-print" href="/views/moving"><ArrowLeft size={16} aria-hidden="true" />Moving</a>
  {#if !boxId}
    <div class="card empty"><h2>No box chosen</h2><p class="muted">Open a manifest from the Moving list or a box's page.</p></div>
  {:else if manifest === null}
    <div class="card"><span class="skeleton line"></span><span class="skeleton line"></span></div>
  {:else}
    <div class="row head">
      <div>
        <h2>{label(manifest.box)}{manifest.box.labelCode ? ` · ${manifest.box.labelCode}` : ''}</h2>
        <p class="goes">Goes to: <strong>{manifest.destinationPath.length ? manifest.destinationPath.map((place) => place.name).join(' › ') : 'not decided yet'}</strong></p>
      </div>
      <button type="button" class="primary with-icon no-print" onclick={() => window.print()}><Printer size={16} aria-hidden="true" />Print or save as PDF</button>
    </div>
    <div class="card">
      <h3>Inside ({plural(manifest.contents.length, 'thing')}{manifest.more ? ' and more' : ''})</h3>
      {#if manifest.contents.length}
        <ul class="contents">
          {#each manifest.contents as item (item.id)}
            <li>{label(item)}{#if item.quantity != null} <span class="muted">× {amount(item)}</span>{/if}{#if item.holdsItems} <span class="muted">({plural(item.contents, 'thing')} inside)</span>{/if}</li>
          {/each}
        </ul>
      {:else}
        <p class="muted">Nothing inside yet.</p>
      {/if}
    </div>
  {/if}
</section>

<style>
  .manifest { margin-top: 1.25rem; }
  .head { align-items: flex-start; }
  .head h2 { margin: .5rem 0 .2rem; font-size: 1.6rem; overflow-wrap: anywhere; }
  .goes { font-size: 1.2rem; margin: 0 0 .5rem; }
  h3 { margin: 0 0 .5rem; }
  .contents { columns: 2 14rem; margin: 0; padding-left: 1.2rem; }
  .contents li { break-inside: avoid; padding: .15rem 0; }
</style>
