/* ============================================================
   LOUPE — port fedele del loupe di MengTo/sketchbook: copia del
   libro in .zoominner, mascherata in cerchio, scalata MAG× attorno
   al punto sotto il vetro. La lente vive FUORI dal tilt: non si inclina.

   Ottimizzazioni (resa identica):
   - la maschera radiale NON viene ricreata a ogni movimento: si sposta
     con mask-position, che è una pura traslazione sul compositor
   - le scritture di stile ripetute vengono saltate (memo)
   - una sola catena di animazione per lo "shove"
   ============================================================ */
(() => {
'use strict';

const book=document.getElementById('sbBook');
if(!book){ console.error('[loupe] sbBook mancante'); return; }
const sb3d=document.getElementById('sb3d');

const loupe=document.getElementById('loupe');
const zoomWrap=document.getElementById('zoomWrap');
const zoomInner=document.getElementById('zoomInner');
const live=document.getElementById('liveLayer');
if(!loupe||!zoomWrap||!zoomInner){ console.error('[loupe] DOM della lente mancante'); return; }

const MAG=3.2;
let lx=null, ly=null, lgrab=null, lTarget=null;
let easeOff=null;                 /* unsubscribe della catena di easing attiva */

const loupeSize=()=>Math.round(Math.max(165,Math.min(262,book.clientWidth*.235)));

/* ---------- copia del libro dentro la lente ---------- */
/* ---------- copia del libro dentro la lente ---------- */
function syncZoomLayer(){
  zoomInner.textContent='';
  for(const c of book.children){
    if(c.classList.contains('sb-zone'))continue;
    zoomInner.appendChild(c.cloneNode(true));
  }
  if(live && !live.classList.contains('hidden')){
    const clone=live.cloneNode(true);
    zoomInner.appendChild(clone);
  }
  syncCanvasPairs();       /* ricalcola le coppie originale→clone */
  syncCanvases();          /* prima copia immediata dei pixel */
}

/* ---------- sync leggera SOLO dei pixel dei canvas ----------
   syncZoomLayer() ricostruisce il DOM del clone (costoso, solo a
   book:settle). I canvas però possono cambiare pixel DOPO il settle
   (frottage, macchia, tabella, termometro): syncCanvases() ricopia
   solo i pixel, senza toccare il DOM, ed è agganciata a RAF a bassa
   frequenza SOLO mentre la lente è visibile sulla carta. */
let canvasPairs=[];
function syncCanvasPairs(){
  canvasPairs=[];
  if(!live || live.classList.contains('hidden'))return;
  const liveClone=zoomInner.querySelector('.live-layer');
  if(!liveClone)return;
  const orig=live.querySelectorAll('canvas');
  const copy=liveClone.querySelectorAll('canvas');
  const n=Math.min(orig.length,copy.length);
  for(let i=0;i<n;i++) canvasPairs.push([orig[i],copy[i]]);
}
function syncCanvases(){
  for(const [o,c] of canvasPairs){
    if(!o.width||!o.height)continue;          /* canvas 0×0: drawImage lancia */
    try{ c.getContext('2d').drawImage(o,0,0); }catch(_){}
  }
}
let canvasSyncOff=null;
function ensureCanvasSync(active){
  if(active && !canvasSyncOff && window.RAF && RAF.add){
    canvasSyncOff=RAF.add(syncCanvases, 10);
  }else if(!active && canvasSyncOff){
    canvasSyncOff(); canvasSyncOff=null;
  }
}

function restLoupe(){
  lx=book.clientWidth*.88; ly=book.clientHeight*.855;
  placeLoupe();
}

/* ---------- posa della lente ---------- */
/* memo delle ultime scritture: evita di riscrivere stringhe identiche */
const memo={lr:'',tf:'',k:'',r:'',mp:'',zi:''};

function placeLoupe(){
  if(lx===null)return;
  const bw=book.clientWidth, bh=book.clientHeight;
  if(!bw)return;
const bRect=book.getBoundingClientRect();
  const wRect=sb3d ? sb3d.getBoundingClientRect() : bRect;
  const ox=bRect.left-wRect.left, oy=bRect.top-wRect.top;
  const R=loupeSize()/2, bez=R*2*.058;

  const lr=R*2+'px';
  if(memo.lr!==lr){ loupe.style.setProperty('--lr',lr); memo.lr=lr; }

  const tf=`translate3d(${(lx-R).toFixed(1)}px,${(ly-R).toFixed(1)}px,0)`;
  if(memo.tf!==tf){ loupe.style.transform=tf; memo.tf=tf; }
  loupe.classList.add('on');

  /* quanto la lente è "dentro" l'area carta → opacità dello zoom */
  const x0=bw*.051, x1=bw*.949, y0=bh*.218, y1=bh*.782;
  const nx=Math.max(x0,Math.min(lx,x1)), ny=Math.max(y0,Math.min(ly,y1));
  const inside=(lx>x0&&lx<x1&&ly>y0&&ly<y1)
    ? Math.min(lx-x0,x1-lx,ly-y0,y1-ly)
    : -Math.hypot(lx-nx,ly-ny);
  const k=Math.max(0,Math.min(1,(inside+R*.30)/(R*.55)));

  const ks=k.toFixed(3);
  if(memo.k!==ks){ zoomWrap.style.opacity=ks; memo.k=ks; }
  ensureCanvasSync(k>.002);
  if(k<=.002)return;

  /* La maschera è SEMPRE lo stesso cerchio: cambia solo dove sta.
     Ricreare la stringa del gradiente costringeva il browser a
     rigenerare la texture della mask a ogni pointermove. */
  const r=(R-bez).toFixed(1);
  if(memo.r!==r){
    const mask=`radial-gradient(circle ${r}px at ${r}px ${r}px,#000 calc(100% - 1px),transparent 100%)`;
    zoomWrap.style.webkitMaskImage=mask;   zoomWrap.style.maskImage=mask;
    zoomWrap.style.webkitMaskRepeat='no-repeat'; zoomWrap.style.maskRepeat='no-repeat';
    memo.r=r;
  }
  const mp=`${(lx-r).toFixed(1)}px ${(ly-r).toFixed(1)}px`;
  if(memo.mp!==mp){
    zoomWrap.style.webkitMaskPosition=mp; zoomWrap.style.maskPosition=mp;
    memo.mp=mp;
  }

  const zi=`translate(${((lx+ox)*(1-MAG)).toFixed(1)}px,${((ly+oy)*(1-MAG)).toFixed(1)}px) scale(${MAG})`;
  if(memo.zi!==zi){ zoomInner.style.transform=zi; memo.zi=zi; }
}

/* ---------- il foglio in volo spinge via la lente (shove) ---------- */
function shoveLoupe(){
  if(lx===null||lgrab)return;
  const bw=book.clientWidth, bh=book.clientHeight;
  if(!bw||!bh)return;
  if(lx/bw<.02||lx/bw>.98||ly/bh<.17||ly/bh>.83)return;
  lTarget={x:bw*.12,y:bh*.855};
  startEase();
}

function easeStep(){
  if(!lTarget||lgrab){ lTarget=null; return false; }
  const dx=lTarget.x-lx, dy=lTarget.y-ly;
  if(Math.abs(dx)<.5&&Math.abs(dy)<.5){
    lx=lTarget.x; ly=lTarget.y; lTarget=null;
    placeLoupe(); return false;
  }
  lx+=dx*.17; ly+=dy*.17;
  placeLoupe();
  return true;
}

function stopEase(){ if(easeOff){ easeOff(); easeOff=null; } }

function startEase(){
  if(easeOff)return;                          /* niente catene parallele */
  if(window.RAF && RAF.add){
    easeOff=RAF.add(()=>{ if(!easeStep()) stopEase(); });
  }else{
    /* fallback se raf.js non è caricato */
    easeOff=()=>{ lTarget=null; };
    (function loop(){
      if(easeStep()) requestAnimationFrame(loop);
      else easeOff=null;
    })();
  }
}

/* ---------- trascinamento della lente ---------- */
loupe.addEventListener('pointerdown',e=>{
  if(e.button!==0)return;
  e.preventDefault(); e.stopPropagation();
  lTarget=null; stopEase();
  lgrab={cx:e.clientX,cy:e.clientY,lx0:lx,ly0:ly};
  loupe.classList.add('held');
  try{ loupe.setPointerCapture(e.pointerId); }catch(_){}
});

loupe.addEventListener('pointermove',e=>{
  if(!lgrab)return;
  const R=loupeSize()/2, bw=book.clientWidth, bh=book.clientHeight;
  lx=Math.max(-R*.7,Math.min(bw+R*.7,lgrab.lx0+(e.clientX-lgrab.cx)));
  ly=Math.max(-R*.7,Math.min(bh+R*1, lgrab.ly0+(e.clientY-lgrab.cy)));
  placeLoupe();
});

const drop=e=>{
  if(!lgrab)return;
  lgrab=null;
  loupe.classList.remove('held');
  try{ if(e&&e.pointerId!=null) loupe.releasePointerCapture(e.pointerId); }catch(_){}
};
loupe.addEventListener('pointerup',drop);
loupe.addEventListener('pointercancel',drop);
loupe.addEventListener('lostpointercapture',drop);   /* rilascio fuori finestra */

/* ---------- sincronizzazione col libro ---------- */
let settleTimer=null;
document.addEventListener('book:settle',()=>{
  clearTimeout(settleTimer);                 /* niente sync accodate */
  settleTimer=setTimeout(()=>{
    settleTimer=null;
    syncZoomLayer();
    memo.zi='';                              /* il contenuto è cambiato */
    memo.k='';                               /* forza ricalcolo opacità/shift */
    placeLoupe();
  },50);
});

document.addEventListener('book:turn',()=>{
  clearTimeout(settleTimer); settleTimer=null;
  shoveLoupe();
  zoomWrap.style.opacity='0'; memo.k='0.000';
  ensureCanvasSync(false);
});

addEventListener('resize',()=>{
  memo.lr=memo.tf=memo.k=memo.r=memo.mp=memo.zi='';   /* tutto ricalcolato */
  lTarget=null; stopEase();
  if(lgrab){ lgrab=null; loupe.classList.remove('held'); }  /* C3: chiudi il grab prima */
  restLoupe();
},{passive:true});

addEventListener('load',()=>{ syncZoomLayer(); restLoupe(); });

/* se il libro è già pronto quando arriviamo qui (script in fondo al body) */
if(document.readyState==='complete'){ syncZoomLayer(); restLoupe(); }
})();