<!-- The Views tab: saved searches with how to show them (docs/developers/design.md, Views). /views lists the space's
     shared views and the person's own; /views/{id} opens one. Anyone can make views for themselves; editors and owners
     share them and change shared ones. Pinned views also show on Find. -->
<script>
  import { untrack } from 'svelte';
  import { app, run, ask, currentSpace } from '../lib/app.svelte.js';
  import { go } from '../lib/addresses.js';
  import { api } from '../lib/api.js';
  import { pendingClick } from '../lib/pending.js';
  import { displayLabel } from '../lib/views.js';
  import NoSpace from '../components/NoSpace.svelte';
  import HelpLink from '../components/HelpLink.svelte';
  import ViewForm from '../views/ViewForm.svelte';
  import ViewResults from '../views/ViewResults.svelte';
  import LayoutList from '@lucide/svelte/icons/layout-list';
  import Plus from '@lucide/svelte/icons/plus';
  import Pin from '@lucide/svelte/icons/pin';
  import PinOff from '@lucide/svelte/icons/pin-off';
  import Pencil from '@lucide/svelte/icons/pencil';
  import Copy from '@lucide/svelte/icons/copy';
  import Trash from '@lucide/svelte/icons/trash-2';
  import Download from '@lucide/svelte/icons/download';
  import ShieldCheck from '@lucide/svelte/icons/shield-check';
  import SearchCheck from '@lucide/svelte/icons/search-check';
  import { has } from '../lib/features.js';
  import ValueTotals from '../views/ValueTotals.svelte';
  import InsuranceReport from '../views/InsuranceReport.svelte';
  import MissingReport from '../views/MissingReport.svelte';
  import ArrowLeft from '@lucide/svelte/icons/arrow-left';

  let views = $state(null);
  let types = $state([]);
  // The form: null while closed, else { view } to change one or { start } for a new one.
  let form = $state(null);

  const space = $derived(currentSpace());
  const open = $derived(views?.find((view) => view.id === app.viewId) ?? null);

  async function load(id) {
    const [found, spaceTypes] = await Promise.all([api('GET', `/api/spaces/${id}/views`), api('GET', `/api/spaces/${id}/types`)]);
    if (id !== app.spaceId) return;
    views = found;
    types = spaceTypes;
  }

  // Loads again for another space. run() reads app.busy, which mustn't count.
  $effect(() => {
    const id = app.spaceId;
    untrack(() => {
      views = null;
      if (id) run(() => load(id));
    });
  });

  // Find's "Save as a view" opens /views?new=<search>.
  $effect(() => {
    const query = new URLSearchParams(location.search).get('new');
    if (query !== null) untrack(() => { form = { start: { query } }; history.replaceState(history.state, '', '/views'); });
  });

  function saved(view) {
    views = [...(views ?? []).filter((other) => other.id !== view.id), view]
      .sort((a, b) => Number(b.pinned) - Number(a.pinned) || a.name.localeCompare(b.name));
    form = null;
    if (app.viewId !== view.id) go(`/views/${view.id}`);
  }

  const request = (view, changes) => ({
    name: view.name, query: view.query, display: view.display, columns: view.columns, sort: view.sort,
    groupBy: view.groupBy, shared: view.shared, pinned: view.pinned, ...changes,
  });

  const togglePin = (view) => run(async () => {
    const changed = await api('PUT', `/api/spaces/${app.spaceId}/views/${view.id}`, request(view, { pinned: !view.pinned }));
    views = views.map((other) => (other.id === changed.id ? changed : other));
  });

  async function remove(view) {
    const sure = await ask({
      title: `Remove ${view.name}?`,
      message: view.shared ? 'It goes for everyone in this space. The items it finds stay as they are.' : 'The items it finds stay as they are.',
      confirmLabel: 'Remove view', danger: true,
    });
    if (!sure) return undefined;
    return run(async () => {
      await api('DELETE', `/api/spaces/${app.spaceId}/views/${view.id}`);
      views = views.filter((other) => other.id !== view.id);
      await go('/views');
    });
  }

  const who = (view) => (view.shared ? 'Shared' : 'Just you');
</script>

{#if !space}
  <NoSpace area="var(--area-views)" />
{:else if views === null}
  <section class="views">
    <h2>Views</h2>
    <div class="card"><span class="skeleton line"></span><span class="skeleton line"></span></div>
  </section>
{:else if app.viewId === 'insurance'}
  <InsuranceReport {types} />
{:else if app.viewId === 'missing'}
  <MissingReport />
{:else if app.viewId}
  <section class="views">
    <a class="button back" href="/views"><ArrowLeft size={16} aria-hidden="true" />All views</a>
    {#if !open}
      <div class="card empty">
        <span class="empty-icon" style="--area: var(--area-views)" aria-hidden="true"><LayoutList size={26} /></span>
        <h2>That view is gone</h2>
        <p class="muted">It was removed, or it belongs to someone else. Pick another view.</p>
      </div>
    {:else}
      <div class="row head">
        <div>
          <h2>{open.name}</h2>
          <p class="muted">
            {displayLabel(open.display)} · {who(open)}{open.pinned ? ' · Pinned to Find' : ''}
            {#if open.query}· <code>{open.query}</code>{:else}· Everything{/if}
          </p>
        </div>
        <div class="actions">
          {#if open.canChange}
            <button type="button" class="with-icon" onclick={pendingClick(() => togglePin(open))} aria-pressed={open.pinned}>
              {#if open.pinned}<PinOff size={16} aria-hidden="true" />Unpin{:else}<Pin size={16} aria-hidden="true" />Pin to Find{/if}
            </button>
            <button type="button" class="with-icon" onclick={() => { form = { view: open }; }}><Pencil size={16} aria-hidden="true" />Change</button>
            <button type="button" class="with-icon" onclick={pendingClick(() => remove(open))}><Trash size={16} aria-hidden="true" />Remove</button>
          {:else}
            <button type="button" class="with-icon" onclick={() => { form = { start: open }; }}><Copy size={16} aria-hidden="true" />Save a copy</button>
          {/if}
          <a class="button with-icon" href="/api/spaces/{app.spaceId}/views/{open.id}/export.csv" download><Download size={16} aria-hidden="true" />Export CSV</a>
        </div>
      </div>
      <ViewResults view={open} {types} onedit={open.canChange ? () => { form = { view: open }; } : null} />
    {/if}
  </section>
{:else}
  <section class="views">
    <div class="row head">
      <div>
        <h2>Views</h2>
        <p class="muted">Searches you keep, shown as a table, cards, a tree, a board or a timeline. <HelpLink page="using/views">About views</HelpLink></p>
      </div>
      <div class="actions">
        <button type="button" class="primary with-icon" onclick={() => { form = { start: null }; }}><Plus size={16} aria-hidden="true" />New view</button>
      </div>
    </div>
    {#if views.length === 0}
      <div class="card empty">
        <span class="empty-icon" style="--area: var(--area-views)" aria-hidden="true"><LayoutList size={26} /></span>
        <h3>No views yet</h3>
        <p class="muted">Keep a search you use often, such as seen&gt;1y for things not seen in a year, and pin it to Find.</p>
      </div>
    {:else}
      <div class="card">
        <ul class="plain" aria-label="Views in {space.name}">
          {#each views as view (view.id)}
            <li class="view-row">
              <a href="/views/{view.id}" class="name">{view.name}</a>
              <span class="chips">
                <span class="chip">{displayLabel(view.display)}</span>
                <span class="chip" class:info={view.shared}>{who(view)}</span>
                {#if view.pinned}<span class="chip good">Pinned</span>{/if}
              </span>
              <code class="muted clip query">{view.query || 'Everything'}</code>
            </li>
          {/each}
        </ul>
      </div>
    {/if}

    {#if has('values') || has('audits')}
      <h2 class="reports-title">Reports</h2>
      <p class="muted"><HelpLink page="using/reports">About reports</HelpLink></p>
      {#if has('values')}<ValueTotals />{/if}
      <ul class="reports grid">
        {#if has('values')}
          <li class="card">
            <a href="/views/insurance" class="with-icon report-name"><ShieldCheck size={18} aria-hidden="true" />Insurance inventory</a>
            <p class="muted">Everything by room, with photos and values, to print or save as a PDF.</p>
            <a href="/api/spaces/{app.spaceId}/reports/insurance.csv" download aria-label="Download CSV of the insurance inventory">Download CSV</a>
          </li>
        {/if}
        {#if has('audits')}
          <li class="card">
            <a href="/views/missing" class="with-icon report-name"><SearchCheck size={18} aria-hidden="true" />Missing items</a>
            <p class="muted">What audits didn't find, with where each belongs.</p>
            <a href="/api/spaces/{app.spaceId}/reports/missing.csv" download aria-label="Download CSV of the missing items">Download CSV</a>
          </li>
        {/if}
      </ul>
    {/if}
  </section>
{/if}

{#if form}
  <ViewForm view={form.view ?? null} start={form.start ?? null} {types} onsaved={saved} onclose={() => { form = null; }} />
{/if}

<style>
  .views { margin-top: 1.25rem; }
  .head { align-items: flex-start; margin-bottom: .5rem; }
  .head h2 { margin: .5rem 0 .2rem; overflow-wrap: anywhere; }
  .empty h2, .empty h3 { margin: .25rem 0; }
  .view-row { display: grid; grid-template-columns: minmax(0, 1fr) auto; gap: .15rem .75rem; align-items: center; padding: .55rem 0; }
  .view-row .chips { margin: 0; justify-content: flex-end; }
  .name { font-weight: 600; overflow-wrap: anywhere; }
  .query { grid-column: 1 / -1; font-size: .85rem; }
  code { font-size: .9em; }
  .reports-title { margin-top: 1.75rem; }
  .reports { list-style: none; padding: 0; margin: .5rem 0; }
  .reports li { margin: 0; display: grid; gap: .3rem; align-content: start; }
  .reports p { margin: 0; }
  .report-name { font-weight: 600; }
  .report-name :global(svg) { color: var(--area-views); }
</style>
