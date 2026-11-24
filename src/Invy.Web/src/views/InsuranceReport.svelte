<!-- The insurance inventory (/views/insurance): everything by room, with photos, details and values, totalled per room.
     It prints cleanly, and the browser's print dialog saves it as a PDF (docs/roadmap/README.md, Decisions); the
     CSV comes from the server. Shown behind the values feature. -->
<script>
  import { untrack } from 'svelte';
  import { app, run, currentSpace } from '../lib/app.svelte.js';
  import { api } from '../lib/api.js';
  import { label } from '../lib/items.js';
  import { plural, fullDate } from '../lib/format.js';
  import { money, fieldsText } from '../lib/views.js';
  import Printer from '@lucide/svelte/icons/printer';
  import Download from '@lucide/svelte/icons/download';
  import ArrowLeft from '@lucide/svelte/icons/arrow-left';
  import Package from '@lucide/svelte/icons/package';

  /** types: the space's types, for each thing's details. */
  let { types } = $props();

  let report = $state(null);
  const space = $derived(currentSpace());

  $effect(() => {
    const id = app.spaceId;
    untrack(() => {
      report = null;
      run(async () => { report = await api('GET', `/api/spaces/${id}/reports/insurance`); });
    });
  });

  /** Where a thing is inside its room: "Drawer › Box F", or '' when it is straight in the room. */
  const within = (result) => result.chain.slice(1).map((place) => place.name).join(' › ');
  const thumbnail = (result) => `/api/spaces/${app.spaceId}/attachments/${result.details.photoId}/thumbnail`;
</script>

<section class="report">
  <a class="button back no-print" href="/views"><ArrowLeft size={16} aria-hidden="true" />All views</a>
  <div class="row head">
    <div>
      <h2>Insurance inventory: {space?.name}</h2>
      {#if report}<p class="muted">Made {fullDate(null, report.madeAt)}. {plural(report.count, 'thing')}, worth {money(report.total)} in all.
        Currencies are added as they are.</p>{/if}
    </div>
    <div class="actions no-print">
      <button type="button" class="primary with-icon" onclick={() => window.print()}><Printer size={16} aria-hidden="true" />Print or save as PDF</button>
      <a class="button with-icon" href="/api/spaces/{app.spaceId}/reports/insurance.csv" download><Download size={16} aria-hidden="true" />Download CSV</a>
    </div>
  </div>

  {#if report === null}
    <div class="card"><span class="skeleton line"></span><span class="skeleton line"></span><span class="skeleton line"></span></div>
  {:else if report.count === 0}
    <div class="card empty">
      <span class="empty-icon" style="--area: var(--area-views)" aria-hidden="true"><Package size={26} /></span>
      <h3>Nothing to list yet</h3>
      <p class="muted">Add things on the Browse tab, with a price on their type, and they show up here by room.</p>
    </div>
  {:else}
    {#if report.truncated}
      <p class="card note" role="note">This lists the first {plural(report.count, 'thing')} by name. The CSV has everything.</p>
    {/if}
    {#each report.rooms as room (room.room?.id ?? 'top')}
      <section class="room" aria-labelledby="room-{room.room?.id ?? 'top'}">
        <h3 id="room-{room.room?.id ?? 'top'}" class="row">
          <span>{room.room ? label(room.room) : 'Not in a room'}</span>
          <span class="muted">{plural(room.items.length, 'thing')} · {money(room.value)}</span>
        </h3>
        <div class="card table-wrap">
          <table>
            <thead>
              <tr><th scope="col"><span class="sr-only">Photo</span></th><th scope="col">Thing</th><th scope="col">Details</th>
                <th scope="col" class="num">Value</th></tr>
            </thead>
            <tbody>
              {#each room.items as result (result.item.id)}
                <tr>
                  <td class="photo">{#if result.details?.photoId}<img src={thumbnail(result)} alt="" loading="lazy" />{/if}</td>
                  <td>
                    <a href="/browse/{result.item.id}">{label(result.item)}</a>
                    {#if within(result)}<span class="muted block">in {within(result)}</span>{/if}
                  </td>
                  <td class="details">{[types.find((type) => type.id === result.item.typeId)?.name, fieldsText(result, types)].filter(Boolean).join(' · ')}</td>
                  <td class="num">{result.item.value == null ? '' : money(result.item.value)}</td>
                </tr>
              {/each}
            </tbody>
          </table>
        </div>
      </section>
    {/each}
  {/if}
</section>

<style>
  .report { margin-top: 1.25rem; }
  .head { align-items: flex-start; }
  .head h2 { margin: .5rem 0 .2rem; overflow-wrap: anywhere; }
  .empty h3 { margin: .25rem 0; }
  .note { margin: .5rem 0; }
  .room { margin-top: 1.25rem; }
  .room h3 { margin: 0; }
  .table-wrap { overflow-x: auto; padding: 0; }
  table { width: 100%; border-collapse: collapse; font-size: .92rem; }
  th, td { text-align: left; padding: .4rem .6rem; border-top: 1px solid var(--line); vertical-align: top; }
  thead th { border-top: 0; }
  .num { text-align: right; white-space: nowrap; }
  .photo { width: 4.5rem; }
  .photo img { width: 4rem; height: 4rem; object-fit: cover; border-radius: .3rem; display: block; }
  .details { color: var(--muted); }
  .block { display: block; font-size: .85rem; }
  @media print {
    .report { margin: 0; }
    tr, .room h3 { break-inside: avoid; }
    .room h3 { break-after: avoid; }
    .table-wrap { overflow: visible; }
  }
</style>
