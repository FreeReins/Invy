// Every screen of the page, checked with axe in each project of playwright.config.js (light, dark, phone).
// To cover a new screen or state, add a row to VIEWS: where to go, any answers to change, and what to click first.
import { test, expect, open, reply } from './fixtures.js';
import { expectAccessible } from './axe.js';
import * as data from './data.js';

const signedOut = { 'GET /api/session': { setupRequired: false, user: null } };
const asMember = { 'GET /api/session': data.session(data.member()) };

/** { name, path, answers?: routes for api.set(), act?: async (page, api) => {} run before checking } */
const VIEWS = [
  // Before signing in
  { name: 'sign in', path: '/', answers: signedOut },
  { name: 'sign in, with the theme button', path: '/', answers: signedOut,
    act: (page) => page.getByRole('button', { name: /^Switch to (light|dark) mode$/ }).click() },
  { name: 'join from an invite', path: '/?invite=invite-token', answers: signedOut },
  { name: 'choose a new password', path: '/?reset=reset-token', answers: signedOut },
  { name: 'setup: create the account', path: '/', answers: { 'GET /api/session': { setupRequired: true, user: null } } },

  // Find
  { name: 'find: recently changed', path: '/find', act: (page) => page.getByText('Recently changed').waitFor() },
  { name: 'find: nothing in the space yet', path: '/find',
    answers: { 'GET /api/spaces/*/search': { results: [], total: 0, totalIsEstimate: false, next: null } } },
  { name: 'find: no space yet', path: '/find', answers: { 'GET /api/spaces': [] } },
  { name: 'find for a member', path: '/find', answers: asMember },
  { name: 'find: results from the address', path: '/find?q=baseball', act: (page) => page.getByText('2 items found').waitFor() },
  {
    name: 'find: typing a search', path: '/find',
    act: async (page) => {
      await page.getByRole('searchbox').fill('baseball');
      await page.getByText('2 items found').waitFor();
    },
  },
  {
    name: 'find: a search that cannot be read yet', path: '/find',
    act: async (page) => {
      await page.getByRole('searchbox').fill('type:car');
      await page.getByText('There is no type called car.').waitFor();
    },
  },
  {
    name: 'find: nothing found', path: '/find',
    act: async (page) => { await page.getByRole('searchbox').fill('nothing here'); await page.getByText('Nothing found').waitFor(); },
  },
  {
    name: 'find: scrolled down a long list', path: '/find?q=part',
    act: async (page) => {
      await page.getByText('120 items found').waitFor();
      // Each scroll to the end asks for the next page, as a person scrolling would.
      for (let page2 = 0; page2 < 6 && !await page.getByText('Part 120').isVisible(); page2++) {
        await page.evaluate(() => window.scrollTo(0, document.body.scrollHeight));
        await page.waitForTimeout(250);
      }
      await page.getByText('Part 120').waitFor();
      await expect(page.getByRole('listitem').filter({ hasText: 'Part 001' })).toHaveCount(0);
    },
  },
  {
    name: 'dark mode chosen with the sun and moon button', path: '/find',
    act: async (page) => {
      const before = await page.evaluate(() => matchMedia('(prefers-color-scheme: dark)').matches);
      // In the dark project this picks light, so both themes are also checked when chosen rather than followed.
      await page.getByRole('button', { name: before ? 'Switch to light mode' : 'Switch to dark mode' }).click();
    },
  },

  // Spaces
  { name: 'space picker: new space dialog', path: '/find', act: (page) => page.getByRole('button', { name: 'New space' }).click() },
  { name: 'types: no space yet', path: '/types', answers: { 'GET /api/spaces': [] } },
  { name: 'people: no space yet', path: '/people', answers: { 'GET /api/spaces': [] } },

  // Browse and items
  { name: 'browse: the top of a space', path: '/browse', act: (page) => page.getByText('Mystery cable').waitFor() },
  { name: 'browse: nothing yet', path: '/browse',
    answers: { 'GET /api/spaces/*/items': { items: [], next: null }, 'GET /api/spaces/*/inbox': { items: [], next: null } } },
  { name: 'browse: a new item', path: '/browse', act: (page) => page.getByRole('button', { name: 'New item' }).click() },
  { name: 'item: a box with things inside', path: `/browse/${data.itemIds.f}`, act: (page) => page.getByText('Where it has been').waitFor() },
  { name: 'item: a tool with fields', path: `/browse/${data.itemIds.drill}`, act: (page) => page.getByText('DeWalt').first().waitFor() },
  { name: 'item: a counted thing', path: `/browse/${data.itemIds.screws}`, act: (page) => page.getByRole('button', { name: 'One more' }).waitFor() },
  { name: 'item: editing', path: `/browse/${data.itemIds.drill}`, act: (page) => page.getByRole('button', { name: 'Edit' }).click() },
  { name: 'item: moving', path: `/browse/${data.itemIds.f}`,
    act: async (page) => { await page.getByRole('button', { name: 'Move' }).click(); await page.getByRole('button', { name: /^Move to|^Already in/ }).waitFor(); } },
  { name: 'item: as a viewer', path: `/browse/${data.itemIds.f}`,
    answers: { 'GET /api/spaces': data.spaces().map((space) => ({ ...space, role: 'Viewer' })) },
    act: (page) => page.getByText('Where it has been').waitFor() },

  { name: 'item: sticking on a label', path: `/browse/${data.itemIds.drill}`,
    act: (page) => page.getByRole('button', { name: 'Stick on a label' }).click().then(() => page.getByRole('dialog').waitFor()) },

  // Scan and labels
  { name: 'scan: open', path: '/scan', act: (page) => page.getByRole('button', { name: 'Start the camera' }).waitFor() },
  { name: 'scan: an unclaimed label from its address', path: `/l/${data.labelCodes.unclaimed}`,
    act: (page) => page.getByText(`Label ${data.labelCodes.unclaimed} isn't on anything yet`).waitFor() },
  {
    name: 'scan: claiming labels into the sticky place', path: '/scan',
    act: async (page) => {
      await page.getByRole('radio', { name: 'Claim' }).check();
      await page.getByLabel('Or type the label code').fill(data.labelCodes.unclaimed);
      await page.getByRole('button', { name: 'Go' }).click();
      await page.getByText(`Made Box ${data.labelCodes.unclaimed}`).waitFor();
    },
  },
  {
    name: 'scan: picking the sticky place', path: '/scan',
    act: async (page) => {
      await page.getByRole('radio', { name: 'Claim' }).check();
      await page.getByRole('button', { name: 'Change' }).click();
      await page.getByRole('button', { name: 'Add to the top' }).waitFor();
    },
  },
  {
    name: 'scan: move, waiting for where it goes', path: '/scan',
    act: async (page) => {
      await page.getByRole('radio', { name: 'Move' }).check();
      await page.getByLabel('Or type the label code').fill(data.labelCodes.onBoxF);
      await page.getByRole('button', { name: 'Go' }).click();
      await page.getByText('Now scan where it is going').waitFor();
    },
  },
  {
    name: 'scan: adding to a scanned box', path: '/scan',
    act: async (page) => {
      await page.getByRole('radio', { name: 'Add' }).check();
      await page.getByLabel('Or type the label code').fill(data.labelCodes.onBoxF);
      await page.getByRole('button', { name: 'Go' }).click();
      await page.getByLabel('Quick add to Box F').waitFor();
    },
  },
  { name: 'scan: as a viewer', path: '/scan', answers: { 'GET /api/spaces': data.spaces().map((space) => ({ ...space, role: 'Viewer' })) } },
  { name: 'scan: no space yet', path: '/scan', answers: { 'GET /api/spaces': [] } },
  { name: 'settings: labels and printing', path: '/settings/labels' },
  {
    name: 'settings: a sheet of labels ready to print', path: '/settings/labels',
    act: async (page) => {
      await page.getByRole('button', { name: /^Make 30 labels/ }).click();
      await page.getByRole('button', { name: 'Print', exact: true }).waitFor();
    },
  },
  {
    name: 'settings: unclaimed labels on a small sheet', path: '/settings/labels',
    act: async (page) => {
      await page.getByLabel('Sticker sheet').selectOption('avery-l7651');
      await page.getByRole('button', { name: 'Print the unclaimed labels again' }).click();
      await page.getByText('12 unclaimed labels').waitFor();
    },
  },

  // Activity
  { name: 'activity: the feed', path: '/activity', act: (page) => page.getByText('took 15 from').waitFor() },
  { name: 'activity: nothing yet', path: '/activity', answers: { 'GET /api/spaces/*/activity': { events: [], next: null } } },

  // Types
  { name: 'types: the list', path: '/types', act: (page) => page.getByRole('heading', { name: 'Tool' }).waitFor() },
  { name: 'types: none yet', path: '/types', answers: { 'GET /api/spaces/*/types': [] } },
  {
    name: 'types: editing a type', path: '/types',
    act: (page) => page.getByRole('button', { name: 'Edit Tool' }).click(),
  },
  {
    name: 'types: a new type with a choice field', path: '/types',
    act: async (page) => {
      await page.getByRole('button', { name: 'New type' }).click();
      await page.getByRole('button', { name: 'Add a field' }).click();
      await page.getByLabel('Kind').selectOption('Select');
    },
  },
  {
    name: 'types: as a viewer', path: '/types',
    answers: { 'GET /api/spaces': data.spaces().map((space) => ({ ...space, role: 'Viewer' })) },
    act: (page) => page.getByRole('button', { name: 'View Tool' }).click(),
  },

  // People
  { name: 'people: as an owner', path: '/people', act: (page) => page.getByText('Jo Park', { exact: true }).waitFor() },
  {
    name: 'people: as a viewer', path: '/people',
    answers: { 'GET /api/spaces': data.spaces().map((space) => ({ ...space, role: 'Viewer' })) },
    act: (page) => page.getByText('Jo Park', { exact: true }).waitFor(),
  },

  // Settings
  { name: 'settings: login', path: '/settings/login' },
  { name: 'settings: appearance', path: '/settings/appearance' },
  {
    // Phones only: the sidebar opened from the menu button. Wide screens show it all the time.
    name: 'settings: the sections menu open', path: '/settings/login',
    act: async (page) => {
      const menu = page.getByRole('button', { name: /^Settings sections/ });
      if (await menu.isVisible()) await menu.click();
    },
  },
  {
    // Phones only: scrolled past the menu button, the button that brings it back shows in the corner.
    name: 'settings: scrolled past the sections menu', path: '/settings/assistants',
    act: async (page) => {
      const menu = page.getByRole('button', { name: /^Settings sections/ });
      if (!await menu.isVisible()) return;
      await page.evaluate(() => window.scrollTo(0, document.body.scrollHeight));
      await expect(page.getByRole('button', { name: 'Back to the settings menu' })).toBeVisible();
    },
  },
  { name: 'settings: https', path: '/settings/https' },
  { name: 'settings: notifications', path: '/settings/notifications' },
  {
    name: 'settings: notifications, saved by itself', path: '/settings/notifications',
    act: async (page) => {
      await page.getByLabel('Send daily reminders at').fill('08:15');
      await page.getByLabel('Send daily reminders at').blur();
      await page.getByRole('status').filter({ hasText: 'Saved' }).waitFor();
    },
  },
  {
    name: 'settings: notifications, a field that stops it saving', path: '/settings/notifications',
    act: async (page) => {
      await page.getByLabel('Send daily reminders at').fill('');
      await page.getByLabel('Send daily reminders at').blur();
      await page.getByText('Not saved yet').waitFor();
    },
  },
  {
    name: 'settings: notifications, a save that failed', path: '/settings/notifications',
    answers: { 'PUT /api/system/notifications': reply(500, { detail: 'The settings could not be saved. Try again in a minute.' }) },
    act: async (page) => {
      await page.getByLabel('Send daily reminders at').fill('08:15');
      await page.getByLabel('Send daily reminders at').blur();
      await page.getByRole('button', { name: 'Try again' }).waitFor();
    },
  },
  { name: 'settings: backups', path: '/settings/backups' },
  {
    name: 'settings: backups with one opened', path: '/settings/backups',
    act: (page) => page.locator('#settings-body').getByRole('button', { expanded: false }).first().click()
      .then(() => page.getByText('Accounts').waitFor()),
  },
  { name: 'settings: logs', path: '/settings/logs' },
  {
    name: 'settings: logs with a long entry opened', path: '/settings/logs',
    act: (page) => page.getByRole('button', { name: 'Show all' }).click(),
  },
  { name: 'settings: AI assistants with no tokens', path: '/settings/assistants' },
  { name: 'settings: AI assistants with models set up', path: '/settings/assistants', answers: { 'GET /api/ai/settings': data.aiSettings() } },
  {
    name: 'settings: editing a saved AI model', path: '/settings/assistants', answers: { 'GET /api/ai/settings': data.aiSettings() },
    act: async (page) => {
      await page.locator('summary').filter({ hasText: 'claude-opus-5-5' }).click();
      await page.getByLabel('API key', { exact: true }).waitFor();
    },
  },
  {
    name: 'settings: adding an Ollama model', path: '/settings/assistants',
    act: async (page) => {
      await page.getByLabel('Start from').selectOption('ollama');
      await page.getByRole('button', { name: 'Add a model' }).click();
      await page.getByText('not saved yet').waitFor();
    },
  },
  { name: 'settings: AI assistants with tokens', path: '/settings/assistants', answers: { 'GET /api/tokens': data.apiTokens() } },
  {
    name: 'settings: AI assistants with a new token shown', path: '/settings/assistants',
    act: async (page) => {
      await page.getByLabel(/Name for a new token/).fill('Workshop tablet');
      await page.getByRole('button', { name: 'Make a token' }).click();
      await page.getByLabel('Token', { exact: true }).waitFor();
    },
  },

  // Someone who isn't an admin sees fewer sections.
  { name: 'settings for a member', path: '/settings/https', answers: asMember },
  { name: 'AI assistants for a member', path: '/settings/assistants', answers: asMember },

  // Things laid over the page
  {
    name: 'the confirm dialog', path: '/settings/assistants', answers: { 'GET /api/tokens': data.apiTokens() },
    act: (page) => page.getByRole('button', { name: 'Revoke Claude Desktop' }).click().then(() => page.getByRole('dialog').waitFor()),
  },
  {
    name: 'a message in the bell', path: '/find', answers: { 'GET /api/inbox': data.inbox() },
    act: (page) => page.getByRole('button', { name: /^Notifications, 1 unread/ }).click()
      .then(() => page.getByRole('region', { name: 'Notifications' }).waitFor()),
  },
  {
    name: 'an error message', path: '/settings/assistants',
    answers: { 'POST /api/tokens': reply(500, { detail: 'The token could not be made. Try again in a minute.' }) },
    act: async (page) => {
      await page.getByLabel(/Name for a new token/).fill('Workshop tablet');
      await page.getByRole('button', { name: 'Make a token' }).click();
      await page.getByText('The token could not be made').waitFor();
    },
  },
];

for (const view of VIEWS) {
  test(view.name, async ({ page, api }) => {
    if (view.answers) api.set(view.answers);
    await open(page, view.path);
    await view.act?.(page, api);
    await expectAccessible(page);
  });
}

test('setup wizard, every step', async ({ page, api }) => {
  api.set({
    'GET /api/session': { setupRequired: true, user: null },
    'POST /api/setup/account': data.admin(),
    // A new install has no spaces yet.
    'GET /api/spaces': [],
  });
  await open(page, '/');
  await page.getByLabel('Your name').fill('Alex Rivera');
  await page.getByLabel('Username').fill('alex');
  await page.getByLabel(/^Password/).fill('correct horse');
  await page.getByRole('button', { name: 'Create account' }).click();

  // A welcome before moving on, rather than a jump straight to the next step.
  await page.getByRole('heading', { name: 'Welcome, Alex Rivera.' }).waitFor();
  await expectAccessible(page);
  await page.getByRole('button', { name: 'Continue' }).click();

  await page.getByRole('heading', { name: 'Secure connection' }).waitFor();
  await expect(page.getByRole('heading', { name: 'Secure connection' })).toBeFocused();
  await expectAccessible(page);

  // The last step finishes setup and opens Find, which offers the first space.
  await page.getByRole('button', { name: 'Finish' }).click();
  await page.getByRole('heading', { name: 'Make your first space' }).waitFor();
  await expect(page).toHaveURL(/\/find$/);
  await expectAccessible(page);
});
