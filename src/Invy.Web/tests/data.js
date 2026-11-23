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

export const security = () => ({ maxFailedSignIns: 5, lockoutMinutes: 15, requireHttps: false });

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

/** AiFeaturesResponse: no AI task exists yet, so none is ready. */
export const aiFeatures = () => ({ ready: [] });

/** AiSettingsResponse. Invy has no AI tasks yet, so the list of tasks is empty. */
export const aiSettings = (withModels = true) => (withModels ? {
  models: [
    { id: '00000000-0000-4000-8000-000000000b01', name: 'Claude', provider: 'Anthropic', model: 'claude-opus-5-5', endpoint: '', hasApiKey: true },
    { id: '00000000-0000-4000-8000-000000000b02', name: 'Ollama', provider: 'OpenAiCompatible', model: 'qwen2.5vl', endpoint: 'http://ollama.local:11434/v1', hasApiKey: false },
  ],
  defaultModel: 'Claude',
  tasks: [],
} : { models: [], defaultModel: null, tasks: [] });

/** ApiTokenResponse[] */
export const apiTokens = () => [
  { id: 'a7f0c2d1-0000-4000-8000-000000000002', name: 'Claude Code on the laptop', createdAt: '2026-11-02T18:30:00Z' },
  { id: 'a7f0c2d1-0000-4000-8000-000000000001', name: 'Claude Desktop', createdAt: '2026-10-14T09:00:00Z' },
];
