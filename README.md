# Papír-repülő ágens
Az általános cél megfogalmazása, egy papírrepülőt vezérlő pilóta betanítása volt, konkrét
feladat viszont a projekt haladásával helyenként változott. Változás nem feltétlen eltörlés jelent,
inkább betanításuk prioritásának csökkenését. Eredeti cél szerint egy papírrepülőt szeretnénk
betanítani, egy három dimenziós koordinátákból álló útvonal követésére. A repülő három fő
irányítási bemenete van: „yaw” (elfordulás), „roll” (gördülés) és „pitch” (orr fel vagy le emelése),
valamint nem rendelkezik meghajtóval. Sebességét zuhanással szerezheti így kellő lendület
hiányában hamar lefulladhat. Ezenfelül a pilóta válthat repülőt a levegőben. Különböző
repülőgépek, eltérő tulajdonságai előnyöket és hátrányokat adhatnak. Három különböző típus
van:
  - alap repülő (default), nincs se előnye, se hátránya
  - gyors repülő (dart), jóval nagyobb sebességeket ér el, viszont kanyarodása sokkal lassabb
  - vadászrepülő (hunting), sokkal gyorsabban kanyarodik, viszont jóval lassabban repül
  
Ezen felül létezik egy opcionálisan is használható, negyedik típus, a papír galacsin (paperball).
Ebben a formában szabadesést végez a ”repülő”, és a pilóta által ekkor kontrollálhatatlan. Ha
nekicsapódik a földnek a repülő, szigorúan ebbe az állapotba kerül, viszont manuálisan is lehet
váltani rá, olykor akár hasznos is lehet. A környezetben két fajta akadály létezik: föld (ground)
és a víz (water). A kettő különbsége, hogy földbe (avagy akadályba) csapódáskor, a repülőnek
még van lehetősége túlélni, feltéve hogy a papír golyó bele esik vagy gurul a célba. Vízhez
éréskor azonban rögtön vége a játéknak. A környezet ezen felül tartalmazhat zónákat, szintén
két külön típussal: szél és eső. A repülőnek nincs meghajtása, viszont ha széllel egy irányban
repül, kap egy folyamatos löketet (ha ellenirányban, akkor lefullad). Az esőzóna akadályokat,
óriási vízcseppeket generál, amik a veszélyesebb akadály fajta és ráadásul mozognak is.

Modellek (.onnx) a results mappában, az ágens kódja az Scripts-ben 
