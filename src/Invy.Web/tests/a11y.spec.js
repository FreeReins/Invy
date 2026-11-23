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
  { name: 'find, nothing yet', path: '/find' },
  { name: 'find for a member', path: '/find', answers: asMember },
  {
    name: 'dark mode chosen with the sun and moon button', path: '/find',
    act: async (page) => {
      const before = await page.evaluate(() => matchMedia('(prefers-color-scheme: dark)').matches);
      // In the dark project this picks light, so both themes are also checked when chosen rather than followed.
      await page.getByRole('button', { name: before ? 'Switch to light mode' : 'Switch to dark mode' }).click();
    },
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

  // The last step finishes setup and opens Find.
  await page.getByRole('button', { name: 'Finish' }).click();
  await page.getByRole('heading', { name: 'Nothing to find yet' }).waitFor();
  await expect(page).toHaveURL(/\/find$/);
  await expectAccessible(page);
});
