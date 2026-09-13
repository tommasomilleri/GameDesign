/* ============================================================
   ENGINE — flip sketchbook (MengTo): strisce annidate, molla,
   luce per striscia, drag, tilt.
   - lock anti re-entrancy: mai due giri sovrapposti
   - applyTurn scrive sul DOM solo se il valore è davvero cambiato
   - accetta SPREADS come array di stringhe o di oggetti {url}
   ============================================================ */
(() => {
'use strict';

/* ---------- prerequisiti ---------- */
if(!window.SPREADS || !SPREADS.length){
  console.error('[engine] SPREADS mancante: controlla js/pages.js');
  return;
}

const $=id=>document.getElementById(id);
const sb3d=$('sb3d'), book=$('sbBook'), stage=$('sbStage'), hint=$('sbHint');
if(!sb3d||!book||!stage){ console.error('[engine] DOM del libro mancante'); return; }

/* Se pages.js restituisce oggetti {url} vivi (blob:), li teniamo per
   riferimento così l'URL si aggiorna da solo. Se restituisce stringhe,
   le avvolgiamo. Entrambe le forme funzionano. */
const PAGES = SPREADS.map(s => (typeof s === 'string' ? {url:s} : s));
const M = PAGES.length;

/* ---------- costanti di tuning ---------- */
const N=18;            /* strisce della superficie curva            */
const SPAN=0.449;      /* dorso → bordo esterno (frazione libro)    */
const BETA=0.78;       /* curvatura di picco (rad)                  */
const COMMIT_K=140, COMMIT_C=20;
const CANCEL_K=150, CANCEL_C=24;

let idx=0, turn=null, drag=null, locked=false; let lastVisited = 0;      /* ultimo spread SETTLED: lo marca il nastro */
const strips=[];

/* ---------- preload immagini ---------- */
const cache=new Map();
function preload(i){
  i=((i%M)+M)%M;
  const url=PAGES[i].url;
  const img=new Image();
  img.decoding='async';
  img.src=url;
  cache.set(i,{url,img});  
}
function preloadAround(i){ preload(i); preload(i+1); preload(i-1); }

/* ---------- helpers DOM ---------- */
function el(t,c){ const e=document.createElement(t); if(c)e.className=c; return e; }
function halfEl(pos,i){
  const d=el('div','sb-half '+pos);
  const im=new Image();
  im.className='sb-half-img '+pos; im.draggable=false; im.src=PAGES[i].url;
  d.appendChild(im);
  d.appendChild(el('div','gutter-shade '+pos));
  return d;
}

/* ---------- catena di strisce ---------- */
function buildCurl(dir,from,to){
  const c=el('div','curl '+dir+(turn&&turn.rigid?' rigid':''));
  strips.length=0;
  c.style.setProperty('--n',N);
  c.style.setProperty('--span',SPAN);
  const gut='calc(var(--bw) * 0.5)';
  const sw='calc(var(--bw) * '+SPAN+' / '+N+')';
  const uFrom='url('+PAGES[from].url+')';
  const uTo  ='url('+PAGES[to].url+')';
  let host=c;
  for(let i=0;i<N;i++){
    const s=el('div','strip');
    const A='calc(-1 * ('+gut+' + '+i+' * '+sw+'))';
    const B='calc('+(i+1)+' * '+sw+' - '+gut+')';
    const f=el('div','face front'), b=el('div','face back');
    f.style.backgroundImage=uFrom;
    f.style.backgroundPositionX = dir==='next'?A:B;
    b.style.backgroundImage=uTo;
    b.style.backgroundPositionX = dir==='next'?B:A;
    f.appendChild(el('div','sh')); f.appendChild(el('div','gl'));
    b.appendChild(el('div','sh')); b.appendChild(el('div','gl'));
    s.appendChild(f); s.appendChild(b);
    if(i===N-1)s.classList.add('edge');
    host.appendChild(s); host=s;
    strips.push(s);
  }
  return c;
}

/* ---------- posa del foglio ---------- */
/* ---------- posa del foglio ---------- */
let lastT=-1, lastTime=0, lastMB='';
function applyTurn(t){
  if(t===lastT)return;
  /* H1: sfocatura di movimento proporzionale alla velocità reale del
     foglio. Il valore va SOLO sul nodo .curl (radice): applicarlo alle
     strisce annidate significherebbe N filtri composti, e il flip
     perderebbe i 60fps. A foglio lento --mb resta 0 e il filter è un
     no-op per il compositor. */
  const now = performance.now();
  const dt  = now - lastTime;
  if(dt > 0 && dt < 120 && lastT >= 0){
    const speed = Math.abs(t - lastT) / (dt/1000);      /* unità t al secondo */
    const mb = Math.min(1.2, speed * 0.42).toFixed(2);
    if(mb !== lastMB){ sb3d.style.setProperty('--mb', mb+'px'); lastMB = mb; }
  }else if(lastMB !== '0.00'){
    sb3d.style.setProperty('--mb','0px'); lastMB = '0.00';
  }
  lastTime = now;
  lastT=t;
  const th=Math.PI*t;
  const rigid = !!(turn && turn.rigid);
  const beta=(rigid?0.06:BETA)*Math.sin(th);
  const D=180/Math.PI;
  const tt=th+beta, td=2*beta/N;
  sb3d.style.setProperty('--tt',(tt*D).toFixed(2)+'deg');
  sb3d.style.setProperty('--td',(td*D).toFixed(3)+'deg');
  sb3d.style.setProperty('--shade',Math.sin(th).toFixed(3));
  const m = rigid ? .25 : .62;
  for(let i=0;i<strips.length;i++){
    const l1=rigid ? Math.abs(Math.cos(tt)) : Math.abs(Math.cos(tt-i*td));
    const l2=rigid ? l1 : Math.abs(Math.cos(tt-(i+1)*td));
    const s=strips[i], st=s.style;
    /* scrivere una custom property su un nodo annidato invalida tutti i
       discendenti: qui le strisce SONO annidate, quindi salta i no-op */
    const v1=l1.toFixed(3), v2=((1-l1)*m).toFixed(3), v3=((1-l2)*m).toFixed(3);
    if(s._lit!==v1){ st.setProperty('--lit',v1); s._lit=v1; }
    if(s._a1 !==v2){ st.setProperty('--a1', v2); s._a1 =v2; }
    if(s._a2 !==v3){ st.setProperty('--a2', v3); s._a2 =v3; }
  }
}

/* ---------- pittura dello stato corrente ---------- */
function paint(){
  const cur = turn ? turn.to : idx;
  const shift = cur===0 ? -25 : (cur===M-1 ? 25 : 0);
  sb3d.style.setProperty('--shift', shift+'%');
  /* H1: a foglio fermo la sfocatura deve sparire, non decadere:
     un residuo di blur sulla pagina posata si vedrebbe subito. */
  sb3d.style.setProperty('--mb','0px'); lastMB='0.00'; lastTime=0;


  /* A5/A6: stato copertina + posizione del segnalibro.
     Solo scritture di classe/custom property: la logica di flip
     non viene toccata. */
  const onCover = (cur===0 || cur===M-1);
  sb3d.classList.toggle('on-cover', onCover);
  if(!onCover && lastVisited!==cur && lastVisited>0 && lastVisited<M-1){
    const k = (lastVisited-1)/(M-3);           /* spread 1..M-2 → 0..1 */
    sb3d.style.setProperty('--ribbon-x', (8+k*84).toFixed(1)+'%');
  }
  if(!turn) lastVisited = idx;
  book.textContent='';
  strips.length=0;
  lastT=-1;
  document.dispatchEvent(new CustomEvent(turn?'book:turn':'book:settle',
    {detail:{idx:turn?turn.from:idx}}));
  if(!turn){
    const f=el('div','sb-full');
    const im=new Image(); im.src=PAGES[idx].url; im.draggable=false;
    f.appendChild(im); book.appendChild(f);
    sb3d.style.setProperty('--shade','0');
    preloadAround(idx);
  }else{
    const next=turn.dir==='next';
    book.appendChild(halfEl('left',  next?turn.from:turn.to));
    book.appendChild(halfEl('right', next?turn.to:turn.from));
    book.appendChild(buildCurl(turn.dir,turn.from,turn.to));
    applyTurn(turn.t);
  }
  const a=el('button','sb-zone sb-prev'), b=el('button','sb-zone sb-next');
  a.type=b.type='button';
  book.appendChild(a); book.appendChild(b);
  layout();
}
function layout(){ sb3d.style.setProperty('--bw',book.clientWidth+'px'); }
addEventListener('resize',layout,{passive:true});

/* ---------- loop unico: molla del foglio + molla del tilt ---------- */
let spring=null, raf=null, last=0;
function animateTo(target,onDone,k,c){
  spring={v:0,target,done:onDone,k,c};
  last=performance.now();              /* evita un primo dt gigante */
  kick();
}
function tick(now){
  raf=null;
  const dt=Math.min(0.032,(now-last)/1000||0.016); last=now;
  if(spring&&turn){
    const s=spring, x=turn.t-s.target;
    s.v += (-s.k*x - s.c*s.v)*dt;
    turn.t += s.v*dt;
    if(Math.abs(turn.t-s.target)<0.002 && Math.abs(s.v)<0.02){
      turn.t=s.target; spring=null; applyTurn(turn.t);
      const d=s.done; if(d) d();
    }else applyTurn(turn.t);
  }
  viewSpring();
  if((spring||viewActive)&&raf===null) raf=requestAnimationFrame(tick);
}
function kick(){ if(raf===null){ last=performance.now(); raf=requestAnimationFrame(tick); } }

/* ---------- tilt verso il cursore ---------- */
const TILT_X=4.5, TILT_Y=7;
const view={rx:0,ry:0,trx:0,try_:0};
let viewActive=false;
function viewSpring(){
  const e=0.14; let moved=false;
  for(const [k,t] of [['rx','trx'],['ry','try_']]){
    const d=view[t]-view[k];
    if(Math.abs(d)>0.0006){ view[k]+=d*e; moved=true; } else view[k]=view[t];
  }
  if(moved){
    sb3d.style.setProperty('--glint',(Math.abs(view.ry)/TILT_Y*.25).toFixed(3));
    sb3d.style.setProperty('--rx',view.rx.toFixed(2)+'deg');
    sb3d.style.setProperty('--ry',view.ry.toFixed(2)+'deg');
  }
  viewActive=moved; return moved;
}
addEventListener('pointermove',e=>{
  if(e.pointerType==='touch'||drag)return;
  const r=book.getBoundingClientRect(); if(!r.width)return;
  view.trx=-Math.max(-1,Math.min(1,(e.clientY-(r.top+r.height/2))/(r.height*0.9)))*TILT_X;
  view.try_= Math.max(-1,Math.min(1,(e.clientX-(r.left+r.width/2))/(r.width*0.62)))*TILT_Y;
  viewActive=true; kick();
},{passive:true});
addEventListener('pointerout',e=>{
  if(!e.relatedTarget){ view.trx=0; view.try_=0; viewActive=true; kick(); }
});
addEventListener('blur',()=>{ view.trx=0; view.try_=0; viewActive=true; kick(); });

/* ---------- controllo del giro (lock anti re-entrancy) ---------- */
function startTurn(dir,t){
  if(turn) return false;               /* MAI un giro sopra un altro */
  spring=null;
  turn={dir, from:idx, to: dir==='next'?(idx+1)%M:(idx-1+M)%M, t:t||0};
  turn.rigid=(turn.from===0||turn.to===0||turn.from===M-1||turn.to===M-1);
  preload(turn.to+(dir==='next'?1:-1));
  paint();
  return true;
}
function finish(newIdx){
  idx=newIdx; turn=null; spring=null; locked=false;
  paint();
}
function commit(){
  if(!turn||locked)return;
  locked=true;
  const to=turn.to;
  animateTo(1,()=>finish(to),COMMIT_K,COMMIT_C);
}
function cancel(){
  if(!turn||locked)return;
  locked=true;
  const from=turn.from;
  animateTo(0,()=>finish(from),CANCEL_K,CANCEL_C);
}
function step(dir){
  if(turn||locked||drag)return;        /* un giro alla volta, punto */
  if(startTurn(dir,0)) commit();
}

/* ---------- navigazione diretta (indice) ----------
   Salto secco con un breve velo: animare N giri di fila rischierebbe
   di sovrapporre turn, e il lock li scarterebbe silenziosamente. */
let jumpTimer=null;
window.BookNav={
  get index(){ return idx; },
  get length(){ return M; },
  /* veil: durata del velo in ms. L'indice usa il default secco (250);
     il sonno (G1) chiede 600, perché addormentarsi non è un salto. */
  goTo(n, veil){
    if(turn||locked||drag) return false;
    n=Math.max(0,Math.min(M-1,n|0));
    if(n===idx) return false;
    idx=n;
    preloadAround(idx);
    sb3d.classList.add('jumping');
    paint();
    clearTimeout(jumpTimer);
    jumpTimer=setTimeout(()=>{ jumpTimer=null; sb3d.classList.remove('jumping'); },
      Math.max(120, veil|0 || 250));
    return true;
  }
};

/* ---------- drag della pagina ---------- */
stage.addEventListener('pointerdown',e=>{
  if(e.button!==0)return;
  if(e.target.closest('.live-zone'))return;
  if(!e.target.closest('.sb-zone'))return;
  e.preventDefault();
  if(locked)return;                          /* sta finendo un giro */
  const r=book.getBoundingClientRect();
  const dir=(e.clientX-r.left)/r.width>0.5?'next':'prev';
  if(turn){
    if(turn.dir!==dir)return;                /* direzione opposta: ignora */
    spring=null;                             /* riprendi il foglio al volo */
  }else{
    if(!startTurn(dir,0))return;
  }
  try{ stage.setPointerCapture(e.pointerId); }catch(_){}
  if(hint)hint.classList.add('gone');
  drag={dir,x0:e.clientX,w:r.width,t0:turn.t,moved:0,vel:0,tPrev:performance.now()};
});

stage.addEventListener('pointermove',e=>{
  if(!drag||!turn)return;
  const dx=e.clientX-drag.x0;
  const sgn=drag.dir==='next'?-1:1;
  const t=Math.max(0,Math.min(1,drag.t0+sgn*dx/(drag.w*0.5)));
  const now=performance.now();
  drag.moved=Math.max(drag.moved,Math.abs(dx));
  /* mouse ad alto polling rate: sotto 8ms il dt è rumore e gonfia la velocità */
  if(now-drag.tPrev>=8){
    const dt=(now-drag.tPrev)/1000;
    drag.vel=drag.vel*0.7+((t-turn.t)/dt)*0.3;
    drag.tPrev=now;
  }
  turn.t=t; applyTurn(t);
});

function endDrag(e){
  if(!drag)return;
  const d=drag; drag=null;
  try{ if(e&&e.pointerId!=null) stage.releasePointerCapture(e.pointerId); }catch(_){}
  if(!turn||locked)return;
  if(d.moved<6){ commit(); return; }         /* tap secco = giro completo */
  (turn.t>0.42 || d.vel>1.1) ? commit() : cancel();
}
stage.addEventListener('pointerup',endDrag);
stage.addEventListener('pointercancel',endDrag);
stage.addEventListener('lostpointercapture',endDrag);
stage.addEventListener('dragstart',e=>e.preventDefault());
stage.addEventListener('selectstart',e=>e.preventDefault());

/* ---------- input: tastiera + frecce ---------- */
addEventListener('keydown',e=>{
  if(e.key!=='ArrowLeft'&&e.key!=='ArrowRight')return;
  if(e.repeat)return;
  if(e.metaKey||e.ctrlKey||e.altKey)return;
  const t=e.target;
  if(t&&(t.tagName==='INPUT'||t.tagName==='TEXTAREA'||t.isContentEditable))return;
  e.preventDefault();
  if(hint)hint.classList.add('gone');
  step(e.key==='ArrowRight'?'next':'prev');
});
const btnL=$('sbLeft'), btnR=$('sbRight');
if(btnL)btnL.addEventListener('click',()=>step('prev'));
if(btnR)btnR.addEventListener('click',()=>step('next'));

/* ---------- ridipingi quando pages.js passa alle blob URL ---------- */
document.addEventListener('spreads:ready',()=>{
  cache.clear();
  if(!turn&&!locked) paint();
},{once:true});

/* ---------- via ---------- */
paint();
})();