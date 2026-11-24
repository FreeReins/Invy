// The features a space can switch on and off (docs/developers/design.md, Features). A feature that is off is hidden,
// not blocked: its buttons, modes and totals don't show, but its data stays and comes back when it is switched on.
import { currentSpace } from './spaces.svelte.js';

/** True when the space the page shows has this feature on, such as 'audits'. False with no space. */
export const has = (key) => currentSpace()?.features?.includes(key) ?? false;
