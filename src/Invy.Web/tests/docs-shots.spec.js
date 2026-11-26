// The screenshots the docs show, answered from the test stubs. Runs only through ./dev.sh docs-shots (DOCS_SHOTS
// set; playwright.config.js skips it otherwise), in light mode on a wide screen, and saves each into docs-shots/,
// which dev.sh moves to docs/images/. A page that shows one links it as ../images/<name>.png.
import { devices } from '@playwright/test';
import { test, expect, open } from './fixtures.js';
import * as data from './data.js';

// Tall enough that a whole setup step fits above its pinned bar.
test.use({ viewport: { width: 1100, height: 1400 } });

/** Saves one part of the page as docs/images/<name>.png, once nothing is moving. */
async function shoot(page, locator, name) {
  await expect(locator).toBeVisible();
  await page.mouse.move(0, 0); // no hover colour left on what was last clicked
  await page.waitForFunction(() => document.getAnimations().every((animation) => animation.playState !== 'running'));
  await locator.screenshot({ path: `docs-shots/${name}.png`, animations: 'disabled' });
}

test('setup: the steps for the first space', async ({ page, api }) => {
  let made = [];
  api.set({
    'GET /api/session': { setupRequired: true, user: null },
    'POST /api/setup/account': data.admin(),
    'GET /api/spaces': () => made,
    'POST /api/spaces': (request) => { made = [{ ...data.spaces()[0], ...request.postDataJSON(), role: 'Owner' }]; return made[0]; },
  });
  await open(page, '/');
  await page.getByLabel('Your name').fill('Alex Rivera');
  await page.getByLabel('Username').fill('alex');
  await page.getByLabel(/^Password/).fill('correct horse');
  await page.getByRole('button', { name: 'Create account' }).click();
  await page.getByRole('button', { name: 'Continue' }).click();
  await page.getByRole('heading', { name: 'Secure connection' }).waitFor();
  await page.getByRole('button', { name: 'Next' }).click();

  const step = page.locator('main');
  await page.getByRole('heading', { name: 'Your space' }).waitFor();
  await shoot(page, step, 'setup-your-space');
  await page.getByRole('button', { name: 'Next' }).click();
  await page.getByRole('heading', { name: "What you'll use" }).waitFor();
  await shoot(page, step, 'setup-features');
  await page.getByRole('button', { name: 'Next' }).click();
  await page.getByRole('checkbox', { name: 'Kitchen' }).check();
  await page.getByRole('checkbox', { name: 'Garage' }).check();
  await page.getByRole('checkbox', { name: /^Add a few example items/ }).check();
  await shoot(page, step, 'setup-places');
});

test('find: the getting-started card', async ({ page }) => {
  await open(page, '/find');
  await shoot(page, page.getByRole('region', { name: 'Getting started' }), 'getting-started');
});

test('settings: kind and features', async ({ page }) => {
  await open(page, '/settings/space');
  await page.getByRole('button', { name: 'Remove examples (2)' }).waitFor();
  await shoot(page, page.locator('#settings-body'), 'kind-and-features');
});

test('types: add from another pack', async ({ page }) => {
  await open(page, '/types');
  await page.getByRole('button', { name: 'Add from another pack' }).click();
  await shoot(page, page.getByRole('dialog', { name: 'Add from another pack' }).locator('.box'), 'add-from-another-pack');
});

// The README's screenshots: Find in light and dark on a desktop, then the main tabs on a phone.
test.describe('readme', () => {
  test.use({ viewport: { width: 1280, height: 600 } });

  for (const scheme of ['light', 'dark']) {
    test(`find on a desktop, ${scheme}`, async ({ page }) => {
      await page.emulateMedia({ colorScheme: scheme });
      await open(page, '/find?q=baseball');
      await expect(page.getByText('2 items found')).toBeVisible();
      await shoot(page, page.locator('body'), `find-desktop-${scheme}`);
    });
  }
});

test.describe('readme on a phone', () => {
  // Pixel 7, without its browser type, which a describe block can't change.
  const { defaultBrowserType, ...pixel } = devices['Pixel 7'];
  test.use(pixel);

  const screens = [
    { name: 'find-phone', path: '/find?q=baseball', ready: (page) => page.getByText('2 items found') },
    { name: 'browse-phone', path: `/browse/${data.itemIds.f}`, ready: (page) => page.getByText('Inside Box F') },
    { name: 'scan-phone', path: '/scan', ready: (page) => page.getByRole('button', { name: 'Start the camera' }) },
    { name: 'views-phone', path: '/views', ready: (page) => page.getByRole('link', { name: 'Not seen in a year' }) },
  ];
  for (const screen of screens) {
    test(screen.name, async ({ page }) => {
      await open(page, screen.path);
      await expect(screen.ready(page)).toBeVisible();
      await page.mouse.move(0, 0);
      await page.waitForFunction(() => document.getAnimations().every((animation) => animation.playState !== 'running'));
      await page.screenshot({ path: `docs-shots/${screen.name}.png`, animations: 'disabled' });
    });
  }
});
