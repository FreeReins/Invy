// Small helpers for showing items, their field values and their timeline. Field kinds are listed in lib/fields.js.
import { kindLabel } from './fields.js';
import { parseDate } from './format.js';

/** "3.5 kg", "200", or '' for a unique item. */
export function amount(item) {
  if (item?.quantity == null) return '';
  const number = Number(item.quantity).toLocaleString(undefined, { maximumFractionDigits: 3 });
  return item.unit ? `${number} ${item.unit}` : number;
}

/** A field value as text, the way the item page shows it. */
export function display(field, value) {
  if (value == null) return '';
  switch (field?.kind) {
    case 'Bool': return value ? 'Yes' : 'No';
    case 'Date': return parseDate(value).toLocaleDateString(undefined, { dateStyle: 'medium' });
    case 'Money': return `${Number(value).toLocaleString(undefined, { minimumFractionDigits: 2, maximumFractionDigits: 2 })}${field.unit ? ` ${field.unit}` : ''}`;
    case 'Number': return `${Number(value).toLocaleString()}${field.unit ? ` ${field.unit}` : ''}`;
    case 'MultiSelect': return value.join(', ');
    default: return Array.isArray(value) ? value.join(', ') : String(value);
  }
}

/** "Shed › Box ABC › Box F", or "the top" for an empty chain. */
export const chainText = (chain) => (chain.length ? chain.map((place) => place.name).join(' › ') : 'the top');

/** An item's name with its marking: "Box (ABC)". */
export const label = (item) => (item.marking && !item.name.includes(item.marking) ? `${item.name} (${item.marking})` : item.name);

/** What a field kind is called, for the Other fields list. */
export { kindLabel };

const quantity = (delta) => Number(Math.abs(delta)).toLocaleString(undefined, { maximumFractionDigits: 3 });

/**
 * The parts of a timeline sentence, so links can go between them: [{ text }, { item }]. "Owen moved Box F from Box ABC
 * to Garage".
 */
export function sentence(event) {
  const who = { text: `${event.actor ?? 'Invy'} ` };
  const it = { item: event.item };
  const place = (ref) => (ref ? { item: ref } : { text: 'the top' });
  switch (event.kind) {
    case 'Created': return [who, { text: 'added ' }, it, ...(event.to ? [{ text: ' to ' }, { item: event.to }] : [])];
    case 'Moved': return [who, { text: 'moved ' }, it, { text: ' from ' }, place(event.from), { text: ' to ' }, place(event.to)];
    case 'Changed': return [who, { text: 'changed ' }, it];
    case 'QuantityChanged':
      return event.qtyDelta >= 0
        ? [who, { text: `added ${quantity(event.qtyDelta)} to ` }, it]
        : [who, { text: `took ${quantity(event.qtyDelta)} from ` }, it];
    case 'Disposed': return [who, { text: 'disposed of ' }, it];
    case 'Restored': return [who, { text: 'restored ' }, it];
    case 'Seen': return [who, { text: 'saw ' }, it];
    case 'Lent': return [who, { text: 'lent out ' }, it];
    case 'Returned': return [who, { text: 'got back ' }, it];
    case 'PhotoAdded': return [who, { text: 'added a photo of ' }, it];
    case 'Missing': return [who, { text: 'marked missing ' }, it];
    case 'Found': return [who, { text: 'found ' }, it];
    default: return [who, { text: `${event.kind} ` }, it];
  }
}

/** One change of a Changed event: "Brand: Makita → DeWalt". */
export const changeText = (change) =>
  `${change.what}: ${change.before ?? '(empty)'} → ${change.after ?? '(empty)'}`;
