/* ============================================================
   BOOT — quello che si vede prima del gioco.
     F1  schermata di caricamento con topino animato, in attesa
         che pages.js finisca di generare le texture
     F2  il libro entra da destra e si posa sul tavolo con un
         tonfo e uno sbuffo di polvere
   Solo alla PRIMA apertura per sessione: chi ricarica per provare
   un dettaglio non deve rivedere la cinematica ogni volta.

   Va caricato per PRIMO nel body: deve coprire lo schermo prima
   che pages.js cominci a lavorare.
   ============================================================ */
(() => {
  'use strict';

  const reduce = matchMedia('(prefers-reduced-motion: reduce)').matches;
  let booted = false;
  try{ booted = sessionStorage.getItem('ledger-booted') === '1'; }catch(_){}

  /* già visto in questa sessione: nessun overlay, nessuna scivolata */
  if(booted){
    document.documentElement.classList.add('boot-done');
    return;
  }
  document.documentElement.classList.add('booting');

  /* ---------- l'overlay ---------- */
  const veil = document.createElement('div');
  veil.className = 'boot-veil';
  veil.innerHTML =
    '<canvas class="boot-mouse" width="120" height="120" aria-hidden="true"></canvas>' +
    '<p class="boot-text">The mice are preparing the ledger…</p>';
  const mount = () => document.body.appendChild(veil);
  if(document.body) mount();
  else addEventListener('DOMContentLoaded', mount, {once:true});

  /* ---------- il topino che impagina (due fotogrammi) ---------- */
  const bcv = veil.querySelector('.boot-mouse');
  const bg = bcv.getContext('2d');
  let frame = 0, animId = null;

  function drawBootMouse(f){
    bg.clearRect(0,0,120,120);
    bg.save(); bg.translate(60,74); bg.scale(2.1,2.1);
    /* il foglio che sta trasportando */
    bg.fillStyle = '#efe6d2';
    bg.save(); bg.rotate(f ? -.14 : -.05);
    bg.fillRect(2,-22,17,13);
    bg.strokeStyle = 'rgba(139,90,43,.5)'; bg.lineWidth = .6;
    bg.beginPath(); bg.moveTo(5,-18); bg.lineTo(16,-18);
    bg.moveTo(5,-15); bg.lineTo(14,-15); bg.stroke();
    bg.restore();
    /* coda */
    bg.strokeStyle = '#8b2020'; bg.lineWidth = 1.5;
    bg.beginPath(); bg.moveTo(-12,5);
    bg.quadraticCurveTo(-22, f ? 1 : 6, -19,-3); bg.stroke();
    /* corpo */
    bg.fillStyle = '#8b2020';
    bg.beginPath(); bg.ellipse(0,3,12,9,0,0,7); bg.fill();
    bg.beginPath(); bg.arc(7,-6,4.6,0,7); bg.arc(-2,-8,4.2,0,7); bg.fill();
    bg.beginPath(); bg.ellipse(11,4,6.5,5.5,.2,0,7); bg.fill();
    bg.fillStyle = '#2b1f1a';
    bg.beginPath(); bg.arc(17,4,1.6,0,7); bg.fill();
    bg.beginPath(); bg.arc(11,1,1.3,0,7); bg.fill();
    /* zampe: alternate tra i due fotogrammi */
    bg.strokeStyle = '#8b2020'; bg.lineWidth = 2; bg.lineCap = 'round';
    bg.beginPath();
    bg.moveTo(-5,11); bg.lineTo(-7, f ? 15 : 13);
    bg.moveTo( 5,11); bg.lineTo( 7, f ? 13 : 15);
    bg.stroke();
    bg.restore();
  }
  drawBootMouse(0);
  animId = setInterval(() => { frame ^= 1; drawBootMouse(frame); }, 400);

  /* ---------- attesa: texture pronte + un minimo di scena ---------- */
  const MIN_SHOW = reduce ? 200 : 900;   /* il lampo di mezzo secondo è peggio di niente */
  const t0 = performance.now();
  let ready = false, fired = false;

  document.addEventListener('spreads:ready', () => { ready = true; go(); }, {once:true});
  /* rete di sicurezza: se toBlob non si presentasse, non si resta bloccati */
  setTimeout(() => { ready = true; go(); }, 6000);

  function go(){
    if(fired || !ready) return;
    fired = true;
    const wait = Math.max(0, MIN_SHOW - (performance.now() - t0));
    setTimeout(reveal, wait);
  }

  /* ---------- F2: la scivolata ---------- */
  function reveal(){
    clearInterval(animId); animId = null;
    veil.classList.add('gone');
    setTimeout(() => veil.remove(), 620);

    const root = document.documentElement;
    try{ sessionStorage.setItem('ledger-booted','1'); }catch(_){}

    if(reduce){
      root.classList.remove('booting');
      root.classList.add('boot-done');
      return;
    }

    root.classList.add('boot-slide');       /* parte la transizione CSS */
    /* il tonfo arriva quando il libro tocca il tavolo, non prima */
    setTimeout(() => {
      if(window.Sfx && Sfx.wood) Sfx.wood();
      dustPuff();
      root.classList.remove('booting','boot-slide');
      root.classList.add('boot-done');
    }, 900);
  }

  /* ---------- sbuffo di polvere all'atterraggio ---------- */
  function dustPuff(){
    const book = document.getElementById('sbBook');
    const cv = document.getElementById('dustCv');
    if(!book || !cv || !window.RAF) return;
    const g = cv.getContext('2d');
    const b = book.getBoundingClientRect();
    if(!b.width) return;

    const bits = [];
    for(let i=0;i<40;i++){
      const side = Math.random() < .5 ? -1 : 1;
      bits.push({
        x: b.left + b.width*(.5 + side*(.18 + Math.random()*.3)),
        y: b.bottom - b.height*.10,
        vx: side*(.6 + Math.random()*2.6),
        vy: -Math.random()*2.2 - .4,
        r: Math.random()*2.2 + .8,
        life: 1
      });
    }
    /* un loop dedicato e breve: particles.js continua indisturbato,
       si sovrappone soltanto per una manciata di frame */
    const off = RAF.add(() => {
      let live = false;
      for(const p of bits){
        if(p.life <= 0) continue;
        live = true;
        p.x += p.vx; p.y += p.vy;
        p.vy += .06; p.vx *= .985;
        p.life -= .018;
        g.globalAlpha = Math.max(0, p.life) * .55;
        g.fillStyle = '#e6d3ba';
        g.beginPath(); g.arc(p.x, p.y, p.r, 0, 7); g.fill();
      }
      g.globalAlpha = 1;
      if(!live) off();
    });
  }
})();