/* ============ STEP V — THE ERRANT KNIGHT ============
   Sostituisce il vecchio termometro (duplicava Unity). Qui il libro
   CIFRA l'informazione: un poema + una macchia di spugna su "one
   more" + un segreto rivelabile con la candela. Nessuna simulazione
   di temperatura: il gameplay è leggere, ripulire, illuminare.
   Pattern identico a step3-poema.js: canvas persistente, wire() una
   sola volta, destroy() sgancia i riferimenti al cambio pagina.
   ============================================================ */
(() => {
  'use strict';

  const inkCv = document.createElement('canvas');
  inkCv.className = 'ink-canvas';
  inkCv.width = 120; inkCv.height = 40;
  const ink = inkCv.getContext('2d');

  let inkReady = false, wired = false, scrubbing = false;
  let currentRoot = null, secret = null;
  let inkCleared = 0, candleUsed = false, step5Done = false;

  function checkStep5(){
    if(step5Done) return;
    if(inkCleared >= .70 && candleUsed){
      step5Done = true;
      if(window.State && State.completeStep) State.completeStep(5);
    }
  }

  function initInk(){
    if(inkReady) return;
    ink.fillStyle = '#1a0f0a';
    ink.beginPath();
    ink.arc(inkCv.width/2,   inkCv.height/2, 15, 0, 7);
    ink.arc(inkCv.width/3.2, inkCv.height/2, 10, 0, 7);
    ink.arc(inkCv.width/1.15,inkCv.height/2, 9,  0, 7);
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
            (e.clientY-r.top )*(inkCv.height/r.height), 11, 0, 7);
    ink.fill();
    inkCleared = Math.min(1, inkCleared + .014);
    checkStep5();
  }
  const stopAll = () => {
    if(!scrubbing) return;
    scrubbing = false;
    if(window.Sfx && Sfx.stopScrub) Sfx.stopScrub();
  };

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
    addEventListener('pointerup', stopAll);
    addEventListener('pointercancel', stopAll);

    /* la candela segue il puntatore, filtrata sul root corrente */
    addEventListener('pointermove', e => {
      if(State.tool !== 'candle' || !currentRoot || !secret) return;
      const r = currentRoot.getBoundingClientRect();
      if(!r.width) return;
      const d = parseFloat(secret.style.getPropertyValue('--d')) || 240;
      secret.style.setProperty('--mx',(e.clientX-r.left-d/2)+'px');
      secret.style.setProperty('--my',(e.clientY-r.top -d/2)+'px');
      candleUsed = true;
      checkStep5();
    }, {passive:true});

    setInterval(() => {
      if(!secret) return;
      if(document.documentElement.classList.contains('candle-lit')) return;
      if(State.tool === 'candle'){
        secret.style.setProperty('--d',(240+Math.random()*26-13).toFixed(0)+'px');
      }else{
        secret.style.setProperty('--mx','-999px');
        secret.style.setProperty('--my','-999px');
      }
    }, 60);

    document.addEventListener('book:turn', () => {
      stopAll();
      currentRoot = null;
      secret = null;
    });
  }

  Book.register({
    spread: 6,
    side: 'right',
        build(root){
      initInk();
      root.classList.add('tomo', 'knight-page');
      const rev = (window.Ink && Ink.firstVisit(6)) ? ' ink-reveal' : '';
      root.innerHTML = `
        <h3>Step V — The Errant Knight</h3>
        <div class="poem${rev}">
          <div class="stanza"><span class="minia">A</span>knight came riding, pale and worn,<br>
            his silver cloak by travels torn.<br>
            He sought a bed to rest his head,<br>
            where seldom rain nor damp is spread.</div>
          <div class="stanza"><span class="minia">T</span>oo low, the stones weep cold and wet;<br>
            too high, the rafters hotter yet.<br>
            He climbed one stair, and then
            <span class="stain-wrap">a bit <span class="let">MORE</span></span>,<br>
            and laid his shield upon the floor.</div>
          <div class="stanza"><span class="minia">T</span>here sleeps he still, in armoured white,<br>
            where breath of hearth and stone unite.<br>
            Count as the mice count, from the ground —<br>
            and there your resting wheel is found.</div>
        </div>
        <div class="secret-layer">
          <div class="secret-note"><span class="sline">The knight lies not on wood but stone.</span><span class="sline">Two above the cellar floor, one below the crown.</span><span class="sline">Tell your partner: the third bed from the ground.</span></div>
        </div>`;
      root.querySelector('.stain-wrap').appendChild(inkCv);
    },
    init(root){
      currentRoot = root;
      secret = root.querySelector('.secret-layer');
      wire();
    },
    destroy(){
      stopAll();
      currentRoot = null;
      secret = null;
    }
  });
})();