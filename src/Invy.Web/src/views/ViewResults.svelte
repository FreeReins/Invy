<!-- A saved view's results in its display: a table, cards, a tree, a board or a timeline (docs/developers/design.md,
     Views). It loads the view's search as of now, and again whenever the items live topic says something changed,
     so an open "not seen in a year" view drops a thing as soon as it is scanned. -->
<script>
  import { untrack } from 'svelte';
  import { app, run, onLive } from '../lib/app.svelte.js';
  import { api } from '../lib/api.js';
  import { has } from '../lib/features.js';
  import { amount, label, chainText } from '../lib/items.js';
  import { plural } from '../lib/format.js';
  import { pendingClick } from '../lib/pending.js';
  import { DEFAULT_COLUMNS, cell, columnLabel, fieldsByKey, sortBy, tree, board } from '../lib/views.js';
  import EventLine from '../items/EventLine.svelte';
  import Box from '@lucide/svelte/icons/box';
  import Package from '@lucide/svelte/icons/package';
  import ArrowUp from '@lucide/svelte/icons/arrow-up';
  import ArrowDown from '@lucide/svelte/icons/arrow-down';

  /** view: ViewResponse. types: the space's ItemTypeResponse[]. onedit(): offered when the search no longer reads. */
  let { view, types, onedit = null } = $props();

  let page = $state(null);
  let timeline = $state(null);
  let problem = $state('');
  let sortKey = $state(null);
  let ascending = $state(true);
  let asked = 0;

  const base = $derived(`/api/spaces/${app.spaceId}/views/${view.id}`);
  const columns = $derived((view.columns.length ? view.columns : DEFAULT_COLUMNS).filter((key) => key !== 'value' || has('values')));
  const context = $derived({ types, keys: fieldsByKey(types) });
  const rows = $derived(page && sortKey ? sortBy(page.results, sortKey, ascending, context) : page?.results ?? []);

  /** Loads the first page again. Answers to an older load that arrive late are ignored. */
  function load() {
    const mine = ++asked;
    const path = view.display === 'Timeline' ? `${base}/timeline` : `${base}/results`;
    return run(async () => {
      try {
        const answer = await api('GET', path);
        if (mine !== asked) return;
        if (view.display === 'Timeline') timeline = answer; else page = answer;
        problem = '';
      } catch (error) {
        if (mine !== asked) return;
        // A search that no longer reads (a renamed field or type) is explained here, with a way to fix it.
        if (error.status === 400) problem = error.message; else throw error;
      }
    });
  }

  const more = () => run(async () => {
    if (view.display === 'Timeline') {
      const next = await api('GET', `${base}/timeline?before=${encodeURIComponent(timeline.next)}`);
      timeline = { events: [...timeline.events, ...next.events], next: next.next };
    } else {
      const next = await api('GET', `${base}/results?after=${encodeURIComponent(page.next)}`);
      page = { ...page, results: [...page.results, ...next.results], next: next.next };
    }
  });

  // Loads again when the view itself changes (edited, or another one opened). run() reads app.busy, which mustn't count.
  $effect(() => {
    void view.id; void view.updatedAt;
    untrack(() => { page = null; timeline = null; sortKey = null; load(); });
  });
  $effect(() => onLive('items', () => load()));

  function sortOn(key) {
    if (sortKey === key) ascending = !ascending;
    else { sortKey = key; ascending = true; }
  }

  const ariaSort = (key) => (sortKey === key ? (ascending ? 'ascending' : 'descending') : undefined);
  const thumbnail = (result) => `/api/spaces/${app.spaceId}/attachments/${result.details.photoId}/thumbnail`;
  const countText = $derived(page ? `${page.totalIsEstimate ? 'About ' : ''}${plural(page.total ?? page.results.length, 'item')}` : '');
</script>

{#snippet itemLink(result)}
  <a href="/browse/{result.item.id}" class="item-link">
    <span class="icon" aria-hidden="true">{#if result.item.holdsItems}<Box size={16} />{:else}<Package size={16} />{/if}</span>
    <span class="clip">{label(result.item)}</span>
  </a>
{/snippet}

{#snippet branch(node)}
  {#each node.items as result (result.item.id)}
    <li>{@render itemLink(result)}{#if amount(result.item)}<span class="muted extra">{amount(result.item)}</span>{/if}</li>
  {/each}
  {#each node.children as child (child.ref.id)}
    <li class="place">
      <a href="/browse/{child.ref.id}" class="place-name">{label(child.ref)}</a>
      <ul class="branch">{@render branch(child)}</ul>
    </li>
  {/each}
{/snippet}

{#if problem}
  <div class="card problem" role="alert">
    <p class="bad-text">{problem}</p>
    {#if onedit}<p><button type="button" onclick={onedit}>Change the search</button></p>{/if}
  </div>
{:else if view.display === 'Timeline'}
  {#if timeline === null}
    <div class="card"><span class="skeleton line"></span><span class="skeleton line"></span></div>
  {:else if timeline.events.length === 0}
    <div class="card empty"><h3>Nothing has happened yet</h3><p class="muted">Adding, moving and seeing the items this view finds shows up here.</p></div>
  {:else}
    <div class="card">
      <ul class="plain" aria-label="What happened to {view.name}">
        {#each timeline.events as event (event.id)}<li><EventLine {event} /></li>{/each}
      </ul>
    </div>
    {#if timeline.next}<button type="button" onclick={pendingClick(more)}>Show older</button>{/if}
  {/if}
{:else if page === null}
  <div class="card"><span class="skeleton line"></span><span class="skeleton line"></span><span class="skeleton line"></span></div>
{:else if page.results.length === 0}
  <div class="card empty"><h3>Nothing matches right now</h3><p class="muted">Things show up here as soon as they match the search.</p></div>
{:else}
  <p class="count muted" aria-live="polite">{countText}</p>
  {#if view.display === 'Table'}
    <div class="card table-wrap">
      <table>
        <caption class="sr-only">{view.name}</caption>
        <thead>
          <tr>
            {#each columns as key (key)}
              <th scope="col" aria-sort={ariaSort(key)}>
                <button type="button" class="sort" onclick={() => sortOn(key)}>
                  {columnLabel(key, types)}
                  {#if sortKey === key}{#if ascending}<ArrowUp size={14} aria-hidden="true" />{:else}<ArrowDown size={14} aria-hidden="true" />{/if}{/if}
                </button>
              </th>
            {/each}
          </tr>
        </thead>
        <tbody>
          {#each rows as result (result.item.id)}
            <tr>
              {#each columns as key (key)}
                {#if key === 'name'}<td>{@render itemLink(result)}</td>{:else}<td>{cell(key, result, context)}</td>{/if}
              {/each}
            </tr>
          {/each}
        </tbody>
      </table>
    </div>
  {:else if view.display === 'Cards'}
    <ul class="cards" aria-label={view.name}>
      {#each page.results as result (result.item.id)}
        <li class="card item-card">
          <a href="/browse/{result.item.id}">
            {#if result.details?.photoId}
              <img src={thumbnail(result)} alt="" loading="lazy" />
            {:else}
              <span class="no-photo" aria-hidden="true">{#if result.item.holdsItems}<Box size={32} />{:else}<Package size={32} />{/if}</span>
            {/if}
            <span class="clip card-name">{label(result.item)}</span>
          </a>
          <span class="clip muted">{result.chain.length ? `in ${chainText(result.chain)}` : 'At the top'}</span>
        </li>
      {/each}
    </ul>
  {:else if view.display === 'Tree'}
    <div class="card">
      <ul class="branch top" aria-label={view.name}>{@render branch(tree(page.results))}</ul>
    </div>
  {:else if view.display === 'Board'}
    <div class="board">
      {#each board(page.results, view.groupBy, types) as column (column.title)}
        <section class="card lane" aria-label="{column.title}: {plural(column.items.length, 'item')}">
          <h3>{column.title} <span class="muted">{column.items.length}</span></h3>
          <ul class="plain">
            {#each column.items as result (result.item.id)}<li>{@render itemLink(result)}</li>{/each}
          </ul>
        </section>
      {/each}
    </div>
  {/if}
  {#if page.next}<button type="button" class="more" onclick={pendingClick(more)}>Show more</button>{/if}
{/if}

<style>
  .count { margin: .25rem 0; }
  .empty h3 { margin: .25rem 0; }
  .problem p { margin: .3rem 0; }
  .item-link { display: inline-flex; align-items: center; gap: .4rem; min-width: 0; max-width: 100%; color: var(--ink); text-decoration: none; }
  .item-link:hover .clip { text-decoration: underline; }
  .item-link .icon { display: inline-flex; flex: none; color: var(--area-views); }
  .extra { margin-left: .5rem; font-size: .85rem; }
  .table-wrap { overflow-x: auto; padding: 0; }
  table { width: 100%; border-collapse: collapse; font-size: .92rem; }
  th, td { text-align: left; padding: .45rem .7rem; border-top: 1px solid var(--line); vertical-align: top; }
  thead th { border-top: 0; white-space: nowrap; }
  .sort { display: inline-flex; align-items: center; gap: .25rem; border: 0; background: none; padding: .1rem .2rem; font-weight: 600; color: var(--ink); }
  .cards { list-style: none; padding: 0; margin: .5rem 0; display: grid; gap: .75rem; grid-template-columns: repeat(auto-fill, minmax(min(11rem, 100%), 1fr)); }
  .item-card { display: grid; gap: .2rem; margin: 0; padding: .5rem; min-width: 0; }
  .item-card a { display: grid; gap: .35rem; color: var(--ink); text-decoration: none; min-width: 0; }
  .item-card a:hover .card-name { text-decoration: underline; }
  .item-card img, .no-photo { width: 100%; aspect-ratio: 4 / 3; object-fit: cover; border-radius: .35rem; background: var(--chip); }
  .no-photo { display: grid; place-items: center; color: var(--area-views); }
  .card-name { font-weight: 600; }
  .branch { list-style: none; margin: 0; padding-left: 1.1rem; border-left: 1px solid var(--line); }
  .branch.top { padding-left: 0; border-left: 0; }
  .branch li { padding: .2rem 0; }
  .place-name { font-weight: 600; }
  .board { display: grid; gap: .75rem; grid-auto-flow: column; grid-auto-columns: minmax(13rem, 1fr); overflow-x: auto; align-items: start; }
  .lane { margin: .5rem 0; }
  .lane h3 { display: flex; justify-content: space-between; margin-bottom: .3rem; }
  .more { margin-top: .5rem; }
</style>
