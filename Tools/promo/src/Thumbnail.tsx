import React from 'react';
import { AbsoluteFill, Img, staticFile } from 'remotion';
import { fontFamily, INK, PAPER } from './theme';

// YouTube Shorts covers for the three preview videos: 1080 x 1920 (9:16), one peak gameplay frame from the video plus
// the video's own caption style. Text and logo stay inside the middle band so the Shorts UI (top bar, title and buttons
// at the bottom, the right-hand action column) never covers them.

const text = (px: number, color: string, stroke = true): React.CSSProperties => ({
  fontFamily, fontSize: px, fontWeight: 800, lineHeight: 1, color, margin: 0, letterSpacing: -1, whiteSpace: 'nowrap',
  ...(stroke ? { WebkitTextStroke: `${Math.round(px * 0.11)}px ${INK}`, paintOrder: 'stroke fill' } : {}),
});

const Frame: React.FC<{ src: string; zoom?: number; origin?: [number, number] }> = ({ src, zoom = 1, origin = [0.5, 0.5] }) => (
  <Img src={staticFile(src)} style={{
    position: 'absolute', width: 1080, height: 1920, transform: `scale(${zoom})`,
    transformOrigin: `${origin[0] * 100}% ${origin[1] * 100}%`,
  }} />
);

const Logo: React.FC<{ src: string; top: number }> = ({ src, top }) => (
  // logo PNGs are 1600 x 600 with ~45% transparent margin
  <Img src={staticFile(src)} style={{ position: 'absolute', left: -260, top, width: 1600, filter: `drop-shadow(0 14px 0 ${INK})` }} />
);

export const MergeThumb: React.FC = () => (
  <AbsoluteFill style={{ background: '#2E2552' }}>
    <Frame src="frames/m_classic/00242.jpg" zoom={1.55} origin={[0.2, 0.8]} />
    <AbsoluteFill style={{ background: 'linear-gradient(180deg, rgba(46,37,82,0.95) 0%, rgba(46,37,82,0.75) 40%, rgba(46,37,82,0) 56%)' }} />
    <Logo src="logos/meh-merge-logo.png" top={150} />
    <div style={{ position: 'absolute', left: 0, top: 720, width: 1080, display: 'flex', flexDirection: 'column', alignItems: 'center', gap: 14, transform: 'rotate(-4deg)' }}>
      <p style={text(150, PAPER)}>One drop.</p>
      <p style={text(170, '#FFD23F')}>x8 combo!</p>
    </div>
  </AbsoluteFill>
);

export const BlocksThumb: React.FC = () => (
  <AbsoluteFill style={{ background: '#2B2F55' }}>
    <Frame src="frames/b_hook/00042.jpg" zoom={1.25} origin={[0.5, 0.42]} />
    <AbsoluteFill style={{ background: 'linear-gradient(180deg, rgba(43,47,85,1) 0%, rgba(43,47,85,1) 20%, rgba(43,47,85,0) 30%)' }} />
    <Logo src="logos/nah-blocks-logo.png" top={110} />
    <div style={{
      position: 'absolute', left: -42, top: 1060, width: 1164, height: 300, background: '#3DDC97', border: `14px solid ${INK}`,
      transform: 'rotate(-5deg)', display: 'flex', alignItems: 'center', justifyContent: 'center', boxShadow: `0 24px 0 ${INK}`, boxSizing: 'border-box',
    }}>
      <p style={text(150, PAPER)}>TRIPLE CLEAR</p>
    </div>
  </AbsoluteFill>
);

const Tape: React.FC<{ style: React.CSSProperties }> = ({ style }) => (
  <div style={{
    position: 'absolute', width: 240, height: 84, background: 'repeating-linear-gradient(45deg, #FFC2B8 0 26px, rgba(255,255,255,0.45) 26px 46px)', ...style,
  }} />
);

export const ArrowsThumb: React.FC = () => (
  <AbsoluteFill style={{ background: '#F5F1EA' }}>
    <Frame src="frames/a_blocked/00022.jpg" zoom={1.2} origin={[0.5, 0.58]} />
    <AbsoluteFill style={{ background: 'linear-gradient(180deg, #F5F1EA 0%, #F5F1EA 26%, rgba(245,241,234,0) 34%)' }} />
    <div style={{
      position: 'absolute', left: 60, top: 250, width: 960, height: 380, background: '#EBDCBC', borderBottom: '12px solid #C9B48F',
      transform: 'rotate(-2deg)', boxShadow: '0 22px 36px rgba(30,34,64,0.28)',
    }}>
      <Tape style={{ left: -70, top: -36, transform: 'rotate(-14deg)' }} />
      <Tape style={{ right: -70, top: -36, transform: 'rotate(12deg)' }} />
      <Img src={staticFile('logos/bruh-arrows-logo.png')} style={{ position: 'absolute', left: -320, top: -175, width: 1600 }} />
    </div>
    <div style={{ position: 'absolute', left: 0, top: 1080, width: 1080, textAlign: 'center', transform: 'rotate(-6deg)' }}>
      <p style={{ ...text(300, '#FF5A5F'), display: 'inline-block' }}>Bruh.</p>
    </div>
  </AbsoluteFill>
);
