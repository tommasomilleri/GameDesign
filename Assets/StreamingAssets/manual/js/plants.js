/* Piante a CATENA con CICLO STAGIONI:
   primavera: germogli + crescita · estate: fioritura
   autunno: ingiallimento + caduta · inverno: rami spogli.
   Gira a 30 fps: il movimento è lentissimo, indistinguibile da 60. */
(() => {
'use strict';
const cv = document.getElementById('plantsCv');
if(!cv) return;
if(!window.RAF){ console.error('[plants] js/raf.js non caricato'); return; }
const g = cv.getContext('2d');

const SEASON = 20;                 /* secondi per stagione (80s l'anno) */
const YEAR = SEASON*4;
const FPS = 30;

/* ---- costruzione ---- */
function plant(x,y,dir,s){
  const segs=[];
  (function branch(parent,ang,len,w,depth){
    if(depth>4||len<8*s)return;
    const idx=segs.length;
    segs.push({parent,ang,len,w,depth,
      leaf:depth>=2&&Math.random()<.6,
      flower:depth>=3&&Math.random()<.35,
      jit:Math.random(),
      dropped:false,
      ex:0,ey:0,ease:0});
    branch(idx,ang-.35-Math.random()*.25,len*.72,w*.65,depth+1);
    branch(idx,ang+.30+Math.random()*.25,len*.68,w*.65,depth+1);
  })(-1,-Math.PI/2+dir*.25,(90+Math.random()*40)*s,7*s,0);
  return {segs,rx:x,ry:y};
}

let plants=[];
function buildPlants(){
  const s=Math.max(.7,Math.min(1.4,innerHeight/900));
  plants=[plant(innerWidth*.04,innerHeight,.4,s),
          plant(innerWidth*.96,innerHeight,-.4,s),
          plant(innerWidth*.88,innerHeight,-.1,s)];
}
function fit(){ cv.width=innerWidth; cv.height=innerHeight; buildPlants(); }
fit();
addEventListener('resize',fit,{passive:true});

/* ---- particelle in caduta ---- */
const falling=[];
const FALL_MAX=80;
function drop(x,y,kind){
  if(falling.length>=FALL_MAX)return;
  falling.push({x,y,kind,
    vx:(Math.random()-.5)*.4, vy:.3,
    rot:Math.random()*6.28, vr:(Math.random()-.5)*.06, t:0});
}

function mix(a,b,k){
  return `rgb(${Math.round(a[0]+(b[0]-a[0])*k)},${Math.round(a[1]+(b[1]-a[1])*k)},${Math.round(a[2]+(b[2]-a[2])*k)})`;
}
const GREEN=[74,92,48], YELLOW=[176,138,52];
const GREEN_CSS=`rgb(${GREEN[0]},${GREEN[1]},${GREEN[2]})`;

const t0=performance.now();
const world=document.getElementById('worldRipostiglio');

RAF.add(now => {
  if(world && !world.hidden) return;

  const t=(now-t0)/1000;
  const yt=(t%YEAR)/YEAR;
  const season=yt*4;                    /* 0-1 prim, 1-2 est, 2-3 aut, 3-4 inv */

  const autumnCol = (season>=2 && season<3)
    ? mix(GREEN, YELLOW, Math.min(1,(season-2)/.6))
    : GREEN_CSS;
  g.clearRect(0,0,cv.width,cv.height);
  g.globalAlpha=.55;

  for(const pl of plants){
    const total=pl.segs.length;
    let growT;
    if(season<1)      growT=Math.min(1,(yt*YEAR)/6);
    else if(season<3) growT=1;
    else              growT=Math.max(.55,1-(season-3)*.45);
    if(season<1) for(const s of pl.segs) s.dropped=false;   /* reset annuale */

    for(let i=0;i<total;i++){
      const s=pl.segs[i];
      const px = s.parent<0 ? pl.rx : pl.segs[s.parent].ex;
      const py = s.parent<0 ? pl.ry : pl.segs[s.parent].ey;
      const pEase = s.parent<0 ? 1 : pl.segs[s.parent].ease;
      const start=(i/total)*.75;
      const k=Math.max(0,Math.min(1,(growT-start)/.22))*Math.min(1,pEase*1.2);
      s.ease=k*k*(3-2*k);
      if(s.ease<=0){ s.ex=px; s.ey=py; continue; }

      const droop = season>=3 ? (season-3)*.18*(s.depth+1)*.25 : 0;
      const sway=Math.sin(t*.8+i*.7)*(s.depth+1)*.9*s.ease;
      s.ex=px+Math.cos(s.ang+droop)*s.len*s.ease+sway;
      s.ey=py+Math.sin(s.ang+droop)*s.len*s.ease;

      g.strokeStyle= season>=3 ? '#4a3f30' : '#3a4a28';
      g.lineWidth=Math.max(.5,s.w*s.ease);
      g.beginPath(); g.moveTo(px,py); g.lineTo(s.ex,s.ey); g.stroke();

      /* ---- FOGLIE ---- */
      if(s.leaf && s.ease>.9){
        let leafK=1;
        if(season>=2 && season<3){                      /* autunno */
          const a=season-2;
          g.fillStyle=autumnCol;
          if(a > .3+s.jit*.6){
            if(!s.dropped){ s.dropped=true; drop(s.ex,s.ey,'dryleaf'); }
            leafK=0;
          }
        }else if(season>=3){ leafK=0; }                 /* inverno */
        else g.fillStyle=GREEN_CSS;
        if(leafK>0){
          g.beginPath();
          g.ellipse(s.ex,s.ey,7*s.ease,3.2*s.ease,s.ang,0,7);
          g.fill();
          if(Math.random()<.00024) drop(s.ex,s.ey,'leaf');  /* x2: metà frame */
        }
      }

      /* ---- FIORI ---- */
      if(s.flower && s.ease>=1){
        let kb=0;
        if(season>=.6 && season<1) kb=(season-.6)/.4;   /* sboccia */
        else if(season<2)          kb=1;                /* estate  */
        else if(season<2.4){                            /* sfiorisce */
          kb=1-(season-2)/.4;
          if(Math.random()<.04*(1-kb)) drop(
            s.ex+(Math.random()-.5)*6, s.ey+(Math.random()-.5)*6, 'petal');
        }
        if(kb>0){
          const e2=kb*kb*(3-2*kb), r=4.5*Math.max(e2,.55);
          const nPet=Math.max(0,Math.round(5*kb));
          g.fillStyle='#c9788a';
          for(let p=0;p<nPet;p++){
            const a=p/5*6.28+t*.1+s.jit*6.28;
            g.beginPath();
            g.ellipse(s.ex+Math.cos(a)*r*.8,s.ey+Math.sin(a)*r*.8,r*.55,r*.3,a,0,7);
            g.fill();
          }
          g.fillStyle='#e8c23a'; g.beginPath(); g.arc(s.ex,s.ey,r*.34,0,7); g.fill();
        }
      }
    }
  }

  /* ---- particelle in caduta ---- */
  for(let i=falling.length-1;i>=0;i--){
    const f=falling[i];
    if(f.y>cv.height+20){ falling.splice(i,1); continue; }
    f.t+=1/FPS;                                   /* compensa i 30 fps */
    f.x+=f.vx+Math.sin(f.t*2.4)*1.1;
    f.y+=f.vy+Math.abs(Math.sin(f.t*2.4))*.5;
    f.rot+=f.vr;
    g.save(); g.translate(f.x,f.y); g.rotate(f.rot);
    if(f.kind==='petal'){
      g.fillStyle='#c9788a';
      g.beginPath(); g.ellipse(0,0,4,2.2,0,0,7); g.fill();
    }else{
      g.fillStyle = f.kind==='dryleaf' ? '#b08a34' : '#5a6c38';
      g.beginPath(); g.ellipse(0,0,7,3.2,0,0,7); g.fill();
    }
    g.restore();
  }

  g.globalAlpha=1;
}, FPS);
})();