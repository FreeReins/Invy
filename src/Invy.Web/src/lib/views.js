// Helpers for saved views (docs/developers/design.md, Views): their displays and columns, a found item's cell in a
// table, and the tree and board groupings. A view's results are SearchResults whose `details` carry field values,
// dates and a photo (ViewManager.RunViewAsync).
import { amount, chainText, display, label } from './items.js';

/** The displays, in the order the form offers them. */
export const DISPLAYS = [
  { key: 'Table', label: 'Table', hint: 'Rows with the columns you choose, sortable.' },
  { key: 'Cards', label: 'Cards', hint: 'Photos first.' },
  { key: 'Tree', label: 'Tree', hint: 'The results under their containers.' },
  { key: 'Board', label: 'Board', hint: 'Columns grouped by a choice field, such as condition.' },
  { key: 'Timeline', label: 'Timeline', hint: 'What happened to the items it finds, newest first.' },
];

export const displayLabel = (key) => DISPLAYS.find((entry) => entry.key === key)?.label ?? key;

/** The columns every table can show; a field's search name is a column too. */
export const BUILT_IN = [
  { key: 'name', label: 'Name' },
  { key: 'type', label: 'Type' },
  { key: 'place', label: 'Place' },
  { key: 'quantity', label: 'Amount' },
  { key: 'value', label: 'Value', feature: 'values' },
  { key: 'seen', label: 'Last seen' },
  { key: 'added', label: 'Added' },
  { key: 'label', label: 'Label' },
];

export const DEFAULT_COLUMNS = ['name', 'type', 'place'];

/** The space's fields by search name; types can share a name, so each holds every field with it. */
export function fieldsByKey(types) {
  const keys = new Map();
  for (const type of types ?? []) {
    for (const field of type.fields) {
      if (!keys.has(field.key)) keys.set(field.key, []);
      keys.get(field.key).push(field);
    }
  }
  return keys;
}

/** The field columns a table can choose: one per search name, labelled by the first field with it. */
export const fieldColumns = (types) => [...fieldsByKey(types)].map(([key, fields]) => ({ key, label: fields[0].label }));

/** The fields a board can group by: choice fields, one per search name. */
export const boardFields = (types) =>
  [...fieldsByKey(types)].filter(([, fields]) => fields.some((field) => field.kind === 'Select')).map(([key, fields]) => ({ key, label: fields[0].label }));

/** A column's heading. */
export function columnLabel(key, types) {
  return BUILT_IN.find((column) => column.key === key)?.label ?? fieldsByKey(types).get(key)?.[0]?.label ?? key;
}

const day = (iso) => new Date(iso).toLocaleDateString(undefined, { dateStyle: 'medium' });
/** A plain sum of money, with no currency: totals add currencies without converting them. */
export const money = (value) => Number(value).toLocaleString(undefined, { minimumFractionDigits: 2, maximumFractionDigits: 2 });

/** The field with this search name that the item has a value for, and the value. */
function fieldValue(result, key, keys) {
  const field = (keys.get(key) ?? []).find((candidate) => result.details?.fields?.[candidate.id] != null);
  return field ? { field, value: result.details.fields[field.id] } : null;
}

/** A table cell as text. `context` is { types, keys: fieldsByKey(types) }. */
export function cell(key, result, { types, keys }) {
  const { item } = result;
  switch (key) {
    case 'name': return label(item);
    case 'type': return types.find((type) => type.id === item.typeId)?.name ?? '';
    case 'place': return chainText(result.chain);
    case 'quantity': return amount(item);
    case 'value': return item.value == null ? '' : money(item.value);
    case 'seen': return result.details?.lastSeenAt ? day(result.details.lastSeenAt) : 'Never';
    case 'added': return result.details ? day(result.details.createdAt) : '';
    case 'label': return item.labelCode ?? '';
    default: {
      const found = fieldValue(result, key, keys);
      return found ? display(found.field, found.value) : '';
    }
  }
}

/** What a column sorts by: a number or a string; empty cells go last. */
export function sortValue(key, result, context) {
  const { item, details } = result;
  switch (key) {
    case 'quantity': return item.quantity == null ? null : Number(item.quantity);
    case 'value': return item.value == null ? null : Number(item.value);
    case 'seen': return details?.lastSeenAt ? Date.parse(details.lastSeenAt) : null;
    case 'added': return details ? Date.parse(details.createdAt) : null;
    default: {
      const found = ['name', 'type', 'place', 'label'].includes(key) ? null : fieldValue(result, key, context.keys);
      if (found && ['Number', 'Money'].includes(found.field.kind)) return Number(found.value);
      const text = cell(key, result, context);
      return text === '' ? null : text;
    }
  }
}

/** Results sorted by a column, ascending or not; empty cells last either way. */
export function sortBy(results, key, ascending, context) {
  const keyed = results.map((result) => ({ result, value: sortValue(key, result, context) }));
  keyed.sort((a, b) => {
    if (a.value == null || b.value == null) return a.value == null ? (b.value == null ? 0 : 1) : -1;
    const order = typeof a.value === 'number' ? a.value - b.value : String(a.value).localeCompare(String(b.value), undefined, { numeric: true });
    return ascending ? order : -order;
  });
  return keyed.map((entry) => entry.result);
}

/**
 * The results under their containers: [{ ref, items, children }] nested as the chains say, top first. Items at the
 * top are in a node whose ref is null.
 */
export function tree(results) {
  const root = { ref: null, items: [], children: new Map() };
  for (const result of results) {
    let node = root;
    for (const ref of result.chain) {
      if (!node.children.has(ref.id)) node.children.set(ref.id, { ref, items: [], children: new Map() });
      node = node.children.get(ref.id);
    }
    node.items.push(result);
  }
  const flatten = (node) => ({ ref: node.ref, items: node.items, children: [...node.children.values()].map(flatten) });
  return flatten(root);
}

/** A board's columns: one per option of the choice field, then one for items without a value. */
export function board(results, key, types) {
  const fields = (fieldsByKey(types).get(key) ?? []).filter((field) => field.kind === 'Select');
  const options = [...new Set(fields.flatMap((field) => field.options))];
  const columns = new Map(options.map((option) => [option, []]));
  const none = [];
  for (const result of results) {
    const field = fields.find((candidate) => result.details?.fields?.[candidate.id] != null);
    const value = field ? result.details.fields[field.id] : null;
    if (value != null && columns.has(value)) columns.get(value).push(result);
    else none.push(result);
  }
  const label = fields[0]?.label ?? key;
  return [...[...columns].map(([option, items]) => ({ title: option, items })), { title: `No ${label.toLowerCase()}`, items: none }];
}

/** A found item's field values as "Brand: DeWalt · Serial: 123", for a report's details column. */
export function fieldsText(result, types) {
  const type = types.find((candidate) => candidate.id === result.item.typeId);
  if (!type || !result.details) return '';
  return type.fields
    .filter((field) => field.kind !== 'Money' && result.details.fields[field.id] != null && result.details.fields[field.id] !== '')
    .map((field) => `${field.label}: ${display(field, result.details.fields[field.id])}`)
    .join(' · ');
}
