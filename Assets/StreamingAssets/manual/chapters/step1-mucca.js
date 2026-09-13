/* ============ CAP. II — TORN SECRETS (puzzle libero) ============
   - drag con listener SUL PEZZO (pointer capture): niente listener globali
   - posizioni salvate: girando pagina il puzzle non si rimescola più
   - z-order crescente: l'ultimo pezzo toccato resta sopra
   - snap a catena: un pezzo può chiudere più lati in un colpo solo
   ============================================================ */
(() => {
  'use strict';

  /* ---- mucca dipinta a canvas → dataURL locale ---- */
  const IMG=(()=>{
    const W=258,H=192;
    const c=document.createElement('canvas'); c.width=W; c.height=H;
    const g=c.getContext('2d');
    g.fillStyle='#efe6d2'; g.fillRect(0,0,W,H);
    g.fillStyle='#f6f3ec'; g.strokeStyle='#2b1f1a'; g.lineWidth=3;
    g.beginPath(); g.ellipse(120,105,72,44,0,0,7); g.fill(); g.stroke();
    for(const x of [70,100,140,170]){
      g.fillRect(x,138,14,38); g.strokeRect(x,138,14,38);
      g.fillStyle='#2b1f1a'; g.fillRect(x,168,14,10); g.fillStyle='#f6f3ec';
    }
    g.beginPath(); g.ellipse(205,72,30,24,.2,0,7); g.fill(); g.stroke();
    g.fillStyle='#d8b09a';
    g.beginPath(); g.ellipse(222,82,14,10,.2,0,7); g.fill(); g.stroke();
    g.fillStyle='#8b2020';
    g.beginPath(); g.moveTo(190,52); g.quadraticCurveTo(184,38,194,34);
    g.quadraticCurveTo(196,46,199,50); g.fill();
    g.beginPath(); g.moveTo(214,48); g.quadraticCurveTo(216,34,226,36);
    g.quadraticCurveTo(220,44,219,52); g.fill();
    g.fillStyle='#2b1f1a'; g.beginPath(); g.arc(208,68,3,0,7); g.fill();
    g.fillStyle='#f6f3ec'; g.beginPath(); g.ellipse(184,58,10,6,-.6,0,7); g.fill(); g.stroke();
    g.fillStyle='#2b1f1a';
    g.beginPath(); g.ellipse(90,92,18,13,.4,0,7); g.fill();
    g.beginPath(); g.ellipse(150,120,15,10,-.3,0,7); g.fill();
    g.fillStyle='#8b2020';
    g.save(); g.translate(125,86); g.scale(.8,.8);
    g.beginPath(); g.moveTo(0,6);
    g.bezierCurveTo(-12,-6,-4,-16,0,-8); g.bezierCurveTo(4,-16,12,-6,0,6);
    g.fill(); g.restore();
    g.strokeStyle='#8a8a8a'; g.lineWidth=4;
    g.beginPath(); g.moveTo(50,90); g.quadraticCurveTo(30,110,38,140); g.stroke();
    return c.toDataURL('image/png');
  })();

  const COLS=3, ROWS=3, PW=86, PH=64;
  const EATEN=[4];
  const TOL=12;

  /* posizioni salvate tra un giro di pagina e l'altro:
     { "c,r": {left, top, rot, z} } */
  const saved={};

  /* z-order globale: sopravvive ai giri di pagina, così l'ordine di
     sovrapposizione non si rimescola tornando sullo spread */
  let zTop=1;

  if(!document.getElementById('torn-svg')){
    const svg=document.createElementNS('http://www.w3.org/2000/svg','svg');
    svg.id='torn-svg'; svg.setAttribute('width','0'); svg.setAttribute('height','0');
    svg.style.position='absolute';
    svg.innerHTML=`<filter id="torn-edges">
      <feTurbulence type="fractalNoise" baseFrequency="0.055" numOctaves="3" result="n"/>
      <feDisplacementMap in="SourceGraphic" in2="n" scale="7"
        xChannelSelector="R" yChannelSelector="G"/></filter>`;
    document.body.appendChild(svg);
  }

  Book.register({
    spread: 2,
    side: 'right',
    build(root){
      root.classList.add('puzzle-page');
            root.innerHTML=`
        <h3>Step I — The Torn Portrait</h3>
        <p class="note">Rebuild her. Then describe her.</p>
        <div class="puzzle-field"></div>`;
      const field=root.querySelector('.puzzle-field');
      const cells=[];
      for(let cy=0;cy<3;cy++)for(let cx=0;cx<3;cx++)cells.push([cx,cy]);
      cells.sort(()=>Math.random()-.5);
      let ci=0;
      for(let r=0;r<ROWS;r++)for(let c=0;c<COLS;c++){
        const i=r*COLS+c;
        if(EATEN.includes(i))continue;
        const p=document.createElement('div');
        p.className='ppiece';
        p.dataset.c=c; p.dataset.r=r;
        p.style.backgroundImage=`url(${IMG})`;
        p.style.backgroundSize=`${PW*COLS}px ${PH*ROWS}px`;
        p.style.backgroundPosition=`${-c*PW}px ${-r*PH}px`;
        const key=c+','+r, mem=saved[key];
        if(mem){
          /* riprendi da dove era rimasto, z compreso */
          p.style.left=mem.left; p.style.top=mem.top;
          p.style.setProperty('--rot',mem.rot);
          if(mem.z) p.style.zIndex=mem.z;
        }else{
          const [gx,gy]=cells[ci++];
          p.style.left=(4+gx*31+Math.random()*6)+'%';
          p.style.top =(6+gy*30+Math.random()*6)+'%';
          p.style.setProperty('--rot',(Math.random()*24-12).toFixed(1)+'deg');
        }
        field.appendChild(p);
      }
    },

    init(root){
      const field=root.querySelector('.puzzle-field');
      if(!field)return;
      const pieces=[...field.querySelectorAll('.ppiece')];
      if(!pieces.length)return;

      let groups=pieces.map(p=>[p]);
      const groupOf=p=>groups.find(g=>g.includes(p));

      function remember(p){
      const fw=field.clientWidth||1, fh=field.clientHeight||1;
        saved[p.dataset.c+','+p.dataset.r]={
          left : (p.offsetLeft/fw*100).toFixed(3)+'%',
          top  : (p.offsetTop /fh*100).toFixed(3)+'%',
          rot  : p.style.getPropertyValue('--rot')||'0deg',
          z    : p.style.zIndex||''
        };
      }

      /* prova UN aggancio; restituisce il gruppo fuso, o null se nessuno */
      function snapOnce(grp){
        for(const a of grp){
          for(const b of pieces){
            const gb=groupOf(b);
            if(!gb||gb===grp)continue;
            const ex=(+b.dataset.c-+a.dataset.c)*PW;
            const ey=(+b.dataset.r-+a.dataset.r)*PH;
            if(Math.abs((b.offsetLeft-a.offsetLeft)-ex)<TOL &&
               Math.abs((b.offsetTop -a.offsetTop )-ey)<TOL){
              const fx=(b.offsetLeft-ex)-a.offsetLeft;
              const fy=(b.offsetTop -ey)-a.offsetTop;
              for(const q of grp){
                q.style.left=(q.offsetLeft+fx)+'px';
                q.style.top =(q.offsetTop +fy)+'px';
                q.style.setProperty('--rot','0deg');
                remember(q);
              }
              gb.forEach(q=>{ q.style.setProperty('--rot','0deg'); remember(q); });
              const merged=[...grp,...gb];
              groups=groups.filter(g=>g!==grp&&g!==gb);
              groups.push(merged);
              if(window.Sfx&&Sfx.wood)Sfx.wood();
              return merged;
            }
          }
        }
        return null;
      }

      /* ripete finché ci sono lati da chiudere */
      function trySnap(grp){
        let g=grp, next, guard=0;
        while((next=snapOnce(g)) && guard++ < 12) g=next;
        if(groups.length===1 && groups[0].length===pieces.length){
          const note=root.querySelector('.note');
          if(note)note.innerHTML=
	'<b style="color:#8b0000">Now describe her!</b><br>'+
            'Mind every red detail.';
        if(window.State && State.completeStep) State.completeStep(1);
        document.dispatchEvent(new CustomEvent('puzzle:done'));
        }
      }

      /* drag: listener direttamente sul pezzo (pointer capture) */
      for(const p of pieces){
        let grab=null;

        p.addEventListener('pointerdown',e=>{
          if(e.button!==0)return;
          e.preventDefault(); e.stopPropagation();
          const grp=groupOf(p);
          if(!grp)return;
          try{ p.setPointerCapture(e.pointerId); }catch(_){}
          zTop++;                                   /* il gruppo afferrato va sopra tutti */
          grp.forEach(q=>{ q.style.zIndex=zTop; });
          grab={grp, x0:e.clientX, y0:e.clientY,
            starts:grp.map(q=>({q, l:q.offsetLeft, t:q.offsetTop}))};
          grp.forEach(q=>q.classList.add('held'));
        });

        p.addEventListener('pointermove',e=>{
          if(!grab)return;
          e.preventDefault(); e.stopPropagation();
           const fr=field.getBoundingClientRect();
          const fw=fr.width||1, fh=fr.height||1;
          const dx=e.clientX-grab.x0, dy=e.clientY-grab.y0;
          for(const s of grab.starts){
            const nx=Math.max(-PW*.4,Math.min(fw-PW*.6,s.l+dx));
            const ny=Math.max(-PH*.4,Math.min(fh-PH*.6,s.t+dy));
           s.q.style.left=nx+'px';
            s.q.style.top =ny+'px';
          }
        });

        const up=e=>{
          if(!grab)return;
          const grp=grab.grp;
          grab=null;
          grp.forEach(q=>{ q.classList.remove('held'); remember(q); });
          try{ if(e && e.pointerId!=null) p.releasePointerCapture(e.pointerId); }catch(_){}
          trySnap(grp);
        };
        p.addEventListener('pointerup',up);
        p.addEventListener('pointercancel',up);
        p.addEventListener('lostpointercapture',up);   /* rilascio fuori finestra */
      }
    }
  });
})();