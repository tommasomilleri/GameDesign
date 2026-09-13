/* ============ STEP II — HEAT THE MILK (tavola dei topi) ============
   Tabella 3 fasi × 3 topi. Nessuna validazione: è una tavola di
   consultazione, il gameplay è la comunicazione a voce.
   Click su una cella = cerchio a matita rossa (toggle), persistente.
   ============================================================ */
(() => {
  'use strict';

  const CW = 420, CH = 300;                 /* risoluzione interna del canvas */
  const COL0 = 118, COLW = 96;              /* colonna etichette / colonne topi */
  const ROW0 = 116, ROWH = 58;

  const MICE = [
    { key:'red',    label:'RED',    body:'#8b2020', hat:'#f6f3ec' },
    { key:'yellow', label:'YELLOW', body:'#c8952b', hat:'#f6f3ec' },
    { key:'blue',   label:'BLUE',   body:'#4a5a80', hat:'#f6f3ec' }
  ];
  const PHASES = [
    { key:'still', label:'pot still'   },
    { key:'steam', label:'first steam' },
    { key:'curds', label:'soft curds'  }
  ];
  /* valori DEFINITIVI, concordati col termometro di Lvl2 */
  const T = {
    still : { red:68, yellow:72, blue:65 },
    steam : { red:74, yellow:70, blue:77 },
    curds : { red:62, yellow:66, blue:60 }
  };

  /* cerchi a matita: sopravvivono ai giri di pagina */
  const marks = new Set();                  /* "phaseKey|miceKey" */

  const cv = document.createElement('canvas');
  cv.className = 'mice-table';
  cv.width = CW; cv.height = CH;
  const g = cv.getContext('2d');

  /* ---- topo con cappello da cuoco e cucchiaio ---- */
  function mouse(g, x, y, m){
    g.save(); g.translate(x, y);
    /* coda */
    g.strokeStyle = m.body; g.lineWidth = 1.6;
    g.beginPath(); g.moveTo(-13,6); g.quadraticCurveTo(-22,4,-20,-3); g.stroke();
    /* corpo */
    g.fillStyle = m.body;
    g.beginPath(); g.ellipse(0,3,13,10,0,0,7); g.fill();
    /* orecchie */
    g.beginPath(); g.arc(8,-6,5,0,7); g.arc(-2,-8,4.5,0,7); g.fill();
    g.fillStyle = 'rgba(255,235,235,.55)';
    g.beginPath(); g.arc(8,-6,2.6,0,7); g.fill();
    /* muso */
    g.fillStyle = m.body;
    g.beginPath(); g.ellipse(12,4,7,6,.2,0,7); g.fill();
    g.fillStyle = '#2b1f1a';
    g.beginPath(); g.arc(18,4,1.8,0,7); g.fill();         /* naso  */
    g.beginPath(); g.arc(12,1,1.4,0,7); g.fill();         /* occhio */
    /* cappello da cuoco */
    g.fillStyle = m.hat;
    g.beginPath(); g.roundRect(-3,-19,15,6,2); g.fill();
    g.beginPath(); g.arc(-1,-21,4.5,0,7); g.arc(4,-23,5.5,0,7); g.arc(10,-21,4.5,0,7); g.fill();
    /* cucchiaio */
    g.strokeStyle = '#8a6b42'; g.lineWidth = 2.2;
    g.beginPath(); g.moveTo(-8,8); g.lineTo(-14,-6); g.stroke();
    g.fillStyle = '#8a6b42';
    g.beginPath(); g.ellipse(-15,-9,3.4,4.4,-.3,0,7); g.fill();
    g.restore();
  }

  /* ---- icone di fase, a sinistra ---- */
  function phaseIcon(g, x, y, key){
    g.save(); g.translate(x, y);
    /* pentola, comune a tutte */
    g.fillStyle = '#5c5c5c';
    g.beginPath(); g.moveTo(-11,-4); g.lineTo(11,-4); g.lineTo(8,8); g.lineTo(-8,8); g.closePath(); g.fill();
    g.strokeStyle = '#3d2314'; g.lineWidth = 1.4;
    g.beginPath(); g.moveTo(-13,-4); g.lineTo(13,-4); g.stroke();
    if(key === 'still'){
      g.strokeStyle = 'rgba(61,35,20,.45)'; g.lineWidth = 1.2;
      g.beginPath(); g.moveTo(-6,0); g.lineTo(6,0); g.stroke();
    }else if(key === 'steam'){
      g.strokeStyle = 'rgba(61,35,20,.5)'; g.lineWidth = 1.4;
      for(const dx of [-5,0,5]){
        g.beginPath();
        g.moveTo(dx,-8); g.quadraticCurveTo(dx+3,-13,dx,-18);
        g.stroke();
      }
    }else{
      g.fillStyle = '#efe6d2';
      for(const [cx,cy,r] of [[-4,1,3],[2,2,3.4],[6,0,2.4],[-1,4,2.6]]){
        g.beginPath(); g.arc(cx,cy,r,0,7); g.fill();
      }
    }
    g.restore();
  }

  /* ---- cerchio a matita rossa, tratto irregolare ---- */
  function pencilRing(g, cx, cy, rx, ry, seed){
    g.save();
    g.strokeStyle = 'rgba(139,0,0,.8)'; g.lineWidth = 1.6; g.lineJoin = 'round';
    for(let pass=0; pass<2; pass++){
      g.beginPath();
      for(let i=0;i<=26;i++){
        const a = i/26*6.283 + pass*.4;
        const w = 1 + Math.sin(a*3 + seed + pass)*.055;
        const x = cx + Math.cos(a)*rx*w;
        const y = cy + Math.sin(a)*ry*w;
        i ? g.lineTo(x,y) : g.moveTo(x,y);
      }
      g.stroke();
    }
    g.restore();
  }

  function draw(){
    g.clearRect(0,0,CW,CH);

    /* intestazioni dei topi */
    MICE.forEach((m,c)=>{
      const x = COL0 + c*COLW + COLW/2;
      mouse(g, x, 46, m);
      g.fillStyle = '#4a2e15'; g.font = 'bold 12px Georgia'; g.textAlign = 'center';
      g.fillText(m.label, x, 84);
    });

    /* righe */
    PHASES.forEach((p,r)=>{
      const y = ROW0 + r*ROWH;
      phaseIcon(g, 30, y + 2, p.key);
      g.fillStyle = '#5c3a21'; g.font = 'italic 13px Georgia'; g.textAlign = 'left';
      g.fillText(p.label, 50, y + 6);

      MICE.forEach((m,c)=>{
        const x = COL0 + c*COLW + COLW/2;
        /* pallino colore = zona del termometro */
        g.fillStyle = m.body; g.globalAlpha = .5;
        g.beginPath(); g.arc(x - 24, y, 5, 0, 7); g.fill();
        g.globalAlpha = 1;
        g.fillStyle = '#3d2314'; g.font = '17px Georgia'; g.textAlign = 'left';
        g.fillText(T[p.key][m.key] + '°', x - 12, y + 6);
        if(marks.has(p.key + '|' + m.key)) pencilRing(g, x, y, 34, 17, r*3 + c);
      });
    });

    /* righelli */
    g.strokeStyle = 'rgba(139,90,43,.35)'; g.lineWidth = 1;
    g.beginPath();
    g.moveTo(14, 94); g.lineTo(CW-14, 94);
    for(let r=0;r<PHASES.length;r++){
      const y = ROW0 + r*ROWH + ROWH/2 - 4;
      g.moveTo(14, y); g.lineTo(CW-14, y);
    }
    g.stroke();
  }

  function hit(e){
    const r = cv.getBoundingClientRect();
    if(!r.width) return null;
    const x = (e.clientX - r.left) * (CW / r.width);
    const y = (e.clientY - r.top ) * (CH / r.height);
    const c = Math.floor((x - COL0) / COLW);
    if(c < 0 || c >= MICE.length) return null;
    for(let i=0;i<PHASES.length;i++){
      const cy = ROW0 + i*ROWH;
      if(Math.abs(y - cy) <= ROWH/2 - 4) return PHASES[i].key + '|' + MICE[c].key;
    }
    return null;
  }

  let wired = false;
  function wire(){
    if(wired) return;
    wired = true;
    cv.addEventListener('pointerdown', e => {
      if(e.button !== 0) return;
      const k = hit(e);
      if(!k) return;
      e.preventDefault(); e.stopPropagation();
      marks.has(k) ? marks.delete(k) : marks.add(k);
      if(window.Sfx && Sfx.scrub){ Sfx.scrub('pencil'); setTimeout(()=>Sfx.stopScrub(), 90); }
      draw();
    });
  }

  Book.register({
    spread: 3,
    side: 'right',
    build(root){
      root.classList.add('mice-page');
      root.innerHTML = `
        <h3>The Stirring Table</h3>
        <p class="note">Each mouse marks a different heat.<br>
           Tell your partner which one to follow.</p>
        <div class="mice-wrap"></div>`;
      root.querySelector('.mice-wrap').appendChild(cv);
      draw();
    },
    init(){ wire(); }
  });
})();