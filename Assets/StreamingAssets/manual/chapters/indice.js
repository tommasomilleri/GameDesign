/* ============ INDICE — THE FIVE STEPS (voci vive) ============
   Vive sulla pagina DESTRA dello spread 1. Cinque voci manoscritte
   che saltano allo spread del capitolo con BookNav.goTo().
   Il sigillo di ceralacca compare sulle voci già completate
   (State.stepDone), e si aggiorna da solo all'evento 'step:done'.
   ============================================================ */
(() => {
  'use strict';

  const ENTRIES = [
    { n:1, spread:2, label:'I — Find the Right Milk' },
    { n:2, spread:3, label:'II — Heat the Milk'      },
    { n:3, spread:4, label:'III — Curdle the Milk'   },
    { n:4, spread:5, label:'IV — Press the Cheese'   },
    { n:5, spread:6, label:'V — Age the Cheese'      }
  ];

  let listEl = null;           /* <ol> corrente, null a pagina girata */

  /* sigillo di ceralacca disegnato a canvas (niente emoji, niente file) */
  function sealCanvas(){
    const c = document.createElement('canvas');
    c.width = 26; c.height = 26;
    const g = c.getContext('2d');
    g.translate(13,13);
    /* goccia irregolare */
    g.fillStyle = '#8b0000';
    g.beginPath();
    for(let i=0;i<=18;i++){
      const a = i/18*6.283;
      const r = 10 + Math.sin(a*3+1.2)*1.4 + Math.cos(a*5)*.8;
      const x = Math.cos(a)*r, y = Math.sin(a)*r;
      i ? g.lineTo(x,y) : g.moveTo(x,y);
    }
    g.closePath(); g.fill();
    /* lucido */
    g.fillStyle = 'rgba(255,255,255,.18)';
    g.beginPath(); g.ellipse(-3,-4,4,2.6,-.5,0,7); g.fill();
    /* spunta impressa */
    g.strokeStyle = 'rgba(60,10,10,.75)'; g.lineWidth = 2.2;
    g.lineCap = 'round'; g.lineJoin = 'round';
    g.beginPath(); g.moveTo(-4.5,.5); g.lineTo(-1,4); g.lineTo(5,-4); g.stroke();
    return c;
  }

  function refreshSeals(){
    if(!listEl) return;
    for(const b of listEl.querySelectorAll('.idx-link')){
      const n = +b.dataset.step;
      const done = !!(window.State && State.stepDone && State.stepDone(n));
      b.classList.toggle('done', done);
      let seal = b.querySelector('.idx-seal');
      if(done && !seal){
        seal = document.createElement('span');
        seal.className = 'idx-seal';
        seal.appendChild(sealCanvas());
        b.appendChild(seal);
      }else if(!done && seal){
        seal.remove();
      }
    }
  }

  /* un solo listener per tutta la vita della pagina: se l'indice non è
     montato, refreshSeals() è un no-op grazie alla guardia su listEl */
  document.addEventListener('step:done', refreshSeals);

  Book.register({
    spread: 1,
    side: 'right',
    build(root){
      root.classList.add('index-page');
      const ol = document.createElement('ol');
      ol.className = 'idx-list';
      /* B1: anche l'indice si scrive da solo, ma solo la prima volta */
      if(window.Ink && Ink.firstVisit(1)) ol.classList.add('ink-reveal');
      for(const e of ENTRIES){
        const li = document.createElement('li');
        const b = document.createElement('button');
        b.type = 'button';
        b.className = 'idx-link';
        b.dataset.step = e.n;
        b.dataset.spread = e.spread;
        const t = document.createElement('span');
        t.className = 'idx-title';
        t.textContent = e.label;
        b.appendChild(t);
        li.appendChild(b);
        ol.appendChild(li);
      }
      root.appendChild(ol);
      listEl = ol;
      refreshSeals();
    },
    init(root){
      const ol = root.querySelector('.idx-list');
      if(!ol) return;
      listEl = ol;
      refreshSeals();
      /* delega: un listener per la lista, non cinque */
      /* Il salto avviene su POINTERUP, non su click: l'engine cattura il
         puntatore sullo stage e in certi percorsi il 'click' non viene
         mai sintetizzato. stopPropagation su pointerdown impedisce al
         drag della pagina di partire da sotto la voce. */
      ol.addEventListener('pointerdown', ev => {
        const b = ev.target.closest('.idx-link');
        if(!b) return;
        ev.preventDefault(); ev.stopPropagation();
      });

      ol.addEventListener('pointerup', ev => {
        const b = ev.target.closest('.idx-link');
        if(!b) return;
        ev.preventDefault(); ev.stopPropagation();
        const n = +b.dataset.spread;
        if(!window.BookNav || typeof BookNav.goTo !== 'function'){
          console.error('[indice] BookNav assente: engine.js non ha esposto goTo');
          return;
        }
        if(window.Sfx && Sfx.wood) Sfx.wood();
        const ok = BookNav.goTo(n);
        if(!ok) console.warn('[indice] salto a spread ' + n + ' rifiutato ' +
          '(giro in corso, o sei già lì)');
      });

      /* la tastiera deve continuare a funzionare sui <button> */
      ol.addEventListener('keydown', ev => {
        if(ev.key !== 'Enter' && ev.key !== ' ') return;
        const b = ev.target.closest('.idx-link');
        if(!b) return;
        ev.preventDefault();
        if(window.BookNav) BookNav.goTo(+b.dataset.spread);
      });
    },
    destroy(){
      listEl = null;
    }
  });
})();