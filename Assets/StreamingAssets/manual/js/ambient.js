/* ============================================================
   AMBIENT — vita ambientale della hero.
     B1  Ink.firstVisit(spread)   memoria dello svelamento
     B4  schizzi d'inchiostro al cursore veloce
     D1  topo passante sul bordo basso
     D2  orme sulla carta dopo il passaggio
     D4  topo osservatore che segue il cursore con gli occhi
     D6  briciole sul tavolo che scappano dal cursore
   Tutte le animazioni girano su RAF con fps esplicito, si
   fermano quando il ripostiglio è aperto e sono disattivate
   sotto prefers-reduced-motion (R14).
   Deve essere caricato PRIMA dei capitoli: usano Ink.firstVisit.
   ============================================================ */
(() => {
  'use strict';

  /* ============ B1 — memoria delle pagine viste (per sessione) ============ */
  const KEY = 'ledger-visited';
  let seen;
  try{ seen = new Set(JSON.parse(sessionStorage.getItem(KEY) || '[]')); }
  catch(_){ seen = new Set(); }

  window.Ink = {
    firstVisit(n){
      if(seen.has(n)) return false;
      seen.add(n);
      try{ sessionStorage.setItem(KEY, JSON.stringify([...seen])); }catch(_){}
      return true;
    }
  };

  const stage = document.getElementById('sbStage');
  const tilt  = document.getElementById('sbTilt');
  const world = document.getElementById('worldRipostiglio');
  const reduce = matchMedia('(prefers-reduced-motion: reduce)').matches;
  const coarse = matchMedia('(pointer: coarse)').matches;
  if(!stage || !tilt) return;

  /* asleep: true mentre il ripostiglio è aperto. Le scene della hero
     non devono consumare frame per pixel che nessuno guarda. */
  const asleep = () => !!(world && !world.hidden);

  /* ============ B4 — schizzi d'inchiostro al cursore veloce ============ */
  if(!reduce && !coarse){
    const layer = document.createElement('div');
    layer.className = 'ink-spatter';
    layer.setAttribute('aria-hidden','true');
    tilt.appendChild(layer);

    const MAX = 12, THRESH = 1400, THROTTLE = 120;
    let alive = 0, lastSpawn = 0, lastMove = 0, px = null, py = null;

    stage.addEventListener('pointermove', e => {
      const now = performance.now();
      if(px === null){ px = e.clientX; py = e.clientY; lastMove = now; return; }
      const dt = now - lastMove;
      if(dt < 16) return;
      const v = Math.hypot(e.clientX - px, e.clientY - py) / (dt/1000);
      px = e.clientX; py = e.clientY; lastMove = now;

      if(v < THRESH || alive >= MAX) return;
      if(now - lastSpawn < THROTTLE) return;
      lastSpawn = now;

      const r = layer.getBoundingClientRect();
      if(!r.width) return;
      const nx = (e.clientX - r.left) / r.width;
      const ny = (e.clientY - r.top ) / r.height;
      if(nx < .051 || nx > .949 || ny < .218 || ny > .782) return;

      const n = 2 + (Math.random() < .5 ? 1 : 0);
      for(let i=0;i<n;i++){
        const d = document.createElement('i');
        d.className = 'ink-dot';
        d.style.left = (nx*100 + (Math.random()-.5)*1.6).toFixed(2) + '%';
        d.style.top  = (ny*100 + (Math.random()-.5)*1.6).toFixed(2) + '%';
        d.style.setProperty('--s',(1.4 + Math.random()*1.4).toFixed(1)+'px');
        layer.appendChild(d);
        alive++;
        setTimeout(() => { d.remove(); alive--; }, 600);
      }
    }, {passive:true});
  }

  if(reduce || !window.RAF) return;    /* da qui in giù è tutta animazione */

  /* ============ pittore condiviso: il topo ============
     Stesso disegno della tavola dello Step II, ma parametrico su
     verso e scala. Un solo topo canonico in tutto il gioco. */
  function mouseArt(g, s, flip, blink, pupil){
    g.save(); g.scale(flip ? -s : s, s);
    /* coda */
    g.strokeStyle = '#8b2020'; g.lineWidth = 1.6;
    g.beginPath(); g.moveTo(-13,6); g.quadraticCurveTo(-24,4,-21,-4); g.stroke();
    /* corpo */
    g.fillStyle = '#8b2020';
    g.beginPath(); g.ellipse(0,3,13,10,0,0,7); g.fill();
    /* orecchie */
    g.beginPath(); g.arc(8,-6,5,0,7); g.arc(-2,-8,4.5,0,7); g.fill();
    g.fillStyle = 'rgba(255,235,235,.55)';
    g.beginPath(); g.arc(8,-6,2.6,0,7); g.fill();
    /* muso */
    g.fillStyle = '#8b2020';
    g.beginPath(); g.ellipse(12,4,7,6,.2,0,7); g.fill();
    g.fillStyle = '#2b1f1a';
    g.beginPath(); g.arc(18,4,1.8,0,7); g.fill();          /* naso */
    /* occhio: sclera + pupilla mobile, oppure palpebra chiusa */
    if(blink){
      g.strokeStyle = '#2b1f1a'; g.lineWidth = 1.4;
      g.beginPath(); g.moveTo(9.6,1); g.lineTo(14,1); g.stroke();
    }else{
      g.fillStyle = '#f6f3ec';
      g.beginPath(); g.arc(11.8,1,2.6,0,7); g.fill();
      g.fillStyle = '#2b1f1a';
      g.beginPath();
      g.arc(11.8 + (pupil ? pupil.x : 0), 1 + (pupil ? pupil.y : 0), 1.3, 0, 7);
      g.fill();
    }
    /* baffi */
    g.strokeStyle = 'rgba(40,30,25,.5)'; g.lineWidth = .8;
    for(const dy of [-1.5,.5,2.5]){
      g.beginPath(); g.moveTo(16,3+dy*.4); g.lineTo(25,dy); g.stroke();
    }
    g.restore();
  }

  /* ============ D1 — il topo passante ============ */
  const runCv = document.createElement('canvas');
  runCv.className = 'amb-runner';
  runCv.setAttribute('aria-hidden','true');
  document.body.appendChild(runCv);
  const rg = runCv.getContext('2d');
  const RUN_H = 90;
  function fitRunner(){ runCv.width = innerWidth; runCv.height = RUN_H; }
  fitRunner();
  addEventListener('resize', fitRunner, {passive:true});

  /* stato: null = nessun topo in scena */
  let runner = null, nextRun = performance.now() + 8000 + Math.random()*20000;

  function spawnRunner(){
    const dir = Math.random() < .5 ? 1 : -1;
    runner = {
      dir,
      x: dir > 0 ? -60 : innerWidth + 60,
      t: 0,
      paused: 0,
      didPause: false,
      dropped: false
    };
  }

  /* ============ D2 — orme sulla carta ============ */
  const pawCv = document.createElement('canvas');
  pawCv.className = 'amb-paws';
  pawCv.setAttribute('aria-hidden','true');
  tilt.appendChild(pawCv);
  const pg = pawCv.getContext('2d');
  let paws = [], turning = false;

  document.addEventListener('book:turn',   () => { turning = true;  paws.length = 0; });
  document.addEventListener('book:settle', () => { turning = false; });

  function dropPaws(){
    const r = pawCv.getBoundingClientRect();
    if(!r.width) return;
    pawCv.width  = Math.round(r.width);
    pawCv.height = Math.round(r.height);
    const x0 = pawCv.width*.10, y0 = pawCv.height*(.30 + Math.random()*.36);
    const step = pawCv.width*.055, dir = Math.random() < .5 ? 1 : -1;
    const sx = dir > 0 ? x0 : pawCv.width - x0;
    paws = [];
    const n = 4 + (Math.random() < .5 ? 1 : 0);
    for(let i=0;i<n;i++) paws.push({
      x: sx + dir*i*step,
      y: y0 + (i%2 ? 7 : -7) + (Math.random()-.5)*4,
      a: dir > 0 ? .25 : -.25 + Math.PI,
      born: performance.now() + i*130
    });
  }

  function drawPaws(now){
    if(!paws.length) return;
    if(turning){ paws.length = 0; pg.clearRect(0,0,pawCv.width,pawCv.height); return; }
    pg.clearRect(0,0,pawCv.width,pawCv.height);
    let live = false;
    for(const p of paws){
      const age = (now - p.born) / 8000;
      if(age < 0){ live = true; continue; }
      if(age >= 1) continue;
      live = true;
      pg.save();
      pg.globalAlpha = .12 * Math.min(1, age*8) * (1 - age);
      pg.translate(p.x, p.y); pg.rotate(p.a);
      pg.fillStyle = '#3d2314';
      pg.beginPath(); pg.ellipse(0,0,3.4,2.4,0,0,7); pg.fill();
      for(const [dx,dy] of [[-3,-2.6],[-1,-3.4],[1.4,-3],[3.2,-1.6]]){
        pg.beginPath(); pg.arc(dx,dy,1.1,0,7); pg.fill();
      }
      pg.restore();
    }
    if(!live) paws.length = 0;
  }

  /* ============ D4 — il topo osservatore ============ */
  const watchCv = document.createElement('canvas');
  watchCv.className = 'amb-watcher';
  watchCv.setAttribute('aria-hidden','true');
  watchCv.width = 90; watchCv.height = 90;
  document.body.appendChild(watchCv);
  const wg = watchCv.getContext('2d');
  let mx = innerWidth/2, my = innerHeight/2;
  let blinkUntil = 0, nextBlink = performance.now() + 12000;

  addEventListener('pointermove', e => { mx = e.clientX; my = e.clientY; }, {passive:true});

  function drawWatcher(now){
    const r = watchCv.getBoundingClientRect();
    wg.clearRect(0,0,90,90);
    if(!r.width) return;
    const cx = r.left + r.width/2, cy = r.top + r.height/2;
    const a = Math.atan2(my-cy, mx-cx);
    const d = Math.min(1, Math.hypot(mx-cx, my-cy)/300);
    const pupil = { x: Math.cos(a)*2*d, y: Math.sin(a)*2*d };
    if(now > nextBlink){ blinkUntil = now + 150; nextBlink = now + 14000 + Math.random()*12000; }
    wg.save(); wg.translate(45,52);
    mouseArt(wg, 1.5, mx < cx, now < blinkUntil, pupil);
    wg.restore();
  }

  /* ============ D6 — briciole sul tavolo ============ */
  const crumbCv = document.createElement('canvas');
  crumbCv.className = 'amb-crumbs';
  crumbCv.setAttribute('aria-hidden','true');
  document.body.appendChild(crumbCv);
  const cg = crumbCv.getContext('2d');
  let crumbs = [];

  function seedCrumbs(){
    crumbCv.width = innerWidth; crumbCv.height = innerHeight;
    const bookEl = document.getElementById('sbBook');
    const b = bookEl ? bookEl.getBoundingClientRect() : null;
    crumbs = [];
    const n = 8 + Math.floor(Math.random()*3);
    for(let i=0;i<n;i++){
      let x, y, guard = 0;
      do{
        x = innerWidth *(.08 + Math.random()*.84);
        y = innerHeight*(.55 + Math.random()*.38);
        guard++;
      }while(b && guard < 20 &&
             x > b.left-20 && x < b.right+20 && y > b.top-20 && y < b.bottom+20);
      crumbs.push({
        x, y, hx:x, hy:y,               /* hx/hy = casa: ci tornano piano */
        r: 1.4 + Math.random()*2.2,
        rot: Math.random()*6.28,
        tone: Math.random() < .5 ? '#d9c88f' : '#c8b078'
      });
    }
  }
  seedCrumbs();
  addEventListener('resize', seedCrumbs, {passive:true});

  function drawCrumbs(){
    cg.clearRect(0,0,crumbCv.width,crumbCv.height);
    for(const c of crumbs){
      const dx = c.x-mx, dy = c.y-my, d = Math.hypot(dx,dy);
      if(d < 24 && d > .01){
        const push = (24-d)*.18;
        c.x += dx/d*push; c.y += dy/d*push;
      }else{
        c.x += (c.hx-c.x)*.02; c.y += (c.hy-c.y)*.02;
      }
      cg.save(); cg.translate(c.x,c.y); cg.rotate(c.rot);
      cg.globalAlpha = .5;
      cg.fillStyle = c.tone;
      cg.fillRect(-c.r, -c.r*.5, c.r*2, c.r);
      cg.restore();
    }
    cg.globalAlpha = 1;
  }

  /* ============ un solo loop per tutte le scene, 30 fps ============ */
  /* ============ G1 — il libro si addormenta ============
     Dopo due minuti di inattività il ledger si richiude da solo sulla
     copertina, con un filo di polvere che sale. Al primo movimento
     riapre esattamente dove eri: il segnalibro (A5) fa il resto. */
  const IDLE_MS = 120000;
  let lastAct = performance.now();
  let dozing = false, wokenPage = null;

  const zzzCv = document.createElement('canvas');
  zzzCv.className = 'amb-zzz';
  zzzCv.setAttribute('aria-hidden','true');
  zzzCv.width = 160; zzzCv.height = 200;
  tilt.appendChild(zzzCv);
  const zg = zzzCv.getContext('2d');
  let puffs = [], nextPuff = 0;

  function poke(){
    lastAct = performance.now();
    if(!dozing) return;
    dozing = false;
    puffs.length = 0;
    zg.clearRect(0,0,160,200);
    zzzCv.classList.remove('on');
    if(wokenPage != null && window.BookNav && BookNav.goTo){
      BookNav.goTo(wokenPage);
    }
    wokenPage = null;
  }
  for(const ev of ['pointerdown','pointermove','keydown','wheel','touchstart'])
    addEventListener(ev, poke, {passive:true});

  function doze(now){
    if(dozing) return;
    if(now - lastAct < IDLE_MS) return;
    if(!window.BookNav || !BookNav.goTo) return;
    const i = BookNav.index;
    /* già su copertina o retro: sta già riposando, niente da fare */
    if(i === 0 || i === BookNav.length-1){ lastAct = now; return; }
    if(!BookNav.goTo(0, 600)){ lastAct = now - IDLE_MS + 2000; return; }    dozing = true;
    wokenPage = i;
    zzzCv.classList.add('on');
  }

  function drawZzz(now, dt){
    if(!dozing) return;
    if(now > nextPuff && puffs.length < 14){
      nextPuff = now + 900 + Math.random()*700;
      puffs.push({ x: 80 + (Math.random()-.5)*26, y: 186, t: 0,
                   r: 4 + Math.random()*3, sw: Math.random()*6.28 });
    }
    zg.clearRect(0,0,160,200);
    for(let i=puffs.length-1;i>=0;i--){
      const p = puffs[i];
      p.t += dt;
      if(p.t > 4.2){ puffs.splice(i,1); continue; }
      const k = p.t/4.2;
      const x = p.x + Math.sin(p.sw + p.t*1.2)*13*k;
      const y = p.y - k*168;
      zg.globalAlpha = .28 * Math.sin(k*Math.PI);
      zg.fillStyle = '#e6d3ba';
      zg.beginPath(); zg.arc(x, y, p.r*(1+k*1.9), 0, 7); zg.fill();
    }
    zg.globalAlpha = 1;
  }

  /* ============ un solo loop per tutte le scene, 30 fps ============ */
  RAF.add((now, dt) => {
    if(asleep()){
      rg.clearRect(0,0,runCv.width,RUN_H);
      lastAct = now;                 /* il ripostiglio è attività */
      return;
    }

    doze(now);
    drawZzz(now, dt);

    /* --- D1: topo passante --- */
    if(!runner && now > nextRun) spawnRunner();
    rg.clearRect(0,0,runCv.width,RUN_H);
    if(runner){
      const R = runner;
      if(R.paused > 0){
        R.paused -= dt*1000;
      }else{
        R.x += R.dir * 460 * dt;
        R.t += dt;
        const mid = innerWidth/2;
        if(!R.didPause && Math.abs(R.x - mid) < 40){
          R.didPause = true; R.paused = 400;
          if(!R.dropped && Math.random() < .4){ R.dropped = true; dropPaws(); }
        }
      }
      const bob = R.paused > 0 ? 0 : Math.abs(Math.sin(R.t*17))*2.4;
      rg.save();
      rg.translate(R.x, RUN_H - 22 - bob);
      /* durante la pausa alza il muso verso il libro */
      if(R.paused > 0) rg.rotate(R.dir > 0 ? -.28 : .28);
      mouseArt(rg, 1.4, R.dir < 0, false, R.paused > 0 ? {x:0,y:-1.4} : null);
      rg.restore();
      if(R.x < -80 || R.x > innerWidth + 80){
        runner = null;
        nextRun = now + 45000 + Math.random()*75000;
      }
    }

    drawPaws(now);
    drawWatcher(now);
    drawCrumbs();
  }, 30);

  /* ============ H3 — monitor di sviluppo (Ctrl+Shift+F) ============
     Non è gameplay: è lo strumento per capire se una feature costa
     frame. Mai attivo di default, mai visibile a un giocatore che
     non lo cerchi. Vive qui e non in raf.js: lo scheduler resta un
     modulo puro (R5). */
  let mon = null, monOff = null, fpsBuf = [];

  addEventListener('keydown', e => {
    if(!e.ctrlKey || !e.shiftKey) return;
    if(e.key !== 'F' && e.key !== 'f') return;
    e.preventDefault();
    mon ? closeMonitor() : openMonitor();
  });

  function openMonitor(){
    mon = document.createElement('pre');
    mon.className = 'dev-monitor';
    document.body.appendChild(mon);
    fpsBuf = [];
    let last = performance.now();
    monOff = RAF.add(now => {
      const dt = now - last; last = now;
      if(dt > 0){
        fpsBuf.push(1000/dt);
        if(fpsBuf.length > 30) fpsBuf.shift();
      }
      const fps = fpsBuf.reduce((a,b)=>a+b,0) / (fpsBuf.length||1);
      const mem = performance.memory
        ? (performance.memory.usedJSHeapSize/1048576).toFixed(1)+' MB'
        : 'n/d';
      mon.textContent =
        'fps   ' + fps.toFixed(1).padStart(6) + '\n' +
        'raf   ' + String(RAF.size).padStart(6) + '\n' +
        'heap  ' + mem.padStart(9) + '\n' +
        'page  ' + String(window.BookNav ? BookNav.index : '?').padStart(6) + '\n' +
        'room  ' + (asleep() ? 'closet' : 'hero').padStart(6);
    }, 6);
  }

  function closeMonitor(){
    if(monOff){ monOff(); monOff = null; }
    if(mon){ mon.remove(); mon = null; }
    fpsBuf = [];
  }
})();