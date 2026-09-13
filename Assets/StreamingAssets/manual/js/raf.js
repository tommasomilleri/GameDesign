/* ============================================================
   RAF — un solo loop per tutte le animazioni.
   RAF.add(fn, fps?) → funzione di rimozione.
   fn riceve (now, dt) con dt in secondi, già clampato.
   Deve essere il PRIMO script caricato.
   ============================================================ */
window.RAF = (() => {
  'use strict';
  const subs = new Set();
  let running = false, last = 0, id = 0;

  function frame(now){
    id = 0;
    const dt = Math.min(0.05, (now - last) / 1000 || 0.016);
    last = now;
    /* copia: un sub può rimuoversi durante l'iterazione */
    for(const s of Array.from(subs)){
      if(!subs.has(s)) continue;
      if(s.interval){
        s.acc += dt;
        if(s.acc < s.interval) continue;
        s.acc = 0;
      }
      try{ s.fn(now, dt); }
      catch(err){ console.error('[raf] sub rimosso dopo errore:', err); subs.delete(s); }
    }
    if(subs.size) id = requestAnimationFrame(frame);
    else running = false;
  }

  /* al ritorno da scheda nascosta il primo dt sarebbe enorme */
  document.addEventListener('visibilitychange', () => {
    if(!document.hidden) last = performance.now();
  });

  return {
    add(fn, fps){
      if(typeof fn !== 'function') throw new TypeError('RAF.add: serve una funzione');
      const s = { fn, interval: fps ? 1/fps : 0, acc: 0 };
      subs.add(s);
      if(!running){
        running = true; last = performance.now();
        id = requestAnimationFrame(frame);
      }
      return () => subs.delete(s);
    },
    get size(){ return subs.size; }
  };
})();