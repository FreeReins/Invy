<!-- Settings, This space: the calendar feed of reminder dates and loans' due dates, for calendar apps to subscribe to.
     Calendar apps can't sign in, so the address carries a secret: everyone in the space sees it, and owners turn it
     on, replace it (the old address stops working) or turn it off. Shown while Reminders or Lending is on. -->
<script>
  import { untrack } from 'svelte';
  import { app, run, toast, ask, isOwner } from '../lib/app.svelte.js';
  import { api } from '../lib/api.js';
  import { pendingClick } from '../lib/pending.js';
  import CalendarIcon from '@lucide/svelte/icons/calendar';

  let feed = $state(null);
  const path = $derived(`/api/spaces/${app.spaceId}/calendar`);

  $effect(() => {
    const id = app.spaceId;
    untrack(() => { feed = null; if (id) run(async () => { feed = await api('GET', path); }); });
  });

  const start = () => run(async () => {
    if (feed?.url) {
      const sure = await ask({
        title: 'Make a new address?', message: 'Calendars subscribed to the old address stop getting updates until they use the new one.',
        confirmLabel: 'New address',
      });
      if (!sure) return;
    }
    feed = await api('POST', path);
    toast('The calendar feed is on. Copy its address into your calendar app.');
  });

  const stop = () => run(async () => {
    feed = await api('DELETE', path);
    toast('The calendar feed is off. Subscribed calendars stop getting updates.');
  });

  const copy = () => run(async () => {
    await navigator.clipboard.writeText(feed.url);
    toast('Address copied.');
  });
</script>

<h3 class="with-icon"><CalendarIcon size={18} aria-hidden="true" />Calendar feed</h3>
<p class="muted hint">Warranty, upkeep and expiry dates, and when loans are due back, in your calendar app. Anyone with the address can read it.</p>
{#if feed}
  {#if feed.url}
    <label>Address to subscribe to <input readonly value={feed.url} onfocus={(event) => event.currentTarget.select()} /></label>
    <div class="actions">
      <button type="button" onclick={pendingClick(copy)}>Copy address</button>
      {#if isOwner()}
        <button type="button" onclick={pendingClick(start)}>New address</button>
        <button type="button" onclick={pendingClick(stop)}>Turn off</button>
      {/if}
    </div>
  {:else if isOwner()}
    <button type="button" onclick={pendingClick(start)}>Turn on the calendar feed</button>
  {:else}
    <p class="muted">The feed is off. An owner of this space can turn it on.</p>
  {/if}
{/if}

<style>
  h3 { margin: 1.25rem 0 .4rem; font-size: 1.05rem; }
  .hint { font-size: .85rem; margin: 0 0 .5rem; }
  .actions { margin-top: .5rem; }
</style>
