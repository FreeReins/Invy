// Field kinds of an item type (FieldKind in Invy.Core) as the page names them, and which of them can have options, a
// unit, or be sorted on. Keep in step with ValidationEngine.Types.cs.

export const KINDS = [
  { kind: 'Text', label: 'Text' },
  { kind: 'LongText', label: 'Long text' },
  { kind: 'Number', label: 'Number' },
  { kind: 'Money', label: 'Money' },
  { kind: 'Date', label: 'Date' },
  { kind: 'Bool', label: 'Yes or no' },
  { kind: 'Select', label: 'One choice' },
  { kind: 'MultiSelect', label: 'Several choices' },
  { kind: 'Url', label: 'Web address' },
  { kind: 'Barcode', label: 'Barcode' },
  { kind: 'Person', label: 'Person' },
  { kind: 'Item', label: 'Another item' },
];

export const kindLabel = (kind) => KINDS.find((entry) => entry.kind === kind)?.label ?? kind;
export const hasOptions = (kind) => kind === 'Select' || kind === 'MultiSelect';
export const hasUnit = (kind) => kind === 'Number' || kind === 'Money';
export const canSort = (kind) => !['LongText', 'MultiSelect', 'Item'].includes(kind);

/** The search name a label suggests: "Best before" becomes best_before. */
export const keyFor = (label) => label.toLowerCase().normalize('NFKD').replace(/[^a-z0-9]+/g, '_').replace(/^[^a-z]+|_+$/g, '').slice(0, 40);

/** Colours offered for a type, from the starter packs; null is the default. */
export const COLOURS = [
  { value: '#64748b', name: 'Slate' }, { value: '#a16207', name: 'Brown' }, { value: '#b45309', name: 'Amber' },
  { value: '#dc2626', name: 'Red' }, { value: '#db2777', name: 'Pink' }, { value: '#7c3aed', name: 'Purple' },
  { value: '#0284c7', name: 'Blue' }, { value: '#059669', name: 'Green' }, { value: '#ca8a04', name: 'Yellow' },
];
