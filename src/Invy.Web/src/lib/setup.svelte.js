import { enterApp } from './session.svelte.js';
import { createSpace, loadSpaceChoices, newDraft } from './spaces.svelte.js';
import { app, run } from './state.svelte.js';

// ---------- setup ----------

/** The setup wizard's steps in order, with their labels. */
const SETUP_STEPS = [
  { screen: 'setup-account', label: 'Your account' },
  { screen: 'setup-https', label: 'Secure connection' },
  { screen: 'setup-space', label: 'Your space' },
  { screen: 'setup-features', label: "What you'll use" },
  { screen: 'setup-places', label: 'Places' },
];

/** The steps that set up the first space, which New space runs in its dialog too. */
export const SPACE_STEPS = ['setup-space', 'setup-features', 'setup-places'];

/**
 * The steps this server shows. The secure connection step is left out once HTTPS is known to be off; that is only
 * known after the account exists, so the first step counts it.
 */
export const setupSteps = () => SETUP_STEPS.filter((step) => step.screen !== 'setup-https' || app.https?.enabled !== false);

/**
 * Shows a setup step. Every step after the first needs the account, so until it exists only the first can be shown.
 */
export function openSetupStep(screen) {
  return run(async () => {
    if (screen !== 'setup-account' && !app.session?.user) return;
    const steps = setupSteps();
    const from = steps.findIndex((step) => step.screen === app.screen);
    const to = steps.findIndex((step) => step.screen === screen);
    if (SPACE_STEPS.includes(screen)) await startDraft();
    app.setupDirection = to < from ? -1 : 1;
    app.screen = screen;
  });
}

/** Moves to the next setup step (1) or the previous one (-1). Moving on from the last step finishes setup. */
export function moveSetup(offset) {
  const steps = setupSteps();
  const index = steps.findIndex((step) => step.screen === app.screen) + offset;
  if (index >= steps.length) return finishSetup();
  const next = steps[index];
  return next ? openSetupStep(next.screen) : Promise.resolve();
}

/** Loads what the space steps offer and starts their draft, the first time one of them opens. */
async function startDraft() {
  await loadSpaceChoices();
  app.spaceDraft ??= newDraft();
}

/**
 * Finishes setup: makes the first space from the draft (once), then opens `path`, Find unless asked otherwise.
 * Without a name it goes back to the step that asks for one.
 */
export function finishSetup(path = '/find') {
  return run(async () => {
    const draft = app.spaceDraft;
    if (draft && !draft.made) {
      if (!draft.name.trim()) {
        await openSetupStep('setup-space');
        throw new Error('Give the space a name, such as Home or Workshop.');
      }
      draft.made = await createSpace(draft);
    }
    history.replaceState(null, '', path);
    await enterApp();
  });
}
