// Deterministic flat-vector asset batches for Casual Game.
// Run: node Tools/art/generate-full-run.mjs
import fs from 'node:fs';
import path from 'node:path';
import sharp from 'sharp';
import { fileURLToPath } from 'node:url';

const HERE = path.dirname(fileURLToPath(import.meta.url));
const OUT = path.join(HERE, 'full-run');
fs.mkdirSync(OUT, { recursive: true });

const C = {
  ink:'#1E2240', cream:'#FFF8EC', white:'#FFFFFF', red:'#FF5A5F', orange:'#FF9F1C',
  yellow:'#FFD23F', green:'#3DDC97', blue:'#4EA8DE', purple:'#9B5DE5', pink:'#F15BB5',
  gray:'#AEB3C8', pale:'#DCE1F2', dark:'#34385F', blush:'#FF8FA3', glass:'#BFE8FF'
};
const dark = {red:'#D9404B',orange:'#DA7D0F',yellow:'#D5AC29',green:'#24B978',blue:'#3189BD',purple:'#7443BC',pink:'#C83E91',gray:'#858BA4'};
const svg=(w,h,b,bg='')=>`<svg xmlns="http://www.w3.org/2000/svg" width="${w}" height="${h}" viewBox="0 0 ${w} ${h}">${bg}${b}</svg>`;
const g=(x,y,s,b,extra='')=>`<g transform="translate(${x} ${y}) scale(${s})" ${extra}>${b}</g>`;
const circle=(x,y,r,fill,stroke='none',sw=0,extra='')=>`<circle cx="${x}" cy="${y}" r="${r}" fill="${fill}" stroke="${stroke}" stroke-width="${sw}" ${extra}/>`;
const ellipse=(x,y,rx,ry,fill,stroke='none',sw=0,extra='')=>`<ellipse cx="${x}" cy="${y}" rx="${rx}" ry="${ry}" fill="${fill}" stroke="${stroke}" stroke-width="${sw}" ${extra}/>`;
const rect=(x,y,w,h,rx,fill,stroke='none',sw=0,extra='')=>`<rect x="${x}" y="${y}" width="${w}" height="${h}" rx="${rx}" fill="${fill}" stroke="${stroke}" stroke-width="${sw}" ${extra}/>`;
const pathEl=(d,fill='none',stroke=C.ink,sw=8,extra='')=>`<path d="${d}" fill="${fill}" stroke="${stroke}" stroke-width="${sw}" stroke-linecap="round" stroke-linejoin="round" ${extra}/>`;
const poly=(pts,fill,stroke=C.ink,sw=8,extra='')=>`<polygon points="${pts}" fill="${fill}" stroke="${stroke}" stroke-width="${sw}" stroke-linejoin="round" ${extra}/>`;
const starPts=(cx,cy,ro,ri,n=5,rot=-90)=>Array.from({length:n*2},(_,i)=>{const r=i%2?ri:ro,a=(rot+i*180/n)*Math.PI/180;return`${cx+r*Math.cos(a)},${cy+r*Math.sin(a)}`}).join(' ');
const heartD=(cx,cy,s)=>`M${cx},${cy+s} C${cx-s*1.5},${cy} ${cx-s},${cy-s*1.15} ${cx},${cy-s*.35} C${cx+s},${cy-s*1.15} ${cx+s*1.5},${cy} ${cx},${cy+s}Z`;
const hi=(x=78,y=64,w=62)=>rect(x,y,w,18,9,C.white,'none',0,'opacity=".6"');
const face=(kind='happy')=>{
  const eyes=ellipse(98,116,13,20,C.ink)+circle(94,110,4,C.white)+ellipse(158,116,13,20,C.ink)+circle(154,110,4,C.white);
  if(kind==='tiny') return eyes+pathEl('M112 154 Q128 168 144 154','none',C.ink,7);
  return eyes+ellipse(73,148,15,8,C.blush)+ellipse(183,148,15,8,C.blush)+pathEl('M103 154 Q128 177 153 154','none',C.ink,8);
};
const ball=(fill=C.pink,withFace=true)=>circle(128,128,105,fill,C.ink,10)+pathEl('M49 143 A105 105 0 0 0 207 185 L205 203 A105 105 0 0 1 49 143Z',dark.pink,'none',0)+hi(68,55,55)+(withFace?face():'');
const block=(fill=C.green,shade=dark.green,withFace=true)=>rect(26,26,204,204,46,fill,C.ink,10)+pathEl('M31 174 Q31 225 78 225 L178 225 Q225 225 225 174Z',shade,'none',0)+hi(58,48,72)+(withFace?face():'');
const pill=(fill=C.green,shade=dark.green,pressed=false)=>{
  const y=pressed?90:74, topY=pressed?72:54, baseH=pressed?76:92;
  return rect(24,y,208,baseH,46,C.ink)+rect(30,y+6,196,baseH-12,40,shade)+rect(24,topY,208,78,39,fill,C.ink,10)+hi(58,topY+15,74);
};
const puff=()=>pathEl('M62 161 Q32 135 56 107 Q70 91 91 98 Q88 65 119 60 Q145 54 158 80 Q183 65 202 86 Q220 105 205 128 Q230 146 215 171 Q198 195 169 181 Q151 210 123 193 Q98 211 78 190 Q49 190 62 161Z',C.cream,C.ink,10);
const jar=()=>pathEl('M69 63 Q68 48 88 44 L168 44 Q188 48 187 63 L179 196 Q177 220 154 226 L102 226 Q79 220 77 196Z',C.glass,C.ink,10,'fill-opacity=".48"')+pathEl('M78 65 Q128 84 178 65','none',C.ink,9)+rect(91,85,16,92,8,C.white,'none',0,'opacity=".65"');

async function write(name,w,h,body,bg=''){
  const file=path.join(OUT,name);
  await sharp(Buffer.from(svg(w,h,body,bg))).png().toFile(file);
  return file;
}
function sheet(cols,rows,cw,ch,items){return items.map((b,i)=>b?g((i%cols)*cw,(Math.floor(i/cols))*ch,Math.min(cw,ch)/256,b):'').join('');}

// 01 / BATCH 00
await write('01_batch00_style_bible.png',1536,1024,sheet(3,2,512,512,[ball(),block(),pill(),pathEl('M35 220 L35 72 Q35 35 72 35 L220 35 L220 220Z',C.cream,C.ink,10)+pathEl('M58 220 L58 88 Q58 58 88 58 L220 58','none','#DDD5C7',5),puff(),jar()]));

// 02 / BATCH 01
const eyeOpen=ellipse(128,128,34,48,C.white,C.ink,11)+ellipse(128,137,17,24,C.ink)+circle(121,128,6,C.white);
const eyeHalf=pathEl('M91 129 Q128 97 165 129 Q128 151 91 129Z',C.white,C.ink,10)+circle(128,132,14,C.ink)+circle(122,126,5,C.white)+pathEl('M91 129 L165 129','none',C.ink,10);
const eyeClosed=(up=true)=>pathEl(up?'M92 140 Q128 99 164 140':'M92 118 Q128 156 164 118','none',C.ink,12);
const eyeShape=(shape)=>shape==='heart'?pathEl(heartD(128,128,30),C.red,C.ink,9):shape==='star'?poly(starPts(128,130,45,20),C.yellow,C.ink,9):shape==='spiral'?pathEl('M128 128 C128 99 88 103 93 137 C99 179 169 173 169 119 C169 70 91 72 80 126','none',C.ink,10):ellipse(128,128,47,57,C.white,C.ink,10)+ellipse(128,137,28,36,C.ink)+circle(118,124,8,C.white);
const mouth=(k)=>({smile:pathEl('M88 116 Q128 164 168 116','none',C.ink,12),grin:pathEl('M82 105 Q128 179 174 105Z',C.white,C.ink,10),laugh:pathEl('M82 100 Q128 186 174 100Z','#7A2436',C.ink,10)+ellipse(128,150,28,12,C.blush),o:ellipse(128,130,27,36,'#7A2436',C.ink,10),tongue:pathEl('M91 107 Q128 179 165 107Z','#7A2436',C.ink,10)+ellipse(128,151,24,14,C.blush),frown:pathEl('M91 151 Q128 111 165 151','none',C.ink,12),wavy:pathEl('M80 134 Q96 114 112 134 T144 134 T176 134','none',C.ink,11)})[k];
const shades=pathEl('M53 102 L111 102 L105 151 Q82 168 59 148Z',C.ink,C.ink,8)+pathEl('M145 102 L203 102 L197 148 Q174 168 151 151Z',C.ink,C.ink,8)+pathEl('M111 110 L145 110','none',C.ink,10);
await write('02_batch01_face_kit.png',1024,1024,sheet(4,4,256,256,[eyeOpen,eyeHalf,eyeClosed(true),eyeClosed(false),eyeShape('heart'),eyeShape('star'),eyeShape('spiral'),eyeShape('wide'),mouth('smile'),mouth('grin'),mouth('laugh'),mouth('o'),mouth('tongue'),mouth('frown'),mouth('wavy'),shades]));

// 03 / BATCH 01b
const extras=[pathEl('M72 145 Q128 88 184 123','none',C.ink,14),pathEl('M70 105 L118 137 M186 105 L138 137','none',C.ink,14),ellipse(128,128,57,26,C.blush,'none',0,'opacity=".8"'),pathEl('M128 58 Q84 123 128 187 Q172 123 128 58Z',C.blue,C.ink,9),pathEl('M128 62 Q89 119 128 176 Q167 119 128 62Z',C.blue,C.ink,9),circle(128,128,62,C.glass,C.ink,9,'fill-opacity=".62"')+circle(105,100,15,C.white,'none',0,'opacity=".65"'),circle(128,128,43,C.ink),circle(128,128,18,C.white)];
await write('03_batch01b_face_extras.png',1024,1024,sheet(4,4,256,256,[...extras,...Array(8).fill('')]));

// 04 / BATCH 02
const blockAsset=(f,s,ghost=false)=>rect(34,34,188,188,42,f,C.ink,10,ghost?'opacity=".4"':'')+pathEl('M39 170 Q39 217 82 217 L174 217 Q217 217 217 170Z',s,'none',0,ghost?'opacity=".4"':'')+hi(62,54,68);
const blockCols=[[C.red,dark.red],[C.orange,dark.orange],[C.yellow,dark.yellow],[C.green,dark.green],[C.blue,dark.blue],[C.purple,dark.purple],[C.pink,dark.pink],[C.gray,dark.gray],[C.white,C.pale]];
await write('04_batch02_blocks.png',1024,1024,sheet(3,3,1024/3,1024/3,blockCols.map((x,i)=>blockAsset(x[0],x[1],i===8))));

// Shared merge orb and theme details.
const orb=(fill,detail='',faceOn=false,shade=null)=>circle(128,128,88,fill,C.ink,9)+(shade?pathEl('M48 145 A88 88 0 0 0 208 145 A88 88 0 0 1 48 145Z',shade,'none',0):'')+hi(70,60,50)+detail+(faceOn?g(0,8,.72,face('tiny')):'');
const shop=(accent)=>rect(45,53,166,151,32,C.cream,C.ink,9)+pathEl('M82 72 Q128 22 174 72','none',C.ink,10)+circle(128,134,43,accent,C.ink,8)+poly(starPts(128,136,25,11),C.white,'none',0);
const eyeDetail=(kind)=>({dot:circle(128,132,24,C.ink)+circle(119,122,7,C.white),cat:ellipse(128,132,25,38,C.ink)+ellipse(128,132,7,31,C.yellow),goat:ellipse(128,132,35,22,C.ink)+rect(103,126,50,12,6,C.yellow),star:poly(starPts(128,132,32,14),C.ink,'none',0),heart:pathEl(heartD(128,132,25),C.ink,'none',0),spiral:pathEl('M128 132 C128 106 95 112 101 141 C109 174 163 157 158 119','none',C.ink,9),rings:circle(128,132,31,'none',C.ink,8)+circle(128,132,15,C.ink),flame:pathEl('M128 164 Q89 137 116 91 Q119 119 137 95 Q169 135 128 164Z',C.ink,'none',0),flower:Array.from({length:6},(_,i)=>ellipse(128+27*Math.cos(i*Math.PI/3),132+27*Math.sin(i*Math.PI/3),13,24,C.ink,'none',0,`transform="rotate(${i*60+90} ${128+27*Math.cos(i*Math.PI/3)} ${132+27*Math.sin(i*Math.PI/3)})"`)).join('')+circle(128,132,14,C.yellow),rainbow:circle(128,132,39,C.pink)+circle(128,132,31,C.yellow)+circle(128,132,23,C.blue)+circle(128,132,14,C.ink),god:ellipse(128,132,49,31,C.white,C.ink,8)+ellipse(128,132,23,28,C.purple,C.ink,7)+circle(128,132,10,C.ink)})[kind];
async function mergeSheet(n,theme,items,accent){await write(`${String(n).padStart(2,'0')}_batch03_${theme}.png`,1024,1024,sheet(4,4,256,256,[...items,shop(accent),'','','']));}
const eyeKinds=['dot','cat','goat','star','heart','spiral','rings','flame','flower','rainbow','god'];
await mergeSheet(5,'eyes',eyeKinds.map((k,i)=>orb(C.white,eyeDetail(k),false,i%2?C.pale:null)),C.purple);
const pal=[C.red,C.orange,C.yellow,C.green,C.blue,C.purple,C.pink,'#65D6C6','#86BBD8','#B8E986','#FF8FA3'];
await mergeSheet(6,'emoji',pal.map((x,i)=>orb(x,'',false,Object.values(dark)[i%8])),C.green);
const fruitNames=['cherry','strawberry','grape','mandarin','persimmon','apple','pear','peach','pineapple','melon','watermelon'];
const fruitCols=[C.red,C.red,C.purple,C.orange,C.orange,C.red,C.green,C.pink,C.yellow,C.green,C.green];
const leaf=pathEl('M128 51 Q145 29 164 49 Q145 61 128 65Z',C.green,C.ink,7);
await mergeSheet(7,'fruit',fruitNames.map((_,i)=>orb(fruitCols[i],leaf,true,Object.values(dark)[i%8])),C.red);
const nums=Array.from({length:11},(_,i)=>orb(i%2?C.white:pal[i%pal.length],circle(128,128,35,C.white,C.ink,6)+`<text x="128" y="143" text-anchor="middle" font-family="Arial" font-size="42" font-weight="700" fill="${C.ink}">${i+1}</text>`,false,i%2?C.pale:Object.values(dark)[i%8]));
await mergeSheet(8,'billiards',nums,C.blue);
const planetCols=['#8C7A6B','#D8DBE6','#AEB3C8',C.red,C.orange,C.blue,'#3979C6','#65D6C6',C.yellow,'#D58B38',C.yellow];
const planetDetails=planetCols.map((_,i)=>i===8?pathEl('M47 128 Q128 92 209 128 Q128 164 47 128','none',C.ink,13):i===5?pathEl('M75 108 Q96 87 111 109 Q130 93 147 112 Q171 100 185 124','none',C.green,17):i===10?Array.from({length:10},(_,k)=>pathEl(`M128 ${27+k%2*8} L128 ${43+k%2*8}`,'none',C.orange,8,`transform="rotate(${k*36} 128 128)"`)).join(''):'' );
await mergeSheet(9,'planets',planetCols.map((x,i)=>orb(x,planetDetails[i],false,i===10?C.orange:C.dark)),C.blue);
const candyCols=[C.pink,C.red,C.white,C.orange,C.purple,C.pink,C.orange,C.cream,C.red,C.yellow,C.purple];
await mergeSheet(10,'candy',candyCols.map((x,i)=>orb(x,i===3?circle(128,128,30,C.cream,C.ink,7):i===4?pathEl('M48 128 L208 128','none',C.cream,24):i===1?pathEl('M128 202 L128 229','none',C.ink,12):'',true,Object.values(dark)[i%8])),C.pink);
const petCols=['#C99052',C.yellow,C.white,C.gray,C.orange,C.orange,C.white,'#A06E44',C.orange,C.yellow,C.blue];
const ears=(i)=>i===1||i===10?'':(i===2?ellipse(92,48,18,38,C.white,C.ink,8)+ellipse(164,48,18,38,C.white,C.ink,8):circle(76,67,26,petCols[i],C.ink,8)+circle(180,67,26,petCols[i],C.ink,8));
await mergeSheet(11,'pets',petCols.map((x,i)=>orb(x,ears(i),true,Object.values(dark)[i%8])),C.orange);
const sportCols=[C.orange,C.white,C.green,C.white,C.red,C.purple,C.white,C.white,C.orange,C.orange,C.yellow];
const seams=[circle(128,128,18,'none',C.ink,4),circle(128,128,62,'none',C.gray,4),pathEl('M62 94 Q128 128 194 94 M62 162 Q128 128 194 162','none',C.white,6),pathEl('M59 87 Q128 128 197 87 M59 169 Q128 128 197 169','none',C.red,7),pathEl('M82 66 L174 190 M174 66 L82 190','none',C.ink,6),circle(128,128,24,C.ink),pathEl('M50 128 L206 128 M128 50 L128 206','none',C.ink,6),poly('128,65 162,90 150,130 106,130 94,90',C.ink)+pathEl('M94 90 L60 78 M162 90 L196 78 M106 130 L83 181 M150 130 L173 181','none',C.ink,7),pathEl('M63 100 Q128 127 193 100 M63 156 Q128 129 193 156','none',C.ink,7),pathEl('M59 92 Q128 128 197 92 M59 164 Q128 128 197 164','none',C.cream,6),pathEl('M48 128 Q128 88 208 128 Q128 168 48 128','none',C.white,18)];
await mergeSheet(12,'sports',sportCols.map((x,i)=>orb(x,seams[i],false,Object.values(dark)[i%8])),C.yellow);
const gemCols=['#E7D7FF',C.purple,C.orange,C.green,C.blue,C.red,'#E7F7FF',C.white,'#D28B24','#70C8FF','#EAF7FF'];
const facets=poly('128,51 190,86 205,148 163,202 93,202 51,148 66,86',C.white,'none',0,'opacity=".18"')+pathEl('M66 86 L128 128 L190 86 M51 148 L128 128 L205 148 M128 51 L128 202','none',C.ink,4,'opacity=".35"');
await mergeSheet(13,'gems',gemCols.map((x,i)=>orb(x,facets,false,Object.values(dark)[i%8])),C.purple);
await mergeSheet(14,'neon',pal.map((x,i)=>orb(C.dark,circle(128,128,60,'none',x,13)+g(0,8,.7,face('tiny')),false)),C.pink);

// 15 / BATCH 04 - exact overlapping geometry, clear 9-slice center.
const jarBack=pathEl('M111 72 L111 872 Q111 955 201 978 L567 978 Q657 955 657 872 L657 72Z',C.glass,C.ink,14,'fill-opacity=".32"')+pathEl('M128 96 L128 880 Q128 930 184 948','none',C.blue,10,'opacity=".45"');
const jarFront=pathEl('M879 72 L879 872 Q879 955 969 978 L1335 978 Q1425 955 1425 872 L1425 72Z','none',C.ink,14)+pathEl('M896 96 L896 880 Q896 930 952 948','none',C.white,20,'opacity=".7"')+pathEl('M884 84 Q1152 134 1420 84','none',C.blue,16);
await write('15_batch04_glass_jar.png',1536,1024,jarBack+jarFront);

// 16 / BATCH 05a
const splatFrame=(i)=>{const s=.35+i<5?(.35+i*.16):(1-(i-4)*.13),a=i>5?1-(i-5)*.3:1;return g(128*(1-s),128*(1-s),s,puff(),`opacity="${a}"`)};
const puffFrame=(i)=>{const s=.25+i*.11,a=i>5?1-(i-5)*.32:1;return g(128*(1-s),128*(1-s),s,puff(),`opacity="${a}"`)};
await write('16_batch05a_splat_puff.png',1024,1024,sheet(4,4,256,256,[...Array.from({length:8},(_,i)=>splatFrame(i)),...Array.from({length:8},(_,i)=>puffFrame(i))]));

// 17 / BATCH 05b
const fx=[poly(starPts(128,128,92,35,8),C.white,C.ink,8),pathEl('M128 32 Q138 118 224 128 Q138 138 128 224 Q118 138 32 128 Q118 118 128 32Z',C.white,C.ink,7),pathEl('M128 48 Q134 122 208 128 Q134 134 128 208 Q122 134 48 128 Q122 122 128 48Z',C.white,C.ink,7),circle(128,128,76,'none',C.ink,18),pathEl('M128 43 Q77 112 93 161 Q105 194 128 194 Q151 194 163 161 Q179 112 128 43Z',C.white,C.ink,8),poly('74,73 188,52 169,189 96,173',C.white,C.ink,8),poly(starPts(128,132,67,30),C.white,C.ink,8),pathEl(heartD(128,128,55),C.white,C.ink,8),rect(95,61,66,134,12,C.white,C.ink,8),circle(128,128,55,C.white,C.ink,8),poly('128,57 203,190 53,190',C.white,C.ink,8),pathEl('M68 159 Q91 73 128 130 Q163 184 190 83','none',C.ink,18),puff(),Array.from({length:12},(_,i)=>pathEl('M128 74 L128 27','none',C.white,12,`transform="rotate(${i*30} 128 128)"`)).join(''),ellipse(128,144,91,28,'#000','none',0,'opacity=".35"'),`<defs><radialGradient id="gl"><stop offset="0" stop-color="#fff" stop-opacity=".75"/><stop offset="1" stop-color="#fff" stop-opacity="0"/></radialGradient></defs>${circle(128,128,92,'url(#gl)')}`];
await write('17_batch05b_single_fx.png',1024,1024,sheet(4,4,256,256,fx));

// 18 / BATCH 06
const buttons=[[C.green,dark.green,false],[C.green,dark.green,true],[C.blue,dark.blue,false],[C.blue,dark.blue,true],[C.yellow,dark.yellow,false],[C.yellow,dark.yellow,true],[C.white,C.pale,false],[C.white,C.pale,true],[C.red,dark.red,false],[C.red,dark.red,true],[C.gray,dark.gray,false],null].map(x=>x?pill(...x):circle(128,128,82,C.green,C.ink,10)+hi(82,74,48));
await write('18_batch06_buttons.png',1536,1024,sheet(3,4,512,256,buttons));

// 19 / BATCH 07
const panel=(fill)=>rect(25,25,206,206,44,fill,C.ink,10);
const ribbon=pathEl('M30 88 L66 69 L66 178 L30 162 L49 128Z',dark.orange,C.ink,8)+pathEl('M226 88 L190 69 L190 178 L226 162 L207 128Z',dark.orange,C.ink,8)+rect(54,63,148,106,18,C.yellow,C.ink,8);
const ui=[panel(C.cream),panel(C.dark),rect(46,32,164,192,30,C.white,C.ink,9),ribbon,pathEl('M39 197 L39 83 Q39 54 68 54 L188 54 Q217 54 217 83 L217 197Z',C.white,C.ink,9),pathEl('M39 197 L39 83 Q39 54 68 54 L188 54 Q217 54 217 83 L217 197Z',C.gray,C.ink,9),circle(128,128,58,C.red,C.ink,9),rect(49,72,158,112,32,C.blue,C.ink,9),rect(33,96,190,64,32,C.dark),rect(33,96,190,64,32,C.green),rect(34,82,188,92,46,C.green,C.ink,9)+circle(169,128,34,C.white,C.ink,7),rect(34,82,188,92,46,C.gray,C.ink,9)+circle(87,128,34,C.white,C.ink,7),rect(36,36,184,184,39,C.cream,C.ink,9),rect(36,36,184,184,39,C.yellow,C.ink,9),rect(36,36,184,184,39,C.red,C.ink,9),rect(36,36,184,184,39,C.gray,C.ink,9)+pathEl('M102 120 L102 98 Q102 72 128 72 Q154 72 154 98 L154 120','none',C.ink,10)+rect(91,116,74,60,12,C.ink)];
await write('19_batch07_frames_controls.png',1024,1024,sheet(4,4,256,256,ui));

// 20 / BATCH 08
const lineIcon=(d,accent='none',sw=13)=>pathEl(d,accent,C.ink,sw);
const icons=[poly('96,61 181,128 96,195',C.green,C.ink,12),rect(83,66,28,124,8,C.blue)+rect(145,66,28,124,8,C.blue),pathEl('M60 124 L128 62 L196 124 L196 198 L60 198Z',C.cream,C.ink,12),circle(128,128,45,'none',C.ink,14)+Array.from({length:8},(_,i)=>rect(121,51,14,31,5,C.ink,'none',0,`transform="rotate(${i*45} 128 128)"`)).join(''),pathEl('M59 111 L91 111 L124 79 L124 177 L91 145 L59 145Z',C.blue,C.ink,10)+pathEl('M148 100 Q181 128 148 156','none',C.ink,12),pathEl('M59 111 L91 111 L124 79 L124 177 L91 145 L59 145Z',C.gray,C.ink,10)+pathEl('M148 104 L190 152 M190 104 L148 152','none',C.ink,12),circle(128,128,49,'none',C.pink,12)+pathEl('M113 95 L113 161 M143 95 L143 161','none',C.ink,10),circle(128,128,49,'none',C.gray,12)+pathEl('M91 91 L165 165','none',C.ink,12),rect(91,50,74,156,17,'none',C.ink,12)+pathEl('M65 95 L65 161 M191 95 L191 161','none',C.purple,10),circle(128,128,69,'none',C.blue,10)+pathEl('M59 128 L197 128 M128 59 Q95 128 128 197 M128 59 Q161 128 128 197','none',C.ink,8),poly('128,57 193,95 178,174 128,205 78,174 63,95',C.yellow,C.ink,10),pathEl('M61 187 L61 119 Q61 79 99 79 L157 79 Q195 79 195 119 L195 187Z',C.blue,C.ink,10)+pathEl('M93 79 Q128 38 163 79','none',C.ink,10),poly(starPts(128,128,75,33),C.yellow,C.ink,10),pathEl('M55 160 Q65 94 117 103 Q130 61 168 82 Q205 94 197 143 Q178 176 128 174 L75 174Z',C.blue,C.ink,10)+pathEl('M128 117 L128 178 M100 147 L128 118 L156 147','none',C.white,10),pathEl('M128 70 L128 174 M96 99 L128 70 L160 99','none',C.green,12)+pathEl('M78 184 L178 184','none',C.ink,12),pathEl('M73 92 Q128 48 183 92 L174 187 L82 187Z',C.cream,C.ink,10)+circle(111,128,10,C.ink)+circle(145,128,10,C.ink),rect(77,113,102,82,18,C.ink)+pathEl('M96 113 L96 91 Q96 59 128 59 Q160 59 160 91 L160 113','none',C.ink,12),pathEl('M77 77 L179 179 M179 77 L77 179','none',C.red,15),circle(128,112,50,'none',C.yellow,11)+rect(106,164,44,18,6,C.ink),pathEl('M185 91 A72 72 0 1 0 190 151','none',C.green,13)+poly('175,67 205,91 170,103',C.green,C.ink,7),pathEl('M161 58 L91 128 L161 198','none',C.blue,15),pathEl('M95 58 L165 128 L95 198','none',C.blue,15),Array.from({length:9},(_,i)=>rect(55+(i%3)*53,55+Math.floor(i/3)*53,38,38,10,C.green)).join(''),poly(starPts(128,130,78,35),C.yellow,C.ink,10),poly(starPts(128,130,78,35),C.white,C.ink,10),pathEl(heartD(128,128,67),C.red,C.ink,10),pathEl(heartD(128,128,67),C.white,C.ink,10),rect(48,78,160,112,27,C.ink)+poly('109,101 169,134 109,167',C.white,'none',0),rect(48,78,160,112,27,C.ink)+pathEl('M51 205 L205 51','none',C.red,16),pathEl('M74 132 L111 169 L187 84','none',C.green,16),pathEl('M128 128 C96 74 43 93 55 132 C67 171 102 166 128 128 C154 90 189 85 201 124 C213 163 160 182 128 128','none',C.purple,12),rect(58,72,140,132,20,C.cream,C.ink,10)+rect(58,72,140,35,12,C.red)+pathEl('M94 54 L94 87 M162 54 L162 87','none',C.ink,11),rect(57,89,142,119,18,C.yellow,C.ink,10)+rect(49,74,158,38,13,C.red,C.ink,9)+pathEl('M128 75 L128 208','none',C.ink,9)+pathEl('M128 75 Q88 35 77 68 Q76 91 128 75 M128 75 Q168 35 179 68 Q180 91 128 75','none',C.ink,8),pathEl('M60 94 L196 94 L184 199 L72 199Z',C.pink,C.ink,10)+pathEl('M92 94 Q94 53 128 53 Q162 53 164 94','none',C.ink,10),pathEl('M72 128 L161 128 M137 91 L174 128 L137 165','none',C.blue,13)+circle(62,128,14,C.ink),circle(128,128,76,C.cream,C.ink,10)+circle(128,91,10,C.ink)+rect(120,116,16,67,8,C.ink)];
await write('20_batch08_icons.png',1024,1024,sheet(6,6,1024/6,1024/6,icons));

// 21-23 / BATCH 09a backgrounds, opaque.
const bgShape=(fill,opacity,x,y,r)=>circle(x,y,r,fill,'none',0,`opacity="${opacity}"`);
await write('21_batch09a_background_merge.png',1024,1536,bgShape(C.purple,.16,80,170,330)+bgShape(C.blue,.12,930,260,380)+bgShape(C.pink,.08,870,1390,420),rect(0,0,1024,1536,0,'#20254D'));
await write('22_batch09a_background_blast.png',1024,1536,bgShape(C.blue,.14,95,210,360)+bgShape(C.purple,.12,940,1320,430)+bgShape(C.pink,.08,820,90,260),rect(0,0,1024,1536,0,'#171C3D'));
await write('23_batch09a_background_arrow.png',1024,1536,bgShape(C.yellow,.13,60,140,350)+bgShape(C.orange,.09,980,1300,430)+bgShape(C.blue,.06,930,100,260),rect(0,0,1024,1536,0,C.cream));

// 24 / BATCH 09b
let tray=rect(40,40,688,944,52,C.dark,C.ink,14);
for(let r=0;r<8;r++)for(let c=0;c<8;c++)tray+=rect(74+c*78,114+r*94,62,62,14,'#454A74',C.ink,4);
const card=rect(808,54,688,916,60,C.cream,C.ink,14)+rect(848,94,608,836,42,C.white,'none',0,'opacity=".5"');
await write('24_batch09b_play_surfaces.png',1536,1024,tray+card);

// 25-30 / BATCH 10 store art. Opaque, simple hero-led compositions with clean negative space.
const appBg=(base,a)=>rect(0,0,1024,1024,0,base)+circle(180,180,240,a,'none',0,'opacity=".18"')+circle(870,870,300,C.white,'none',0,'opacity=".08"');
await write('25_batch10_eye_merge_app_icon.png',1024,1024,g(256,256,2,ball(C.pink,true)),appBg('#272C5B',C.purple));
const heroMerge=g(940,120,2.3,ball(C.pink,true))+g(730,520,1.2,orb(C.green,'',true,dark.green))+g(1170,600,1.05,orb(C.yellow,'',true,dark.yellow));
await write('26_batch10_eye_merge_feature.png',1536,1024,heroMerge,rect(0,0,1536,1024,0,'#272C5B')+circle(1310,180,360,C.purple,'none',0,'opacity=".2"'));
await write('27_batch10_eye_blast_app_icon.png',1024,1024,g(256,256,2,block(C.green,dark.green,true)),appBg('#171C3D',C.blue));
const heroBlast=g(940,160,2.3,block(C.green,dark.green,true))+g(760,610,1.15,block(C.yellow,dark.yellow,true))+g(1210,620,1.05,block(C.pink,dark.pink,true));
await write('28_batch10_eye_blast_feature.png',1536,1024,heroBlast,rect(0,0,1536,1024,0,'#171C3D')+circle(1330,160,360,C.blue,'none',0,'opacity=".18"'));
const arrowHero=pathEl('M405 731 C405 554 584 554 584 382 C584 244 720 244 720 116','none',C.green,82)+poly('720,55 650,151 790,151',C.green,C.ink,16);
await write('29_batch10_arrow_out_app_icon.png',1024,1024,arrowHero,appBg(C.cream,C.orange));
const arrowFeature=pathEl('M986 862 C986 666 1201 666 1201 465 C1201 298 1373 298 1373 129','none',C.green,88)+poly('1373,61 1299,166 1447,166',C.green,C.ink,16);
await write('30_batch10_arrow_out_feature.png',1536,1024,arrowFeature,rect(0,0,1536,1024,0,C.cream)+circle(1350,160,360,C.orange,'none',0,'opacity=".14"'));

console.log(`Generated 30 PNG batches in ${OUT}`);
