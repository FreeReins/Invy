<!-- On the Views tab: a card for each built-in list of a feature the space has on (low stock, loans, reminders,
     moving, projects). Nothing shows while all of them are off. -->
<script>
  import { has } from '../lib/features.js';
  import PackageMinus from '@lucide/svelte/icons/package-minus';
  import Handshake from '@lucide/svelte/icons/handshake';
  import Bell from '@lucide/svelte/icons/bell';
  import Truck from '@lucide/svelte/icons/truck';
  import Hammer from '@lucide/svelte/icons/hammer';

  const LISTS = [
    { feature: 'stock', href: '/views/low-stock', name: 'Running low', about: 'Counted things at or below their low-stock level.', icon: PackageMinus },
    { feature: 'lending', href: '/views/loans', name: 'Lent out', about: 'What’s lent out, to whom, and when it’s due back.', icon: Handshake },
    { feature: 'reminders', href: '/views/reminders', name: 'Coming up', about: 'Warranty, upkeep and expiry dates in the next 30 days.', icon: Bell },
    { feature: 'moving', href: '/views/moving', name: 'Moving', about: 'Boxes with a destination, their manifests, and arriving.', icon: Truck },
    { feature: 'projects', href: '/views/projects', name: 'Projects', about: 'What each project sets aside, to use up or release.', icon: Hammer },
  ];
  const shown = $derived(LISTS.filter((list) => has(list.feature)));
</script>

{#if shown.length}
  <h2 class="lists-title">Lists</h2>
  <ul class="lists grid">
    {#each shown as list (list.href)}
      {@const Icon = list.icon}
      <li class="card">
        <a href={list.href} class="with-icon list-name"><Icon size={18} aria-hidden="true" />{list.name}</a>
        <p class="muted">{list.about}</p>
      </li>
    {/each}
  </ul>
{/if}

<style>
  .lists-title { margin-top: 1.75rem; }
  .lists { list-style: none; padding: 0; margin: .5rem 0; }
  .lists li { margin: 0; display: grid; gap: .3rem; align-content: start; }
  .lists p { margin: 0; }
  .list-name { font-weight: 600; }
  .list-name :global(svg) { color: var(--area-views); }
</style>
