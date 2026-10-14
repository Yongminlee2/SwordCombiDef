# Combination-defense implementation status

## Current candidate — 0.18.0 (23): step stars

- Each learning step is rated ★1–★3 by the highest enemy count during the run
  (clear = ★1, under 40 = ★2, under 25 = ★3; `LearningPath.Stars`,
  `CombinationDefenseSession.PeakPressure`). Each newly earned star pays 40 iron.
- The result card shows the stars, new-star reward or the next star's target; the
  step list shows the best stars ("✓ ★★☆"); during a step the enemy counter shows
  the stars still reachable.
- Tests: three more `LearningPathTests` (stars, rewards, peak tracking).

## Current candidate — 0.17.0 (22): learning path

Spec `Documentation/Specs/2026-10-12-learning-path.md`. Player feedback: too much
at once; systems should open step by step and cleared stages must be visible.

- The first rift is seven steps (5/6/6/8/10/10/15 waves), each opening one group of
  systems: merge → recipes → selling + missions → engravings → bosses + ultimate +
  chest → constellations + focus → full run (`LearningPath`, `GameFeature`).
- The session follows the step (waves, 0.7–0.85 enemy health in steps 1–4, bosses,
  constellation bonus); the battle screen hides or locks what is not open yet
  ("2단계에 열림" on the recipe button) and shows one intro card per step.
- Rift select is a list with ✓ 클리어 / ▶ 도전 / 잠김 and best waves; home shows
  "다음 목표"; a cleared step offers "다음 단계". Clearing step 7 completes the
  rift as before; Daily waits for it. The five-page tutorial is no longer forced.
- With only merges open, a full board with no pair counts as stuck so the
  reforge button can free it.
- CoreSmoke runs every step: steps 1–6 100/100, step 7 60/100 for a beginner bot.
- Tests: `LearningPathTests` (7), PlayMode step-one gating and rift-select status;
  home-goal test updated.

## Current candidate — 0.16.0 (21): hit feel and jackpot moments

- **Critical hits**: each sword attack rolls a crit (10% at ★1, +2% per extra star)
  for double damage (`RunBalanceRules.CriticalChance`, `BattleAttackEvent.IsCritical`).
- **Damage numbers** float over enemies (`DamagePopupLayer`); crits pop large and
  gold with "!". Ordinary numbers yield to crits when the screen is busy.
- **Jackpot summon**: 5% of summons arrive at ★2 with a fanfare toast.
- **Boss treasure**: a defeated boss opens a chest — pick 1 of 3 from star
  blessing (best sword ★+1), gold hoard, full ultimate, 3 free summons, rare sword
  (`BossTreasureRules`). It waits until no other card or book is open.
- Late-wave health curve 0.014 to offset crits (CoreSmoke beginner 183/300,
  growth 294/300, 3.6 min; Frozen Clocktower 105/150, Worldroot 38/150).
- **Wave missions**: every wave sets one small goal (summon 4–6, merge 2–3,
  forge 1–2, sell 2 or use the ultimate) shown opposite the power number; finishing
  it pays 25 + 6×wave gold (`WaveMission`, `WaveMissionRules`).
- Tests: `FunPassTests` (4), `WaveMissionTests` (4) and four PlayMode tests
  (damage numbers, treasure pick, treasure effects, mission reward). The old
  per-attack damage labels in `BattleAttackEffectView` were removed (they doubled
  the new numbers).

## Current candidate — 0.15.1 (20): forging no longer pauses the battle

- Recipe forging (one-tap guide, recipe book, hidden legends) plays its effect while
  enemies keep moving: the hit-stop (`Time.timeScale = 0`) is gone and the session
  advances during the effect. The board stays locked until the effect ends, but the
  ultimate and enemy focus work. Normal crafts dim the screen less (0.22) and hold
  0.3 s; the effect no longer blocks taps.
- Test `BattleKeepsRunningWhileForging`. EditMode 83/83, PlayMode 36/36; installed
  and checked on SM-A165N.
- Layout pass from a device sweep of every screen: battle Summon/Recipe/Codex
  buttons keep their frame proportions (`SlicedFrameFitter`) with centred labels
  and no icons; codex/recipe-book overlays put the title below the frame ornament
  and the close button at the bottom; codex screens get a centred progress line
  with tabs underneath; engraving card, loadout ultimate and pause/result buttons
  stay inside the corner ornaments (result buttons stacked); Daily/Endless are
  one-line buttons with the explanation below; settings −/+ are wider; sword
  grades are localised ("일반/희귀/영웅/전설/신화").
- Second sweep: home portrait moved clear of the title and the three small home
  buttons share one size; records drop the portrait and read 처치/보스; the shop
  materials card is centred; the enemy codex shows enemy art (silhouette when
  unseen) and 체력/피해 in Korean; the engraving codex uses grade-coloured round
  marks; the in-run engraving list stacks grade, name and effect; the 2× label is
  "▶ 2x" so it fits its frame. Boss bar, danger counter, tutorial and summon-pool
  screens checked on device without changes.
- Every scrolling list (codex tabs, battle codex, engraving list, recipe book) was
  100 px wider than its viewport because new RectTransforms default to a 100×100
  sizeDelta, so rows were clipped 50 px on each side; content width now matches
  the viewport, and lists get extra top padding so the first row clears the frame.

## Previous candidate — 0.15.0 (19): tempo pass

Player feedback on 0.14.0: still dull — runs too long, too slow, no tension.

- **15-wave runs** (bosses 5/10/15) that walk the old 30-wave health curve two steps
  per wave; 1.5× enemies per wave, 1.5 s breaks, faster enemies, 2× kill gold,
  faster ultimate charge. A run is ~3.5 minutes.
- **2× speed** toggle next to the gold counter (kept for the app session).
- **Boss time limit 40 s**: an unkilled boss rampages (faster, weighs as 10 of the
  60-enemy limit); the boss bar counts the seconds down and turns red.
- **Danger**: from 45/60 the screen edges pulse red and the enemy counter reads
  "위험 · 적 N/60".
- **Wave cleared** before the next wave: +gold bonus toast.
- **Readability**: every label is ~20% larger (minimum 22, still shrinks to fit),
  and labels are centred except the screen-edge HUD readouts.
- Balance (CoreSmoke): beginner 178/300, 3.6 min; Frozen Clocktower 100/150,
  Worldroot Void 44/150 with a level-6 bot. Static one-star wall at wave 7.

## Previous candidate — 0.14.0 (18): constellations and unlocks (stages 2–3)

Spec `Documentation/Specs/2026-10-11-run-redesign-stage2-3.md`.

- **Move and swap**: dragging a sword onto an empty slot moves it; onto a different
  sword swaps them (identical same-rank swords still merge).
- **Element constellations**: orthogonally adjacent swords of one element (not
  neutral) form a group; every sword in it gains +10/20/30/40% damage for groups of
  2/3/4/5+. Element-coloured lines connect them. New legendary engraving
  "Constellation Mastery" doubles the bonus (`ConstellationRules`).
- **Engraving unlocks**: 6 of the 19 new engravings are open from the start; the
  rest open one per smith level (LV.2–LV.14). Locked ones never appear as cards.
  The home screen shows the next unlock, the result screen names newly opened
  engravings or the XP left to the next one, and the engraving codex says when a
  locked one opens. Locked rifts now read "잠김".
- **Owned engraving list**: tap the power number to see this run's engravings.
- Tutorial is five pages (adds engravings/ultimate and constellations/focus);
  "룬" is called "각인" everywhere.
- The ultimate button no longer looks ready when there is nothing to hit.
- Late-wave health curve 0.031 with the beginner bot limited to starter
  engravings (CoreSmoke beginner 185/300, 6.5 min). Later rifts probed with a
  level-6 bot: Frozen Clocktower 110/150, Worldroot Void 71/150.

Verification: EditMode 78/78, PlayMode 34/34, CoreSmoke as above. Development APK
0.14.0 (18), 41,717,382 bytes, SHA-256
`DBB55DE32AC4DA5AC3EAD887493E4D956AD276CE268146379B7D49CD504B742A`, installed and
played on SM-A165N (home goal, cards, move/swap, constellation lines, engraving
list, focus ring, ultimate button).

Known minor gaps: on boards with 13+ slots the ultimate button sits beside the
right path and enemy tap areas can reach the bottom row; one burn/chain test does
not prove a bonus kill happened; Rewind's floor is the normal 15-gold minimum.

## Previous candidate — 0.13.0 (17): engraving runs, stage 1

Spec `Documentation/Specs/2026-10-11-run-redesign-stage1.md`, plan alongside it.
Player feedback on 0.12.0: nothing to decide, no hands-on combat, growth not felt,
no reason to play again. Stage 1 of a roguelike-build direction:

- **Engraving every wave**: when a wave finishes spawning, pick 1 of 3 engravings
  (time paused). Runes are now engravings with a grade (common/rare/legendary,
  70/25/5, boss waves guarantee rare+) and a tag (forge, sell, recipes, element,
  hands, rules). The 24 old runes are the commons; 18 new rare/legendary ones add
  forge stacking, ★3 extra target, endless chain, sell 1.5×/discount/charge,
  recipe charge, discovery stacking, resonance boost, burn, chain lightning,
  frost slow, elemental resonance, hunter's mark, free summons after the
  ultimate, overcharged ultimate, empty-slot power and early rare summons.
  Cards show the grade, tag and how many of that tag the run already holds.
- **Manual ultimate**: a circular charge button under the board; it no longer
  fires on its own.
- **Focus**: tap an enemy on the loop to make swords in range hit it first for 5 s.
- **Power** number (board damage per second) replaces the ultimate % in the HUD;
  picking a card toasts the power gain.
- **Design**: Pretendard Bold for UI, Hahmlet Bold for titles/sword and
  engraving names (both OFL, licences in `Documentation/ThirdPartyLicenses`),
  NotoSansKR removed; theme reduced to Night/Nebula/Starlight/Muted/EmberGold
  (numbers)/ConstellationCyan (actionable)/WarningCoral.
- Late-wave health curve 0.025 (CoreSmoke fast bot 192/300, 6.3 min).

Not yet in this stage: element adjacency synergy (stage 2), engraving
collection/unlocks between runs (stage 3), a mid-run list of owned engravings.

## Previous candidate — 0.12.0 (16): pacing pass

Diagnosis of 0.11.0 with a pacing probe (greedy player, one action per second):
a run had only ~33 summons, the player had nothing to do for ~70% of the time,
and by wave 10 the 12-slot board usually filled with swords that no longer paired
(mismatched ranks, unrelated rares). No hidden legend was reached in 100 runs.

Changes:

- **Sell by drag**: drag a sword onto the Summon button to sell it for half of the
  summons it represents (`RunBalanceRules.SellValue`). While dragging, the button
  reads "판매 +✦N"; on a full board it reads "보드 가득 / 검 끌어와 판매".
- **Cheap summons**: 30 gold, +2 per summon, starting gold 150
  (`RunBalanceRules.SummonCost` / `StartingGold`, shared by runtime and CoreSmoke).
  Forge chains take 10% off per step, up to 30%.
- **One-tap forging from the board guide**: the three targets above the board are
  buttons; a ready one crafts immediately ("눌러서 제작"), otherwise it opens the book.
- **Directed resonance**: resonance summons only follow recipes already started on
  the board, and a missing rare ingredient points at its own base materials
  (previously every recipe counted, so resonance was nearly random).
- **Reachable legends**: hidden legendary recipes trigger once all heroic materials
  share the board (the old all-★2 rule was effectively unreachable). Mythic still
  needs a ★5 material.
- **Late-wave health growth** (`LateWaveHealthGrowth`, quadratic after wave 6,
  bosses at half rate) keeps heroic/legendary crafting necessary.
- Action-button values (cost, sell price) now carry a dark outline for readability.

Probe after the change (same player model): ~110 summons per run, ~27 actions per
minute, idle time ~20–27%, 17% of runs reach a legend, win rate ~54%.
Screens: `Documentation/Design/Audit-2026-10-11`.

### Device QA pass (Galaxy A16, 1080×2340)

Played on a real phone; bugs found and fixed:

- **Retry stacked battles**: "다시 도전" created a new battle without disposing the
  old one, so the result card stayed on screen and taps did nothing
  (`GameBootstrap.StartBattle` now disposes first).
- **Enemy gauge never filled**: the gauge frame's opaque interior was drawn over
  the fill; the frame now renders behind it.
- **Android back did nothing on menus**: each menu's back button action is now
  also bound to the back key.
- **Boss fights hid the forge guide**: the boss bar replaced the guide strip; it
  now sits above it, so one-tap forging works through boss waves.
- **Ornate frames crushed small buttons** (sliced borders rendered at the art's
  1000+ px size, ornaments crossing labels): `SlicedFrameFitter` scales borders
  with the element and turns short list rows into plain bordered panels.
- Labels shrink to fit instead of clipping (`UiFactory.CreateText` best fit),
  button labels sit inside the frame, headers are centred, and HUD, guide, codex,
  recipe-book and stage texts were enlarged.
- Recipes now spend the lowest-rank copy of each ingredient, so an automatic
  hidden-legend reveal cannot eat a ★4 while a ★1 duplicate exists.
- Selling is ignored while paused/recipe book open and only sells the sword that
  was actually picked up; Endless shows "WAVE 31" instead of "WAVE 31/2147483647".
- Tutorial pages explain selling and one-tap forging.

Verification: EditMode 53/53, PlayMode 18/18, CoreSmoke beginner 195/300, growth
300/300, average 7.2 min, static one-star wall at wave 11. Development APK
0.12.0 (16) installed and played on SM-A165N (tutorial, menus, battle, selling,
one-tap forge, rune choice, pause, abandon, retry, English).

## Previous candidate — 0.11.0 (15)

- Approved **Starlight Corridor** portrait art direction
- One-screen 12-slot board, permanently expandable to 15 slots
- Closed outer enemy loop and a single 60-enemy defeat gauge
- Automatic sword combat with real health, damage, kills, score, and gold rewards
- 12 sword families with distinct attack presentation and targeting ranges
- Thirty waves, bosses every five waves, intermissions, victory, defeat, retry, and
  run-abandon results
- Three automatic-charge ultimates and five in-run three-choice rune rewards
- Eleven live elite/boss abilities: shields, healing, rushing, attack delay, and
  resource drain
- Two-copy drag rank merge, two-to-three-material recipe crafting, and automatic
  hidden-legend discovery conditions
- Constellation-line forge presentation, first-discovery presentation, and permanent
  recipe/codex registration
- Rare Aurora, Starforged, and Voidglass appearances with a dedicated reveal and
  permanent collection registration
- Permanent starting economy, board capacity, global sword control, player level,
  and per-sword mastery progression
- Korean and English UI, reduced-motion option, audio controls, Android lifecycle
  pause, and local save backup recovery
- Battle Recipe Book and Codex both open as overlays without destroying the run
- Combination planner continuously exposes matching pairs and recipes reachable from
  the current board, with a compact three-target forge guide above the board
- Matching equal-rank swords pulse and connect with a constellation line, while the
  recipe action displays and pulses with its live ready count
- Resonance-biased summons reduce dead rolls, a short forge chain discounts following
  summons, and a completely deadlocked full board can use resonance reforge
- The recipe book sorts craftable combinations first and presents checked materials,
  readable result names, and separated craft controls without ornamental distortion
- Portrait UI now scales with the full 1080×1920 safe composition; action titles and
  values use separate rows and no longer collide on tall Android screens
- Daily mode uses a stable local-date seed; Endless mode unlocks with the final stage

## Verification

- Unity EditMode: 45 passed, 0 failed
- Unity PlayMode: 14 passed, 0 failed
- Content: 60 swords, 50 recipes, 26 enemies, 3 stages
- Resource coverage: every sword/enemy image plus required battle UI and Korean font
- Balance: 600 deterministic runs
  - beginner build: 146/300 victories
  - growth build: 300/300 victories
  - beginner average: 5.7 minutes, wave 26.7
  - a full static one-star common board fails at wave 11
- Development APK:
  - package `com.ymlee.swordstellation`
  - version `0.11.0` (`15`)
  - IL2CPP ARM64, minimum API 25, target API 36
  - 45,199,484 bytes
  - SHA-256 `5D3C561EC6526F941FE2A8BF4F4F2EF7904E2698748245CFB0E28F86EFBC13E4`
  - valid Android APK Signature Scheme v2 debug signature

## External work intentionally outside this offline candidate

- Google Play publishing and production signing
- A real rewarded-ad SDK and verified completion callbacks
- Google Play Billing products and purchase restoration
- Cloud saves, crash/analytics SDKs, leaderboards, rankings, and PvP

Those integrations require console identifiers, legal disclosures, and external test
accounts. The offline game does not display a fake ad or real-money purchase action.

## Device state

The APK at `Builds/Android/Swordstellation-development.apk` was installed successfully
with `adb install -r -d` on an authorized Samsung Galaxy S20 Ultra (SM-G988N). Package
metadata on-device reports version `0.11.0` (`15`), the Unity process remains alive,
and the post-launch AndroidRuntime/Unity error log is clean. A full human play session,
frame timing, and thermal soak remain device QA rather than build gates.
