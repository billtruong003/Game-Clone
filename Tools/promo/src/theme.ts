import { loadFont } from '@remotion/google-fonts/Baloo2';

// Baloo 2 is the games' own UI font (Art/Fonts), so store art and clips read like the games.
export const { fontFamily } = loadFont('normal', { weights: ['600', '700', '800'] });

export const INK = '#1E2240';
export const PAPER = '#FFF8EC';
export const AMBER = '#FFB84D';

/** Per-game colours taken from each game's splash / feature art (Tools/art/make-brand.mjs). */
export const GAMES = {
  merge: { name: 'Meh Merge', bg: '#2E2552', deep: '#221B40', accent: '#FFD23F', soft: '#B9A8FF', logo: 'logos/meh-merge-logo.png' },
  blocks: { name: 'Nah Blocks', bg: '#2B2F55', deep: '#1A1D3A', accent: '#3DDC97', soft: '#9FD9FF', logo: 'logos/nah-blocks-logo.png' },
  arrows: { name: 'Bruh Arrows', bg: '#F5F1EA', deep: '#E6DCCB', accent: '#FF5A5F', soft: '#2E3A59', logo: 'logos/bruh-arrows-logo.png' },
} as const;

export type GameKey = keyof typeof GAMES;

/** Headline text with the logo's chunky ink outline. */
export const outlined = (px: number, color = PAPER) => ({
  fontFamily,
  fontWeight: 800,
  color,
  fontSize: px,
  lineHeight: 1.0,
  letterSpacing: -0.5,
  WebkitTextStroke: `${Math.round(px * 0.11)}px ${INK}`,
  paintOrder: 'stroke fill' as const,
});
