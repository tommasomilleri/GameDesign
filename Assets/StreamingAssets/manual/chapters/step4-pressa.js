/* ============ STEP IV — PRESS THE CHEESE ============
   Fagotto di tela → pressa a vite → ruota. Click sulla pressa:
   squash + tacca romana. La risposta esatta è 2 (le corna della
   mucca dello Step I). 3+ pressature → reset con briciole.

   Layout e frecce ricalcati sullo sketch di riferimento:
     - fagotto di tela    → basso-sinistra
     - pressa a vite      → alto-centro/destra (grande, dominante)
     - ruota di formaggio → basso-destra
     - freccia A     : fagotto → pressa, UNA sola, arco che SALE da
                       sinistra e arriva puntando in alto-a-destra sul
                       fianco sinistro della pressa. NON ridiscende.
     - frecce B1/B2  : pressa → ruota, due archi che scendono a destra,
                       punte rivolte IN BASSO sopra la ruota
     - frecce C1/C2  : ruota → pressa, due archi che risalgono a sinistra,
                       punte rivolte IN ALTO verso la base della pressa

   NB sulla direzione delle punte: `curvedArrow` orienta la punta sulla
   tangente (x1,y1) − (cx,cy). Per una punta che arriva verso DESTRA il
   punto di controllo deve stare a SINISTRA dell'arrivo (cx < x1); per
   una punta che arriva SCENDENDO, il controllo sta sopra (cy < y1);
   per una che arriva SALENDO, sotto (cy > y1).
   ============================================================ */
(() => {
  'use strict';

  const CW = 400, CH = 320;
  const TARGET = 2;                 /* cross-ref: le corna della mucca */
  const SETTLE = 2000;              /* ms di attesa prima di validare  */

  const cv = document.createElement('canvas');
  cv.className = 'press-cv';
  cv.width = CW; cv.height = CH;
  const g = cv.getContext('2d');

  /* ---------- geometria di scena ----------
     Un solo posto dove toccare le posizioni. La pressa è il perno:
     tutte le frecce sono ancorate ai suoi bordi + un margine (GAP),
     così non toccano mai il legno.                              */
  const PRESS_X = 208, PRESS_Y = 112;      /* centro telaio              */
  const PRESS_HW = 58;                     /* mezza larghezza telaio     */
  const PRESS_TOP = PRESS_Y - 78;          /* traversa alta              */
  const PRESS_BOT = PRESS_Y + 92;          /* base                       */
  const CLOTH_X = 72,  CLOTH_Y = 236;      /* basso-sinistra             */
  const WHEEL_X = 330, WHEEL_Y = 246;      /* basso-destra               */
  const WHEEL_R = 30;
  const GAP = 14;                          /* aria fra freccia e oggetto */

  const INK = '#8b0000';

  let count = 0;                    /* pressature */
  let squash = 0;                   /* 0..1, animazione dello schiaccio */
  let screwRot = 0;                 /* rotazione cumulativa volantino, rad */
  let screwTarget = 0;
  let wheelX = 0;                   /* rotolamento della ruota, 0..1    */
  let crumbs = [];
  let solved = false;
  let rafOff = null, settleTimer = null;
  let noteEl = null;

  /* ---------- painter ---------- */
  function cloth(g, x, y, k){
    /* fagotto di tela: si appiattisce con k */
    const h = 46 * (1 - k*.42), w = 62 * (1 + k*.16);
    g.save(); g.translate(x, y);
    g.fillStyle = '#e6ddc6';
    g.beginPath();
    g.moveTo(-w/2, 0);
    g.quadraticCurveTo(-w/2, -h, 0, -h);
    g.quadraticCurveTo(w/2, -h, w/2, 0);
    g.closePath(); g.fill();
    g.strokeStyle = '#a8987a'; g.lineWidth = 1.4; g.stroke();
    /* pieghe */
    g.strokeStyle = 'rgba(120,105,75,.45)'; g.lineWidth = 1;
    for(const dx of [-16,0,16]){
      g.beginPath(); g.moveTo(dx, -2); g.quadraticCurveTo(dx+4, -h*.6, dx, -h+3); g.stroke();
    }
    /* nodo in cima */
    g.fillStyle = '#c8b78f';
    g.beginPath(); g.ellipse(0, -h+1, 8, 5, 0, 0, 7); g.fill();
    /* base/cesto */
    g.strokeStyle = 'rgba(160,110,150,.55)'; g.lineWidth = 2;
    g.beginPath(); g.moveTo(-w/2-3, 3); g.lineTo(w/2+3, 3); g.stroke();
    g.restore();
  }

  function press(g, x, y, k, rot){
    g.save(); g.translate(x, y);
    /* montanti */
    g.fillStyle = '#6e4629';
    g.fillRect(-58, -66, 12, 150);
    g.fillRect( 46, -66, 12, 150);
    /* traversa alta */
    g.fillRect(-58, -78, 116, 14);
    /* vite */
    g.strokeStyle = '#8a8a8a'; g.lineWidth = 5;
    g.beginPath(); g.moveTo(0, -74); g.lineTo(0, -14 + k*16); g.stroke();
    g.strokeStyle = 'rgba(255,255,255,.28)'; g.lineWidth = 1.4;
    for(let i=0;i<7;i++){
      const yy = -70 + i*8;
      g.beginPath(); g.moveTo(-4, yy); g.lineTo(4, yy+4); g.stroke();
    }
    /* volantino: ruota di 90° per pressata */
    g.save(); g.translate(0, -80); g.rotate(rot||0);
    g.strokeStyle = '#5a3a20'; g.lineWidth = 4;
    g.beginPath(); g.arc(0, 0, 17, 0, 7); g.stroke();
    g.beginPath(); g.moveTo(-17,0); g.lineTo(17,0);
    g.moveTo(0,-17); g.lineTo(0,17); g.stroke();
    g.restore();
    /* piatto mobile */
    g.fillStyle = '#5a3a20';
    g.fillRect(-46, -16 + k*16, 92, 12);
    /* base */
    g.fillStyle = '#4a3319';
    g.fillRect(-62, 78, 124, 14);
    g.restore();
  }

  function wheel(g, x, y, rot){
    g.save(); g.translate(x, y); g.rotate(rot);
    g.fillStyle = '#eee6c0';
    g.beginPath(); g.arc(0, 0, WHEEL_R, 0, 7); g.fill();
    g.strokeStyle = '#8a7c4a'; g.lineWidth = 1.8; g.stroke();
    g.fillStyle = 'rgba(190,175,120,.55)';
    for(const [cx,cy,r] of [[-9,-5,4],[7,4,3.2],[2,-11,2.6]]){
      g.beginPath(); g.arc(cx,cy,r,0,7); g.fill();
    }
    g.strokeStyle = 'rgba(150,135,80,.45)'; g.lineWidth = 1.2;
    g.beginPath(); g.moveTo(-20,0); g.lineTo(20,0); g.stroke();
    g.restore();
  }

  /* ---------- frecce ----------
     Curva quadratica + punta calcolata sulla tangente reale al
     capolinea, e arretrata di metà punta così il tratto non spunta
     oltre il triangolo.                                          */
  function curvedArrow(g, x0, y0, cx, cy, x1, y1, opt){
    const o = opt || {};
    const col  = o.color || INK;
    const lw   = o.width || 3.4;
    const head = o.head  || 14;      /* lunghezza della punta */
    const spread = o.spread || .38;  /* apertura della punta, rad */

    /* tangente in t=1 della quadratica: P1 - C */
    const tx = x1 - cx, ty = y1 - cy;
    const tl = Math.hypot(tx, ty) || 1;
    const ux = tx / tl, uy = ty / tl;

    /* accorcia la curva: il tratto finisce dove inizia la punta */
    const bx = x1 - ux * head * .72;
    const by = y1 - uy * head * .72;

    g.save();
    g.lineCap = 'round'; g.lineJoin = 'round';
    g.strokeStyle = col; g.fillStyle = col; g.lineWidth = lw;

    g.beginPath();
    g.moveTo(x0, y0);
    g.quadraticCurveTo(cx, cy, bx, by);
    g.stroke();

    const a = Math.atan2(uy, ux);
    g.beginPath();
    g.moveTo(x1, y1);
    g.lineTo(x1 - Math.cos(a - spread) * head, y1 - Math.sin(a - spread) * head);
    g.lineTo(x1 - Math.cos(a) * head * .62,    y1 - Math.sin(a) * head * .62);
    g.lineTo(x1 - Math.cos(a + spread) * head, y1 - Math.sin(a + spread) * head);
    g.closePath(); g.fill();
    g.restore();
  }

  function tally(g, x, y, n){
    g.save();
    g.strokeStyle = INK; g.lineWidth = 2.6; g.lineCap = 'round';
    for(let i=0;i<n;i++){
      const xx = x + i*11;
      g.beginPath(); g.moveTo(xx, y-11); g.lineTo(xx + (i%2?1.6:-1.6), y+11); g.stroke();
    }
    g.restore();
  }

  function slotRow(g, x, y, n, filled){
    g.save();
    for(let i=0;i<n;i++){
      const cx = x + i*24;
      g.strokeStyle = 'rgba(139,0,0,.7)'; g.lineWidth = 1.8;
      g.beginPath(); g.arc(cx, y, 8.5, 0, 7); g.stroke();
      if(i < filled){
        g.fillStyle = 'rgba(139,0,0,.6)';
        g.beginPath(); g.arc(cx, y, 5, 0, 7); g.fill();
      }
    }
    g.restore();
  }

  function draw(){
    g.clearRect(0,0,CW,CH);

    const L = PRESS_X - PRESS_HW, R = PRESS_X + PRESS_HW;

    /* ---- A: fagotto (drain) → pressa. UNA sola curva che SALE.
             Controllo a SINISTRA e SOTTO l'arrivo ⇒ tangente finale
             verso l'alto-a-destra: la punta entra nel fianco sinistro
             della pressa e NON torna indietro.                ---- */
    cloth(g, CLOTH_X, CLOTH_Y, 0);
    curvedArrow(g,
      CLOTH_X + 46, CLOTH_Y - 20,     /* fianco dx del fagotto          */
      CLOTH_X + 46, CLOTH_Y - 66,     /* cx < x1, cy > y1 ⇒ sale a dx   */
      L - GAP,      PRESS_Y + 36);    /* fianco sx della pressa         */

    press(g, PRESS_X, PRESS_Y, squash, screwRot);
    if(!solved) cloth(g, PRESS_X, PRESS_Y + 8 + squash*14, squash);

    /* ---- B1/B2: pressa → ruota. Partono dalla traversa alta e
             scendono a destra: punte in basso sopra la ruota.  ---- */
    curvedArrow(g,
      R + GAP,      PRESS_TOP + 10,
      WHEEL_X + 34, PRESS_TOP + 26,
      WHEEL_X + 26, WHEEL_Y - WHEEL_R - GAP);
    curvedArrow(g,
      R + GAP,      PRESS_TOP + 34,
      WHEEL_X + 12, PRESS_TOP + 52,
      WHEEL_X + 2,  WHEEL_Y - WHEEL_R - GAP);

    wheel(g, WHEEL_X + wheelX*40, WHEEL_Y, wheelX * 5.4);

    /* ---- C1/C2: ruota → pressa. Corte, ripide, punte in alto
             verso la base della pressa.                        ---- */
    curvedArrow(g,
      WHEEL_X - WHEEL_R - 6, WHEEL_Y + 26,
      PRESS_X + 74,          WHEEL_Y + 16,
      PRESS_X + 46,          PRESS_BOT + GAP);
    curvedArrow(g,
      WHEEL_X - WHEEL_R - 30, WHEEL_Y + 32,
      PRESS_X + 44,           WHEEL_Y + 22,
      PRESS_X + 16,           PRESS_BOT + GAP);

    /* ---- contatore ---- */
    const HUD_Y = 298;
    g.fillStyle = '#5c3a21'; g.font = 'italic 13px Georgia';
    g.textAlign = 'left'; g.textBaseline = 'middle';
    g.fillText('presses', 18, HUD_Y);
    slotRow(g, 84, HUD_Y, 4, Math.min(count, 4));
    tally(g, 214, HUD_Y, Math.min(count, 6));
    g.textBaseline = 'alphabetic';

    /* briciole del reset */
    for(const c of crumbs){
      g.globalAlpha = Math.max(0, c.life);
      g.fillStyle = '#d9c88f';
      g.fillRect(c.x, c.y, c.r*1.8, c.r);
    }
    g.globalAlpha = 1;
  }

  /* ---------- animazione ---------- */
  function tickOn(){
    if(rafOff || !window.RAF) return;
    rafOff = RAF.add(() => {
      let live = false;
      if(squash > 0 && !solved){ squash = Math.max(0, squash - .07); live = live || squash > 0; }
      if(Math.abs(screwTarget - screwRot) > .001){
        screwRot += (screwTarget - screwRot) * .18; live = true;
      }else screwRot = screwTarget;
      if(solved && wheelX < 1){ wheelX = Math.min(1, wheelX + .022); live = true; }
      for(let i=crumbs.length-1;i>=0;i--){
        const c = crumbs[i];
        c.x += c.vx; c.y += c.vy; c.vy += .28; c.life -= .03;
        if(c.life <= 0) crumbs.splice(i,1); else live = true;
      }
      draw();
      if(!live) tickOff();
    });
  }
  function tickOff(){ if(rafOff){ rafOff(); rafOff = null; } }

  function onPress(){
    if(solved) return;
    count++;
    squash = 1;
    screwTarget += Math.PI/2;             /* 90° in più per pressata */
    if(window.Sfx && Sfx.wood) Sfx.wood();
    clearTimeout(settleTimer);

    if(count > TARGET){
      /* troppo: reset con briciole */
      count = 0;
      screwTarget = 0; screwRot = 0;
      for(let i=0;i<18;i++) crumbs.push({
        x: PRESS_X + (Math.random()-.5)*70, y: PRESS_Y + 10,
        vx: (Math.random()-.5)*4, vy: -Math.random()*3 - 1,
        r: Math.random()*2.4 + 1, life: 1
      });
      if(noteEl) noteEl.innerHTML =
        'Pressed too many times — the curds crumbled.<br>Start again.';
    }else{
      settleTimer = setTimeout(() => {
        settleTimer = null;
        if(count === TARGET && !solved){
          solved = true;
          /* scia di 3 briciole quando la ruota rotola fuori */
          for(let i=0;i<3;i++) crumbs.push({
            x: WHEEL_X - 20 + i*8, y: WHEEL_Y,
            vx: .6 + Math.random()*.6, vy: -Math.random()*1.2 - .3,
            r: Math.random()*1.6 + .8, life: 1
          });
          if(noteEl) noteEl.innerHTML =
            '<b style="color:#8b0000">The wheel takes shape.</b><br>Two presses. No more.';
          if(window.State && State.completeStep) State.completeStep(4);
          tickOn();
        }
      }, SETTLE);
    }
    tickOn();
  }

  let wired = false;
  function wire(){
    if(wired) return;
    wired = true;
    cv.addEventListener('pointerdown', e => {
      if(e.button !== 0) return;
      const r = cv.getBoundingClientRect();
      if(!r.width) return;
      const x = (e.clientX - r.left) * (CW / r.width);
      const y = (e.clientY - r.top ) * (CH / r.height);
      /* zona attiva: il corpo della pressa, derivata dalla geometria */
      if(x < PRESS_X - PRESS_HW || x > PRESS_X + PRESS_HW) return;
      if(y < PRESS_TOP - 26 || y > PRESS_BOT) return;
      e.preventDefault(); e.stopPropagation();
      onPress();
    });
  }

  Book.register({
    spread: 5,
    side: 'right',
    build(root){
      root.classList.add('press-page');
      root.innerHTML = `
        <h3>Step IV — The Pressing</h3>
        <p class="note">Turn the screw. The beast counts for you.</p>
        <div class="press-wrap"></div>`;
      root.querySelector('.press-wrap').appendChild(cv);
      noteEl = root.querySelector('.note');
      if(solved) noteEl.innerHTML =
        '<b style="color:#8b0000">The wheel takes shape.</b><br>Two presses. No more.';
      draw();
    },
    init(){ wire(); if(crumbs.length || squash > 0) tickOn(); },
    destroy(){ tickOff(); clearTimeout(settleTimer); settleTimer = null; noteEl = null; }
  });
})();