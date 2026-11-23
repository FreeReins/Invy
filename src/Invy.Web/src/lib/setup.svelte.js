import { enterApp } from './session.svelte.js';
import { app, run } from './state.svelte.js';

// ---------- setup ----------

/** The setup wizard's steps in order, with their labels. */
const SETUP_STEPS = [
  { screen: 'setup-account', label: 'Your account' },
  { screen: 'setup-https', label: 'Secure connection' },
];

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

/** Leaves setup for the app. */
export function finishSetup() {
  return run(enterApp);
}
