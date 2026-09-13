/* ============================================================
   LIVE — registro capitoli + overlay interattivo a libro fermo.
   API capitoli:
   Book.register({ spread:3, side:'right', build(root), init(root), destroy?() })
   - spread  : indice dello spread in SPREADS
   - side    : 'left' | 'right'
   - build   : riempie root (usa SOLO classi, mai id)
   - init    : opzionale, aggancia la logica dopo il build
   - destroy : opzionale, chiamato PRIMA che il DOM venga distrutto
   ============================================================ */
window.Book = {
  chapters: [],
  register(ch){
    if(!ch || typeof ch.build!=='function' || typeof ch.spread!=='number'){
      console.error('[live] capitolo non valido:', ch); return;
    }
    this.chapters.push(ch);
  }
};

(() => {
  'use strict';
  const layer = document.getElementById('liveLayer');
  if(!layer){ console.error('[live] liveLayer mancante'); return; }

  let mounted = [];          /* capitoli attualmente montati */
  let clearTimer = null;

  function unmount(){
    for(const ch of mounted){
      if(typeof ch.destroy === 'function'){
        try{ ch.destroy(); }catch(err){ console.error('[live] destroy:', err); }
      }
    }
    mounted.length = 0;
    layer.textContent = '';
  }

  function show(idx){
    clearTimeout(clearTimer); clearTimer = null;
    unmount();
    layer.classList.remove('hidden');
    /* un solo reflow: costruisci fuori dal documento */
    const frag = document.createDocumentFragment();
    const pending = [];
    for(const ch of Book.chapters){
      if(ch.spread !== idx) continue;
      const zone = document.createElement('div');
      zone.className = 'live-zone ' + (ch.side === 'left' ? 'left' : 'right');
      const content = document.createElement('div');
      content.className = 'live-content';
      zone.appendChild(content);
      frag.appendChild(zone);
      try{ ch.build(content); }
      catch(err){ console.error('[live] build:', err); continue; }
      pending.push([ch, content]);
    }
    layer.appendChild(frag);
layer.appendChild(frag);
/* la maschera ink-reveal muore con la sua animazione */
for(const el of layer.querySelectorAll('.ink-reveal')){
  el.addEventListener('animationend',
    () => el.classList.remove('ink-reveal'), {once:true});
}
    /* init DOPO l'inserimento: i capitoli usano getBoundingClientRect */
    for(const [ch, content] of pending){
      mounted.push(ch);
      if(ch.init){
        try{ ch.init(content); }catch(err){ console.error('[live] init:', err); }
      }
    }
  }

  function hide(){
    if(layer.classList.contains('hidden')) return;   /* già nascosto */
    layer.classList.add('hidden');
    clearTimeout(clearTimer);
    /* svuota DOPO la transizione, così il fade è pulito */
    clearTimer = setTimeout(() => {
      clearTimer = null;
      if(layer.classList.contains('hidden')) unmount();
    }, 200);
  }

  document.addEventListener('book:settle', e => show(e.detail.idx));
  document.addEventListener('book:turn', hide);
})();