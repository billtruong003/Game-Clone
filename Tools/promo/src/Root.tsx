import React from 'react';
import { AbsoluteFill, Composition, Still } from 'remotion';
import { StoreShot, StoreShotProps, ShotStyle } from './StoreShot';
import { Avatar, Banner } from './Channel';

// Mockup boards: three Meh Merge screens side by side per frame style, so the styles can be compared and one locked.
const mergeSet: Omit<StoreShotProps, 'style'>[] = [
  { game: 'merge', image: 'shots/merge/raw_classic.jpg', line1: 'Drop. Merge.', line2: "Don't overflow.", sticker: 'Two mehs make a bigger meh', index: 0 },
  { game: 'merge', image: 'shots/merge/raw_skin_eyeballs.jpg', line1: 'Eyeball skin', line2: 'They watch you', sticker: 'Every eye follows your ball', index: 1 },
  { game: 'merge', image: 'shots/merge/raw_skin_billiard.jpg', line1: 'Billiard, Sports', line2: '& more skins', sticker: 'Skins change the look, never the rules', index: 2 },
];

const Board: React.FC<{ style: ShotStyle }> = ({ style }) => (
  <AbsoluteFill style={{ background: '#D9D6CF', flexDirection: 'row', gap: 40, padding: 40 }}>
    {mergeSet.map((s, k) => (
      <div key={k} style={{ width: 1080, height: 1920, position: 'relative', flex: 'none', overflow: 'hidden' }}>
        <StoreShot {...s} style={style} />
      </div>
    ))}
  </AbsoluteFill>
);

export const Root: React.FC = () => (
  <>
    {(['band', 'phone', 'panorama'] as ShotStyle[]).map((s) => (
      <Still key={s} id={`board-${s}`} component={Board} width={3400} height={2000} defaultProps={{ style: s }} />
    ))}
    <Still id="avatar-dark" component={Avatar} width={800} height={800} defaultProps={{ variant: 'dark' as const }} />
    <Still id="avatar-amber" component={Avatar} width={800} height={800} defaultProps={{ variant: 'amber' as const }} />
    <Still id="banner-clean" component={Banner} width={2560} height={1440} defaultProps={{ variant: 'clean' as const, showSafe: true }} />
    <Still id="banner-games" component={Banner} width={2560} height={1440} defaultProps={{ variant: 'games' as const, showSafe: true }} />
  </>
);
