<!-- Shown on a space-owned tab while the person is in no space yet: what a space is, and the button to make one. -->
<script>
  import { app } from '../lib/app.svelte.js';
  import HelpLink from './HelpLink.svelte';
  import HousePlus from '@lucide/svelte/icons/house-plus';

  /** area: the tab's colour, such as var(--area-types). */
  let { area } = $props();
</script>

<section class="card empty no-space">
  <span class="empty-icon" style="--area: {area}" aria-hidden="true"><HousePlus size={26} /></span>
  <h2>Make your first space</h2>
  <p class="muted">A space holds your items, their types and the people who share them, such as Home or Workshop.
    If someone has already made one, ask them to add you to it.</p>
  <div class="actions centre">
    <button type="button" class="primary" onclick={() => { app.newSpaceOpen = true; }}>New space</button>
  </div>
  <p class="muted"><HelpLink page="using/spaces">How spaces work</HelpLink></p>
</section>

<style>
  .no-space { margin-top: 1.25rem; }
  .no-space h2 { margin: .25rem 0 .4rem; }
  .centre { justify-content: center; margin: .9rem 0 .6rem; }
</style>
