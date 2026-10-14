# Game Design Document v2 — Constellation Forge Defense

## Identity

This is a **sword-combination game with defense pressure attached**. The player
summons, arranges, and combines swords while enemies circulate around the edge of the
same screen. Every feature is judged by one question: does it make combination more
interesting?

The combination-discovery reference is One Piece Random Defense. The portrait board
structure is informed by Random Dice and Rush Royale. Vertical shooting,
Vampire-Survivors-style movement, projectiles fired from a player ship, and a base-HP
loss condition are outside the design.

## Product pillars

1. **Combination first** — combining swords is the main action and the only reliable
   way to survive the difficulty curve.
2. **Readable discovery** — formal recipes expose silhouettes and material hints;
   hidden legends retain riddle-only clues until first discovery.
3. **A sword worth sharing** — rare appearances and newly discovered swords receive
   strong visual presentation and permanent collection records.
4. **One-screen, one-hand clarity** — battle, summoning, and combining coexist on one
   portrait screen.

## Main battle screen

- Enemies circulate along a closed path around the outer edge.
- A 3×4 sword board occupies the center: 12 starting slots, expandable to 15 through
  permanent growth.
- Swords remain in their board slots and automatically attack enemies that enter
  range. Each of the 12 sword families will eventually use a distinct attack pattern.
- The only loss pressure is the enemy population gauge. Reaching 60 enemies loses the
  run; there is no base health.
- The bottom bar contains exactly three actions: **Summon**, **Recipe Book**, and
  **Codex**.
- Recipe crafting opens as an overlay on the battle screen, not as a separate gameplay
  scene.

## Three-layer combination structure

1. **Rank merge:** drag one sword onto the same sword at the same rank. Two swords
   become one sword of the next rank.
2. **Formal recipe:** combine two or three different swords from a lit recipe-book
   entry. Materials fly from the board to the center before the result appears.
3. **Hidden legend:** satisfy a special condition and solve a riddle-only hint.
   First discovery triggers a full-screen reveal, permanent codex entry, and account
   bonus.

After a recipe is discovered, its complete material list remains permanently visible.

## Run loop

1. Enter a 30-wave portrait battle.
2. Spend gold to summon a random base sword into an empty board slot.
3. Earn gold from defeated circulating enemies.
4. Drag two matching swords together to create board space and increase rank.
5. Use lit recipe entries to craft stronger cross-family swords.
6. Face a boss every fifth wave, with a three-to-five-second pause between waves.
7. Win or lose, then retain discoveries and permanent growth before starting another
   short run.

Summon cost rises gradually within a run. Wave 5–7 tuning must make rank-one swords
insufficient so that combination is a survival requirement, not an optional bonus.
Target session length remains 5–10 minutes.

### Pacing rule (0.12.0)

The 12-slot board, not gold, is the scarce resource. Summons are cheap (30 gold,
+2 per summon) so a player acts every one to two seconds, and a full board is solved
by **dragging an unwanted sword onto the Summon button to sell it** for half of the
summons it represents. A run therefore keeps cycling summon → merge/craft → sell
instead of freezing once the board fills with swords that no longer pair. Hidden
legends reveal the moment all of their heroic materials share the board; mythic
swords still require a ★5 material.

## Content direction

- 12 sword families: straight, curved, great, rapier, twin, magic, holy, dragon,
  scythe, axe, spear, and spirit swords.
- Grade color language: common blue-gray, rare blue, heroic violet, legendary gold,
  hidden constellation iridescence.
- Art direction: a mysterious nighttime constellation forge using deep navy and
  violet backgrounds, star particles, and grade-colored glows.
- Combination signature: starlight links materials into a constellation, followed by
  a flash, short hit-stop, and a large named sword reveal.
- The illustrated protagonist is one permanently growing character.
- Korean and English ship together.

## Meta loop

Defeat still preserves codex entries, first discoveries, and permanent progression.
The initial growth targets are starting gold, board capacity, high-rank summon chance,
sword level, sword statistics, and protagonist growth. The current single-player
architecture must keep deterministic rules separate enough to support later rankings
and real-time competition without implementing those online systems now.

## Content rating direction

- All-ages presentation
- No blood, wounds, dismemberment, or realistic suffering
- Fantasy enemies disappear through non-graphic effects
- No casino terminology or real-money wagering presentation

## Delivery stages

1. Approved visual direction: **Starlight Corridor**.
2. Gray-box interaction prototype: loop path, 12-slot board, summon, and two-sword
   drag merge.
3. Formal recipes, hidden legends, and combination presentation.
4. Codex, permanent growth integration, balance, and polish.

Each stage requires passing EditMode and PlayMode tests plus an APK build and
installation check on the connected Galaxy S20 Ultra before proceeding.
