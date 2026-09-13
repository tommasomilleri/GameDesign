/* Ripostiglio: matter-js + oggetti disegnati (sprite pre-renderizzati),
   vaso con mosca, finestra con infissi, raggio che respira, pulviscolo.
   - fisica a passo FISSO (Matter è instabile a delta variabile)
   - grab rigido: il corpo segue il cursore, niente elastico
   - hit-test corretto: vince l'oggetto visivamente in cima */
/* ============================================================
   APERTURA/CHIUSURA DELLA STANZA — blindata.
   Vive in una IIFE PROPRIA, eseguita per prima: qualunque errore
   nel motore fisico più sotto non può toccarla. Il pulsante deve
   funzionare anche se la stanza dentro fosse rotta.
   ============================================================ */
(() => {
  const btn=document.getElementById('closetBtn');
  const back=document.getElementById('backBtn');
  const w=document.getElementById('worldRipostiglio');
  if(!btn||!back||!w){ console.error('[ripostiglio] DOM di navigazione mancante'); return; }
  btn.addEventListener('click',()=>{
    w.hidden=false; btn.style.display='none';
    document.dispatchEvent(new Event('closet:open'));
  });
  back.addEventListener('click',()=>{
    document.dispatchEvent(new Event('closet:close'));
    w.hidden=true; btn.style.display='';
  });
})();

/* ---------- MOTORE FISICO DELLA STANZA ---------- */
(() => {
if(!window.Matter){ console.error('[ripostiglio] matter.min.js non caricato'); return; }
const {Engine,Bodies,Body,Composite,Mouse,MouseConstraint,Events,Query}=Matter;
const W=900,H=560;
const cv=document.getElementById('ripCv');
const lcv=document.getElementById('ripLight');
const world=document.getElementById('worldRipostiglio');
if(!cv||!lcv||!world){ console.error('[ripostiglio] canvas mancanti'); return; }
const g=cv.getContext('2d'), lg=lcv.getContext('2d');

/* enableSleeping: i corpi fermi smettono di essere simulati e si
   risvegliano da soli al contatto o al grab. Nessuna differenza visiva. */
const engine=Engine.create({enableSleeping:true});
engine.gravity.y=1;

Composite.add(engine.world,[
  Bodies.rectangle(W/2,H+20,W,40,{isStatic:true}),
  Bodies.rectangle(-20,H/2,40,H,{isStatic:true}),
  Bodies.rectangle(W+20,H/2,40,H,{isStatic:true}),
  Bodies.rectangle(W/2,H*.62,W*.7,14,{isStatic:true}),
  Bodies.rectangle(W/2,H*.32,W*.55,14,{isStatic:true})
]);

/* ---------- DISEGNATORI ---------- */
function rr(g,x,y,w,h,r){ g.beginPath(); g.roundRect(x,y,w,h,r); }
const P={
  barrel(g,w,h){
    g.fillStyle='#6e4629'; rr(g,-w/2,-h/2,w,h,w*.18); g.fill();
    g.fillStyle='#5a3a20';
    g.fillRect(-w/2,-h*.32,w,h*.09); g.fillRect(-w/2,h*.23,w,h*.09);
    g.strokeStyle='#3d2314'; g.lineWidth=2;
    for(let i=-2;i<=2;i++){g.beginPath();g.moveTo(i*w*.18,-h/2);g.lineTo(i*w*.22,h/2);g.stroke();}
  },
  box(g,w,h){
    g.fillStyle='#8a5a30'; g.fillRect(-w/2,-h/2,w,h);
    g.fillStyle='#6e4629'; g.fillRect(-w/2,-h/2,w,h*.18);
    g.strokeStyle='#3d2314'; g.lineWidth=3; g.strokeRect(-w/2,-h/2,w,h);
    g.beginPath();g.moveTo(-w/2,-h/2);g.lineTo(w/2,h/2);g.moveTo(w/2,-h/2);g.lineTo(-w/2,h/2);
    g.lineWidth=1.5; g.stroke();
  },
  books(g,w,h){
    const cols=['#7a3b2e','#3e5a45','#4a4668'];
    cols.forEach((c,i)=>{
      g.fillStyle=c;
      g.fillRect(-w/2+i*3,-h/2+i*(h/3),w-i*6,h/3-2);
      g.fillStyle='rgba(230,211,186,.6)';
      g.fillRect(-w/2+i*3+4,-h/2+i*(h/3)+3,3,h/3-8);
    });
  },
  cheese(g,w,h){
    g.fillStyle='#e8b23a';
    g.beginPath();g.moveTo(-w/2,h/2);g.lineTo(w/2,h/2);g.lineTo(w*.35,-h/2);g.lineTo(-w/2,-h*.2);
    g.closePath();g.fill();
    g.fillStyle='#c8952b';
    [[-w*.15,h*.1,5],[w*.1,-h*.05,4],[-w*.3,h*.28,3.4]].forEach(([x,y,r])=>{
      g.beginPath();g.arc(x,y,r,0,7);g.fill();});
  },
  vase(g,w,h){
    g.fillStyle='#b85c38';
    g.beginPath();
    g.moveTo(-w*.5,-h*.35); g.quadraticCurveTo(-w*.55,h*.5,-w*.25,h*.5);
    g.lineTo(w*.25,h*.5); g.quadraticCurveTo(w*.55,h*.5,w*.5,-h*.35);
    g.closePath(); g.fill();
    g.fillStyle='#9c4a2c'; g.fillRect(-w*.5,-h*.5,w,h*.16);
    g.fillStyle='rgba(255,255,255,.12)';
    g.beginPath(); g.ellipse(-w*.2,0,w*.08,h*.3,0,0,7); g.fill();
  },
  sponge(g,w,h){
    g.fillStyle='#d9c26a'; rr(g,-w/2,-h/2,w,h,6); g.fill();
    g.fillStyle='rgba(90,70,20,.35)';
    for(let i=0;i<8;i++){g.beginPath();
      g.arc((Math.random()-.5)*w*.8,(Math.random()-.5)*h*.7,2.2,0,7);g.fill();}
  },
  pencil(g,w,h){
    g.fillStyle='#c8952b'; g.fillRect(-w/2,-h/2,w*.8,h);
    g.fillStyle='#e6d3ba';
    g.beginPath();g.moveTo(w*.3,-h/2);g.lineTo(w/2,0);g.lineTo(w*.3,h/2);g.closePath();g.fill();
    g.fillStyle='#2b1f1a';
    g.beginPath();g.moveTo(w*.43,-h*.15);g.lineTo(w/2,0);g.lineTo(w*.43,h*.15);g.closePath();g.fill();
  },
  candle(g,w,h){
    g.fillStyle='#e6c280'; rr(g,-w/2,-h*.1,w,h*.6,3); g.fill();
    g.strokeStyle='#c8a25c'; g.lineWidth=1;
    g.beginPath();g.moveTo(-w*.2,-h*.1);g.lineTo(-w*.3,h*.5);g.stroke();
    g.strokeStyle='#3d2314'; g.beginPath();g.moveTo(0,-h*.1);g.lineTo(0,-h*.22);g.stroke();
    const fl=g.createRadialGradient(0,-h*.32,1,0,-h*.32,h*.16);
    fl.addColorStop(0,'#fff3c0'); fl.addColorStop(.5,'#ff9900'); fl.addColorStop(1,'rgba(255,120,0,0)');
    g.fillStyle=fl;
    g.beginPath();g.ellipse(0,-h*.32,w*.22,h*.17,0,0,7);g.fill();
  }
};

/* ---------- SPRITE pre-renderizzati (fix sfarfallio) ---------- */
function makeSprite(painter,w,h){
  const s=document.createElement('canvas');
  const pad=20; s.width=w+pad*2; s.height=h+pad*2;
  const sg=s.getContext('2d');
  sg.translate(w/2+pad,h/2+pad);
  painter(sg,w,h);
  return s;
}
function item(x,y,w,h,painter,opt){
  const b=Bodies.rectangle(x,y,w,h,Object.assign({restitution:.25,friction:.6},opt));
  b.w=w; b.h=h; b.sprite=makeSprite(painter,w,h);
  return b;
}

/* ---------- CORPI ----------
   L'ordine dell'array è anche l'ordine di disegno: l'ultimo è in cima. */
const bodies=[
  item(120,H-90,85,110,P.barrel,{isStatic:true}),
  item(700,H*.20,95,90,P.box,{isStatic:true}),
  item(300,H*.54,85,60,P.books,{isStatic:true}),
  item(200,H*.24,70,50,P.cheese,{isStatic:true}),
  item(500,H*.24,70,90,P.vase,{label:'vase', density:.004, frictionAir:.02}),
  item(135,H-55,50,38,P.sponge,{label:'tool-sponge', density:.0012, frictionAir:.03}),
  item(690,H*.24,55,16,P.pencil,{label:'tool-pencil', density:.0015, frictionAir:.012}),
  item(320,H*.55,32,60,P.candle,{label:'tool-candle', density:.002, frictionAir:.015})
];
Composite.add(engine.world,bodies);

const mouse=Mouse.create(cv);
/* stiffness alta + length 0: il corpo segue il cursore invece di
   penzolare da una molla. È il motivo per cui prima "non agganciava bene". */
const mc=MouseConstraint.create(engine,{
  mouse,
  constraint:{stiffness:.35, damping:.25, length:0}
});
Composite.add(engine.world,mc);

/* ---------- navigazione ---------- */
function releaseGrab(){
  mc.constraint.bodyB=null;
  mc.constraint.pointB=null;
  mc.body=null;
  mouse.button=-1;
  downAt=null;
}
/* L'apertura della stanza non deve MAI dipendere dal fatto che il resto
   del modulo sia arrivato in fondo senza errori: se qualcosa a valle
   solleva, il pulsante deve comunque funzionare. Le variabili del loop
   vengono resettate solo se già esistono (typeof, non ReferenceError). */
/* Il motore reagisce agli EVENTI emessi dalla IIFE di navigazione, invece
   di possedere i pulsanti: separazione netta fra "aprire la stanza" e
   "simulare la stanza". Se il motore muore, la porta continua ad aprirsi. */
document.addEventListener('closet:open',()=>{
  lastFrame=performance.now(); acc=0;       /* niente salto al rientro */
});
document.addEventListener('closet:close',()=>{
  releaseGrab();                            /* non lasciare corpi appesi */
});
/* ---------- SUONO URTI (throttled) ---------- */
let lastWood=0;
Events.on(engine,'collisionStart',e=>{
  const now=performance.now();
  if(now-lastWood<70)return;                /* max ~14 tonfi al secondo */
  for(const p of e.pairs){
    const v=Math.hypot(p.bodyA.velocity.x-p.bodyB.velocity.x,
                       p.bodyA.velocity.y-p.bodyB.velocity.y);
    if(v>3){ lastWood=now; if(window.Sfx&&Sfx.wood)Sfx.wood(); return; }
  }
});

/* ---------- MOSCA ---------- */
let fly=null, flyFreed=false;
function freeFly(x,y){
  if(flyFreed)return; flyFreed=true;
  fly={x,y,t:0};
  if(window.Sfx&&Sfx.fly)Sfx.fly();
}
function drawFly(){
  if(!fly)return;
  fly.t+=1/60;
  fly.x+=Math.sin(fly.t*17)*3+(fly.t*1.5);
  fly.y-=2.2+Math.sin(fly.t*23)*2;
  g.save(); g.translate(fly.x,fly.y);
  g.fillStyle='#222';
  g.beginPath();g.ellipse(0,0,5,3.5,0,0,7);g.fill();
  const wob=Math.sin(fly.t*60)*.6;
  g.fillStyle='rgba(200,200,220,.7)';
  g.beginPath();g.ellipse(-4,-3+wob,4,2,-.5,0,7);g.fill();
  g.beginPath();g.ellipse(4,-3-wob,4,2,.5,0,7);g.fill();
  g.restore();
  if(fly.y<-20||fly.x>W+20)fly=null;
}

/* ---------- CLICK: raccolta + vaso ---------- */
let downAt=null;

/* Query.point restituisce i corpi nell'ordine dell'ARRAY, non in quello
   di disegno: prendendo [0] si beccava la scatola invece della matita.
   L'ultimo è quello disegnato per ultimo, cioè visivamente in cima. */
function pickAt(pt){
  const under=Query.point(bodies,pt);
  if(under.length) return under[under.length-1];
  /* tolleranza per oggetti sottili (la matita è alta 16px) */
  let best=null, bestD=14;
  for(const b of bodies){
    const d=Math.hypot(b.position.x-pt.x,b.position.y-pt.y)-Math.max(b.w,b.h)*.5;
    if(d<bestD){ bestD=d; best=b; }
  }
  return best;
}

Events.on(mc,'mousedown',()=>{
  downAt={x:mouse.position.x,y:mouse.position.y,t:Date.now()};
});
Events.on(mc,'mouseup',()=>{
  if(!downAt)return;
  const moved=Math.hypot(mouse.position.x-downAt.x,mouse.position.y-downAt.y);
  if(moved<8&&Date.now()-downAt.t<400){
    const hit=pickAt(mouse.position);
    if(hit){
      if(hit.label.startsWith('tool-')){
        State.collect(hit.label.slice(5));
        if(mc.body===hit) releaseGrab();     /* non tenere un corpo rimosso */
        Composite.remove(engine.world,hit);
        const i=bodies.indexOf(hit); if(i>=0) bodies.splice(i,1);
      }else if(hit.label==='vase'){
        freeFly(hit.position.x,hit.position.y-50);
        Body.applyForce(hit,hit.position,{x:.02,y:-.01});
      }
    }
  }
  downAt=null;
});

/* ---------- CICLO GIORNO/NOTTE (120s per giornata completa) ---------- */
/* ---------- CICLO GIORNO/NOTTE ----------
   G4: stessa formula di js/candela.js, DUPLICATA di proposito.
   Importarla accoppierebbe il ripostiglio alla hero, e questa
   stanza deve poter funzionare da sola. Con REAL_TIME il buio
   della stanza e quello del tavolo coincidono: chiudendo la porta
   non si passa più dalla notte al mezzogiorno.                */
const REAL_TIME=true;
const DAY_LEN=120;                       /* secondi, se !REAL_TIME */
const t0=performance.now();
function dayPhase(){
  if(REAL_TIME){
    const d=new Date();
    const p=(d.getHours()*3600+d.getMinutes()*60+d.getSeconds())/86400;
    return (p+.75)%1;                    /* mezzogiorno = picco di luce */
  }
  return ((performance.now()-t0)/1000%DAY_LEN)/DAY_LEN;
}
function daylight(){ return Math.max(0,Math.sin(dayPhase()*Math.PI*2)); }/* ---------- DECOR STATICO (pre-renderizzato una volta) ---------- */
const decor=document.createElement('canvas');
decor.width=W; decor.height=H;
(function paintDecor(){
  const d=decor.getContext('2d');
  /* assi di legno della parete */
  for(let x=0;x<W;x+=90){
    d.fillStyle=x/90%2?'#241510':'#2b1a12';
    d.fillRect(x,0,90,H);
    d.strokeStyle='rgba(0,0,0,.35)'; d.lineWidth=2;
    d.beginPath(); d.moveTo(x,0); d.lineTo(x,H); d.stroke();
    d.fillStyle='rgba(0,0,0,.25)';
    d.beginPath(); d.ellipse(x+45,(x*7)%H,4,7,0,0,7); d.fill();
  }
  /* battiscopa e pavimento */
  d.fillStyle='#1a100a'; d.fillRect(0,H-26,W,26);
  /* mensole: spessore e supporti */
  d.fillStyle='#4a3319';
  d.fillRect(W*.225,H*.32-7,W*.55,14);
  d.fillStyle='#3a2712';
  d.fillRect(W*.15,H*.62+7,W*.7,4);  d.fillRect(W*.225,H*.32+7,W*.55,4);
  for(const [sx,sy] of [[W*.2,H*.62],[W*.8,H*.62],[W*.28,H*.32],[W*.72,H*.32]]){
    d.fillStyle='#2b1a10';
    d.beginPath(); d.moveTo(sx-9,sy+9); d.lineTo(sx+9,sy+9); d.lineTo(sx,sy+34); d.closePath(); d.fill();
  }
  /* ragnatela nell'angolo alto sinistro */
  d.strokeStyle='rgba(220,220,220,.14)'; d.lineWidth=1;
  for(let i=0;i<5;i++){
    d.beginPath(); d.moveTo(0,0); d.lineTo(Math.cos(i*.3)*130,Math.sin(i*.3+.2)*130); d.stroke();
  }
  for(let r=25;r<=115;r+=30){
    d.beginPath();
    for(let i=0;i<=5;i++){
      const a=i*.3+.1, x=Math.cos(a)*r, y=Math.sin(a+.2)*r;
      i?d.lineTo(x,y):d.moveTo(x,y);
    }
    d.stroke();
  }
  /* sacco di iuta a terra a destra */
  d.fillStyle='#8a6b42';
  d.beginPath(); d.moveTo(W-120,H-26); d.quadraticCurveTo(W-150,H-120,W-95,H-125);
  d.quadraticCurveTo(W-40,H-118,W-60,H-26); d.closePath(); d.fill();
  d.strokeStyle='#5c4526'; d.lineWidth=3;
  d.beginPath(); d.moveTo(W-108,H-118); d.lineTo(W-80,H-112); d.stroke();
  d.strokeStyle='rgba(0,0,0,.2)';
  d.beginPath(); d.moveTo(W-110,H-90); d.quadraticCurveTo(W-90,H-80,W-70,H-88); d.stroke();
  /* barattoli sulla mensola alta */
  for(const [jx,fill] of [[W*.3,'#7a8a3a'],[W*.37,'#a04a2c'],[W*.44,'#c8952b']]){
    d.fillStyle='rgba(200,220,220,.16)'; d.fillRect(jx,H*.32-52,34,45);
    d.fillStyle=fill; d.globalAlpha=.75; d.fillRect(jx+3,H*.32-32,28,25); d.globalAlpha=1;
    d.fillStyle='#5c3a21'; d.fillRect(jx-2,H*.32-58,38,8);
  }
})();

/* ---------- LUCE (giorno/notte + candela) ---------- */
let mx=W/2,my=H/2;
cv.addEventListener('pointermove',e=>{
  const r=cv.getBoundingClientRect();
  mx=(e.clientX-r.left)*(W/r.width); my=(e.clientY-r.top)*(H/r.height);
},{passive:true});

function drawLight(t){
  const dl=daylight();
  lg.clearRect(0,0,W,H);
  lg.fillStyle=`rgba(8,4,2,${(.9-dl*.35).toFixed(3)})`;
  lg.fillRect(0,0,W,H);
  lg.globalCompositeOperation='destination-out';
  if(dl>.02){
    const win=lg.createRadialGradient(W-210,60,20,W-210,60,420);
    win.addColorStop(0,`rgba(0,0,0,${(.6*dl).toFixed(3)})`);
    win.addColorStop(1,'rgba(0,0,0,0)');
    lg.fillStyle=win; lg.beginPath(); lg.arc(W-210,60,420,0,7); lg.fill();
  }
  const R=240+Math.sin(t*.05)*10+Math.random()*8;
  const hole=lg.createRadialGradient(mx,my,0,mx,my,R);
  hole.addColorStop(0,'rgba(0,0,0,1)'); hole.addColorStop(.45,'rgba(0,0,0,.85)');
  hole.addColorStop(1,'rgba(0,0,0,0)');
  lg.fillStyle=hole; lg.beginPath(); lg.arc(mx,my,R,0,7); lg.fill();
  lg.globalCompositeOperation='lighter';
  /* respiro lento (sin t*.007) sopra il flicker veloce già presente in R */
  const wa=(.22+Math.sin(t*.007)*.05).toFixed(3);
  const warm=lg.createRadialGradient(mx,my,0,mx,my,R*.7);
  warm.addColorStop(0,`rgba(255,190,110,${wa})`); warm.addColorStop(1,'rgba(255,170,80,0)');
  lg.fillStyle=warm; lg.beginPath(); lg.arc(mx,my,R*.7,0,7); lg.fill();
  lg.globalCompositeOperation='source-over';
}

/* ---------- CIELO NELLA FINESTRA ---------- */
function drawWindow(){
  const ph=dayPhase(), dl=daylight();
  let sky;
  if(dl<.05) sky='#0a1030';
  else{
    const warm=Math.max(0,1-Math.abs(dl-.15)*6);
    const r=Math.round(30+dl*190+warm*60);
    const gg=Math.round(40+dl*180-warm*20);
    const b=Math.round(80+dl*135-warm*60);
    sky=`rgb(${r},${gg},${b})`;
  }
  g.fillStyle=sky; g.fillRect(W-270,10,120,64);
  const cx=W-270+((ph*2)%1)*120;
  if(dl>.05){
    g.fillStyle='#fff3c0';
    g.beginPath(); g.arc(cx,30+Math.sin(ph*Math.PI*2)*-8,9,0,7); g.fill();
  }else{
    g.fillStyle='#e8e8f4';
    g.beginPath(); g.arc(cx,28,7,0,7); g.fill();
    g.fillStyle=sky;
    g.beginPath(); g.arc(cx+3,26,6,0,7); g.fill();
    g.fillStyle='#e8e8f4';
    for(let i=0;i<6;i++) g.fillRect(W-262+(i*37)%110, 16+(i*23)%50, 1.6,1.6);
  }
  g.strokeStyle='#1a0f0a'; g.lineWidth=6; g.strokeRect(W-270,10,120,64);
  g.lineWidth=3;
  g.beginPath(); g.moveTo(W-210,10); g.lineTo(W-210,74);
  g.moveTo(W-270,42); g.lineTo(W-150,42); g.stroke();
}

/* ---------- RENDER ---------- */
/* ---------- RENDER ---------- */

/* --- pulviscolo a 3 layer di profondità ---
   Le motes vivono in TUTTA la stanza, non solo nel raggio: dentro il
   cono di luce sono piene, fuori restano un accenno (alpha*.15).
   Ogni mote ha fase/frequenza/ampiezza proprie: niente sciame sincrono. */
const DUST_LAYERS=[
  { n:10, r:[1.6,2.2], a:.50, vy:-.05, amp:[5,9],  frq:[.010,.018] }, /* vicine, lente */
  { n:16, r:[1.0,1.6], a:.34, vy:-.09, amp:[3,6],  frq:[.016,.028] },
  { n:14, r:[ .5,1.0], a:.20, vy:-.16, amp:[2,4],  frq:[.026,.044] }  /* lontane, veloci */
];
const rnd=(a,b)=>a+Math.random()*(b-a);
const dust=[];
for(const L of DUST_LAYERS){
  for(let i=0;i<L.n;i++) dust.push({
    x:Math.random()*W, y:Math.random()*H,
    r:rnd(L.r[0],L.r[1]), a:L.a, vy:L.vy,
    amp:rnd(L.amp[0],L.amp[1]), frq:rnd(L.frq[0],L.frq[1]),
    ph:Math.random()*6.28
  });
}

/* il cono del raggio, come geometria riutilizzabile (stessi vertici
   del poligono disegnato: 74 in alto, H in basso) */
function inBeam(x,y){
  if(y<74) return 0;
  const k=(y-74)/(H-74);
  const l=(W-266)+((W-380)-(W-266))*k;
  const r=(W-154)+((W- 40)-(W-154))*k;
  if(x<l||x>r) return 0;
  const half=(r-l)*.5, mid=(l+r)*.5;
  return 1-Math.min(1,Math.abs(x-mid)/half);   /* sfuma sui bordi */
}

function drawDust(dl){
  for(const d of dust){
    d.ph+=d.frq;
    d.y+=d.vy;
    if(d.y<-6){ d.y=H+6; d.x=Math.random()*W; }
    const dx=d.x+Math.sin(d.ph)*d.amp;
    const lit=inBeam(dx,d.y)*dl;
    g.globalAlpha=d.a*(.15+lit*.85);
    g.fillStyle='rgba(255,240,205,1)';
    g.beginPath(); g.arc(dx,d.y,d.r,0,7); g.fill();
  }
  g.globalAlpha=1;
}

/* --- vignettatura: pre-renderizzata UNA volta, zero costo per frame --- */
const vign=document.createElement('canvas');
vign.width=W; vign.height=H;
(function paintVignette(){
  const v=vign.getContext('2d');
  const rg=v.createRadialGradient(W*.5,H*.46,Math.min(W,H)*.28,
                                  W*.5,H*.46,Math.max(W,H)*.72);
  rg.addColorStop(0,'rgba(0,0,0,0)');
  rg.addColorStop(.62,'rgba(0,0,0,.22)');
  rg.addColorStop(1,'rgba(0,0,0,.55)');
  v.fillStyle=rg; v.fillRect(0,0,W,H);
})();

/* --- grana pellicola: un tile 128×128 generato una volta, ridisegnato
       con offset random (2 drawImage per frame, non 900×560 pixel) --- */
const GRAIN=128;
const grain=document.createElement('canvas');
grain.width=GRAIN; grain.height=GRAIN;
(function paintGrain(){
  const gc=grain.getContext('2d');
  const im=gc.createImageData(GRAIN,GRAIN);
  for(let i=0;i<im.data.length;i+=4){
    const n=200+Math.random()*55|0;
    im.data[i]=im.data[i+1]=im.data[i+2]=n;
    im.data[i+3]=255;
  }
  gc.putImageData(im,0,0);
})();
const grainPat=g.createPattern(grain,'repeat');

function drawPost(){
  g.drawImage(vign,0,0);
  if(!grainPat) return;
  g.save();
  g.globalAlpha=.04;
  g.globalCompositeOperation='overlay';
  g.translate(-(Math.random()*GRAIN|0), -(Math.random()*GRAIN|0));
  g.fillStyle=grainPat;
  g.fillRect(0,0,W+GRAIN,H+GRAIN);
  g.restore();
}
/* ---------- D3: OCCHI NEL BUIO ----------
   Qualcosa ti guarda quando la stanza è al buio. Si spegne se il
   cerchio di luce del cursore gli arriva addosso: non è un nemico,
   è timido. */
const EYE_SPOTS=[[46,H-70],[W-58,H*.44],[W*.42,26]];
let eye={ i:0, until:0, blink:0, nextBlink:0, nextMove:0 };
function drawEyes(dl,now){
  if(dl>=.05){ eye.until=0; return; }
  if(now>eye.nextMove){
    eye.i=(eye.i+1+Math.floor(Math.random()*2))%EYE_SPOTS.length;
    eye.nextMove=now+9000+Math.random()*9000;
  }
  const [ex,ey]=EYE_SPOTS[eye.i];
  if(Math.hypot(mx-ex,my-ey)<120) return;         /* la luce li scaccia */
  if(now>eye.nextBlink){ eye.blink=now+150; eye.nextBlink=now+3000+Math.random()*3000; }
  const k = now<eye.blink ? .08 : 1;
  g.save(); g.globalAlpha=.85;
  g.fillStyle='#e8c23a';
  for(const dx of [-7,7]){
    g.beginPath(); g.ellipse(ex+dx,ey,2.6,2.6*k,0,0,7); g.fill();
  }
  g.restore(); g.globalAlpha=1;
}

/* ---------- D5: IL RAGNO ----------
   Cala dal soffitto su un filo, ondeggia, e risale di corsa se la
   luce del cursore lo raggiunge. Uno solo alla volta. */
let spider=null, nextSpider=performance.now()+30000+Math.random()*60000;
function drawSpider(now,dt){
  if(!spider && now>nextSpider){
    spider={ x:120+Math.random()*(W-240), y:-14, tgt:120+Math.random()*180, t:0, up:false };
  }
  if(!spider) return;
  const s=spider;
  s.t+=dt;
  if(!s.up && Math.hypot(mx-s.x,my-s.y)<100) s.up=true;
  s.y += s.up ? -300*dt : Math.min(s.tgt-s.y,1)*140*dt;
  if(s.up && s.y<-20){ spider=null; nextSpider=now+60000+Math.random()*120000; return; }
  const sway=s.up?0:Math.sin(s.t*1.6)*7;
  const bx=s.x+sway;
  g.save();
  g.strokeStyle='rgba(220,220,220,.28)'; g.lineWidth=1;
  g.beginPath(); g.moveTo(s.x,0); g.lineTo(bx,s.y); g.stroke();
  g.fillStyle='#1a1410';
  g.beginPath(); g.ellipse(bx,s.y,5,4,0,0,7); g.fill();
  g.beginPath(); g.arc(bx,s.y-5,2.6,0,7); g.fill();
  g.strokeStyle='#1a1410'; g.lineWidth=1.2; g.lineCap='round';
  for(let i=0;i<4;i++){
    const a=.5+i*.42, w=Math.sin(s.t*7+i)*1.6;
    g.beginPath(); g.moveTo(bx,s.y);
    g.lineTo(bx-Math.cos(a)*9, s.y+Math.sin(a)*7+w); g.stroke();
    g.beginPath(); g.moveTo(bx,s.y);
    g.lineTo(bx+Math.cos(a)*9, s.y+Math.sin(a)*7-w); g.stroke();
  }
  g.restore();
}

const STEP=1000/60;                        /* passo fisso della fisica */
let t=0, lastFrame=performance.now(), acc=0;

function frame(){
  const now=performance.now();
  if(world.hidden){ lastFrame=now; acc=0; return; }
  const dt=Math.min(.05,(now-lastFrame)/1000);
  t = (now-t0)/16.667;

  /* accumulatore a passo fisso: Matter è molto più stabile e il grab
     smette di sembrare "molle". Il clamp evita la spiral of death. */
  acc+=Math.min(100,now-lastFrame);
  lastFrame=now;
  let steps=0;
  while(acc>=STEP&&steps<5){ Engine.update(engine,STEP); acc-=STEP; steps++; }
  if(steps===5) acc=0;

  /* cintura anti-fionda: massa piccola + stiffness del grab possono
     ancora produrre lanci innaturali su urti multipli */
  for(const b of bodies){
    if(b.isStatic) continue;
    const speed=Math.hypot(b.velocity.x,b.velocity.y);
    if(speed>18) Body.setSpeed(b,18);
  }

  g.clearRect(0,0,W,H);
  g.drawImage(decor,0,0);
  drawWindow();

  /* raggio: intensità legata alla luce del giorno */
  const dl=daylight();
  if(dl>.03){
    const ray=g.createLinearGradient(W-210,10,W-330,H);
    ray.addColorStop(0,`rgba(255,235,180,${(.05+dl*.16+Math.sin(t*.01)*.03).toFixed(3)})`);
    ray.addColorStop(1,'rgba(255,235,180,0)');
    g.fillStyle=ray;
    g.beginPath(); g.moveTo(W-266,74); g.lineTo(W-154,74);
    g.lineTo(W-40,H); g.lineTo(W-380,H); g.closePath(); g.fill();
  }

  drawEyes(dl,now);      /* D3: dietro i corpi, sono nell'angolo buio */

  /* corpi */
  for(const b of bodies){
    g.save(); g.translate(b.position.x,b.position.y); g.rotate(b.angle);
    g.globalAlpha=.3; g.fillStyle='#000';
    g.beginPath(); g.ellipse(0,b.h/2+4,b.w*.5,5,0,0,7); g.fill();
    g.globalAlpha=1;
    g.drawImage(b.sprite,-b.sprite.width/2,-b.sprite.height/2);
    g.restore();
  }
  drawSpider(now,dt);    /* D5: pende dal soffitto, davanti a tutto */
  drawDust(dl);          /* dopo i corpi: il pulviscolo è aria, sta davanti */
  drawFly();
  drawPost();            /* vignetta + grana, prima della maschera di luce */
  drawLight(t);
}

/* usa lo scheduler comune se c'è, altrimenti rAF proprio */
if(window.RAF && RAF.add) RAF.add(frame);
else (function loop(){ frame(); requestAnimationFrame(loop); })();
})();