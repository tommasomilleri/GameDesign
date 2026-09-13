/* ============================================================
   CANDELA — la candela FISICA sul tavolo della hero.
   Non sostituisce lo strumento in inventario: lo affianca.
     · candela EQUIPAGGIATA dal toolbox → torcia che segue il mouse
       (comportamento storico, invariato, vive nei capitoli)
     · candela DA TAVOLO accesa → la stanza è illuminata: i segreti
       della pagina si vedono TUTTI, senza inseguire il cursore
   Due modi d'uso, stessa lore, zero conflitti: il primo scrive
   --mx/--my, il secondo aggiunge .candle-lit su <html>.

   Possiede anche il ciclo giorno/notte della hero (voce 19) e il
   tono del riflesso sul tavolo (voce 24).
   ============================================================ */
(() => {
  'use strict';

  /* true = il buio segue l'orologio vero di chi gioca.
     false = ciclo artificiale di 120s, come nel ripostiglio. */
  const REAL_TIME = true;
  const DAY_LEN = 120;                      /* secondi, se !REAL_TIME */

  const CW = 120, CH = 190;                 /* risoluzione interna canvas */
  const root = document.documentElement;
  const hero = document.getElementById('heroBook');
  if(!hero){ console.error('[candela] heroBook mancante'); return; }

  const reduce = matchMedia('(prefers-reduced-motion: reduce)').matches;

  /* ---------- DOM ---------- */
  const wrap = document.createElement('div');
  wrap.className = 'table-candle';
  wrap.hidden = true;
  const cv = document.createElement('canvas');
  cv.width = CW; cv.height = CH;
  cv.className = 'table-candle-cv';
  const btn = document.createElement('button');
  btn.type = 'button';
  btn.className = 'table-candle-hit';
  btn.setAttribute('aria-label','accendi o spegni la candela');
  wrap.appendChild(cv);
  wrap.appendChild(btn);
  document.body.appendChild(wrap);
  const g = cv.getContext('2d');

  let lit = false, t = 0, rafOff = null;

  /* ---------- il candeliere d'ottone (statico, pre-renderizzato) ---------- */
  const base = document.createElement('canvas');
  base.width = CW; base.height = CH;
  (function paintBase(){
    const b = base.getContext('2d');
    b.translate(CW/2, 0);
    /* piatto e stelo */
    b.fillStyle = '#6b5326';
    b.beginPath(); b.ellipse(0, CH-16, 40, 12, 0, 0, 7); b.fill();
    const brass = b.createLinearGradient(-16,0,16,0);
    brass.addColorStop(0,'#4e3c1a');
    brass.addColorStop(.35,'#c9a25c');
    brass.addColorStop(.55,'#e8c990');
    brass.addColorStop(1,'#5c4520');
    b.fillStyle = brass;
    b.beginPath();
    b.moveTo(-30, CH-20);
    b.quadraticCurveTo(-10, CH-34, -9, CH-60);
    b.lineTo(-9, CH-96);
    b.quadraticCurveTo(-20, CH-104, -19, CH-114);
    b.lineTo(19, CH-114);
    b.quadraticCurveTo(20, CH-104, 9, CH-96);
    b.lineTo(9, CH-60);
    b.quadraticCurveTo(10, CH-34, 30, CH-20);
    b.closePath(); b.fill();
    /* manico ad anello */
    b.strokeStyle = '#8a6b2a'; b.lineWidth = 6; b.lineCap = 'round';
    b.beginPath(); b.arc(34, CH-52, 17, -1.1, 1.5); b.stroke();
    /* cera */
    const wax = b.createLinearGradient(-17,0,17,0);
    wax.addColorStop(0,'#c8a25c'); wax.addColorStop(.4,'#f0dcae');
    wax.addColorStop(1,'#b8934e');
    b.fillStyle = wax;
    b.beginPath(); b.roundRect(-17, CH-206, 34, 96, 4); b.fill();
    /* colature */
    b.fillStyle = 'rgba(240,220,175,.85)';
    for(const [x,h] of [[-13,26],[-4,16],[9,32],[14,12]]){
      b.beginPath();
      b.moveTo(x, CH-118);
      b.quadraticCurveTo(x-3, CH-118+h*.6, x, CH-118+h);
      b.quadraticCurveTo(x+3, CH-118+h*.6, x+3.5, CH-118);
      b.closePath(); b.fill();
    }
  })();

  /* ---------- disegno per frame ---------- */
  function draw(){
    g.clearRect(0,0,CW,CH);
    g.drawImage(base,0,0);
    const wx = CW/2, wy = CH-206;

    /* stoppino */
    g.strokeStyle = lit ? '#2b1f1a' : '#3d3129';
    g.lineWidth = 2; g.lineCap = 'round';
    g.beginPath(); g.moveTo(wx, wy+2); g.lineTo(wx, wy-9); g.stroke();
    if(!lit){
      /* filo di fumo appena spenta: niente stato, solo un accenno */
      g.restore?.();
      return;
    }

    /* fiamma: due ellissi + flicker (sin lento + rumore veloce) */
    const fl = 1 + Math.sin(t*.09)*.10 + (Math.random()-.5)*.08;
    const sw = Math.sin(t*.06)*1.8;
    g.save();
    g.translate(wx + sw, wy - 10);
    const outer = g.createRadialGradient(0,4,1,0,0,26*fl);
    outer.addColorStop(0,'rgba(255,214,120,.95)');
    outer.addColorStop(.45,'rgba(255,150,40,.65)');
    outer.addColorStop(1,'rgba(255,120,0,0)');
    g.fillStyle = outer;
    g.beginPath(); g.ellipse(0,-4,14*fl,24*fl,0,0,7); g.fill();
    g.fillStyle = '#fff6cf';
    g.beginPath(); g.ellipse(0,-2,4.4*fl,10*fl,0,0,7); g.fill();
    g.fillStyle = 'rgba(90,120,220,.35)';
    g.beginPath(); g.ellipse(0,5,3.2,4.4,0,0,7); g.fill();
    g.restore();

    /* alone caldo attorno al candeliere */
    const halo = g.createRadialGradient(wx,wy-12,4,wx,wy-12,70*fl);
    halo.addColorStop(0,'rgba(255,190,110,.22)');
    halo.addColorStop(1,'rgba(255,170,80,0)');
    g.fillStyle = halo;
    g.beginPath(); g.arc(wx,wy-12,70*fl,0,7); g.fill();
  }

  function tickOn(){
    if(rafOff || !window.RAF) return;
    rafOff = RAF.add(() => { t++; draw(); }, reduce ? 6 : 30);
  }
  function tickOff(){ if(rafOff){ rafOff(); rafOff = null; } }

  /* ---------- accensione ---------- */
  function setLit(v){
    if(lit === v) return;
    lit = v;
    wrap.classList.toggle('lit', lit);
    /* C2: la classe globale è l'UNICO ponte verso i capitoli.
       Nessun modulo deve conoscere l'altro. */
    root.classList.toggle('candle-lit', lit);
    if(window.Sfx && Sfx.wood) Sfx.wood();
    lit ? tickOn() : (tickOff(), draw());
  }

  btn.addEventListener('click', e => {
    e.preventDefault(); e.stopPropagation();
    setLit(!lit);
  });

  /* ---------- presenza: solo dopo averla raccolta ---------- */
  function sync(){
    const has = !!(window.State && State.has && State.has('candle'));
    wrap.hidden = !has;
    if(!has && lit) setLit(false);
    if(has) draw();
  }
  document.addEventListener('closet:close', sync);
document.addEventListener('tool:change', sync);
document.addEventListener('inventory:change', sync);
sync();

  /* ---------- C3: ciclo giorno/notte della hero ----------
     La formula è DUPLICATA rispetto a ripostiglio.js di proposito:
     accoppiare i due moduli per risparmiare sei righe li renderebbe
     interdipendenti, e il ripostiglio deve poter morire da solo. */
  const t0 = performance.now();
  function daylight(){
    let ph;
    if(REAL_TIME){
      const d = new Date();
      ph = (d.getHours()*3600 + d.getMinutes()*60 + d.getSeconds()) / 86400;
      ph = (ph + .75) % 1;          /* mezzogiorno = picco di luce */
    }else{
      ph = ((performance.now()-t0)/1000 % DAY_LEN) / DAY_LEN;
    }
    return Math.max(0, Math.sin(ph*Math.PI*2));
  }
  if(window.RAF) RAF.add(() => {
    root.style.setProperty('--daylight', daylight().toFixed(3));
  }, 2);
  else root.style.setProperty('--daylight','1');
})();