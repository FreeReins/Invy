// Sticker sheets that labels print on, measured from the makers' templates, and the QR codes drawn on them.
import { encode } from 'uqr';

/**
 * Each layout: the paper, where the first label starts, the size of a label and the gap between columns.
 * Avery 5160 and its copies (30 per sheet on US Letter); Avery L7651 (65 per sheet on A4). A key is also the name
 * of its paper in LabelSheet.svelte (@page), so a new layout needs a line there too.
 */
export const LAYOUTS = [
  { key: 'avery-5160', label: 'Avery 5160, 30 per sheet (US Letter)', page: ['8.5in', '11in'], top: '0.5in', left: '0.1875in',
    columns: 3, rows: 10, width: '2.625in', height: '1in', gap: '0.125in', small: false },
  { key: 'avery-l7651', label: 'Avery L7651, 65 per sheet (A4)', page: ['210mm', '297mm'], top: '10.7mm', left: '4.7mm',
    columns: 5, rows: 13, width: '38.1mm', height: '21.2mm', gap: '2.5mm', small: true },
];

export const perSheet = (layout) => layout.columns * layout.rows;

/** The labels split into sheets of the layout's size. */
export function sheets(labels, layout) {
  const size = perSheet(layout);
  const pages = [];
  for (let start = 0; start < labels.length; start += size) pages.push(labels.slice(start, start + size));
  return pages;
}

/**
 * A QR code for `text` as one SVG path, a unit square per dark module, with its size in modules. Medium error
 * correction, so a scuffed label still reads.
 */
export function qrPath(text) {
  const { data, size } = encode(text, { ecc: 'M', border: 0 });
  let path = '';
  data.forEach((row, y) => row.forEach((dark, x) => { if (dark) path += `M${x} ${y}h1v1h-1z`; }));
  return { path, size };
}
