<!-- A long list that draws only the rows near the window, so thousands of results scroll smoothly. Rows have one fixed
     height. It follows the page's own scrolling, and asks for more (onend) when the last rows come into view. -->
<script>
  /** items: the rows. key(item): a unique key. rowHeight: px. total: the full count for screen readers, or null.
      onend: called when near the end, or null. name: the list's label. row: snippet(item, index). */
  let { items, key, rowHeight = 64, total = null, onend = null, name, row } = $props();

  let list = $state();
  let scrollY = $state(0);
  let innerHeight = $state(800);
  let top = $state(0);

  // Where the list starts on the page, measured again when anything above it may have moved.
  $effect(() => {
    void items.length;
    void innerHeight;
    if (list) top = list.getBoundingClientRect().top + window.scrollY;
  });

  const BUFFER = 8;
  const start = $derived(Math.max(0, Math.floor((scrollY - top) / rowHeight) - BUFFER));
  const end = $derived(Math.min(items.length, Math.ceil((scrollY - top + innerHeight) / rowHeight) + BUFFER));
  const shown = $derived(items.slice(start, end));

  $effect(() => {
    if (onend && end >= items.length - 3 && items.length > 0) onend();
  });
</script>

<svelte:window bind:scrollY bind:innerHeight />

<ul bind:this={list} class="virtual" aria-label={name} style="height: {items.length * rowHeight}px">
  {#each shown as item, index (key(item))}
    <li style="top: {(start + index) * rowHeight}px; height: {rowHeight}px" aria-setsize={total ?? -1} aria-posinset={start + index + 1}>
      {@render row(item, start + index)}
    </li>
  {/each}
</ul>

<style>
  .virtual { position: relative; list-style: none; margin: 0; padding: 0; }
  .virtual li { position: absolute; left: 0; right: 0; box-sizing: border-box; border-top: 1px solid var(--line); }
  .virtual li[aria-posinset="1"] { border-top: 0; }
</style>
