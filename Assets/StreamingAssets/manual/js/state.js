/* ============================================================
   STATE — strumenti raccolti, strumento in mano, persistenza.
   API: State.has(t) · State.collect(t) · State.equip(t) · State.tool
   Emette: 'tool:change' quando cambia lo strumento in mano.
   ============================================================ */
window.State = (() => {
  'use strict';

  const KEY='ledger-save';

  /* Versione dello SCHEMA del salvataggio. Alzarla di 1 ogni volta che
     cambia la forma dei dati o che serve un reset pulito per i tester:
     un save con v diversa viene scartato in blocco, senza migrazioni.
     v2 = introduzione di steps{} + inventario che DEVE partire vuoto. */
  const SCHEMA_V = 2;

  /* Metti true SOLO per testare i capitoli senza passare dal ripostiglio.
     In gioco reale deve restare false, altrimenti il ripostiglio è inutile. */
  const DEV_ALL_TOOLS = false;

  const fresh = () => ({ v: SCHEMA_V, tools: [], steps: {} });

  let data = fresh();
  try{
    const raw = JSON.parse(localStorage.getItem(KEY) || 'null');
    if(raw && raw.v === SCHEMA_V){
      if(Array.isArray(raw.tools)) data.tools = raw.tools;
      if(raw.steps && typeof raw.steps === 'object') data.steps = raw.steps;
    }else if(raw){
      /* save di una versione precedente (o senza v): si butta.
         Nessuna migrazione: il gioco è corto, ricominciare non costa. */
      console.info('[state] salvataggio obsoleto scartato (v=' +
        (raw.v === undefined ? 'assente' : raw.v) + ' ≠ ' + SCHEMA_V + ')');
      try{ localStorage.removeItem(KEY); }catch(_){}
    }
  }catch(_){
    /* storage corrotto o disabilitato: si riparte puliti */
    data = fresh();
    try{ localStorage.removeItem(KEY); }catch(_){}
  }
  /* rete di sicurezza: un save manomesso può avere i campi giusti ma
     con tipi sbagliati, e renderToolbox() esploderebbe su .length */
  if(!Array.isArray(data.tools)) data.tools = [];
  if(!data.steps || typeof data.steps !== 'object') data.steps = {};
  data.v = SCHEMA_V;

  if(DEV_ALL_TOOLS && !data.tools.length) data.tools = ['sponge','pencil','candle'];

  let tool = null;

  const save = () => {
    try{ localStorage.setItem(KEY, JSON.stringify(data)); }
    catch(_){ /* quota piena o modalità privata: ignora, non è fatale */ }
  };

  const S = {
    get tool(){ return tool; },
    /* State.reset() in console: riparte da zero senza toccare il DevTools
       Application panel. Non è gameplay, è strumento di test. */
    reset(){
      data = fresh();
      tool = null;
      document.body.classList.remove('tool-sponge','tool-candle','tool-pencil');
      try{ localStorage.removeItem(KEY); }catch(_){}
      renderToolbox();
      document.dispatchEvent(new CustomEvent('tool:change',{detail:{tool:null}}));
      console.info('[state] salvataggio azzerato');
    },
    has: t => data.tools.includes(t),
    stepDone: n => data.steps[n] === true,
    completeStep(n){
      if(data.steps[n]) return;
      data.steps[n] = true;
      save();
      document.dispatchEvent(new CustomEvent('step:done',{detail:{step:n}}));
    },
    collect(t){
  if(!DRAW[t]){ console.warn('[state] strumento sconosciuto:', t); return; }
  if(S.has(t)) return;
  data.tools.push(t);
  save();
  document.dispatchEvent(new CustomEvent('inventory:change',{detail:{tool:t}}));
},
    equip(t){
      if(!S.has(t)) return;
      tool = (tool===t) ? null : t;
      document.body.classList.remove('tool-sponge','tool-candle','tool-pencil');
      if(tool) document.body.classList.add('tool-'+tool);
      /* niente re-render dei canvas: basta spostare la classe .active.
         Prima equip() ridisegnava 3 canvas 48×48 a ogni clic. */
      const box = document.getElementById('toolbox');
      if(box) for(const b of box.querySelectorAll('.tool-btn'))
        b.classList.toggle('active', b.dataset.tool === tool);
      document.dispatchEvent(new CustomEvent('tool:change',{detail:{tool}}));
    }
  };

  /* ---- icone disegnate a canvas ---- */
  const DRAW={
    sponge(g){
      g.fillStyle='#d9c26a'; g.beginPath(); g.roundRect(8,14,32,20,5); g.fill();
      g.fillStyle='rgba(90,70,20,.4)';
      [[15,20],[26,28],[33,19],[20,30],[30,24]].forEach(([x,y])=>{
        g.beginPath(); g.arc(x,y,2,0,7); g.fill();
      });
    },
    pencil(g){
      g.save(); g.translate(24,24); g.rotate(-.6);
      g.fillStyle='#c8952b'; g.fillRect(-16,-4,24,8);
      g.fillStyle='#e6d3ba'; g.beginPath();g.moveTo(8,-4);g.lineTo(16,0);g.lineTo(8,4);g.closePath();g.fill();
      g.fillStyle='#2b1f1a'; g.beginPath();g.moveTo(12,-2);g.lineTo(16,0);g.lineTo(12,2);g.closePath();g.fill();
      g.restore();
    },
    candle(g){
      g.fillStyle='#e6c280'; g.beginPath();g.roundRect(18,18,12,22,2);g.fill();
      g.strokeStyle='#3d2314'; g.beginPath();g.moveTo(24,18);g.lineTo(24,13);g.stroke();
      const fl=g.createRadialGradient(24,10,1,24,10,7);
      fl.addColorStop(0,'#fff3c0');fl.addColorStop(.5,'#ff9900');fl.addColorStop(1,'rgba(255,120,0,0)');
      g.fillStyle=fl; g.beginPath();g.ellipse(24,10,5,7,0,0,7);g.fill();
    }
  };

  function renderToolbox(){
    const box=document.getElementById('toolbox');
    if(!box) return;
    box.textContent='';
    if(!data.tools.length){
      const s=document.createElement('span');
      s.className='empty-inv';
      s.textContent="L'inventario è vuoto. Esplora il ripostiglio!";
      box.appendChild(s);
      return;
    }
    for(const t of data.tools){
      if(!DRAW[t]) continue;                      /* save vecchio con nomi obsoleti */
      const b=document.createElement('button');
      b.type='button';
      b.className='tool-btn'+(tool===t?' active':'');
      b.dataset.tool=t;
      b.setAttribute('aria-label', t);
      const c=document.createElement('canvas'); c.width=48; c.height=48;
      DRAW[t](c.getContext('2d'));
      b.appendChild(c);
      b.addEventListener('click',()=>S.equip(t));
      box.appendChild(b);
    }
  }

  if(document.readyState==='loading') addEventListener('DOMContentLoaded', renderToolbox);
  else renderToolbox();

  return S;
})();