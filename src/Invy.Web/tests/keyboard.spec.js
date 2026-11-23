// Keyboard and focus behaviour that axe can't see: where focus goes, and which keys move it.
import { test, expect, open } from './fixtures.js';
import * as data from './data.js';

test('the confirm dialog starts on Cancel, and Escape cancels', async ({ page, api }) => {
  api.set({ 'GET /api/tokens': data.apiTokens() });
  await open(page, '/settings/assistants');
  const opener = page.getByRole('button', { name: 'Revoke Claude Desktop' });
  await opener.click();
  const dialog = page.getByRole('dialog', { name: 'Revoke Claude Desktop?' });
  await expect(dialog).toBeVisible();
  await expect(dialog).toHaveAccessibleDescription(/can no longer reach Invy/);
  await expect(dialog.getByRole('button', { name: 'Cancel' })).toBeFocused();

  await page.keyboard.press('Escape');
  await expect(dialog).toBeHidden();
  await expect(opener).toBeFocused();
  expect(api.requests.filter((request) => request.startsWith('DELETE'))).toEqual([]);
});

test('an unknown address goes to Find', async ({ page }) => {
  await open(page, '/somewhere-else');
  await expect(page).toHaveURL(/\/find$/);
  await expect(page.getByRole('link', { name: 'Find' })).toHaveAttribute('aria-current', 'page');
});

test('the tabs move between Find and Settings without reloading the page', async ({ page }) => {
  await open(page, '/find');
  await page.evaluate(() => { window.stayed = true; });
  await page.getByRole('navigation', { name: 'Views' }).getByRole('link', { name: 'Settings' }).click();
  await expect(page).toHaveURL(/\/settings$/);
  await expect(page).toHaveTitle('Settings · Invy');
  await page.goBack();
  await expect(page).toHaveURL(/\/find$/);
  expect(await page.evaluate(() => window.stayed)).toBe(true);
});

test('a Settings section link moves focus to the section', async ({ page, isMobile }) => {
  await open(page, '/settings/login');
  if (isMobile) await page.getByRole('button', { name: 'Settings sections, Login open' }).click();
  await page.getByRole('navigation', { name: 'Settings' }).getByRole('link', { name: 'Backups' }).click();
  await expect(page.getByRole('heading', { name: 'Backups' })).toBeVisible();
  await expect(page.locator('#settings-body')).toBeFocused();
  if (isMobile) await expect(page.getByRole('dialog', { name: 'Settings' })).toBeHidden();
});

test('on a phone the Settings menu opens a drawer that Escape closes', async ({ page, isMobile }) => {
  test.skip(!isMobile, 'Wide screens show the Settings sidebar all the time.');
  await open(page, '/settings/login');
  const menu = page.getByRole('button', { name: 'Settings sections, Login open' });
  await menu.click();
  await expect(menu).toHaveAttribute('aria-expanded', 'true');
  await expect(page.getByRole('dialog', { name: 'Settings' }).getByRole('link', { name: 'Login' })).toHaveAttribute('aria-current', 'page');
  await page.keyboard.press('Escape');
  await expect(page.getByRole('dialog', { name: 'Settings' })).toBeHidden();
  await expect(menu).toBeFocused();
  await expect(menu).toHaveAttribute('aria-expanded', 'false');
});

test('on a phone, scrolled past the Settings menu, a button brings it back and focuses it', async ({ page, isMobile }) => {
  test.skip(!isMobile, 'Wide screens show the Settings sidebar all the time.');
  await open(page, '/settings/assistants');
  const back = page.getByRole('button', { name: 'Back to the settings menu' });
  await expect(back).toBeHidden();
  await page.evaluate(() => window.scrollTo(0, document.body.scrollHeight));
  await back.click();
  const menu = page.getByRole('button', { name: /^Settings sections/ });
  await expect(menu).toBeFocused();
  await expect(menu).toBeInViewport();
  await expect(back).toBeHidden();
});

test('every control in the header and the tabs shows where focus is', async ({ page }) => {
  await open(page, '/find');
  const controls = page.locator('header.top :is(a, button), nav.tabs a');
  const count = await controls.count();
  expect(count).toBeGreaterThan(4);
  // Tab from the top of the page through all of them, checking each one's outline as it takes focus.
  await page.locator('body').focus();
  for (let index = 0; index < count; index++) {
    await page.keyboard.press('Tab');
    const focused = await page.evaluate(() => {
      const element = document.activeElement;
      const style = getComputedStyle(element);
      return { text: element.textContent.trim() || element.getAttribute('aria-label'), outline: style.outlineStyle, width: style.outlineWidth };
    });
    expect(focused.outline, `the focus outline on "${focused.text}"`).not.toBe('none');
    expect(focused.width, `the focus outline on "${focused.text}"`).not.toBe('0px');
  }
});

test('the sun and moon button switches the theme, and the choice survives a reload', async ({ page }) => {
  await open(page, '/find');
  // What is painted: the page background is #14171b in dark mode and #f5f6f8 in light.
  const dark = () => page.evaluate(() => (getComputedStyle(document.body).backgroundColor === 'rgb(20, 23, 27)' ? 'dark' : 'light'));
  const startedDark = (await dark()) === 'dark';
  await page.getByRole('button', { name: startedDark ? 'Switch to light mode' : 'Switch to dark mode' }).click();
  const chosen = startedDark ? 'light' : 'dark';
  await expect(page.locator('html')).toHaveAttribute('data-theme', chosen);

  await open(page, '/find');
  await expect(page.locator('html')).toHaveAttribute('data-theme', chosen);
  expect(await dark()).toBe(chosen);

  // Settings, Appearance goes back to following the device.
  await open(page, '/settings/appearance');
  await page.getByRole('button', { name: 'Match this device' }).click();
  await expect(page.locator('html')).not.toHaveAttribute('data-theme', /./);
  expect(await dark()).toBe(startedDark ? 'dark' : 'light');
});

test('a settings change saves by itself, once, without a Save button', async ({ page, api }) => {
  await open(page, '/settings/notifications');
  await expect(page.getByRole('button', { name: 'Save' })).toHaveCount(0);
  const saved = page.waitForRequest((request) => request.method() === 'PUT' && request.url().endsWith('/api/system/notifications'));
  await page.getByLabel('Send daily reminders at').fill('08:15');
  await page.getByLabel('Send daily reminders at').blur();
  expect((await saved).postDataJSON()).toMatchObject({ dailyTime: '08:15', webhookUrl: 'https://ntfy.example.com/invy' });
  await expect(page.getByRole('status').filter({ hasText: 'Saved' })).toBeVisible();
  expect(api.requests.filter((request) => request === 'PUT /api/system/notifications')).toHaveLength(1);
});

test('a change waiting to be saved is saved when another section opens', async ({ page, api, isMobile }) => {
  await open(page, '/settings/backups');
  await page.getByLabel(/^Keep this many backups/).fill('7');
  await page.getByLabel(/^Keep this many backups/).press('Tab');
  // Straight to another section, before the pause before saving is over.
  if (isMobile) await page.getByRole('button', { name: /^Settings sections/ }).click();
  await page.getByRole('navigation', { name: 'Settings' }).getByRole('link', { name: 'Login' }).click();
  await expect.poll(() => api.requests.filter((request) => request === 'PUT /api/system/backups').length).toBe(1);
});

test('a form sent twice quickly is only sent once', async ({ page, api }) => {
  let answer;
  api.set({ 'POST /api/tokens': (request) => new Promise((resolve) => { answer = () => resolve(data.issuedApiToken(request.postDataJSON().name)); }) });
  await open(page, '/settings/assistants');
  await page.getByLabel(/Name for a new token/).fill('Workshop tablet');
  const make = page.getByRole('button', { name: 'Make a token' });
  await make.click();
  await expect(make).toHaveAttribute('aria-busy', 'true');
  await make.click();
  answer();
  await expect(make).not.toHaveAttribute('aria-busy', 'true');
  expect(api.requests.filter((request) => request === 'POST /api/tokens')).toHaveLength(1);
});
