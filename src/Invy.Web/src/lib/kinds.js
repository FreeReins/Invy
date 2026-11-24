// The icons for the kinds of space, by the generic name a starter pack's `icon` gives (config/types-*.json).
import Building from '@lucide/svelte/icons/building';
import House from '@lucide/svelte/icons/house';
import Package from '@lucide/svelte/icons/package';
import PackageOpen from '@lucide/svelte/icons/package-open';
import Palette from '@lucide/svelte/icons/palette';
import Star from '@lucide/svelte/icons/star';

const ICONS = { building: Building, house: House, palette: Palette, star: Star };

/** The icon component for a pack's icon name: an open box for no kind, a box for a name without its own icon. */
export const kindIcon = (name) => (name === undefined ? PackageOpen : ICONS[name] ?? Package);
