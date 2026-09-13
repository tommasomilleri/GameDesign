/* ============ CAP. III — THE TALE OF THE WHITE KNIGHT ============
   Canvas persistenti (i disegni sopravvivono ai giri di pagina).
   I listener sono agganciati UNA SOLA VOLTA: init() viene richiamata
   a ogni book:settle, quindi agganciarli lì era un memory leak.
   ============================================================ */
(() => {
  'use strict';

  const inkCv = document.createElement('canvas');
  const rubCv = document.createElement('canvas');
  inkCv.className = 'ink-canvas';
  inkCv.width = 140; inkCv.height = 44;
  rubCv.width = 430; rubCv.height = 80;

  const ink = inkCv.getContext('2d');
  const rub = rubCv.getContext('2d');

  let inkReady = false, wired = false;
  let scrubbing = false, penciling = false;
  let currentRoot = null, secret = null;

  /* --- tracciamento dei tre segreti (FASE B3) --- */
  let inkCleared = 0;      /* stima 0..1 dell'area di spugna ripulita */
  let rubCovered = 0;      /* stima 0..1 dell'area coperta da frottage */
  let candleUsed = false;  /* la candela è passata sul secret almeno una volta */
  let step3Done = false;

  function checkStep3(){
    if(step3Done) return;
    if(inkCleared >= .70 && rubCovered >= .40 && candleUsed){
      step3Done = true;
      if(window.State && State.completeStep) State.completeStep(3);
    }
  }

  function initInk(){
    if(inkReady) return;
    ink.fillStyle = '#1a0f0a';
    ink.beginPath();                                  /* le 3 pozze originali */
    ink.arc(inkCv.width/2,   inkCv.height/2, 16, 0, 7);
    ink.arc(inkCv.width/4,   inkCv.height/2, 12, 0, 7);
    ink.arc(inkCv.width/1.2, inkCv.height/2, 11, 0, 7);
    ink.fill();
    ink.globalCompositeOperation = 'destination-out';
    inkReady = true;
  }

  function wipe(e){
    if(!scrubbing || State.tool !== 'sponge') return;
    const r = inkCv.getBoundingClientRect();
    if(!r.width) return;
    ink.beginPath();
    ink.arc((e.clientX-r.left)*(inkCv.width/r.width),
            (e.clientY-r.top )*(inkCv.height/r.height), 12, 0, 7);
    ink.fill();
    inkCleared = Math.min(1, inkCleared + .012);
    checkStep3();
  }

  function graphite(e){
    if(!penciling || State.tool !== 'pencil') return;
    const r = rubCv.getBoundingClientRect();
    if(!r.width) return;
    const x = (e.clientX-r.left)*(rubCv.width/r.width);
    const y = (e.clientY-r.top )*(rubCv.height/r.height);
    rub.fillStyle = 'rgba(50,50,50,.08)';             /* fuori dal loop: era ridondante */
    for(let i=0;i<15;i++){
      rub.fillRect(x+(Math.random()*30-15), y+(Math.random()*30-15),
                   Math.random()*2+1, Math.random()*2+1);
    }
    rubCovered = Math.min(1, rubCovered + .01);
    checkStep3();
  }
  const stopAll = () => {
    if(!scrubbing && !penciling) return;
    scrubbing = false; penciling = false;
    if(window.Sfx && Sfx.stopScrub) Sfx.stopScrub();
  };

  /* --- agganciato UNA VOLTA SOLA, per tutta la vita della pagina --- */
  function wire(){
    if(wired) return;
    wired = true;

    inkCv.addEventListener('pointerdown', e => {
      if(State.tool !== 'sponge') return;
      e.preventDefault(); e.stopPropagation();
      scrubbing = true;
      if(window.Sfx) Sfx.scrub('sponge');
      try{ inkCv.setPointerCapture(e.pointerId); }catch(_){}
      wipe(e);
    });
    inkCv.addEventListener('pointermove', wipe);
    inkCv.addEventListener('lostpointercapture', stopAll);

    rubCv.addEventListener('pointerdown', e => {
      if(State.tool !== 'pencil') return;
      e.preventDefault(); e.stopPropagation();
      penciling = true;
      if(window.Sfx) Sfx.scrub('pencil');
      try{ rubCv.setPointerCapture(e.pointerId); }catch(_){}
      graphite(e);
    });
    rubCv.addEventListener('pointermove', graphite);
    rubCv.addEventListener('lostpointercapture', stopAll);

    addEventListener('pointerup', stopAll);
    addEventListener('pointercancel', stopAll);

    /* la candela segue il puntatore: un solo listener, filtrato per root */
    addEventListener('pointermove', e => {
      if(State.tool !== 'candle' || !currentRoot || !secret) return;
      const r = currentRoot.getBoundingClientRect();
      if(!r.width) return;
     const d = parseFloat(secret.style.getPropertyValue('--d')) || 260;
      secret.style.setProperty('--mx',(e.clientX-r.left-d/2)+'px');
      secret.style.setProperty('--my',(e.clientY-r.top -d/2)+'px');
      candleUsed = true;
      checkStep3();
    }, {passive:true});

    /* un SOLO timer, non uno per visita */
setInterval(() => {
  if(!secret) return;
  if(document.documentElement.classList.contains('candle-lit')) return;
  if(State.tool === 'candle'){
    secret.style.setProperty('--d',(260+Math.random()*30-15).toFixed(0)+'px');
  }else{
    secret.style.setProperty('--mx','-999px');
    secret.style.setProperty('--my','-999px');
  }
}, 60);

    /* girando pagina il root muore: sgancia i riferimenti */
    document.addEventListener('book:turn', () => {
      stopAll();
      currentRoot = null;
      secret = null;
    });
  }

  Book.register({
    spread: 4,
    side: 'right',
      build(root){
      initInk();
      root.classList.add('tomo');
      /* B1: la scrittura si traccia solo la PRIMA volta in sessione.
         Ink è definito da js/ambient.js; la guardia serve se quel
         file non fosse caricato: il poema deve comparire comunque. */
      const rev = (window.Ink && Ink.firstVisit(4)) ? ' ink-reveal' : '';
      root.innerHTML = `
        <h3>The Tale of the White Knight</h3>
        <div class="poem${rev}">
          <div class="stanza"><span class="minia">A</span>Pilgrim pale from pastures green, the White Knight leaves his horn,<br>
            He seeks the hearth's most gentle glow, where the <span class="keyword">Odd Red Mouse</span> is born.</div>
          <div class="stanza"><span class="minia">H</span>old fast thy shield,
            <span class="stain-wrap">do not <span class="let">LET</span> the</span>
            dragon's breath draw near,<br>
            Lest the sleeping soul within should perish in its fear.</div>
          <div class="stanza"><span class="minia">O</span>ffer tears of woodland beasts, the crystal, and the spring,<br>
            Until the Knight's pure heart doth <span class="keyword">CLEAVE</span>, a truly wondrous thing.</div>
        </div>
        <div class="rub-wrap">
          <div class="debossed">Batch ruined at 40°.<br>Max heat 32°. - E.</div>
        </div>
        <div class="secret-layer">
          <div class="secret-note"><span class="sline">Sword parts the whey.</span><span class="sline">Shield binds the curds.</span><span class="sline">Watch the vat!</span></div>
        </div>`;
      root.querySelector('.stain-wrap').appendChild(inkCv);
      root.querySelector('.rub-wrap').prepend(rubCv);
    },
    init(root){
      currentRoot = root;
      secret = root.querySelector('.secret-layer');
      wire();                                          /* no-op dalla 2ª volta */
    },
      destroy(){
      stopAll();
      currentRoot = null;
      secret = null;
    }
  });
})();