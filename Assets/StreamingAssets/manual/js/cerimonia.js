/* ============================================================
   CERIMONIA — il sigillo di ceralacca non "compare": si stampa.
   Ascolta 'step:done' e mette in scena ~1.1s di rituale sopra la
   pagina completata: goccia che cade, ceralacca che si spande,
   timbro che scende, impatto (thump) e rimbalzo, campanella.

   Contiene anche G3: la firma a matita sul retro del libro
   (spread 7, lato destro), persistente in localStorage.
   ============================================================ */
(() => {
  'use strict';

  /* ===================== E1 — LA STAMPA DEL SIGILLO ===================== */
  const tilt = document.getElementById('sbTilt');
  const reduce = matchMedia('(prefers-reduced-motion: reduce)').matches;

  /* mappa passo → spread, la stessa dell'indice */
  const SPREAD_OF = {1:2, 2:3, 3:4, 4:5, 5:6};

  if(tilt) document.addEventListener('step:done', e => {
    const step = e.detail && e.detail.step;
    if(!step) return;
    /* la cerimonia si celebra SOLO se sei sulla pagina giusta:
       un sigillo che si stampa su uno spread che non stai guardando
       sarebbe un fuoco d'artificio nel garage. */
    if(window.BookNav && BookNav.index !== SPREAD_OF[step]) return;
    if(reduce){ if(window.Sfx && Sfx.chime) Sfx.chime(); return; }
    play();
  });

  function play(){
    const CW = 200, CH = 200;
    const cv = document.createElement('canvas');
    cv.className = 'seal-stage';
    cv.width = CW; cv.height = CH;
    cv.setAttribute('aria-hidden','true');
    tilt.appendChild(cv);
    const g = cv.getContext('2d');

    const T_DROP = 400, T_SPREAD = 300, T_STAMP = 200, T_LIFT = 220;
    const TOTAL = T_DROP + T_SPREAD + T_STAMP + T_LIFT;
    const cx = CW/2, cy = CH/2 + 6;
    const seed = Math.random()*6.28;
    let start = performance.now(), thumped = false, off = null;

    /* ---- la goccia di ceralacca ---- */
    function drop(g,y,squash){
      g.save(); g.translate(cx,y);
      g.fillStyle = '#8b0000';
      g.beginPath();
      g.ellipse(0,0,9*(1+squash*.35),11*(1-squash*.30),0,0,7);
      g.fill();
      g.fillStyle = 'rgba(255,255,255,.20)';
      g.beginPath(); g.ellipse(-2.5,-3,2.6,1.8,-.5,0,7); g.fill();
      g.restore();
    }

    /* ---- la pozza che si allarga, bordo irregolare ---- */
    function pool(g,k,flat){
      const R = 9 + k*(flat ? 21 : 17);
      g.save(); g.translate(cx,cy);
      g.fillStyle = '#8b0000';
      g.beginPath();
      for(let i=0;i<=22;i++){
        const a = i/22*6.283;
        const r = R*(1 + Math.sin(a*3+seed)*.10 + Math.cos(a*5+seed)*.05);
        const x = Math.cos(a)*r, y = Math.sin(a)*r*.92;
        i ? g.lineTo(x,y) : g.moveTo(x,y);
      }
      g.closePath(); g.fill();
      g.fillStyle = 'rgba(255,255,255,.14)';
      g.beginPath(); g.ellipse(-R*.28,-R*.32,R*.30,R*.18,-.5,0,7); g.fill();
      g.restore();
    }

    /* ---- il timbro di legno e ottone ---- */
    function stamp(g,y,press){
      g.save(); g.translate(cx,y);
      g.fillStyle = '#6e4629';
      g.beginPath(); g.roundRect(-9,-52,18,34,4); g.fill();
      g.fillStyle = '#8a6b42';
      g.beginPath(); g.ellipse(0,-54,13,6,0,0,7); g.fill();
      const brass = g.createLinearGradient(-20,0,20,0);
      brass.addColorStop(0,'#6b5326'); brass.addColorStop(.45,'#e8c990');
      brass.addColorStop(1,'#7a5f2c');
      g.fillStyle = brass;
      g.beginPath(); g.roundRect(-20,-20,40,14*(1-press*.25),3); g.fill();
      g.restore();
    }

    /* ---- l'impronta: la spunta impressa nella cera ---- */
    function mark(g,a){
      g.save(); g.translate(cx,cy); g.globalAlpha = a;
      g.strokeStyle = 'rgba(60,10,10,.75)';
      g.lineWidth = 3.4; g.lineCap = 'round'; g.lineJoin = 'round';
      g.beginPath();
      g.moveTo(-7,1); g.lineTo(-1.5,6.5); g.lineTo(8,-6.5);
      g.stroke();
      g.restore();
    }

    off = RAF.add(now => {
      const t = now - start;
      g.clearRect(0,0,CW,CH);

      if(t < T_DROP){
        /* caduta accelerata dall'alto del riquadro */
        const k = t/T_DROP, y = -20 + (cy+20)*k*k;
        drop(g, y, 0);
      }else if(t < T_DROP+T_SPREAD){
        const k = (t-T_DROP)/T_SPREAD;
        pool(g, k*k*(3-2*k), false);
      }else if(t < T_DROP+T_SPREAD+T_STAMP){
        const k = (t-T_DROP-T_SPREAD)/T_STAMP;
        pool(g, 1, k);
        stamp(g, cy - 40*(1-k*k), k);
        if(k > .92 && !thumped){
          thumped = true;
          if(window.Sfx && Sfx.wood)  Sfx.wood();     /* E3 */
          if(window.Sfx && Sfx.chime) Sfx.chime();    /* E2 */
        }
      }else{
        const k = Math.min(1,(t-T_DROP-T_SPREAD-T_STAMP)/T_LIFT);
        pool(g, 1, 1);
        mark(g, k);
        stamp(g, cy - 90*k);                          /* il timbro rimbalza via */
        g.globalAlpha = 1;
      }

      if(t >= TOTAL + 260){
        off(); off = null;
        cv.classList.add('gone');
        setTimeout(() => cv.remove(), 320);
      }
    });
  }

  /* ===================== G3 — LA FIRMA SUL RETRO ===================== */
  const SIG_KEY = 'ledger-signature';
  const sigCv = document.createElement('canvas');
  sigCv.className = 'sig-cv';
  sigCv.width = 420; sigCv.height = 130;
  const sg = sigCv.getContext('2d');

  let sigWired = false, signing = false;
  let sigLoaded = false;

  function loadSignature(){
    if(sigLoaded) return;
    sigLoaded = true;
    let data;
    try{ data = localStorage.getItem(SIG_KEY); }catch(_){ return; }
    if(!data) return;
    const im = new Image();
    im.onload = () => { try{ sg.drawImage(im,0,0); }catch(_){} };
    im.src = data;
  }

  let saveTimer = null;
  function saveSignature(){
    clearTimeout(saveTimer);
    /* si salva a fine tratto, non a ogni pixel: toDataURL è caro */
    saveTimer = setTimeout(() => {
      saveTimer = null;
      try{ localStorage.setItem(SIG_KEY, sigCv.toDataURL('image/png')); }
      catch(_){ /* quota piena: la firma resta solo in sessione */ }
    }, 400);
  }

  let sigLast = null;
  function sigDraw(e){
    if(!signing || State.tool !== 'pencil') return;
    const r = sigCv.getBoundingClientRect();
    if(!r.width) return;
    const x = (e.clientX-r.left)*(sigCv.width /r.width);
    const y = (e.clientY-r.top )*(sigCv.height/r.height);
    if(sigLast){
      sg.strokeStyle = 'rgba(48,44,40,.85)';
      sg.lineWidth = 2.2; sg.lineCap = 'round'; sg.lineJoin = 'round';
      sg.beginPath(); sg.moveTo(sigLast.x, sigLast.y); sg.lineTo(x,y); sg.stroke();
      /* granulosità della grafite: il tratto non è mai netto */
      sg.fillStyle = 'rgba(48,44,40,.25)';
      for(let i=0;i<4;i++)
        sg.fillRect(x+(Math.random()*4-2), y+(Math.random()*4-2), 1, 1);
    }
    sigLast = {x,y};
  }
  const sigStop = () => {
    if(!signing) return;
    signing = false; sigLast = null;
    if(window.Sfx && Sfx.stopScrub) Sfx.stopScrub();
    saveSignature();
  };

  function wireSignature(){
    if(sigWired) return;
    sigWired = true;
    sigCv.addEventListener('pointerdown', e => {
      if(!window.State || State.tool !== 'pencil') return;
      e.preventDefault(); e.stopPropagation();
      signing = true; sigLast = null;
      if(window.Sfx) Sfx.scrub('pencil');
      try{ sigCv.setPointerCapture(e.pointerId); }catch(_){}
      sigDraw(e);
    });
    sigCv.addEventListener('pointermove', sigDraw);
    sigCv.addEventListener('lostpointercapture', sigStop);
    addEventListener('pointerup', sigStop);
    addEventListener('pointercancel', sigStop);
  }

  Book.register({
    spread: 7,
    side: 'right',
    build(root){
      loadSignature();
      root.classList.add('sign-page');
      root.innerHTML = `
        <h3>Colophon</h3>
        <p class="note">Take up the pencil.<br>Sign, and the ledger is yours.</p>
        <div class="sig-wrap"></div>
        <p class="note sig-hint">— scholar of the Grand Fromagerie —</p>`;
      root.querySelector('.sig-wrap').appendChild(sigCv);
    },
    init(){ wireSignature(); },
    destroy(){ sigStop(); }
  });
})();