/* ============ STEP III — THE FOUR VIALS (legenda visiva) ============
   Vive sulla pagina SINISTRA dello spread 4.
   Quattro fiale disegnate a canvas, in ordine SPARSO. Nessuna
   validazione qui: l'ordine si verifica in Unity (contatore
   "Added: 0/4"). Hover/tap evidenzia la fiala con un contorno a
   matita, a scopo di legenda visiva per il poema di step3-poema.js.
   ============================================================ */
(() => {
  'use strict';

  const VW = 54, VH = 92;   /* dimensione interna di ogni fiala */

  const VIALS = [
    { key:'salt',    label:'Salt',             fill:'#f4efe4', cap:'#8a8a8a' },
    { key:'starter', label:'Starter Culture',  fill:'#d9c9a3', cap:'#6e4629' },
    { key:'annatto', label:'Annatto',          fill:'#a52a1c', cap:'#5a1a10' },
    { key:'rennet',  label:'Rennet',           fill:'#e8c23a', cap:'#8a6b1a' }
  ];

  let hovered = null;
  const canvases = new Map();   /* key -> {cv, g} */

  function drawVial(g, v, isHover){
    g.clearRect(0,0,VW,VH);
    g.save();
    /* corpo di vetro */
    g.fillStyle = 'rgba(255,255,255,.12)';
    g.strokeStyle = isHover ? 'rgba(139,0,0,.85)' : 'rgba(90,70,45,.55)';
    g.lineWidth = isHover ? 2.4 : 1.4;
    g.beginPath();
    g.moveTo(VW*.36, 6);
    g.lineTo(VW*.36, VH*.32);
    g.quadraticCurveTo(VW*.05, VH*.5, VW*.12, VH*.86);
    g.quadraticCurveTo(VW*.16, VH-4, VW*.5, VH-4);
    g.quadraticCurveTo(VW*.84, VH-4, VW*.88, VH*.86);
    g.quadraticCurveTo(VW*.95, VH*.5, VW*.64, VH*.32);
    g.lineTo(VW*.64, 6);
    g.closePath();
    g.fill(); g.stroke();

    /* contenuto */
    g.fillStyle = v.fill;
    g.save();
    g.beginPath();
    g.moveTo(VW*.36, VH*.34);
    g.quadraticCurveTo(VW*.08, VH*.52, VW*.14, VH*.85);
    g.quadraticCurveTo(VW*.18, VH-6, VW*.5, VH-6);
    g.quadraticCurveTo(VW*.82, VH-6, VW*.86, VH*.85);
    g.quadraticCurveTo(VW*.92, VH*.52, VW*.64, VH*.34);
    g.closePath();
    g.clip();
    g.fillRect(0, VH*.4, VW, VH*.6);
    g.restore();

    /* granulosità per starter/salt (texture) */
    if(v.key === 'salt' || v.key === 'starter'){
      g.fillStyle = 'rgba(80,60,30,.18)';
      for(let i=0;i<14;i++){
        g.fillRect(VW*.2+Math.random()*VW*.6, VH*.5+Math.random()*VH*.4, 1.4, 1.4);
      }
    }

    /* tappo */
    g.fillStyle = v.cap;
    g.fillRect(VW*.32, 0, VW*.36, 8);

    g.restore();

    /* etichetta piccola sul vetro */
    g.fillStyle = 'rgba(61,35,20,.5)';
    g.font = 'italic 8px Georgia';
    g.textAlign = 'center';
    g.fillText(v.key[0].toUpperCase(), VW/2, VH*.7);
  }

  function redraw(){
    for(const v of VIALS){
      const c = canvases.get(v.key);
      if(c) drawVial(c.g, v, hovered === v.key);
    }
  }

  Book.register({
    spread: 4,
    side: 'left',
    build(root){
      root.classList.add('vials-page');
      root.innerHTML = `
        <h3>The Four Vials</h3>
        <div class="vials-row"></div>
        <p class="note">Four vials. One order.<br>The poem knows.</p>`;
      const row = root.querySelector('.vials-row');
      canvases.clear();
      for(const v of VIALS){
        const cell = document.createElement('div');
        cell.className = 'vial-cell';
        cell.dataset.key = v.key;
        const cv = document.createElement('canvas');
        cv.width = VW; cv.height = VH;
        cv.className = 'vial-cv';
        const label = document.createElement('span');
        label.className = 'vial-label';
        label.textContent = v.label;
        cell.appendChild(cv);
        cell.appendChild(label);
        row.appendChild(cell);
        canvases.set(v.key, { cv, g: cv.getContext('2d') });
      }
      redraw();
    },
    init(root){
      const row = root.querySelector('.vials-row');
      if(!row) return;
      row.addEventListener('pointerenter', e => {
        const cell = e.target.closest('.vial-cell');
        if(!cell) return;
        hovered = cell.dataset.key;
        redraw();
      }, true);
      row.addEventListener('pointerleave', e => {
        const cell = e.target.closest('.vial-cell');
        if(!cell) return;
        if(hovered === cell.dataset.key){ hovered = null; redraw(); }
      }, true);
    },
    destroy(){
      hovered = null;
      canvases.clear();
    }
  });
})();