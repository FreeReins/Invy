<!-- Sheets of QR labels at their real size, for printing on sticker paper: the QR (this Invy's /l/<code> address), the
     code in large type, and the marking or name of what it's on, or a line to write one. On screen they show smaller,
     as a preview; printing shows only the sheets, one per page, with no margins (the layout gives its own). -->
<script>
  import { labelAddress } from '../lib/app.svelte.js';
  import { sheets, qrPath } from '../lib/labelSheets.js';

  /** labels: LabelResponse[]; layout: one of LAYOUTS. */
  let { labels, layout } = $props();

  const pages = $derived(sheets(labels, layout));
  const style = $derived([
    `--page-w: ${layout.page[0]}`, `--page-h: ${layout.page[1]}`, `--top: ${layout.top}`, `--left: ${layout.left}`,
    `--columns: ${layout.columns}`, `--label-w: ${layout.width}`, `--label-h: ${layout.height}`, `--gap: ${layout.gap}`,
  ].join('; '));
  const written = (label) => label.item?.marking ?? label.item?.name ?? null;
</script>

<!-- The preview scrolls, so it takes focus for the keyboard to scroll it. -->
<!-- svelte-ignore a11y_no_noninteractive_tabindex -->
<div class="label-sheets" {style} class:small={layout.small} role="region" aria-label="Print preview" tabindex="0">
  {#each pages as page, index (index)}
    <div class="sheet" role="img" style="page: {layout.key}"
      aria-label="Sheet {index + 1} of {pages.length}: {page.length} labels, {page[0].code} to {page[page.length - 1].code}">
      {#each page as label (label.code)}
        {@const qr = qrPath(labelAddress(label.code))}
        <div class="label" aria-hidden="true">
          <svg class="qr" viewBox="-1 -1 {qr.size + 2} {qr.size + 2}" shape-rendering="crispEdges">
            <rect x="-1" y="-1" width={qr.size + 2} height={qr.size + 2} fill="#fff" /><path d={qr.path} fill="#000" />
          </svg>
          <span class="text">
            <span class="code">{label.code}</span>
            {#if written(label)}<span class="written">{written(label)}</span>{:else}<span class="line"></span>{/if}
          </span>
        </div>
      {/each}
    </div>
  {/each}
</div>

<style>
  .label-sheets { display: grid; gap: 1rem; justify-content: start; }
  .sheet { box-sizing: border-box; width: var(--page-w); height: var(--page-h); padding: var(--top) 0 0 var(--left);
    display: grid; grid-template-columns: repeat(var(--columns), var(--label-w)); grid-auto-rows: var(--label-h);
    column-gap: var(--gap); align-content: start; background: #fff; color: #000; overflow: hidden; }
  .label { box-sizing: border-box; display: flex; align-items: center; gap: .08in; padding: .06in .1in; overflow: hidden; }
  .qr { flex: none; height: 100%; aspect-ratio: 1; }
  .text { display: grid; gap: .04in; min-width: 0; flex: 1 1 auto; }
  .code { font: 700 18pt/1 ui-monospace, "SF Mono", Menlo, Consolas, monospace; letter-spacing: .06em; }
  .written { font: 9pt/1.15 system-ui, sans-serif; overflow: hidden; display: -webkit-box; -webkit-line-clamp: 2; -webkit-box-orient: vertical; }
  .line { display: block; border-bottom: 1px solid #000; height: .16in; }
  .small .label { gap: 1.2mm; padding: 1mm 1.5mm; }
  .small .code { font-size: 10pt; letter-spacing: .03em; }
  .small .written { font-size: 6pt; }
  .small .line { height: 3mm; }

  @media screen {
    /* A preview: smaller, framed like paper. */
    .label-sheets:focus-visible { outline: 2px solid var(--accent); outline-offset: 2px; }
    .label-sheets { overflow: auto; max-width: 100%; max-height: 70vh; padding: .5rem; border-radius: .6rem; background: var(--chip); }
    .sheet { zoom: .55; box-shadow: 0 1px 4px rgb(0 0 0 / .25); }
  }

  /* Each layout's paper, with no printer margins: the layout's own top and left place the labels. The page's
     security policy refuses inline style elements, so every layout's paper is named here (LAYOUTS keys). */
  @page avery-5160 { size: 8.5in 11in; margin: 0; }
  @page avery-l7651 { size: 210mm 297mm; margin: 0; }

  @media print {
    :global(body *) { visibility: hidden; }
    .label-sheets, .label-sheets :global(*) { visibility: visible; }
    .label-sheets { position: absolute; inset: 0 auto auto 0; gap: 0; }
    :global(body) { margin: 0; padding: 0; max-width: none; background: #fff; }
    .sheet { break-after: page; }
    .sheet:last-child { break-after: auto; }
  }
</style>
