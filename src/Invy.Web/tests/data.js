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
  expiresAt: '2027-02-06T12:00:00Z', readOnly: false,
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
  { id: 'a7f0c2d1-0000-4000-8000-000000000002', name: 'Claude Code on the laptop', createdAt: '2026-11-02T18:30:00Z',
    expiresAt: '2027-01-31T18:30:00Z', lastUsedAt: '2026-11-19T08:15:00Z', readOnly: false },
  { id: 'a7f0c2d1-0000-4000-8000-000000000001', name: 'Claude Desktop', createdAt: '2026-10-14T09:00:00Z',
    expiresAt: null, lastUsedAt: null, readOnly: true },
];

const spaceIds = { home: '00000000-0000-4000-8000-000000000c01', shop: '00000000-0000-4000-8000-000000000c02' };
export { spaceIds };

/** SpaceResponse[]: Alex owns Home and can only look in the shop. */
export const spaces = () => [
  { id: spaceIds.home, name: 'Home', preset: 'home', role: 'Owner', createdAt: '2026-11-01T10:00:00Z', features: ['values', 'audits', 'barcodes', 'lending'] },
  { id: spaceIds.shop, name: 'Corner shop', preset: 'business', role: 'Viewer', createdAt: '2026-11-05T10:00:00Z', features: ['values', 'audits', 'barcodes', 'stock'] },
];

/** The spaces, with only these features on in each. */
export const spacesWith = (features) => spaces().map((space) => ({ ...space, features }));

/** GettingStartedResponse: a home's card with two of five steps done and two examples left. */
export const gettingStarted = (extra = {}) => ({
  steps: [
    { key: 'places', title: 'Add your places', description: 'Add the rooms, shelves or cupboards things live in.', done: true },
    { key: 'labels', title: 'Print labels', description: 'Print QR labels to stick on boxes, so a scan opens them.', done: true },
    { key: 'pack', title: 'Put something away', description: 'Add something inside a box or a place.', done: false },
    { key: 'photo', title: 'Try a photo', description: "Add a photo to an item, or let AI list what's in a photo of a box.", done: false },
    { key: 'invite', title: 'Invite someone', description: 'Add someone who shares the space with you.', done: false },
  ],
  examplesLeft: 2, dismissed: false, ...extra,
});

/** SpaceFeatureResponse[]: the catalogue in Utilities/SpaceFeatures.cs, every feature built. */
export const spaceFeatures = () => [
  { key: 'values', name: 'Values', description: 'Add up what things are worth, in each box and across the space.', available: true },
  { key: 'audits', name: 'Audits', description: "Walk round with your phone and tick off what's where.", available: true },
  { key: 'barcodes', name: 'Barcodes', description: "Fill in a product's details from its barcode when you add it.", available: true },
  { key: 'stock', name: 'Stock', description: 'Get told when something you keep counted is running low.', available: true },
  { key: 'lending', name: 'Lending', description: "Lend things to people and get reminded when they're due back.", available: true },
  { key: 'reminders', name: 'Reminders', description: 'Get reminded of warranties, upkeep and expiry dates.', available: true },
  { key: 'moving', name: 'Moving', description: "Pack boxes for a move, with where each one goes and what's in it.", available: true },
  { key: 'projects', name: 'Projects', description: 'Set things aside or use them up for a project.', available: true },
];

/** StarterPackResponse[]: the packs in config/types-*.json as Invy sends them. Keep in step when a pack changes. */
export const starterPacks = () => [
  {"key": "home", "name": "Home", "description": "Rooms, furniture and boxes, with types for appliances, tools, electronics, clothes, books, documents and the pantry.", "types": ["Room", "Furniture", "Box", "Appliance", "Tool", "Electronics", "Clothing", "Book", "Document", "Pantry item"], "icon": "house", "features": ["values", "barcodes", "lending", "reminders"], "places": {"type": "Room", "suggest": ["Kitchen", "Living room", "Bedroom", "Bathroom", "Hallway", "Garage", "Loft", "Shed"]}, "examples": ["Toolbox", "Toaster", "Rice", "Winter clothes"], "gettingStarted": ["places", "labels", "pack", "photo", "lend", "remind", "invite"], "hints": {"find": "Search for anything you've added, or add your first things with Add.", "browse": "Start with your rooms, then add the furniture and boxes in them.", "scan": "Scan a QR label on a box to open it, or a blank one to make it a new box.", "types": "Types such as Appliance and Book give items their own fields, like a serial number or an author."}},
  {"key": "hobby", "name": "Hobby", "description": "Bins and drawers for a workshop or craft room, with types for parts, materials, kits and tools.", "types": ["Workspace", "Bin", "Drawer unit", "Part", "Material", "Kit", "Tool"], "icon": "palette", "features": ["barcodes", "stock", "reminders", "projects"], "places": {"type": "Workspace", "suggest": ["Workbench", "Craft table", "Desk", "Garage", "Shed", "Spare room"]}, "examples": ["Small parts", "PLA filament", "Spitfire model"], "gettingStarted": ["places", "labels", "pack", "stock", "project", "photo", "invite"], "hints": {"find": "Search your parts, materials and kits, or add the first ones with Add.", "browse": "Start with your workbench or craft table, then add the bins and drawers on it.", "scan": "Scan a QR label on a bin to see what's in it, or a blank one to make it a new bin.", "types": "Types such as Part and Material give items their own fields, like a part number or a colour."}},
  {"key": "collection", "name": "Collection", "description": "Display cases, binders and sets, with a collectible type for series, condition and value.", "types": ["Display case", "Binder", "Set", "Collectible"], "icon": "star", "features": ["values", "lending"], "places": {"type": "Display case", "suggest": ["Living room cabinet", "Study shelves", "Glass case", "Storage boxes"]}, "examples": ["Trading cards", "Coins of the year"], "gettingStarted": ["places", "values", "photo", "labels", "lend", "invite"], "hints": {"find": "Search your collection by name, series or year, or add your first pieces with Add.", "browse": "Start with your display cases, then add the binders and sets in them.", "scan": "Scan a QR label on a case or binder to open it, or a blank one to make it a new one.", "types": "Types such as Collectible give items their own fields, like a series, a condition and a value."}},
  {"key": "business", "name": "Business", "description": "Sites, rooms, racks and bins, with types for stock you sell, equipment you own and the documents behind them.", "types": ["Site", "Room", "Rack", "Bin", "Product", "Equipment", "Document"], "icon": "building", "features": ["values", "audits", "barcodes", "stock", "lending", "reminders"], "places": {"type": "Site", "suggest": ["Shop", "Warehouse", "Office", "Workshop", "Stockroom"]}, "examples": ["Rack A", "Office laptop"], "gettingStarted": ["places", "labels", "pack", "audit", "stock", "lend", "invite"], "hints": {"find": "Search stock, equipment and documents by name, SKU or asset tag, or add the first ones with Add.", "browse": "Start with your sites, then add the rooms, racks and bins in them.", "scan": "Scan a QR label on a rack or bin to open it, or use Audit to tick off what's where.", "types": "Types such as Product and Equipment give items their own fields, like a SKU or an asset tag."}},
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
  id, name, marking: null, typeId: null, holdsItems: false, quantity: null, unit: null, status: 'Active', contents: 0, labelCode: null,
  isLow: false, lentTo: null, dueOn: null, destinationId: null, ...extra,
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
  labelCode: null, lowStockAt: null, reserved: 0, isLow: false, lentTo: null, lentAt: null, dueOn: null, destinationId: null, ...extra,
});

/** ItemDetailResponse for each item the tests open. */
export const itemDetail = (id) => ({
  [itemIds.f]: {
    item: item(itemIds.f, 'Box F', { marking: 'F', typeId: boxType, parentId: itemIds.garage, holdsItems: true, labelCode: '7K3Q', contentValue: 129.5,
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

/** The saved views, one per display: ViewResponse[]. "Not seen in a year" is pinned to Find and shared. */
export const viewIds = {
  notSeen: '00000000-0000-4000-8000-000000002001', cards: '00000000-0000-4000-8000-000000002002', tree: '00000000-0000-4000-8000-000000002003',
  board: '00000000-0000-4000-8000-000000002004', timeline: '00000000-0000-4000-8000-000000002005', mine: '00000000-0000-4000-8000-000000002006',
};
const view = (id, name, query, display, extra = {}) => ({
  id, name, query, display, columns: [], sort: null, groupBy: null, shared: true, pinned: false, mine: true, canChange: true,
  updatedAt: '2026-11-20T10:00:00Z', ...extra,
});
export const views = () => [
  view(viewIds.notSeen, 'Not seen in a year', 'seen>1y', 'Table', { pinned: true, columns: ['name', 'type', 'place', 'seen', 'brand', 'value'] }),
  view(viewIds.board, 'Tools by power', 'type:tool', 'Board', { groupBy: 'power' }),
  view(viewIds.cards, 'Everything in the garage', 'in:garage', 'Cards'),
  view(viewIds.timeline, 'What happened to tools', 'type:tool', 'Timeline'),
  view(viewIds.tree, 'Boxes', 'is:container', 'Tree', { shared: false }),
  view(viewIds.mine, "Sam's list", 'tag:garden', 'Table', { mine: false, canChange: false }),
];

/** SearchResponse for a view: the drill (a photo, seen long ago) and the baseball (never seen), with their details. */
export const viewResults = ({ withoutBall = false } = {}) => {
  const chain = [{ id: itemIds.garage, name: 'Garage', marking: null }, { id: itemIds.f, name: 'Box F', marking: 'F' }];
  const results = [
    { item: { id: itemIds.drill, name: 'Drill', marking: null, typeId: '00000000-0000-4000-8000-000000000d03', holdsItems: false, quantity: null, unit: null,
      status: 'Active', contents: 0, value: 129.5, contentValue: 0, labelCode: null }, chain,
      details: { fields: { '00000000-0000-4000-8000-000000000e02': 'DeWalt', '00000000-0000-4000-8000-000000000e04': 'Cordless' }, tags: [],
        lastSeenAt: '2025-06-01T10:00:00Z', createdAt: '2025-01-10T10:00:00Z', photoId: '00000000-0000-4000-8000-000000000b01' } },
    { item: { id: itemIds.ball, name: 'Baseball', marking: null, typeId: null, holdsItems: false, quantity: null, unit: null, status: 'Active',
      contents: 0, value: null, contentValue: 0, labelCode: null }, chain,
      details: { fields: {}, tags: [], lastSeenAt: null, createdAt: '2026-05-03T12:00:00Z', photoId: null } },
  ].filter((result) => !withoutBall || result.item.id !== itemIds.ball);
  return { results, total: results.length, totalIsEstimate: false, next: null };
};

/** ValueTotalsResponse: the drill in the garage, and the rest unvalued. */
export const valueTotals = () => ({
  total: 129.5, items: 6, unvalued: 5,
  byPlace: [
    { place: { id: itemIds.garage, name: 'Garage', marking: null }, count: 4, value: 129.5 },
    { place: { id: itemIds.shed, name: 'Shed', marking: null }, count: 1, value: 0 },
    { place: null, count: 1, value: 0 },
  ],
  byType: [
    { typeId: '00000000-0000-4000-8000-000000000d03', name: 'Tool', count: 1, valued: 1, value: 129.5 },
    { typeId: '00000000-0000-4000-8000-000000000d01', name: 'Box', count: 3, valued: 0, value: 0 },
    { typeId: null, name: 'No type', count: 2, valued: 0, value: 0 },
  ],
});

/** InsuranceReportResponse: the garage with the drill (photo) and the baseball. */
export const insuranceReport = () => {
  const { results } = viewResults();
  return {
    madeAt: '2026-11-24T10:00:00Z', total: 129.5, count: 2, truncated: false,
    rooms: [{ room: { id: itemIds.garage, name: 'Garage', marking: null }, value: 129.5, items: results }],
  };
};

/** SearchResponse for the missing items: the baseball, never seen since. */
export const missingItems = () => {
  const ball = viewResults().results.find((result) => result.item.id === itemIds.ball);
  return { results: [{ ...ball, item: { ...ball.item, status: 'Missing' } }], total: 1, totalIsEstimate: false, next: null };
};

// ---------- Space features (phase 6) ----------

/** Every feature switched on, for the screens they add. */
export const allFeatures = ['values', 'audits', 'barcodes', 'stock', 'lending', 'reminders', 'moving', 'projects'];

/** ItemDetailResponse: the screws, low and partly set aside for a project. */
export const lowScrews = () => {
  const detail = itemDetail(itemIds.screws);
  return { ...detail, item: { ...detail.item, quantity: 12, lowStockAt: 20, reserved: 4, isLow: true } };
};

/** ItemDetailResponse: the drill, lent to Sam and overdue. */
export const lentDrill = () => {
  const detail = itemDetail(itemIds.drill);
  return { ...detail, item: { ...detail.item, status: 'Lent', lentTo: 'Sam', lentAt: '2026-11-01T10:00:00Z', dueOn: '2026-11-10' } };
};

/** ItemDetailResponse: Box F, packed and going to the shed. */
export const packedBox = () => {
  const detail = itemDetail(itemIds.f);
  return { ...detail, item: { ...detail.item, destinationId: itemIds.shed } };
};

/** ItemListResponse: what's running low. */
export const lowStock = () => ({ items: [summary(itemIds.screws, 'Screws', { quantity: 12, isLow: true })], more: false });

/** ItemListResponse: what's lent out. */
export const loans = () => ({
  items: [summary(itemIds.drill, 'Drill', { typeId: toolType, status: 'Lent', lentTo: 'Sam', dueOn: '2026-11-10' }),
    summary(itemIds.ball, 'Baseball', { status: 'Lent', lentTo: 'the church hall', dueOn: null })],
  more: false,
});

/** RemindersResponse: a warranty coming up and some overdue upkeep. */
export const reminders = () => ({
  dates: [
    { item: summary(itemIds.drill, 'Drill', { typeId: toolType }), fieldId: '00000000-0000-4000-8000-000000000e05', field: 'Next service', on: '2026-10-01', upkeep: true, days: -54 },
    { item: summary(itemIds.ball, 'Baseball'), fieldId: '00000000-0000-4000-8000-000000000e06', field: 'Warranty until', on: '2026-12-10', upkeep: false, days: 16 },
  ],
  more: false,
});

/** ItemListResponse: boxes with a destination. */
export const moving = () => ({ items: [summary(itemIds.f, 'Box F', { marking: 'F', holdsItems: true, contents: 3, destinationId: itemIds.shed })], more: false });

/** ManifestResponse: Box F, going to the shed. */
export const manifest = () => ({
  box: summary(itemIds.f, 'Box F', { marking: 'F', holdsItems: true, contents: 3, labelCode: '7K3Q', destinationId: itemIds.shed }),
  destination: ref(itemIds.shed, 'Shed'),
  destinationPath: [ref(itemIds.shed, 'Shed')],
  contents: boxContents().items,
  more: false,
});

/** ProjectResponse[]: one open project setting aside screws and the drill. */
export const projects = () => [{
  id: '00000000-0000-4000-8000-000000003001', name: 'Bookshelf', notes: null, status: 'Open', createdAt: '2026-11-20T10:00:00Z',
  updatedAt: '2026-11-21T10:00:00Z', version: 3,
  items: [
    { item: summary(itemIds.drill, 'Drill', { typeId: toolType }), amount: 1 },
    { item: summary(itemIds.screws, 'Screws', { quantity: 12 }), amount: 4 },
  ],
}];

/** ImportResponse: a Homebox export uploaded and waiting for its columns to be checked. */
export const importDraft = () => ({
  id: '00000000-0000-4000-8000-000000004001', kind: 'Homebox', fileName: 'homebox-export.csv', status: 'Draft',
  headers: ['HB.location', 'HB.labels', 'HB.name', 'HB.quantity', 'HB.serial_number'],
  sample: [['Garage / Shelf 2', 'Tools', 'Sander', '1', 'SN9'], ['Garage', '', 'Nails', '500', '']],
  mapping: [{ column: 0, target: 'location' }, { column: 1, target: 'tags' }, { column: 2, target: 'name' }, { column: 3, target: 'quantity' }, { column: 4, target: 'note' }],
  parentId: null, typeId: null, total: 2, done: 0, added: 0, problems: [], createdAt: '2026-11-24T10:00:00Z', finishedAt: null,
});

/** ImportResponse[]: one finished import, with a row it skipped. */
export const imports = () => [{
  ...importDraft(), id: '00000000-0000-4000-8000-000000004002', fileName: 'parts.csv', kind: 'Csv', status: 'Done', headers: [], sample: [],
  total: 120, done: 120, added: 125, problems: [{ row: 14, problem: 'It has no name.' }], finishedAt: '2026-11-24T10:01:00Z',
}];

/** CalendarFeedResponse while the feed is on. */
export const calendarFeed = () => ({ url: `http://localhost/api/spaces/${spaceIds.home}/calendar/0123456789abcdef.ics` });
