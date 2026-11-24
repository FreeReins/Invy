<!-- The Find tab, Invy's start screen: one search box (docs/developers/design.md, Search), searching 150 ms after the
     last key, with the results in a virtual list. With the box empty it shows what changed most recently. The search
     is kept in the address (/find?q=...), so it can be bookmarked. Above the list, while not searching, is the
     getting-started card (components/GettingStarted.svelte). -->
<script>
  import { untrack } from 'svelte';
  import { app, run, currentSpace, kindHint } from '../lib/app.svelte.js';
  import { api } from '../lib/api.js';
  import { amount, label, chainText } from '../lib/items.js';
  import { plural } from '../lib/format.js';
  import NoSpace from '../components/NoSpace.svelte';
  import HelpLink from '../components/HelpLink.svelte';
  import VirtualList from '../components/VirtualList.svelte';
  import GettingStarted from '../components/GettingStarted.svelte';
  import PinnedViews from '../components/PinnedViews.svelte';
  import Search from '@lucide/svelte/icons/search';
  import Box from '@lucide/svelte/icons/box';
  import Package from '@lucide/svelte/icons/package';

  const DELAY = 150;

  let text = $state(new URLSearchParams(location.search).get('q') ?? '');
  let sort = $state('');
  let results = $state(null);
  let total = $state(null);
  let estimate = $state(false);
  let next = $state(null);
  let problem = $state('');
  let loadingMore = false;
  let asked = 0;
  let timer;

  const space = $derived(currentSpace());
  const searching = $derived(text.trim().length > 0);

  /** Runs the search for the box as it is now. Answers to older searches that arrive late are ignored. */
  function search() {
    const id = app.spaceId;
    if (!id) return;
    const mine = ++asked;
    const q = text.trim();
    const params = new URLSearchParams(q ? { q } : { sort: 'recent' });
    if (q && sort) params.set('sort', sort);
    history.replaceState(history.state, '', q ? `/find?${new URLSearchParams({ q })}` : '/find');
    return run(async () => {
      try {
        const page = await api('GET', `/api/spaces/${id}/search?${params}`);
        if (mine !== asked) return;
        results = page.results;
        total = page.total;
        estimate = page.totalIsEstimate;
        next = page.next;
        problem = '';
      } catch (error) {
        if (mine !== asked) return;
        // A search that can't be read yet (often half typed) is explained under the box, not as an error.
        if (error.status === 400) problem = error.message; else throw error;
      }
    });
  }

  function typed() {
    clearTimeout(timer);
    timer = setTimeout(search, DELAY);
  }

  async function more() {
    if (!next || loadingMore) return;
    loadingMore = true;
    const id = app.spaceId;
    const q = text.trim();
    const params = new URLSearchParams(q ? { q, after: next } : { sort: 'recent', after: next });
    if (q && sort) params.set('sort', sort);
    await run(async () => {
      const page = await api('GET', `/api/spaces/${id}/search?${params}`);
      results = [...results, ...page.results];
      next = page.next;
    });
    loadingMore = false;
  }

  // Searches again for another space. run() reads app.busy, which mustn't count.
  $effect(() => {
    void app.spaceId;
    untrack(() => { results = null; search(); });
  });

  const heading = $derived(searching
    ? total == null ? 'Searching…' : `${estimate ? 'About ' : ''}${plural(total, 'item').replace(/^\d+/, (n) => Number(n).toLocaleString())} found`
    : 'Recently changed');
</script>

{#if !space}
  <NoSpace area="var(--area-find)" />
{:else}
  <section class="find">
    <form role="search" class="box" onsubmit={(event) => { event.preventDefault(); clearTimeout(timer); search(); }}>
      <label class="sr-only" for="find-text">Search {space.name}</label>
      <span class="icon" aria-hidden="true"><Search size={20} /></span>
      <input id="find-text" type="search" bind:value={text} oninput={typed} placeholder="Search {space.name}: baseball, in:shed, type:tool"
        autocomplete="off" spellcheck="false" aria-describedby="find-hint" />
      {#if searching}
        <label class="sr-only" for="find-sort">Sort</label>
        <select id="find-sort" bind:value={sort} onchange={search}>
          <option value="">Best match</option>
          <option value="name">Name</option>
          <option value="recent">Recently changed</option>
        </select>
      {/if}
    </form>
    <p id="find-hint" class="hint" class:bad-text={problem} class:muted={!problem} aria-live="polite">
      {problem || 'Try words, type:tool, in:"Box F", brand:dewalt, price>50, qty<5, tag:garden, is:container or added<30d.'}
    </p>

    {#if !searching}<GettingStarted onexamples={search} /><PinnedViews />{/if}

    {#if results === null}
      <div class="card"><span class="skeleton line"></span><span class="skeleton line"></span><span class="skeleton line"></span></div>
    {:else if results.length === 0}
      <div class="card empty">
        <span class="empty-icon" style="--area: var(--area-find)" aria-hidden="true"><Search size={26} /></span>
        <h2>{searching ? 'Nothing found' : 'Nothing here yet'}</h2>
        <p class="muted">{searching
          ? 'Check the spelling, or try fewer words. Disposed things show with is:disposed.'
          : kindHint('find', 'Add places and things on the Browse tab, and they show up here.')}</p>
        <p class="muted"><HelpLink page="using/search">How to search</HelpLink></p>
      </div>
    {:else}
      <div class="row count-row">
        <h2 class="count" aria-live="polite">{heading}</h2>
        {#if searching}<a class="save" href="/views?{new URLSearchParams({ new: text.trim() })}">Save as a view</a>{/if}
      </div>
      <div class="card results">
        <VirtualList items={results} key={(result) => result.item.id} rowHeight={64} total={searching ? total : null}
          onend={next ? more : null} name={searching ? 'Search results' : 'Recently changed'}>
          {#snippet row(result)}
            <a href="/browse/{result.item.id}" class="result">
              <span class="icon" aria-hidden="true">{#if result.item.holdsItems}<Box size={18} />{:else}<Package size={18} />{/if}</span>
              <span class="text">
                <span class="clip name">{label(result.item)}</span>
                <span class="clip muted where">{result.chain.length ? `in ${chainText(result.chain)}` : 'At the top'}</span>
              </span>
              <span class="muted extra">{result.item.holdsItems ? plural(result.item.contents, 'item') : amount(result.item)}</span>
            </a>
          {/snippet}
        </VirtualList>
      </div>
    {/if}
  </section>
{/if}

<style>
  .find { margin-top: 1.25rem; }
  .box { display: flex; align-items: center; gap: .5rem; padding: .35rem .6rem; border: 1px solid var(--line); border-radius: .6rem;
    background: var(--panel); }
  .box:focus-within { outline: 2px solid var(--accent); outline-offset: 2px; }
  .box input { flex: 1 1 auto; min-width: 0; border: 0; background: none; font-size: 1.05rem; padding: .45rem 0; }
  .box input:focus-visible { outline: none; }
  .box .icon { display: inline-flex; color: var(--area-find); }
  .box select { flex: none; }
  .hint { margin: .4rem 0 1rem; min-height: 1.2em; }
  .count { font-size: 1rem; margin: 0 0 .4rem; }
  .count-row { align-items: baseline; }
  .count-row > :first-child { flex: 1 1 auto; }
  .save { font-size: .9rem; }
  .empty h2 { margin: .25rem 0 .4rem; }
  .results { padding-top: 0; padding-bottom: 0; }
  .result { display: flex; align-items: center; gap: .7rem; height: 100%; padding: 0 .25rem; color: var(--ink); text-decoration: none; min-width: 0; }
  .result:hover { background: color-mix(in srgb, var(--area-find) 8%, transparent); }
  .result:focus-visible { outline: 2px solid var(--accent); outline-offset: -2px; }
  .result .icon { display: inline-flex; color: var(--area-find); flex: none; }
  .text { display: grid; flex: 1 1 auto; min-width: 0; }
  .where { font-size: .85rem; }
  .extra { flex: none; font-size: .85rem; }
</style>
