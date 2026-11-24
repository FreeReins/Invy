<!-- The Views tab's built-in lists for the space features, each at its own address and each shown only while its
     feature is on: what's running low, what's lent out, reminder dates, boxes in a move, and projects. -->
<script>
  import { app, canEdit } from '../lib/app.svelte.js';
  import { api } from '../lib/api.js';
  import { run, toast } from '../lib/app.svelte.js';
  import { pendingClick } from '../lib/pending.js';
  import { amount, label, display } from '../lib/items.js';
  import { has } from '../lib/features.js';
  import { todayIso, plural } from '../lib/format.js';
  import FeatureList from './FeatureList.svelte';
  import PackageMinus from '@lucide/svelte/icons/package-minus';
  import Handshake from '@lucide/svelte/icons/handshake';
  import Bell from '@lucide/svelte/icons/bell';
  import Truck from '@lucide/svelte/icons/truck';

  /** which: 'low-stock', 'loans', 'reminders' or 'moving'. */
  let { which } = $props();

  const giveBack = (item, reload) => run(async () => {
    await api('POST', `/api/spaces/${app.spaceId}/items/${item.id}/return`);
    toast(`${item.name} is back.`);
    await reload();
  });
  const markDone = (date, reload) => run(async () => {
    await api('POST', `/api/spaces/${app.spaceId}/items/${date.item.id}/upkeep-done`, { fieldId: date.fieldId });
    toast(`${date.field} for ${date.item.name} moved on.`);
    await reload();
  });
  const arrived = (item, reload) => run(async () => {
    await api('POST', `/api/spaces/${app.spaceId}/items/${item.id}/arrived`);
    toast(`${item.name} has arrived.`);
    await reload();
  });
  const when = (days) => (days === 0 ? 'today' : days < 0 ? `${plural(-days, 'day')} ago` : `in ${plural(days, 'day')}`);
</script>

{#if which === 'low-stock' && has('stock')}
  <FeatureList title="Running low" about="Counted things at or below their low-stock level, after what projects have set aside. Set a level on a thing's page."
    path="low-stock" rows={(answer) => answer.items} icon={PackageMinus}
    empty={{ title: 'Nothing is running low', text: 'Give something you keep counted a low-stock level, and it shows here when it gets there.' }}>
    {#snippet row(item)}
      <a href="/browse/{item.id}">{label(item)}</a><span class="muted">{amount(item)} left</span>
    {/snippet}
  </FeatureList>
{:else if which === 'loans' && has('lending')}
  <FeatureList title="Lent out" about="What's lent out, who or where has it, and when it's due back, soonest first."
    path="loans" rows={(answer) => answer.items} icon={Handshake}
    empty={{ title: 'Nothing is lent out', text: 'Lend something from its page, and it shows here until it comes back.' }}>
    {#snippet row(item, reload)}
      <span><a href="/browse/{item.id}">{label(item)}</a> <span class="muted">with {item.lentTo}{item.dueOn ? `, due ${display({ kind: 'Date' }, item.dueOn)}` : ''}</span>
        {#if item.dueOn && item.dueOn < todayIso()}<span class="chip warn">Overdue</span>{/if}</span>
      {#if canEdit()}<button type="button" class="small no-print" onclick={pendingClick(() => giveBack(item, reload))}>It's back</button>{/if}
    {/snippet}
  </FeatureList>
{:else if which === 'reminders' && has('reminders')}
  <FeatureList title="Coming up" about="Warranty, upkeep and other reminder dates in the next 30 days, and upkeep that's overdue."
    path="reminders" rows={(answer) => answer.dates} icon={Bell}
    empty={{ title: 'Nothing coming up', text: 'Fill in a date field that reminds you, such as Warranty until, and it shows here as it gets near.' }}>
    {#snippet row(date, reload)}
      <span><a href="/browse/{date.item.id}">{label(date.item)}</a> <span class="muted">{date.field} {display({ kind: 'Date' }, date.on)}, {when(date.days)}</span>
        {#if date.days < 0}<span class="chip warn">Overdue</span>{/if}</span>
      {#if date.upkeep && canEdit()}<button type="button" class="small no-print" onclick={pendingClick(() => markDone(date, reload))}>Done today</button>{/if}
    {/snippet}
  </FeatureList>
{:else if which === 'moving' && has('moving')}
  <FeatureList title="Moving" about="Boxes with a destination. Print each box's manifest, and mark it arrived to move it there."
    path="moving" rows={(answer) => answer.items} icon={Truck}
    empty={{ title: 'No boxes are going anywhere', text: 'Open a box on Browse and say where it’s going.' }}>
    {#snippet row(item, reload)}
      <span><a href="/browse/{item.id}">{label(item)}</a> <span class="muted">{plural(item.contents, 'item')}</span></span>
      <span class="actions no-print">
        <a class="button small" href="/views/manifest?box={item.id}">Manifest</a>
        {#if canEdit()}<button type="button" class="small" onclick={pendingClick(() => arrived(item, reload))}>Arrived</button>{/if}
      </span>
    {/snippet}
  </FeatureList>
{:else}
  <section class="report card empty">
    <h2>That list is switched off</h2>
    <p class="muted">Its feature is off for this space. An owner can switch it on in Settings, This space. <a href="/views">All views</a></p>
  </section>
{/if}

<style>
  .report { margin-top: 1.25rem; }
</style>
