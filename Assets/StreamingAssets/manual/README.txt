IL MANUALE — Cheese CO-OP (giocatore 2)
Avvio: doppio click su index.html (tutto offline, niente server).

REGOLE
1. Nei capitoli: SOLO classi, MAI id (i contenuti vengono clonati da strisce e loupe).
2. engine.js non si tocca (salvo costanti N/SPAN/BETA in testa).
3. Niente CDN/hotlink: tutto in locale (assets/, lib/, fonts/).
4. Nuovo capitolo = file in chapters/ + una riga <script> in index.html.

ORDINE SCRIPT in index.html:
lib/matter.min.js → js/pages.js → js/state.js → js/audio.js → js/live.js
→ chapters/* (in ordine di pagina) → js/engine.js → js/loupe.js
→ js/particles.js → js/plants.js → worlds/ripostiglio/ripostiglio.js

STRUTTURA
index.html                  entry point, solo DOM + script
css/book.css                motore libro (stage, tilt, strisce, ombre)
css/pages.css               tipografia pagine vive + font + .tomo
css/tools.css               toolbox, cursori, macchia/frottage/secret-layer
css/loupe.css               lente d'ingrandimento
css/ripostiglio.css         mondo ripostiglio + canvas piante/polvere
js/pages.js                 texture spread canvas 1760×1240 (window.SPREADS)
js/state.js                 inventario strumenti + localStorage (State)
js/audio.js                 Web Audio condiviso (Sfx)
js/live.js                  registro capitoli + overlay vivo (Book)
js/engine.js                flip sketchbook: strisce annidate + molla
js/loupe.js                 lente: clone + maschera + shove
js/particles.js             pulviscolo ambiente + burst al flip
js/plants.js                piante procedurali ai bordi
chapters/ch01-alchimia.js   calcolatrice (spread 1, destra)
chapters/ch03-tomo.js       poema White Knight + 3 strumenti (spread 2, destra)
worlds/ripostiglio/ripostiglio.js   fisica matter-js + luce candela
fonts/                      4 font locali (.ttf/.otf)
assets/img/ assets/audio/   risorse locali
lib/matter.min.js           matter-js vendored