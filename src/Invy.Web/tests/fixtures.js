// The `test` every spec uses: a page whose /api requests are answered from data.js, plus `api` to change answers.
// A request with no answer gets a 500 and fails the test once it ends, so the stubs can't fall behind the page.
import { test as base, expect } from '@playwright/test';
import * as data from './data.js';

const REPLY = Symbol('reply');

/** An answer other than 200 with JSON, for example `reply(500, { detail: 'The server is down.' })`. */
export const reply = (status, body = null, contentType = 'application/json') => ({ [REPLY]: true, status, body, contentType });

/** What every test starts with: an admin signed in, and everything the Settings tab loads. */
function defaults() {
  return {
    'GET /api/session': data.session(),
    // The live stream: an empty one that asks not to reconnect for ten minutes, so tests see no pushes.
    'GET /api/live': reply(200, Buffer.from('retry: 600000\n\n'), 'text/event-stream'),
    'GET /api/tokens': [],
    'POST /api/tokens': (request) => data.issuedApiToken(request.postDataJSON().name),
    'GET /api/system/https': data.https(),
    'GET /api/system/security': data.security(),
    'PUT /api/system/security': (request) => request.postDataJSON(),
    'GET /api/system/notifications': data.notifications(),
    'PUT /api/system/notifications': (request) => ({ ...data.notifications(), ...request.postDataJSON() }),
    'GET /api/system/backups': data.backups(),
    'PUT /api/system/backups': (request) => ({ ...data.backups(), ...request.postDataJSON() }),
    'GET /api/system/backups/*/summary': data.backupSummary(),
    'GET /api/system/logs': data.logs(),
    'GET /api/users': data.users(),
    'GET /api/links': data.links(),
    'GET /api/account/link': (request) =>
      data.linkDetails(new URL(request.url()).searchParams.get('token') === 'reset-token' ? 'PasswordReset' : 'Invite'),
    // No AI model is set up unless a test says so.
    'GET /api/ai/features': data.aiFeatures(),
    'GET /api/ai/settings': data.aiSettings(false),
    // Spaces: the shop, where Alex is a viewer, answers the same as Home.
    'GET /api/spaces': data.spaces(),
    'GET /api/starter-packs': data.starterPacks(),
    'GET /api/space-features': data.spaceFeatures(),
    'POST /api/spaces': (request) => {
      const { name, preset, features } = request.postDataJSON();
      return { ...data.spaces()[0], id: '00000000-0000-4000-8000-000000000c09', name, preset, features, role: 'Owner' };
    },
    'PUT /api/spaces/*': (request) => ({ ...data.spaces()[0], ...request.postDataJSON() }),
    'DELETE /api/spaces/*': null,
    'PUT /api/spaces/*/features': (request) => ({ ...data.spaces()[0], features: request.postDataJSON().features }),
    'PUT /api/spaces/*/kind': (request) => ({ ...data.spaces()[0], preset: request.postDataJSON().kind }),
    'DELETE /api/spaces/*/examples': { removed: 2 },
    'GET /api/spaces/*/getting-started': data.gettingStarted(),
    'POST /api/spaces/*/getting-started/dismiss': data.gettingStarted({ dismissed: true }),
    'POST /api/spaces/*/getting-started/show': data.gettingStarted(),
    'GET /api/spaces/*/types': data.itemTypes(),
    'POST /api/spaces/*/types/from-pack': (request) => ({ ...data.itemTypes()[1], id: '00000000-0000-4000-8000-000000000d08', name: request.postDataJSON().type }),
    'POST /api/spaces/*/types': (request) => ({ ...request.postDataJSON(), id: '00000000-0000-4000-8000-000000000d09', version: 1 }),
    'PUT /api/spaces/*/types/*': (request) => ({ ...request.postDataJSON(), id: new URL(request.url()).pathname.split('/').pop(), version: request.postDataJSON().version + 1 }),
    'DELETE /api/spaces/*/types/*': null,
    'GET /api/spaces/*/members': data.spaceMembers(),
    'POST /api/spaces/*/members': (request) => [...data.spaceMembers(), { userId: '00000000-0000-4000-8000-000000000004', username: request.postDataJSON().username, displayName: request.postDataJSON().username, role: request.postDataJSON().role }],
    'PUT /api/spaces/*/members/*': data.spaceMembers(),
    'DELETE /api/spaces/*/members/*': data.spaceMembers().slice(0, 2),
    // Items: the shed and garage at the top, Box F in the garage with a baseball, screws and a drill in it.
    'GET /api/spaces/*/items': (request) => (new URL(request.url()).searchParams.get('parent')
      ? (new URL(request.url()).searchParams.get('after') ? { items: [], next: null } : data.boxContents())
      : data.topItems()),
    'GET /api/spaces/*/inbox': data.inboxItems(),
    'GET /api/spaces/*/items/*': (request) => data.itemDetail(new URL(request.url()).pathname.split('/').pop()) ?? reply(404, { detail: 'That item no longer exists.' }),
    'GET /api/spaces/*/items/*/history': data.itemHistory(),
    'GET /api/spaces/*/activity': (request) => (new URL(request.url()).searchParams.get('before') ? { events: [], next: null } : data.activity()),
    'POST /api/spaces/*/items': (request) => ({ ...data.itemDetail(data.itemIds.f).item, ...request.postDataJSON(), id: '00000000-0000-4000-8000-000000000f99' }),
    'PUT /api/spaces/*/items/*': (request) => ({ ...request.postDataJSON(), id: new URL(request.url()).pathname.split('/').pop() }),
    'POST /api/spaces/*/items/*/move': (request) => ({ ...data.itemDetail(data.itemIds.f).item, parentId: request.postDataJSON().parentId }),
    'POST /api/spaces/*/items/*/quantity': data.itemDetail(data.itemIds.screws).item,
    'POST /api/spaces/*/items/*/dispose': data.itemDetail(data.itemIds.f).item,
    'POST /api/spaces/*/items/*/restore': data.itemDetail(data.itemIds.f).item,
    'POST /api/spaces/*/items/*/seen': (request) => ({ ...(data.itemDetail(new URL(request.url()).pathname.split('/').at(-2)) ?? data.itemDetail(data.itemIds.f)).item, lastSeenAt: new Date().toISOString() }),
    'POST /api/spaces/*/items/*/audit': data.auditResult(),
    'GET /api/spaces/*/items/*/attachments': (request) => (request.url().includes(data.itemIds.drill) ? data.attachments() : []),
    'POST /api/spaces/*/items/*/attachments': (request) => ({ ...data.attachments()[0], id: '00000000-0000-4000-8000-000000000b99', name: request.postDataJSON().name, hasThumbnail: false }),
    'DELETE /api/spaces/*/attachments/*': null,
    'POST /api/spaces/*/items/*/attachments/from-web': { ...data.attachments()[0], id: '00000000-0000-4000-8000-000000000b98', hasThumbnail: false },
    // Barcodes: the test product is known; anything else isn't.
    'GET /api/spaces/*/barcodes/*': (request) => (request.url().endsWith(data.product().barcode)
      ? data.product() : reply(404, { detail: 'No product database knows barcode 12345670. Type its name instead.' })),
    'GET /api/spaces/*/attachments/*/thumbnail': reply(200, Buffer.from(data.TINY_PNG, 'base64'), 'image/png'),
    'GET /api/spaces/*/attachments/*/file': reply(200, Buffer.from(data.TINY_PNG, 'base64'), 'image/png'),
    'POST /api/spaces/*/quick-add': [],
    // AI suggestions, once a test sets 'GET /api/ai/features' to data.aiReady().
    'POST /api/spaces/*/items/from-photo': data.photoItems(),
    'POST /api/spaces/*/items/*/details-from-photo': data.photoDetails(),
    'POST /api/spaces/*/types/suggest-fields': data.fieldSuggestions(),
    'POST /api/spaces/*/inbox/suggest-types': reply(202, null),
    'POST /api/spaces/*/items/*/suggested-type/*': (request) => ({ ...data.itemDetail(data.itemIds.drill).item, id: data.itemIds.cable, name: 'Mystery cable', parentId: null,
      typeId: request.url().endsWith('accept') ? '00000000-0000-4000-8000-000000000d03' : null, suggestedTypeId: null }),
    // Labels: 7K3Q is on Box F, other codes aren't on anything yet, and XXXX isn't a label Invy has.
    'GET /api/labels/*': (request) => data.labelFor(decodeURIComponent(new URL(request.url()).pathname.split('/').pop()))
      ?? reply(404, { detail: 'There is no label XXXX. Check the code, or scan it again.' }),
    'GET /api/spaces/*/labels': data.unclaimedLabels(),
    'POST /api/spaces/*/labels': (request) => data.newLabels(request.postDataJSON().count),
    'POST /api/spaces/*/labels/*/claim': (request) => data.claimedBox(new URL(request.url()).pathname.split('/').at(-2), request.postDataJSON().parentId),
    'POST /api/spaces/*/labels/*/attach': (request) => ({ ...data.itemDetail(data.itemIds.drill).item, labelCode: new URL(request.url()).pathname.split('/').at(-2) }),
    // Views: one of each display; each runs to the drill and the baseball.
    'GET /api/spaces/*/views': data.views(),
    'POST /api/spaces/*/views': (request) => ({ ...data.views()[0], ...request.postDataJSON(), id: '00000000-0000-4000-8000-000000002099', mine: true, canChange: true }),
    'PUT /api/spaces/*/views/*': (request) => ({ ...data.views()[0], ...request.postDataJSON(), id: new URL(request.url()).pathname.split('/').pop(), mine: true, canChange: true }),
    'DELETE /api/spaces/*/views/*': null,
    'GET /api/spaces/*/views/*/results': data.viewResults(),
    'GET /api/spaces/*/views/*/timeline': (request) => (new URL(request.url()).searchParams.get('before') ? { events: [], next: null } : data.activity()),
    // Space features: low stock, lending, reminders, moving, projects, imports and the calendar feed.
    'GET /api/spaces/*/low-stock': data.lowStock(),
    'GET /api/spaces/*/loans': data.loans(),
    'GET /api/spaces/*/reminders': data.reminders(),
    'GET /api/spaces/*/moving': data.moving(),
    'GET /api/spaces/*/items/*/manifest': data.manifest(),
    'GET /api/spaces/*/items/*/projects': [],
    'POST /api/spaces/*/items/*/lend': (request) => ({ ...data.lentDrill().item, lentTo: request.postDataJSON().to, dueOn: request.postDataJSON().dueOn }),
    'POST /api/spaces/*/items/*/return': data.itemDetail(data.itemIds.drill).item,
    'POST /api/spaces/*/items/*/upkeep-done': data.itemDetail(data.itemIds.drill).item,
    'PUT /api/spaces/*/items/*/destination': data.packedBox().item,
    'POST /api/spaces/*/items/*/arrived': data.itemDetail(data.itemIds.f).item,
    'GET /api/spaces/*/projects': data.projects(),
    'POST /api/spaces/*/projects': (request) => ({ ...data.projects()[0], id: '00000000-0000-4000-8000-000000003099', name: request.postDataJSON().name, items: [] }),
    'POST /api/spaces/*/projects/*/reserve': data.projects()[0],
    'POST /api/spaces/*/projects/*/use-up': data.projects()[0],
    'POST /api/spaces/*/projects/*/finish': { ...data.projects()[0], status: 'Done', items: [] },
    'DELETE /api/spaces/*/projects/*': null,
    'GET /api/spaces/*/imports': data.imports(),
    'POST /api/spaces/*/imports': data.importDraft(),
    'POST /api/spaces/*/imports/*/start': { ...data.importDraft(), status: 'Queued' },
    'DELETE /api/spaces/*/imports/*': null,
    'GET /api/spaces/*/calendar': { url: null },
    'POST /api/spaces/*/calendar': data.calendarFeed(),
    'DELETE /api/spaces/*/calendar': { url: null },
    // Reports.
    'GET /api/spaces/*/reports/values': data.valueTotals(),
    'GET /api/spaces/*/reports/insurance': data.insuranceReport(),
    'GET /api/spaces/*/reports/missing': data.missingItems(),
    // Search: "baseball" finds two, "type:" can't be read, "nothing" finds none, "part" finds 120 (for scrolling),
    // and an empty box gives the recently changed items.
    'GET /api/spaces/*/search': (request) => {
      const params = new URL(request.url()).searchParams;
      const q = params.get('q') ?? '';
      if (q === 'type:' || q.startsWith('type:car')) return reply(400, { detail: 'There is no type called car. The types are Box, Thing, Tool.' });
      if (q.startsWith('nothing')) return { results: [], total: 0, totalIsEstimate: false, next: null };
      if (q.startsWith('baseball')) return data.baseballResults();
      if (q.startsWith('part')) return data.searchResults(120, Number(params.get('after') ?? 0));
      return data.searchResults(5);
    },
    ...inbox(),
  };
}

/** The signed-in person's inbox, empty at first; it keeps the notes the page sends, as Invy would. */
function inbox() {
  const entries = [];
  const answer = () => ({ entries: [...entries], unread: entries.filter((entry) => !entry.read).length });
  return {
    'GET /api/inbox': () => answer(),
    'POST /api/inbox': (request) => {
      const id = `00000000-0000-4000-8000-${String(900000 + entries.length).padStart(12, '0')}`;
      entries.unshift({ id, text: request.postDataJSON().text, source: 'Page', createdAt: new Date().toISOString(), read: false });
      return answer();
    },
    'POST /api/inbox/read': () => {
      for (const entry of entries) entry.read = true;
      return answer();
    },
    'DELETE /api/inbox': () => {
      entries.length = 0;
      return answer();
    },
  };
}

/**
 * Opens the bell in the header and returns its panel, to check what the page said. Notes go there rather than the
 * corner while someone is signed in.
 */
export async function openInbox(page) {
  await page.getByRole('button', { name: /^Notifications/ }).click();
  return page.getByRole('region', { name: 'Notifications' });
}

/** Checks the inbox has a message, then closes the bell again so the rest of the test can carry on. */
export async function expectInbox(page, text) {
  await expect((await openInbox(page)).getByText(text)).toBeVisible();
  await page.keyboard.press('Escape');
}

/** 'GET /api/system/backups/*' matches any one path segment where the * is. */
const pattern = (key) => new RegExp(`^${key.replace(/[.+?^${}()|[\]\\]/g, '\\$&').replaceAll('*', '[^/]+')}$`);

class ApiStub {
  /** Requests that had no answer, as 'METHOD /path?query'. */
  unknown = [];
  /** Every request made, as 'METHOD /path?query', in order. */
  requests = [];
  /** While true, every request fails as if Invy can't be reached (no signal, server off), and isn't recorded. */
  offline = false;
  #routes = new Map();

  constructor(page) {
    this.page = page;
    this.set(defaults());
  }

  /**
   * Answers requests matching each 'METHOD /path' key from now on. A value is sent as JSON; a function gets the
   * Playwright request and returns the value (or a promise of it); `reply()` sends another status or content type.
   */
  set(routes) {
    for (const [key, value] of Object.entries(routes)) this.#routes.set(key, { match: pattern(key), value });
    return this;
  }

  async install() {
    await this.page.route('**/api/**', async (route) => {
      if (this.offline) return route.abort('internetdisconnected');
      const request = route.request();
      const url = new URL(request.url());
      const key = `${request.method()} ${url.pathname}`;
      this.requests.push(key + url.search);
      // The newest matching route wins, so set() can narrow a default.
      const found = [...this.#routes.values()].reverse().find((entry) => entry.match.test(key));
      if (!found) {
        this.unknown.push(key + url.search);
        return route.fulfill({ status: 500, json: { detail: `The test has no answer for ${key}.` } });
      }
      const value = typeof found.value === 'function' ? await found.value(request) : found.value;
      if (value?.[REPLY]) {
        const body = Buffer.isBuffer(value.body) ? value.body : value.body == null ? '' : JSON.stringify(value.body);
        return route.fulfill({ status: value.status, contentType: value.contentType, body });
      }
      return route.fulfill({ status: 200, contentType: 'application/json', body: JSON.stringify(value ?? null) });
    });
  }
}

export const test = base.extend({
  // Automatic, so a test that never names `api` still gets the stubbed answers.
  api: [async ({ page }, use) => {
    const stub = new ApiStub(page);
    await stub.install();
    await use(stub);
    expect(stub.unknown, 'requests the test had no answer for; add them to tests/fixtures.js').toEqual([]);
  }, { auto: true }],
});

export { expect };

/** Opens an address and waits until the page has left its loading screen. */
export async function open(page, path) {
  await page.goto(path);
  await expect(page.getByRole('status', { name: 'Loading', exact: true })).toHaveCount(0);
}
