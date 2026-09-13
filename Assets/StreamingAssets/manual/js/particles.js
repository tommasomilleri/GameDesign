/* Pulviscolo ambientale + burst di fibre quando una pagina atterra. */
(() => {
'use strict';
const cv = document.getElementById('dustCv');
if(!cv){ console.error('[particles] dustCv mancante'); return; }
if(!window.RAF){ console.error('[particles] js/raf.js non caricato'); return; }
const g = cv.getContext('2d');

function fit(){ cv.width = innerWidth; cv.height = innerHeight; }
fit();
addEventListener('resize', fit, {passive:true});

const MAX = 70, BURST_MAX = 160;   /* invariato: la profondità non aumenta il conto */const motes = [];

function spawn(anywhere){
  /* due profondità: le grandi sono vicine (più opache e lente),
     le piccole lontane (tenui e leggermente più rapide) */
  const near = Math.random() < .38;
  const r = near ? Math.random()*.9+1.3 : Math.random()*.7+.35;
  return {
    x: Math.random()*innerWidth,
    y: anywhere ? Math.random()*innerHeight : -10,
    r,
    vx: (Math.random()-.5)*(near ? .10 : .18),
    vy: (near ? .05 : .11) + Math.random()*.06,
    a: .06 + (r-.35)/1.85*.30,          /* alpha legata alla taglia */
    tw: Math.random()*6.28
  };
}
for(let i=0;i<MAX;i++) motes.push(spawn(true));

const burst = [];
const book = document.getElementById('sbBook');      /* cachato: era una query per settle */

document.addEventListener('book:settle', () => {
  if(!book) return;
  if(burst.length > BURST_MAX) return;               /* niente accumulo se si sfoglia veloce */
  const b = book.getBoundingClientRect();
  if(!b.width) return;
  for(let i=0;i<26;i++) burst.push({
    x: b.left + b.width*(.35+Math.random()*.3),
    y: b.top  + b.height*.5,
    vx: (Math.random()-.5)*2.2,
    vy: -Math.random()*1.6-.3,
    r: Math.random()*1.6+.5,
    life: 1
  });
});

const world = document.getElementById('worldRipostiglio');

RAF.add(() => {
  if(world && !world.hidden) return;
  g.clearRect(0,0,cv.width,cv.height);

  for(const m of motes){
    m.x += m.vx; m.y += m.vy; m.tw += .02;
    if(m.y > innerHeight+10 || m.x < -10 || m.x > innerWidth+10) Object.assign(m, spawn(false));
    g.globalAlpha = m.a*(0.6 + 0.4*Math.sin(m.tw));
    g.fillStyle = '#f4e9cf';
    g.beginPath(); g.arc(m.x, m.y, m.r, 0, 7); g.fill();
  }

  /* iterazione a ritroso con splice: niente array nuovo per frame */
  for(let i = burst.length-1; i >= 0; i--){
    const p = burst[i];                              /* <-- MANCAVA: era un ReferenceError */
    p.x += p.vx; p.y += p.vy; p.vy += .05; p.life -= .02;
    if(p.life <= 0){ burst.splice(i,1); continue; }
    g.globalAlpha = p.life*.6;
    g.fillStyle = '#e6d3ba';
    g.fillRect(p.x, p.y, p.r*2, p.r*.8);
  }

  g.globalAlpha = 1;
});
})();