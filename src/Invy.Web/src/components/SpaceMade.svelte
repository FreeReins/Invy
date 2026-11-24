<!-- Shown on a setup step for the first space once that space is made: what was made, and the way to Settings,
     where its name, kind and features can change. -->
<script>
  import { finishSetup } from '../lib/app.svelte.js';
  import { plural } from '../lib/format.js';

  /** space: the SpaceResponse setup made. */
  let { space } = $props();

  function openSettings(event) {
    event.preventDefault();
    return finishSetup('/settings/space');
  }
</script>

<div class="card">
  <strong>{space.name}</strong>
  <p class="muted">Made, with {plural(space.features.length, 'feature')} switched on. Change its name, kind and features
    in <a href="/settings/space" onclick={openSettings}>Settings</a>.</p>
</div>
