// What the stubbed API answers with: one realistic install, so every screen has something on it. Each shape is
// the JSON of the C# record named beside it (src/Invy.Core/Managers/Contracts/Messages/ unless said otherwise);
// keep them in step when a record changes. Every call returns fresh objects, so a test can change its copy freely.

const ids = {
  admin: '00000000-0000-4000-8000-000000000001',
  sam: '00000000-0000-4000-8000-000000000002',
};
export { ids };

/** UserResponse */
export const admin = () => ({
  id: ids.admin, username: 'alex', displayName: 'Alex Rivera', isAdmin: true,
  createdAt: '2026-01-04T10:00:00Z', lockedUntil: null,
});

/** UserResponse */
export const member = () => ({
  id: ids.sam, username: 'sam', displayName: 'Sam Rivera', isAdmin: false,
  createdAt: '2026-02-11T18:30:00Z', lockedUntil: '2099-01-01T00:00:00Z',
});

/** SessionResponse */
export const session = (user = admin()) => ({ setupRequired: false, user });

/** HttpsStatusResponse (src/Invy.Api/Https/) */
export const https = () => ({
  mode: 'SelfSigned', enabled: true, port: 8443, hostnames: ['invy.local'], expiresOn: '2027-11-01T00:00:00Z',
  canRegenerate: true, isSecureNow: false, requireHttps: false,
});

export const security = () => ({ maxFailedSignIns: 5, lockoutMinutes: 15, requireHttps: false, barcodeLookup: false });

/** BackupsResponse */
export const backups = () => ({
  enabled: true, time: '03:00', keepCount: 14, keepDays: 0, lastResult: 'The last backup finished at 3:00 today.',
  files: [
    { name: 'invy-2026-11-20.zip', createdAt: '2026-11-20T03:00:00Z', sizeBytes: 2_400_000 },
    { name: 'invy-2026-11-19.zip', createdAt: '2026-11-19T03:00:00Z', sizeBytes: 800_000 },
  ],
});

/** LogsResponse (src/Invy.Api/Logging/): a long message, a warning and an error with details. */
export const logs = () => ({
  capacity: 1000,
  entries: [
    { sequence: 1, time: '2026-11-20T03:00:00Z', level: 'Information', category: 'Invy.Backups', message: 'Backup made.', exception: null },
    { sequence: 2, time: '2026-11-20T03:05:00Z', level: 'Warning', category: 'Invy.Backups', message: 'The backup folder took 9 seconds to answer.', exception: null },
    { sequence: 3, time: '2026-11-20T03:10:00Z', level: 'Error', category: 'Invy.Notifications', message: 'The webhook failed.', exception: 'System.Net.Http.HttpRequestException: Connection refused' },
    { sequence: 4, time: '2026-11-20T03:15:00Z', level: 'Information', category: 'Invy.Accounts', message: 'Line one\nLine two\nLine three\nLine four\nLine five', exception: null },
  ],
});

/** UserResponse[] */
export const users = () => [admin(), member()];

/** PendingLink[] */
export const links = () => [
  { id: '00000000-0000-4000-8000-000000000501', purpose: 'Invite', username: null, createdAt: '2026-11-18T12:00:00Z', expiresAt: '2026-11-25T12:00:00Z' },
  { id: '00000000-0000-4000-8000-000000000502', purpose: 'PasswordReset', username: 'sam', createdAt: '2026-11-19T12:00:00Z', expiresAt: '2026-11-20T12:00:00Z' },
];

/** LinkDetails */
export const linkDetails = (purpose = 'Invite') => ({ purpose, username: purpose === 'Invite' ? null : 'sam', expiresAt: '2026-11-27T12:00:00Z' });

/** A token just made, with its secret. */
export const issuedApiToken = (name = 'Workshop tablet') => ({
  id: 'a7f0c2d1-0000-4000-8000-000000000003', name,
  token: 'invy_9e359d02660d4d63bdc9fba67942c7e46a4231d31489474d517c20b37ac3631b', createdAt: '2026-11-08T12:00:00Z',
});

export const png = Buffer.from('iVBORw0KGgoAAAANSUhEUgAAAAEAAAABCAYAAAAfFcSJAAAADUlEQVR42mNk+M9QDwADhgGAWjR9awAAAABJRU5ErkJggg==', 'base64');

/** NotificationSettingsResponse */
export const notifications = () => ({ webhookUrl: 'https://ntfy.example.com/invy', dailyTime: '07:30', lastDailyRunOn: '2026-11-20' });

/** BackupSummary */
export const backupSummary = () => ({ users: 2 });

/** InboxResponse */
export const inbox = ({ read = false } = {}) => ({
  entries: [
    { id: '00000000-0000-4000-8000-000000000801', text: 'The nightly backup finished.', source: 'Page', createdAt: '2026-11-22T03:00:00Z', read },
  ],
  unread: read ? 0 : 1,
});

/** AiFeaturesResponse: no AI model is set up, so no task is ready. */
export const aiFeatures = () => ({ ready: [] });

/** AiFeaturesResponse once a model is set up: every task is ready. */
export const aiReady = () => ({ ready: ['ItemsFromPhoto', 'ItemDetailsFromPhoto', 'SuggestTypeFields', 'SuggestType'] });

/** ItemSuggestionsResponse: what the model saw in a photo of Box F. */
export const photoItems = () => ({
  model: 'Claude',
  items: [
    { name: 'Cordless drill', typeId: '00000000-0000-4000-8000-000000000d03', quantity: null, unit: null,
      fields: { '00000000-0000-4000-8000-000000000e02': 'Makita', '00000000-0000-4000-8000-000000000e04': 'Cordless' } },
    { name: 'AA batteries', typeId: null, quantity: 12, unit: null, fields: {} },
    { name: 'Baseball glove', typeId: null, quantity: null, unit: null, fields: {} },
  ],
});

/** DetailSuggestionsResponse: what the model read on the drill's nameplate. */
export const photoDetails = () => ({
  model: 'Claude',
  fields: { '00000000-0000-4000-8000-000000000e02': 'Makita', '00000000-0000-4000-8000-000000000e03': 129.99 },
});

/** FieldSuggestionsResponse for Lego sets. */
export const fieldSuggestions = () => ({
  model: 'Claude',
  fields: [
    { id: null, key: 'set_number', label: 'Set number', kind: 'Text', unit: null, options: [], required: false, sortable: true },
    { id: null, key: 'theme', label: 'Theme', kind: 'Select', unit: null, options: ['City', 'Technic', 'Star Wars'], required: false, sortable: false },
    { id: null, key: 'pieces', label: 'Pieces', kind: 'Number', unit: null, options: [], required: false, sortable: true },
  ],
});

/** AiTaskResponse[]: every task on the default model but the Inbox sweep, which uses Ollama. */
const aiTasks = () => [
  { task: 'ItemsFromPhoto', label: 'Listing things in photos', description: 'Lists what is in a photo of an open box, with types and field values, for you to tick. Needs a model that can see images.', model: null },
  { task: 'ItemDetailsFromPhoto', label: 'Reading labels and nameplates', description: 'Reads brand, model, serial and other fields from a photo of a label or nameplate. Needs a model that can see images.', model: null },
  { task: 'SuggestTypeFields', label: 'Suggesting fields for types', description: 'Suggests the fields a new item type could have, such as set number and pieces for Lego sets.', model: null },
  { task: 'SuggestType', label: 'Suggesting types for the Inbox', description: 'Suggests a type for each item in the Inbox, in the background, for you to accept or ignore.', model: 'Ollama' },
];

/** AiSettingsResponse. */
export const aiSettings = (withModels = true) => (withModels ? {
  models: [
    { id: '00000000-0000-4000-8000-000000000b01', name: 'Claude', provider: 'Anthropic', model: 'claude-opus-5-5', endpoint: '', hasApiKey: true },
    { id: '00000000-0000-4000-8000-000000000b02', name: 'Ollama', provider: 'OpenAiCompatible', model: 'qwen2.5vl', endpoint: 'http://ollama.local:11434/v1', hasApiKey: false },
  ],
  defaultModel: 'Claude',
  tasks: aiTasks(),
} : { models: [], defaultModel: null, tasks: aiTasks().map((task) => ({ ...task, model: null })) });

/** ApiTokenResponse[] */
export const apiTokens = () => [
  { id: 'a7f0c2d1-0000-4000-8000-000000000002', name: 'Claude Code on the laptop', createdAt: '2026-11-02T18:30:00Z' },
  { id: 'a7f0c2d1-0000-4000-8000-000000000001', name: 'Claude Desktop', createdAt: '2026-10-14T09:00:00Z' },
];

const spaceIds = { home: '00000000-0000-4000-8000-000000000c01', shop: '00000000-0000-4000-8000-000000000c02' };
export { spaceIds };

/** SpaceResponse[]: Alex owns Home and can only look in the shop. */
export const spaces = () => [
  { id: spaceIds.home, name: 'Home', preset: 'home', role: 'Owner', createdAt: '2026-11-01T10:00:00Z' },
  { id: spaceIds.shop, name: 'Corner shop', preset: 'business', role: 'Viewer', createdAt: '2026-11-05T10:00:00Z' },
];

/** StarterPackResponse[] */
export const starterPacks = () => [
  { key: 'home', name: 'Home', description: 'Rooms, furniture and boxes, with types for appliances, tools and more.', types: ['Room', 'Furniture', 'Box', 'Tool'] },
  { key: 'hobby', name: 'Hobby', description: 'Bins and drawers for a workshop or craft room.', types: ['Bin', 'Part', 'Kit'] },
  { key: 'business', name: 'Business', description: 'Sites, racks and bins, with stock and equipment.', types: ['Site', 'Rack', 'Product'] },
];

/** ItemTypeResponse[]: a container, a type with every kind of extra (unit, options) and one with no fields. */
export const itemTypes = () => [
  { id: '00000000-0000-4000-8000-000000000d01', name: 'Box', icon: 'box', colour: '#b45309', holdsItems: true, version: 1, fields: [
    { id: '00000000-0000-4000-8000-000000000e01', key: 'size', label: 'Size', kind: 'Select', unit: null, options: ['Small', 'Medium', 'Large'], required: false, sortable: false },
  ] },
  { id: '00000000-0000-4000-8000-000000000d02', name: 'Thing', icon: null, colour: null, holdsItems: false, version: 1, fields: [] },
  { id: '00000000-0000-4000-8000-000000000d03', name: 'Tool', icon: 'wrench', colour: '#dc2626', holdsItems: false, version: 3, fields: [
    { id: '00000000-0000-4000-8000-000000000e02', key: 'brand', label: 'Brand', kind: 'Text', unit: null, options: [], required: true, sortable: true },
    { id: '00000000-0000-4000-8000-000000000e03', key: 'price', label: 'Price', kind: 'Money', unit: 'USD', options: [], required: false, sortable: true },
    { id: '00000000-0000-4000-8000-000000000e04', key: 'power', label: 'Power', kind: 'Select', unit: null, options: ['Hand', 'Corded', 'Cordless'], required: false, sortable: false },
  ] },
];

/** SpaceMemberResponse[] */
export const spaceMembers = () => [
  { userId: ids.admin, username: 'alex', displayName: 'Alex Rivera', role: 'Owner' },
  { userId: ids.sam, username: 'sam', displayName: 'Sam Rivera', role: 'Editor' },
  { userId: '00000000-0000-4000-8000-000000000003', username: 'jo', displayName: 'Jo Park', role: 'Viewer' },
];

const itemIds = {
  shed: '00000000-0000-4000-8000-000000000f01', garage: '00000000-0000-4000-8000-000000000f02',
  abc: '00000000-0000-4000-8000-000000000f03', f: '00000000-0000-4000-8000-000000000f04',
  ball: '00000000-0000-4000-8000-000000000f05', drill: '00000000-0000-4000-8000-000000000f06',
  screws: '00000000-0000-4000-8000-000000000f07', cable: '00000000-0000-4000-8000-000000000f08',
};
export { itemIds };

const boxType = '00000000-0000-4000-8000-000000000d01';
const toolType = '00000000-0000-4000-8000-000000000d03';

/** ItemSummary */
const summary = (id, name, extra = {}) => ({
  id, name, marking: null, typeId: null, holdsItems: false, quantity: null, unit: null, status: 'Active', contents: 0, labelCode: null, ...extra,
});

/** ItemPageResponse: the top of Home. */
export const topItems = () => ({
  items: [
    summary(itemIds.garage, 'Garage', { typeId: boxType, holdsItems: true, contents: 2 }),
    summary(itemIds.shed, 'Shed', { typeId: boxType, holdsItems: true, contents: 1 }),
  ],
  next: null,
});

/** ItemPageResponse: the Inbox. */
export const inboxItems = () => ({ items: [summary(itemIds.cable, 'Mystery cable')], next: null });

/** ItemPageResponse: inside Box F, with a page after it. */
export const boxContents = () => ({
  items: [
    summary(itemIds.ball, 'Baseball'),
    summary(itemIds.screws, 'Screws', { quantity: 185, unit: null }),
    summary(itemIds.drill, 'Drill', { typeId: toolType }),
  ],
  next: 'bmV4dA',
});

/** ItemResponse */
const item = (id, name, extra = {}) => ({
  id, name, marking: null, typeId: null, parentId: null, holdsItems: false, quantity: null, unit: null, fields: {}, tags: [],
  notes: null, status: 'Active', lastSeenAt: null, createdAt: '2026-05-02T12:00:00Z', updatedAt: '2026-05-10T12:00:00Z', version: 2,
  labelCode: null, ...extra,
});

/** ItemDetailResponse for each item the tests open. */
export const itemDetail = (id) => ({
  [itemIds.f]: {
    item: item(itemIds.f, 'Box F', { marking: 'F', typeId: boxType, parentId: itemIds.garage, holdsItems: true, labelCode: '7K3Q',
      fields: { '00000000-0000-4000-8000-000000000e01': 'Large' }, tags: ['sports'] }),
    chain: [{ id: itemIds.garage, name: 'Garage', marking: null }],
    contents: 3,
  },
  [itemIds.drill]: {
    item: item(itemIds.drill, 'Drill', { typeId: toolType, parentId: itemIds.f, notes: 'Charger is in the kitchen drawer.',
      fields: {
        '00000000-0000-4000-8000-000000000e02': 'DeWalt', '00000000-0000-4000-8000-000000000e03': 129.5,
        '00000000-0000-4000-8000-000000000e04': 'Cordless', '00000000-0000-4000-8000-000000000e99': 'Kept from before',
      } }),
    chain: [{ id: itemIds.garage, name: 'Garage', marking: null }, { id: itemIds.f, name: 'Box F', marking: 'F' }],
    contents: 0,
  },
  [itemIds.screws]: {
    item: item(itemIds.screws, 'Screws', { parentId: itemIds.f, quantity: 185, unit: null }),
    chain: [{ id: itemIds.garage, name: 'Garage', marking: null }, { id: itemIds.f, name: 'Box F', marking: 'F' }],
    contents: 0,
  },
}[id]);

const ref = (id, name, marking = null) => ({ id, name, marking });

/** A 1×1 PNG, base64, for thumbnails and photos. */
export const TINY_PNG = 'iVBORw0KGgoAAAANSUhEUgAAAAEAAAABCAYAAAAfFcSJAAAADUlEQVR42mNk+M9QDwADhgGAWjR9awAAAABJRU5ErkJggg==';

/** AttachmentResponse[]: the drill's photo and its manual. */
export const attachments = () => [
  { id: '00000000-0000-4000-8000-000000000b01', itemId: itemIds.drill, kind: 'Photo', name: 'drill.jpg', contentType: 'image/jpeg', bytes: 182000,
    hasThumbnail: true, createdAt: '2026-11-20T09:00:00Z' },
  { id: '00000000-0000-4000-8000-000000000b02', itemId: itemIds.drill, kind: 'Document', name: 'DeWalt manual.pdf', contentType: 'application/pdf',
    bytes: 2400000, hasThumbnail: false, createdAt: '2026-11-20T09:01:00Z' },
];

/** ProductResponse: a drill found from its barcode. */
export const product = () => ({ barcode: '0012345678905', name: '20V cordless drill', brand: 'DeWalt',
  imageUrl: 'https://images.example/drill.jpg', source: 'UPCitemdb' });

/** AuditResponse for Box F: the screws went missing, the baseball turned up again, and the shed's lamp was in there. */
export const auditResult = () => ({
  container: { ...itemDetail(itemIds.f).item, lastSeenAt: '2026-11-23T10:00:00Z' },
  seen: 3,
  missing: [summary(itemIds.screws, 'Screws', { quantity: 185, status: 'Missing' })],
  found: [summary(itemIds.ball, 'Baseball')],
  elsewhere: [summary('00000000-0000-4000-8000-000000000a77', 'Lamp')],
});

/** ItemEventResponse[]: a few of each kind the page words differently. */
export const events = () => [
  { id: '00000000-0000-4000-8000-000000001004', kind: 'Changed', at: '2026-11-20T09:00:00Z', item: ref(itemIds.drill, 'Drill'), actor: 'Alex Rivera',
    from: null, to: null, qtyDelta: null, changes: [{ what: 'Brand', before: 'Makita', after: 'DeWalt' }] },
  { id: '00000000-0000-4000-8000-000000001003', kind: 'QuantityChanged', at: '2026-11-19T09:00:00Z', item: ref(itemIds.screws, 'Screws'), actor: 'Sam Rivera',
    from: null, to: null, qtyDelta: -15, changes: [] },
  { id: '00000000-0000-4000-8000-000000001002', kind: 'Moved', at: '2026-05-10T12:00:00Z', item: ref(itemIds.f, 'Box F', 'F'), actor: 'Alex Rivera',
    from: ref(itemIds.abc, 'Box ABC', 'ABC'), to: ref(itemIds.garage, 'Garage'), qtyDelta: null, changes: [] },
  { id: '00000000-0000-4000-8000-000000001001', kind: 'Created', at: '2026-05-03T12:00:00Z', item: ref(itemIds.ball, 'Baseball'), actor: 'Alex Rivera',
    from: null, to: ref(itemIds.f, 'Box F', 'F'), qtyDelta: null, changes: [] },
];

/** ItemHistoryResponse */
export const itemHistory = () => ({
  events: events(),
  places: [
    { from: '2026-05-10T12:00:00Z', until: null, chain: [ref(itemIds.garage, 'Garage')] },
    { from: '2026-05-02T12:00:00Z', until: '2026-05-10T12:00:00Z', chain: [ref(itemIds.shed, 'Shed'), ref(itemIds.abc, 'Box ABC', 'ABC')] },
  ],
});

/** ActivityResponse */
export const activity = () => ({ events: events(), next: '638000000000000000.00000000000040008000000000001001' });

/** SearchResponse: `count` results named "Part 001"…, in Box F, from `from` on, with a next page while more remain. */
export const searchResults = (count, from = 0, pageSize = 50) => {
  const results = Array.from({ length: Math.min(pageSize, count - from) }, (_, index) => {
    const n = from + index + 1;
    return {
      item: { id: `00000000-0000-4000-8000-${String(700000 + n).padStart(12, '0')}`, name: `Part ${String(n).padStart(3, '0')}`, marking: null,
        typeId: null, holdsItems: false, quantity: n % 3 === 0 ? n : null, unit: null, status: 'Active', contents: 0, value: null, contentValue: 0 },
      chain: [{ id: itemIds.garage, name: 'Garage', marking: null }, { id: itemIds.f, name: 'Box F', marking: 'F' }],
    };
  });
  const next = from + pageSize < count ? String(from + pageSize) : null;
  return { results, total: from === 0 ? count : null, totalIsEstimate: false, next };
};

/** SearchResponse for "baseball": one result, plus a container. */
export const baseballResults = () => ({
  results: [
    { item: { id: itemIds.ball, name: 'Baseball', marking: null, typeId: null, holdsItems: false, quantity: null, unit: null, status: 'Active',
      contents: 0, value: null, contentValue: 0 }, chain: [{ id: itemIds.garage, name: 'Garage', marking: null }, { id: itemIds.f, name: 'Box F', marking: 'F' }] },
    { item: { id: itemIds.f, name: 'Box F', marking: 'F', typeId: boxType, holdsItems: true, quantity: null, unit: null, status: 'Active',
      contents: 3, value: null, contentValue: 129.5 }, chain: [{ id: itemIds.garage, name: 'Garage', marking: null }] },
  ],
  total: 2, totalIsEstimate: false, next: null,
});

/** The labels the tests scan: 7K3Q is on Box F, N3WW and the rest aren't on anything yet. */
export const labelCodes = { onBoxF: '7K3Q', unclaimed: 'N3WW' };

/** LabelResponse */
export const label = (code, onItem = null) => ({
  code, spaceId: spaceIds.home, item: onItem, createdAt: '2026-11-20T10:00:00Z', printedAt: null,
});

/** LabelResponse for a scanned code, or null when Invy has no such label. */
export function labelFor(code) {
  if (code === labelCodes.onBoxF) return label(code, ref(itemIds.f, 'Box F', 'F'));
  return /^[34679ACDEFGHJKMNPQRTUVWXY]{4,6}$/.test(code) && code !== 'XXXX' ? label(code) : null;
}

/** Codes for new labels, as LabelResponse[]: count of them, made from a running number. */
export const newLabels = (count) => Array.from({ length: count }, (_, index) => label(
  [...index.toString(25).padStart(4, '0')].map((digit) => '34679ACDEFGHJKMNPQRTUVWXY'[parseInt(digit, 25)]).join('')));

/** LabelPageResponse: the space's unclaimed labels. */
export const unclaimedLabels = () => ({ labels: newLabels(12), next: null });

/** ItemResponse for a box made by claiming a label. */
export const claimedBox = (code, parentId = null) => item('00000000-0000-4000-8000-000000000f20', `Box ${code}`, {
  holdsItems: true, parentId, labelCode: code, version: 1,
});
