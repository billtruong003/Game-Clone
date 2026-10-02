import React from 'react';
import { AbsoluteFill, Img, staticFile } from 'remotion';
import { AMBER, fontFamily } from './theme';

// Bill The Dev channel art. The personal brand: black + amber, the fox mark (sunglasses band), nothing else.

/** Avatar 800 x 800 (YouTube crops it to a circle). */
export const Avatar: React.FC<{ variant: 'dark' | 'amber' }> = ({ variant }) => (
  <AbsoluteFill style={{ background: variant === 'dark' ? '#0B0B0C' : AMBER, alignItems: 'center', justifyContent: 'center' }}>
    {variant === 'dark' && <div style={{ position: 'absolute', width: 760, height: 760, borderRadius: '50%', border: `14px solid ${AMBER}` }} />}
    <Img src={staticFile(variant === 'dark' ? 'brand/bill-the-dev-mark-on-dark.svg' : 'brand/bill-the-dev-mark.svg')}
      style={{ width: variant === 'dark' ? 560 : 600, height: variant === 'dark' ? 560 : 600, marginTop: 40 }} />
  </AbsoluteFill>
);

const games = [
  { icon: 'logos/meh-merge-icon.png', name: 'Meh Merge' },
  { icon: 'logos/nah-blocks-icon.png', name: 'Nah Blocks' },
  { icon: 'logos/bruh-arrows-icon.png', name: 'Bruh Arrows' },
];

/** Banner 2560 x 1440. Everything that matters sits in the 1546 x 423 safe area in the middle (all devices). */
export const Banner: React.FC<{ variant: 'clean' | 'games'; showSafe?: boolean }> = ({ variant, showSafe }) => (
  <AbsoluteFill style={{ background: '#0B0B0C', overflow: 'hidden' }}>
    {variant === 'games' && (
      <div style={{ position: 'absolute', inset: -200, display: 'flex', flexWrap: 'wrap', gap: 40, transform: 'rotate(-8deg)', opacity: 0.22 }}>
        {Array.from({ length: 24 }).map((_, k) => (
          <Img key={k} src={staticFile(['logos/meh-merge-feature.png', 'logos/nah-blocks-feature.png', 'logos/bruh-arrows-feature.png'][k % 3])}
            style={{ width: 620, height: 303, borderRadius: 28 }} />
        ))}
      </div>
    )}
    {variant === 'clean' && (
      <svg width={2560} height={1440} style={{ position: 'absolute' }}>
        <path d="M0 1190 L2560 980 L2560 1440 L0 1440 Z" fill="#141416" />
        <path d="M0 1190 L2560 980" stroke={AMBER} strokeWidth={6} />
      </svg>
    )}
    <div style={{ position: 'absolute', left: 507, top: 508, width: 1546, height: 423, display: 'flex', alignItems: 'center', gap: 48 }}>
      <Img src={staticFile('brand/bill-the-dev-mark-on-dark.svg')} style={{ width: 280, height: 280, flex: 'none' }} />
      <div style={{ flex: 1, whiteSpace: 'nowrap' }}>
        <div style={{ fontFamily, fontWeight: 800, fontSize: 128, lineHeight: 0.95, color: '#FFFFFF' }}>
          Bill <span style={{ color: AMBER }}>The Dev</span>
        </div>
        <div style={{ fontFamily, fontWeight: 600, fontSize: 44, color: '#B9B4AA', marginTop: 12 }}>Indie games · gameplay · devlogs</div>
      </div>
      <div style={{ display: 'flex', gap: 22, flex: 'none' }}>
        {games.map((g) => (
          <div key={g.name} style={{ textAlign: 'center' }}>
            <Img src={staticFile(g.icon)} style={{ width: 128, height: 128, borderRadius: 30 }} />
            <div style={{ fontFamily, fontWeight: 700, fontSize: 24, color: '#E9E4DA', whiteSpace: 'nowrap', marginTop: 6 }}>{g.name}</div>
          </div>
        ))}
      </div>
    </div>
    {showSafe && <div style={{ position: 'absolute', left: 507, top: 508, width: 1546, height: 423, border: '4px dashed #FF3B6B' }} />}
  </AbsoluteFill>
);
