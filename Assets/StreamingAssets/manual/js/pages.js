/* ============================================================
   PAGES — texture spread 1760×1240 (window.SPREADS)
   Carta spessa da acquerello: grana, fibre, bordi impilati,
   cucitura nel solco. Spread 0 = copertina chiusa (destra),
   ultimo = retro chiuso (sinistra).
   ============================================================ */
window.SPREADS = (() => {
  const W=1760, H=1240;
  const X0=W*.051, X1=W*.949, Y0=H*.218, Y1=H*.782;
  const MID=W/2;
  /* ---- PRNG deterministico: stesso spread = stessa carta a ogni reload.
     Math.random() qui produrrebbe deckle e aloni diversi a ogni F5,
     e la carta smetterebbe di essere "quella" pagina. ---- */
  function mulberry32(a){
    return function(){
      a |= 0; a = a + 0x6D2B79F5 | 0;
      let t = Math.imul(a ^ a >>> 15, 1 | a);
      t = t + Math.imul(t ^ t >>> 7, 61 | t) ^ t;
      return ((t ^ t >>> 14) >>> 0) / 4294967296;
    };
  }
  /* indice dello spread in costruzione: l'array in fondo al file viene
     valutato in ordine, quindi basta un contatore incrementato da
     base()/cover(). Nessuna dipendenza dall'ordine di chiamata interno. */
  let SI = 0;
  function rr(g,x,y,w,h,r){ g.beginPath(); g.roundRect(x,y,w,h,r); }
let pending = 0;
  function art(c){
    const o = { url: c.toDataURL('image/png') };
    pending++;
    try{
      c.toBlob(b => {
        if(b) o.url = URL.createObjectURL(b);
        if(--pending === 0) document.dispatchEvent(new Event('spreads:ready'));
      }, 'image/png');
    }catch(_){
      if(--pending === 0) document.dispatchEvent(new Event('spreads:ready'));
    }
    return o;
 }
  function paper(g,x0,x1){
    const lg=g.createLinearGradient(x0,0,x1,0);
    if(x0<MID){ lg.addColorStop(0,'#ddd2bc'); lg.addColorStop(.8,'#efe6d2'); lg.addColorStop(1,'#d6c9b0'); }
    else{ lg.addColorStop(0,'#d6c9b0'); lg.addColorStop(.2,'#efe6d2'); lg.addColorStop(1,'#ddd2bc'); }
    g.fillStyle=lg; g.fillRect(x0,Y0,x1-x0,Y1-Y0);
    /* chiazze basse frequenze (carta cotone) */
    for(let i=0;i<9;i++){
      const x=x0+Math.random()*(x1-x0), y=Y0+Math.random()*(Y1-Y0), r=60+Math.random()*130;
      const st=g.createRadialGradient(x,y,0,x,y,r);
      st.addColorStop(0,'rgba(160,135,95,.05)'); st.addColorStop(1,'rgba(160,135,95,0)');
      g.fillStyle=st; g.beginPath(); g.arc(x,y,r,0,7); g.fill();
    }
    /* grana fine bicolore */
    g.save(); g.globalAlpha=.07;
    for(let i=0;i<2200;i++){
      const x=x0+Math.random()*(x1-x0), y=Y0+Math.random()*(Y1-Y0);
      g.fillStyle=Math.random()<.5?'#8a7050':'#fffdf4';
      g.fillRect(x,y,Math.random()*2+.5,Math.random()*2+.5);
    }
    /* fibre orizzontali corte */
    g.globalAlpha=.05; g.strokeStyle='#9a8262'; g.lineWidth=.7;
    for(let i=0;i<260;i++){
      const x=x0+Math.random()*(x1-x0), y=Y0+Math.random()*(Y1-Y0);
      g.beginPath(); g.moveTo(x,y); g.lineTo(x+6+Math.random()*14,y+(Math.random()-.5)*2); g.stroke();
    }
    g.restore();
  }
  /* ---- A1: bordo deckle. Ritaglia l'esterno della carta con un
     profilo irregolare. Va chiamata SUBITO dopo paper(), prima di
     stack()/gutter(): destination-out mangia solo la carta. ---- */
  function deckle(g,x0,x1,rnd){
    const N = 24, J = 2.5;
    g.save();
    g.globalCompositeOperation = 'destination-out';
    g.fillStyle = '#000';

    /* lato sinistro (solo se è un bordo esterno, non il dorso) */
    const edge = (X, dir) => {
      g.beginPath();
      g.moveTo(X, Y0 - 2);
      for(let i=0;i<=N;i++){
        const y = Y0 + (Y1-Y0)*(i/N);
        const j = (rnd()-.5)*2*J + Math.sin(i*1.7)*.8;
        g.lineTo(X + j, y);
      }
      g.lineTo(X + dir*40, Y1 + 2);
      g.lineTo(X + dir*40, Y0 - 2);
      g.closePath(); g.fill();
    };
    /* lati orizzontali */
    const edgeH = (Y, dir) => {
      g.beginPath();
      g.moveTo(x0 - 2, Y);
      for(let i=0;i<=N;i++){
        const x = x0 + (x1-x0)*(i/N);
        const j = (rnd()-.5)*2*J + Math.cos(i*1.3)*.8;
        g.lineTo(x, Y + j);
      }
      g.lineTo(x1 + 2, Y + dir*40);
      g.lineTo(x0 - 2, Y + dir*40);
      g.closePath(); g.fill();
    };

    if(x0 < MID) edge(X0, -1); else edge(X1, 1);
    edgeH(Y0, -1);
    edgeH(Y1,  1);
    g.restore();
  }
  /* ---- bordi impilati: le pagine sotto, come nello sketchbook ---- */
  function stack(g,side){ /* side: 'left'|'right'|'both' */
    const edges=(x,dir)=>{
      for(let i=0;i<5;i++){
        const off=(i+1)*3.2*dir;
        g.strokeStyle=i%2?'rgba(120,95,60,.35)':'rgba(255,250,235,.55)';
        g.lineWidth=1.4;
        g.beginPath(); g.moveTo(x+off,Y0+6+i*2); g.lineTo(x+off,Y1-6-i*2); g.stroke();
      }
    };
    if(side!=='right') edges(X0,-1);
    if(side!=='left')  edges(X1, 1);
    /* pila anche sotto */
    for(let i=0;i<4;i++){
      g.strokeStyle=i%2?'rgba(120,95,60,.3)':'rgba(255,250,235,.5)';
      g.lineWidth=1.4;
      const x0=side==='right'?MID:X0+8+i*3, x1=side==='left'?MID:X1-8-i*3;
      g.beginPath(); g.moveTo(x0,Y1+(i+1)*2.6); g.lineTo(x1,Y1+(i+1)*2.6); g.stroke();
    }
  }
  /* ---- A2: aloni d'umidità + gora di caffè. Seed per spread. ---- */
  function aging(g,x0,x1,rnd,coffee){
    const w = x1-x0, h = Y1-Y0;
    /* 2-4 aloni, con bias verso gli angoli */
    const n = 2 + Math.floor(rnd()*3);
    for(let i=0;i<n;i++){
      const cx = x0 + (rnd()<.5 ? rnd()*w*.28 : w - rnd()*w*.28);
      const cy = Y0 + (rnd()<.5 ? rnd()*h*.28 : h - rnd()*h*.28);
      const r  = 80 + rnd()*120;
      const st = g.createRadialGradient(cx,cy,r*.15,cx,cy,r);
      const a  = (.04 + rnd()*.04).toFixed(3);
      st.addColorStop(0,`rgba(122,90,48,${a})`);
      st.addColorStop(.7,`rgba(122,90,48,${(a*.45).toFixed(3)})`);
      st.addColorStop(1,'rgba(122,90,48,0)');
      g.fillStyle = st;
      g.beginPath(); g.ellipse(cx,cy,r,r*(.7+rnd()*.5),rnd()*3,0,7); g.fill();
    }
    if(!coffee) return;
    /* gora di caffè: interno lavato + anello irregolare */
    const cx = x0 + w*(.25+rnd()*.5), cy = Y0 + h*(.25+rnd()*.5);
    const R  = 46 + rnd()*34;
    g.save();
    g.fillStyle = 'rgba(110,74,36,.035)';
    g.beginPath();
    for(let i=0;i<=28;i++){
      const a = i/28*6.283, rr = R*(1+Math.sin(a*3+rnd()*.2)*.05);
      const x = cx+Math.cos(a)*rr, y = cy+Math.sin(a)*rr*.92;
      i ? g.lineTo(x,y) : g.moveTo(x,y);
    }
    g.closePath(); g.fill();
    g.strokeStyle = 'rgba(96,62,28,.10)'; g.lineWidth = 3.2;
    g.stroke();
    g.strokeStyle = 'rgba(96,62,28,.06)'; g.lineWidth = 7;
    g.stroke();
    g.restore();
  }
  /* ---- A4: numero di pagina con svolazzo, angolo esterno basso ---- */
  const ROMAN = ['','I','II','III','IV','V','VI','VII','VIII','IX','X'];
  function folio(g,n,side){
    if(n<1 || n>=ROMAN.length) return;
    const x = side==='left' ? X0+40 : X1-40;
    const y = Y1-24;
    g.save();
    g.fillStyle = 'rgba(61,35,20,.55)';
    g.font = 'italic 22px Georgia';
    g.textAlign = 'center';
    g.fillText(ROMAN[n], x, y);
    g.strokeStyle = 'rgba(139,90,43,.35)';
    g.lineWidth = 1.4; g.lineCap = 'round';
    g.beginPath();
    g.moveTo(x-16, y+7);
    g.quadraticCurveTo(x, y+13, x+16, y+6);
    g.stroke();
    g.restore();
  }
  /* ---- cucitura nel solco ---- */
  function stitching(g){
    g.strokeStyle='rgba(90,70,45,.75)'; g.lineWidth=2.6; g.lineCap='round';
    for(let y=Y0+40;y<Y1-30;y+=46){
      g.beginPath(); g.moveTo(MID-1,y); g.lineTo(MID+1,y+16); g.stroke();
      g.fillStyle='rgba(40,28,16,.5)';                       /* forellini */
      g.beginPath(); g.arc(MID-1,y,2,0,7); g.arc(MID+1,y+16,2,0,7); g.fill();
    }
  }

  /* ---- solco: ombra morbida a V ---- */
  function gutter(g){
    const gu=g.createLinearGradient(MID-90,0,MID+90,0);
    gu.addColorStop(0,'rgba(70,50,28,0)');  gu.addColorStop(.42,'rgba(70,50,28,.28)');
    gu.addColorStop(.5,'rgba(50,34,18,.45)');
    gu.addColorStop(.58,'rgba(70,50,28,.28)'); gu.addColorStop(1,'rgba(70,50,28,0)');
    g.fillStyle=gu; g.fillRect(MID-90,Y0,180,Y1-Y0);
  }
    function base(draw){
    const idx = SI++;                     /* indice dello spread */
    const rnd = mulberry32(idx*7919 + 13);
    const c=document.createElement('canvas'); c.width=W; c.height=H;
    const g=c.getContext('2d');

    paper(g,X0,MID);  deckle(g,X0,MID,rnd);
    paper(g,MID,X1);  deckle(g,MID,X1,rnd);
    aging(g,X0,MID,rnd, idx%3===0);
    aging(g,MID,X1,rnd, idx%3===2);
    stack(g,'both'); gutter(g); stitching(g);
    /* B5: la filigrana sta SOTTO il testo del capitolo, come una
       marca d'acqua vera: prima di draw(), non dopo. */
    watermark(g,(X0+MID)/2,(Y0+Y1)/2);
    watermark(g,(MID+X1)/2,(Y0+Y1)/2);
    draw(g,{X0,X1,Y0,Y1,MID});
    marginalia(g,idx);                    /* B3: le note di E. */
    folio(g, idx*2-1, 'left');            /* spread 1 → pag. I e II */
    folio(g, idx*2,   'right');
    return art(c);
  }
  /* ---- A6: borchia d'ottone con calotta e viti a croce ---- */
  function stud(g,x,y){
    g.save(); g.translate(x,y);
    g.fillStyle='#6b5326';
    g.beginPath(); g.arc(0,0,8,0,7); g.fill();
    const cap=g.createRadialGradient(-2.5,-3,.5,0,0,8);
    cap.addColorStop(0,'#f2dcaa'); cap.addColorStop(.45,'#e8c990');
    cap.addColorStop(1,'#8a6b2a');
    g.fillStyle=cap;
    g.beginPath(); g.arc(0,0,6.4,0,7); g.fill();
    g.strokeStyle='rgba(50,34,12,.55)'; g.lineWidth=1.2; g.lineCap='round';
    g.beginPath(); g.moveTo(-2.6,0); g.lineTo(2.6,0);
    g.moveTo(0,-2.6); g.lineTo(0,2.6); g.stroke();
    g.strokeStyle='rgba(30,18,8,.35)'; g.lineWidth=1;
    g.beginPath(); g.arc(0,0,8,0,7); g.stroke();
    g.restore();
  }
  function cover(side,draw){
    SI++;                                  /* la copertina consuma un indice */
    const c=document.createElement('canvas'); c.width=W; c.height=H;
    const g=c.getContext('2d');
    const x0=side==='right'?MID:X0, x1=side==='right'?X1:MID;
    /* pelle */
    const lg=g.createLinearGradient(x0,Y0,x1,Y1);
    lg.addColorStop(0,'#6e3f22'); lg.addColorStop(.5,'#5a3018'); lg.addColorStop(1,'#4a2712');
    g.fillStyle=lg; rr(g,x0,Y0-10,x1-x0,(Y1-Y0)+20,14); g.fill();
    /* grana pelle */
    g.save(); g.globalAlpha=.08;
    for(let i=0;i<1600;i++){
      g.fillStyle=Math.random()<.5?'#2e1a0c':'#a06b3a';
      g.fillRect(x0+Math.random()*(x1-x0),Y0-10+Math.random()*((Y1-Y0)+20),2,1.2);
    }
    g.restore();
    /* cornice a secco + dorso */
    g.strokeStyle='rgba(210,170,110,.5)'; g.lineWidth=3;
    rr(g,x0+26,Y0+18,(x1-x0)-52,(Y1-Y0)-36,8); g.stroke();
    g.fillStyle='rgba(30,18,8,.4)';
    g.fillRect(side==='right'?MID:MID-26,Y0-10,26,(Y1-Y0)+20);
    /* pagine che sporgono dal lato aperto */
    const px=side==='right'?X1:X0, dir=side==='right'?1:-1;
    for(let i=0;i<6;i++){
      g.strokeStyle=i%2?'rgba(235,225,200,.9)':'rgba(160,135,95,.7)';
      g.lineWidth=2;
      g.beginPath(); g.moveTo(px+dir*(4+i*2.6),Y0+14); g.lineTo(px+dir*(4+i*2.6),Y1-14); g.stroke();
    }
    /* A6: borchie ai quattro angoli della cornice, inset 14px */
    const fx0=x0+26+14, fx1=x0+26+((x1-x0)-52)-14;
    const fy0=Y0+18+14, fy1=Y0+18+((Y1-Y0)-36)-14;
    stud(g,fx0,fy0); stud(g,fx1,fy0); stud(g,fx0,fy1); stud(g,fx1,fy1);
    draw&&draw(g,{x0,x1,Y0,Y1});
    return art(c);
  }
       const ink='#3d2314', soft='#5c3a21';

  function title(g,t,x,y,size){
    g.fillStyle=ink; g.font=size+'px Georgia'; g.textAlign='center';
    g.fillText(t,x,y);
    g.strokeStyle='rgba(139,90,43,.4)'; g.lineWidth=2;
    g.beginPath(); g.moveTo(x-180,y+18); g.lineTo(x+180,y+18); g.stroke();
  }
  function lines(g,arr,x,y,lh,size,italic){
    g.fillStyle=soft; g.font=(italic?'italic ':'')+size+'px Georgia'; g.textAlign='center';
    arr.forEach((s,i)=>g.fillText(s,x,y+i*lh));
  }

  /* ---- B5: filigrana della Fromagerie (topo + ruota, line-art).
     Alpha .035: a occhio nudo è quasi nulla, ma con la candela
     accesa (Fase C) il contrasto locale la fa emergere. Nessun
     secondo layer DOM: una sola texture, due letture. ---- */
  function watermark(g,cx,cy){
    g.save();
    g.translate(cx,cy);
    g.globalAlpha=.035;
    g.strokeStyle='#3d2314'; g.lineWidth=2.2;
    g.lineJoin='round'; g.lineCap='round';
    /* ruota di formaggio */
    g.beginPath(); g.arc(0,18,58,0,7); g.stroke();
    g.beginPath(); g.moveTo(-58,18); g.lineTo(58,18); g.stroke();
    for(const [x,y,r] of [[-22,4,9],[16,30,7],[6,-6,5]]){
      g.beginPath(); g.arc(x,y,r,0,7); g.stroke();
    }
    /* topo di profilo, appoggiato sopra la ruota */
    g.beginPath(); g.ellipse(0,-46,34,22,0,0,7); g.stroke();
    g.beginPath(); g.arc(22,-60,11,0,7); g.stroke();      /* orecchio dx */
    g.beginPath(); g.arc(-4,-64,9,0,7);  g.stroke();      /* orecchio sx */
    g.beginPath(); g.moveTo(-32,-40);
    g.quadraticCurveTo(-58,-46,-54,-66); g.stroke();      /* coda */
    g.restore();
  }

  /* ---- B3: note a margine di "E.".
     Non sono decorazione: sono INDIZI SECONDARI veri, coerenti coi
     puzzle (corna della mucca = 2 pressate; il topo giallo mente;
     due pressate e basta). Chi legge i margini gioca meglio. ---- */
  const MARGINALIA = {
    2:[{ t:['her horns…','crimson, I am sure','of it — E.'], x:.855, y:.60, rot:-5   }],
    3:[{ t:['the yellow one lies.','— E.'],                  x:.855, y:.30, rot:-6.5 }],
    5:[{ t:['twice. TWICE.','— E.'],                         x:.855, y:.70, rot:-4   }]
  };
  function marginalia(g,idx){
    const set = MARGINALIA[idx];
    if(!set) return;
    for(const n of set){
      const x = X0 + (X1-X0)*n.x, y = Y0 + (Y1-Y0)*n.y;
      g.save();
      g.translate(x,y); g.rotate(n.rot*Math.PI/180);
      g.fillStyle='rgba(60,55,50,.55)';
      g.font="20px 'BiroScript', cursive";
      g.textAlign='left';
      n.t.forEach((s,i)=>g.fillText(s,0,i*23));
      g.restore();
    }
  }
  /* pagina SINISTRA con testo, pagina destra nuda (ospita il capitolo vivo) */
  function stepLeft(t,body){
    return base((g)=>{
      const lx=(X0+MID)/2;
      title(g,t,lx,Y0+130,40);
      lines(g,body,lx,Y0+235,46,26,true);
    });
  }

  return [
    /* 0 — COPERTINA */
    cover('right',(g,{x0,x1,Y0})=>{
      const cx=(x0+x1)/2;
      g.fillStyle='#e8c990'; g.font='72px Georgia'; g.textAlign='center';
      g.fillText('The Cheese Empire',cx,Y0+300);
      g.font='italic 30px Georgia';
      g.fillText('A Manual for the Scholar',cx,Y0+370);
      g.strokeStyle='rgba(232,201,144,.6)'; g.lineWidth=2;
      g.beginPath(); g.moveTo(cx-160,Y0+420); g.lineTo(cx+160,Y0+420); g.stroke();
    }),

    /* 1 — EX LIBRIS + INDICE */
    base((g)=>{
      const lx=(X0+MID)/2, rx=(MID+X1)/2;
      lines(g,['~ ex libris ~'],lx,Y0+300,0,30,true);
      lines(g,['"Property of the Grand Fromagerie.','Do not let the humans find this..."'],
             lx,Y0+380,40,24,true);
            /* le 5 voci NON sono più dipinte qui: le disegna chapters/indice.js
         come pulsanti vivi (spread 1, lato destro). Resta solo il titolo,
         così la carta sotto è pulita e i link ci cadono sopra allineati. */
      title(g,'The Five Steps',rx,Y0+140,38);
    }),

    /* 2 — STEP I (dx VIVO: puzzle mucca) */
    stepLeft('Step I — Find the Right Milk',
      ['The finest wheel begins with the','finest beast.','',
       'The mice tore her portrait to shreds —','rebuild it, then describe her to','your partner.','',
       'Beware: her sisters look almost','the same. Almost.']),

    /* 3 — STEP II (dx VIVO: tabella topi) */
    stepLeft('Step II — Heat the Milk',
      ['Too cold and the culture sleeps.','Too hot and you will kill it.','',
       'Watch the pot, and follow the','temperature marked by the mouse','that stirs.']),

    /* 4 — STEP III (entrambe VIVE: dosaggio sx + poema dx) */
    base(()=>{}),

    /* 5 — STEP IV (dx VIVO: pressa) */
    stepLeft('Step IV — Press the Cheese',
      ['Wrap the curds and press.','Then press again.','',
       'How many times?','The beast you rebuilt already','told you — count what crowns','her head.']),

    /* 6 — STEP V (dx VIVO: stagionatura) */
    stepLeft('Step V — Age the Cheese',
      ['Finally, follow the old mouse','as he rests.','',
       "Stay warm — don't get too cold.",'The shelf he sleeps on is the','shelf your wheel belongs to.']),

    /* 7 — RETRO */
    cover('left',(g,{x0,x1,Y0,Y1})=>{
      const cx=(x0+x1)/2;
      g.fillStyle='rgba(232,201,144,.5)'; g.font='italic 24px Georgia'; g.textAlign='center';
      g.fillText('finis',cx,Y1-80);
      /* 5 cerchi vuoti: il capitolo finale li riempirà di ceralacca */
      g.strokeStyle='rgba(232,201,144,.35)'; g.lineWidth=2;
      for(let i=0;i<5;i++){
        g.beginPath(); g.arc(cx-160+i*80, Y0+430, 26, 0, 7); g.stroke();
      }
    }),
  ];
})();