import React from 'react';
import { AbsoluteFill, Img, staticFile } from 'remotion';
import { GAMES, GameKey, INK, PAPER, fontFamily, outlined } from './theme';

export type ShotStyle = 'band' | 'phone' | 'panorama';

export type StoreShotProps = {
  game: GameKey;
  style: ShotStyle;
  image: string;          // public/ path of a 1080 x 1920 capture
  line1: string;          // headline, first line (paper)
  line2: string;          // headline, second line (game accent)
  sticker?: string;       // phone style: the feature callout
  index?: number;         // panorama: position in the set (the background runs across screens)
};

/** One Google Play phone screenshot, 1080 x 1920. */
export const StoreShot: React.FC<StoreShotProps> = (p) => {
  const g = GAMES[p.game];
  if (p.style === 'band') return <Band {...p} />;
  if (p.style === 'phone') return <Phone {...p} />;
  return <Panorama {...p} />;
};

// A: the logo's look. Big outlined headline on the game colour, the screen as a card that runs off the bottom.
const Band: React.FC<StoreShotProps> = (p) => {
  const g = GAMES[p.game];
  return (
    <AbsoluteFill style={{ background: g.bg }}>
      <div style={{ position: 'absolute', top: 120, width: '100%', textAlign: 'center' }}>
        <div style={outlined(124, PAPER)}>{p.line1}</div>
        <div style={{ ...outlined(124, g.accent), marginTop: 6 }}>{p.line2}</div>
      </div>
      <div style={{
        position: 'absolute', left: 90, top: 520, width: 900, height: 1600, borderRadius: 64, overflow: 'hidden',
        border: `12px solid ${INK}`, boxShadow: `0 28px 0 ${g.deep}`,
      }}>
        <Img src={staticFile(p.image)} style={{ width: '100%', height: '100%', objectFit: 'cover' }} />
      </div>
    </AbsoluteFill>
  );
};

// B: light background, the game on a tilted phone, the feature on a sticker.
const Phone: React.FC<StoreShotProps> = (p) => {
  const g = GAMES[p.game];
  return (
    <AbsoluteFill style={{ background: `linear-gradient(170deg, ${PAPER} 0%, ${g.soft} 100%)` }}>
      <div style={{ position: 'absolute', left: 80, top: 110, fontFamily, fontWeight: 800, color: INK, fontSize: 112, lineHeight: 1.0 }}>
        {p.line1}<br /><span style={{ color: g.bg }}>{p.line2}</span>
      </div>
      <div style={{
        position: 'absolute', left: 150, top: 470, width: 800, height: 1520, background: '#15161C', borderRadius: 96,
        padding: 26, transform: 'rotate(-4deg)', boxShadow: '0 40px 60px rgba(30,34,64,0.35)',
      }}>
        <div style={{ width: '100%', height: '100%', borderRadius: 72, overflow: 'hidden' }}>
          <Img src={staticFile(p.image)} style={{ width: '100%', height: '100%', objectFit: 'cover' }} />
        </div>
      </div>
      {p.sticker && (
        <div style={{
          position: 'absolute', right: 60, top: 560, maxWidth: 520, padding: '22px 34px', background: g.accent,
          border: `8px solid ${INK}`, borderRadius: 40, transform: 'rotate(6deg)', fontFamily, fontWeight: 800,
          fontSize: 54, lineHeight: 1.05, color: INK, boxShadow: `0 10px 0 ${INK}`,
        }}>{p.sticker}</div>
      )}
    </AbsoluteFill>
  );
};

// C: one long background behind the whole set (the aim line of the game runs from screen to screen).
const Panorama: React.FC<StoreShotProps> = (p) => {
  const g = GAMES[p.game];
  const i = p.index ?? 0;
  const left = i % 2 === 0;
  return (
    <AbsoluteFill style={{ background: g.deep, overflow: 'hidden' }}>
      <svg width={1080} height={1920} style={{ position: 'absolute' }} viewBox={`${i * 1080} 0 1080 1920`}>
        <circle cx={1500} cy={1300} r={1050} fill={g.bg} />
        <circle cx={3900} cy={500} r={900} fill={g.bg} />
        <path d="M -100 420 C 700 200, 1400 1700, 2200 900 S 3300 300, 4400 1100" fill="none" stroke={g.accent}
          strokeWidth={22} strokeLinecap="round" strokeDasharray="2 58" />
      </svg>
      <div style={{ position: 'absolute', top: 110, width: '100%', padding: '0 80px', textAlign: left ? 'left' : 'right' }}>
        <div style={{ fontFamily, fontWeight: 700, fontSize: 46, color: g.soft, letterSpacing: 2 }}>{p.line1.toUpperCase()}</div>
        <div style={{ ...outlined(116, g.accent), marginTop: 10 }}>{p.line2}</div>
      </div>
      <div style={{
        position: 'absolute', left: left ? 70 : 150, top: 460, width: 860, height: 1530, borderRadius: 60, overflow: 'hidden',
        border: `12px solid ${INK}`, transform: `rotate(${left ? -2.5 : 2.5}deg)`,
      }}>
        <Img src={staticFile(p.image)} style={{ width: '100%', height: '100%', objectFit: 'cover' }} />
      </div>
    </AbsoluteFill>
  );
};
