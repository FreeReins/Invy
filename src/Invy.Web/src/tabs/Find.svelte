<!-- The Find tab, Invy's start screen: the search box, recent items and pinned views (docs/developers/design.md, Web
     screens). Until phase 1 brings items there is nothing to search, so it shows only an empty state. -->
<script>
  import HelpLink from '../components/HelpLink.svelte';
  import Search from '@lucide/svelte/icons/search';
</script>

<section class="card empty find">
  <span class="empty-icon" style="--area: var(--area-find)" aria-hidden="true"><Search size={26} /></span>
  <h2>Nothing to find yet</h2>
  <p class="muted">Search, recent items and pinned views will show here once Invy can hold your things.</p>
  <p class="muted"><HelpLink page="">Read the guide to see what's coming</HelpLink></p>
</section>

<style>
  .find { margin-top: 1.25rem; }
  .find h2 { margin: .25rem 0 .4rem; }
  .find p + p { margin-top: .6rem; }
</style>
