<!-- The camera, and a box to type a code into for when there's no camera or the code won't read. Each code read or
     typed goes to onscan(text), whatever it is: a label's address, a label code or a product barcode. -->
<script>
  import { onDestroy } from 'svelte';
  import { run } from '../lib/app.svelte.js';
  import { canUseCamera, startScanner } from '../lib/scanner.js';
  import Camera from '@lucide/svelte/icons/camera';
  import CameraOff from '@lucide/svelte/icons/camera-off';
  import ArrowRight from '@lucide/svelte/icons/arrow-right';

  /** onscan(text); prompt: what to scan, read out with the camera; typeLabel: the typed code's label. */
  let { onscan, prompt = 'Point the camera at a label', typeLabel = 'Or type the code' } = $props();

  let video = $state();
  let stop = $state(null);
  let starting = $state(false);
  let typed = $state('');
  const camera = canUseCamera();

  const start = () => run(async () => {
    starting = true;
    try {
      stop = await startScanner(video, (text) => onscan(text));
    } finally {
      starting = false;
    }
  });

  function stopCamera() {
    stop?.();
    stop = null;
  }

  function submit(event) {
    event.preventDefault();
    const text = typed.trim();
    if (!text) return;
    typed = '';
    onscan(text);
  }

  onDestroy(stopCamera);
</script>

<div class="scanner">
  <div class="view" class:live={stop}>
    <!-- The camera's picture has no sound to caption. -->
    <!-- svelte-ignore a11y_media_has_caption -->
    <video bind:this={video} muted playsinline aria-label={prompt}></video>
    {#if stop}
      <span class="frame" aria-hidden="true"></span>
    {:else}
      <div class="idle">
        <Camera size={30} aria-hidden="true" />
        {#if camera}
          <button type="button" class="primary with-icon" onclick={start} disabled={starting}>
            <Camera size={16} aria-hidden="true" />{starting ? 'Starting the camera…' : 'Start the camera'}</button>
        {:else}
          <p class="muted">The camera needs a secure connection (HTTPS). Type the code below, or turn on HTTPS in Settings.</p>
        {/if}
      </div>
    {/if}
  </div>
  {#if stop}
    <p class="hint"><span class="muted">{prompt}.</span>
      <button type="button" class="small with-icon" onclick={stopCamera}><CameraOff size={14} aria-hidden="true" />Stop the camera</button></p>
  {/if}

  <form class="typed" onsubmit={submit}>
    <label>
      <span>{typeLabel}</span>
      <input bind:value={typed} autocomplete="off" autocapitalize="characters" spellcheck="false" placeholder="7K3Q" />
    </label>
    <button type="submit" class="with-icon"><ArrowRight size={16} aria-hidden="true" />Go</button>
  </form>
</div>

<style>
  .scanner { max-width: 34rem; }
  .view { position: relative; display: grid; place-items: center; aspect-ratio: 4 / 3; max-height: 55vh; border-radius: .75rem;
    overflow: hidden; background: color-mix(in srgb, var(--area-scan) 10%, var(--panel)); border: 1px solid var(--line); }
  video { position: absolute; inset: 0; width: 100%; height: 100%; object-fit: cover; opacity: 0; }
  .live video { opacity: 1; }
  .idle { position: relative; display: grid; justify-items: center; gap: .75rem; padding: 1rem; text-align: center; color: var(--area-scan); }
  .idle p { margin: 0; max-width: 24rem; }
  /* Where to hold the code: a square in the middle. */
  .frame { position: absolute; width: 55%; aspect-ratio: 1; border: 3px solid #fff; border-radius: .75rem;
    box-shadow: 0 0 0 100vmax rgb(0 0 0 / .25); }
  .hint { display: flex; flex-wrap: wrap; align-items: center; justify-content: space-between; gap: .5rem; margin: .5rem 0 0; }
  .typed { display: flex; gap: .5rem; align-items: end; margin-top: .75rem; }
  .typed label { flex: 1 1 auto; min-width: 0; }
  .typed input { width: 100%; box-sizing: border-box; font-family: var(--mono, ui-monospace, monospace); letter-spacing: .08em; }
</style>
