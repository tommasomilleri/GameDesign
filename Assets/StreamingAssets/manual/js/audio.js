/* ============================================================
   AUDIO — Web Audio condiviso (un solo AudioContext).
   API:
     Sfx.scrub('sponge'|'pencil')  rumore filtrato (umido/graffiante)
     Sfx.stopScrub()
     Sfx.wood()                    tonfo di legno (urti)
     Sfx.fly()                     ronzio mosca (1.5s)
   ============================================================ */
window.Sfx = (() => {
  let ctx, gain, filter;

  function init(){
    if(ctx) return;
    ctx = new (window.AudioContext || window.webkitAudioContext)();
    /* sorgente di rumore bianco in loop per lo scrub */
    const buf = ctx.createBuffer(1, ctx.sampleRate*2, ctx.sampleRate);
    const d = buf.getChannelData(0);
    for(let i=0;i<d.length;i++) d[i]=Math.random()*2-1;
    const src = ctx.createBufferSource();
    src.buffer=buf; src.loop=true;
    filter = ctx.createBiquadFilter(); filter.type='lowpass';
    gain = ctx.createGain(); gain.gain.value=0;
    src.connect(filter); filter.connect(gain); gain.connect(ctx.destination);
    src.start();
  }
  function resume(){ if(ctx && ctx.state==='suspended') ctx.resume(); }
  const unlock=()=>{ try{ init(); resume(); }catch(_){} };
  addEventListener('pointerdown', unlock, {once:true});
  addEventListener('keydown',     unlock, {once:true});
  return {
    scrub(kind){
      init(); resume();
      filter.frequency.value = kind==='pencil' ? 1200 : 400;
      gain.gain.setTargetAtTime(kind==='pencil' ? .8 : 1.5, ctx.currentTime, .05);
    },
    stopScrub(){
      if(gain) gain.gain.setTargetAtTime(0, ctx.currentTime, .1);
    },
    wood(){
      init(); resume();
      const osc=ctx.createOscillator(), g2=ctx.createGain();
      osc.type='triangle';
      osc.frequency.setValueAtTime(120, ctx.currentTime);
      osc.frequency.exponentialRampToValueAtTime(10, ctx.currentTime+.15);
      g2.gain.setValueAtTime(.5, ctx.currentTime);
      g2.gain.exponentialRampToValueAtTime(.01, ctx.currentTime+.15);
      osc.connect(g2); g2.connect(ctx.destination);
      osc.start(); osc.stop(ctx.currentTime+.15);
    },
    /* E2: campanella del sigillo. Fondamentale + quinta armonica,
       envelope lungo: deve suonare come una piccola campana d'ottone,
       non come un "ding" di notifica. */
    chime(){
      init(); resume();
      const t0=ctx.currentTime, out=ctx.createGain();
      out.gain.setValueAtTime(.0001, t0);
      out.gain.exponentialRampToValueAtTime(.18, t0+.012);
      out.gain.exponentialRampToValueAtTime(.0001, t0+1.2);
      out.connect(ctx.destination);
      for(const [f,a] of [[880,1],[1320,.45],[2640,.12]]){
        const o=ctx.createOscillator(), gg=ctx.createGain();
        o.type='sine'; o.frequency.value=f;
        gg.gain.value=a;
        o.connect(gg); gg.connect(out);
        o.start(t0); o.stop(t0+1.25);
      }
    },
    fly(){
      init(); resume();
      const osc=ctx.createOscillator(), fg=ctx.createGain();
      osc.type='sawtooth'; osc.frequency.value=180;
      osc.connect(fg); fg.connect(ctx.destination);
      fg.gain.setValueAtTime(.12, ctx.currentTime);
      fg.gain.exponentialRampToValueAtTime(.001, ctx.currentTime+1.5);
      const lfo=ctx.createOscillator(), lg2=ctx.createGain();
      lfo.type='sawtooth'; lfo.frequency.value=33;
      lg2.gain.value=30; lfo.connect(lg2); lg2.connect(osc.frequency);
      osc.start(); lfo.start();
      osc.stop(ctx.currentTime+1.5); lfo.stop(ctx.currentTime+1.5);
    }
  };
})();