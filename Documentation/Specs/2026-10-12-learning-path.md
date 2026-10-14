# Learning path: one system at a time

Player feedback on 0.16.0: the game shows too much at once; systems should open one
by one as the player advances, and the player must see which stages are cleared.

## Steps (first rift, Starlit Meadow)

| Step | Name | Waves | Opens |
|---|---|---|---|
| 1 | Summon and merge | 5 | summon, drag-merge |
| 2 | Forge from the recipe book | 6 | forge guide, one-tap forge, recipe book, hidden legends |
| 3 | Selling and missions | 6 | drag-to-sell, wave missions |
| 4 | An engraving every wave | 8 | engraving cards, power number and list |
| 5 | Bosses and the ultimate | 10 | bosses (time limit), ultimate button, boss chest |
| 6 | Constellations and focus | 10 | same-element neighbour bonus and lines, tap-to-focus |
| 7 | Full Starlit Meadow | 15 | everything |

- Always on: crit numbers, jackpot summons, danger warning, 2× speed, elites.
- Steps 1–4 run at reduced enemy health so a first-time player clears them.
- A step opens when the previous one is cleared. Clearing step 7 completes
  `stage_starlit_forge`, which opens Frozen Clocktower as before; Daily and Endless
  also wait for step 7.
- A standard run with no step (tests, later rifts) keeps every system.

## Presentation

- Starting a step that opens something shows one intro card (title, one or two
  lines, "알겠어요"); the game waits until it is dismissed.
- Locked controls stay visible: the recipe button reads "2단계에 열림" and is
  disabled; locked HUD parts (ultimate, power, mission, guide strip) are hidden.
- The first-launch five-page tutorial is no longer forced; it stays in Settings.
- Rift select lists the seven steps with ✓ 클리어 / ▶ 도전 / 잠김 and, for an
  attempted step, the best wave ("최고 4/6"), then the later rifts and Daily/Endless.
- Home shows "다음 목표 · N단계 이름" until the path is done, then the next
  engraving unlock as before.
- A cleared step's result offers "다음 단계"; steps 1–4 start directly, steps 5+
  go through the loadout (ultimate choice).

## Data

- `LearningPath` / `LearningStep` / `GameFeature` (Core) describe the steps.
- `CombinationDefenseSession` takes an optional step: wave count, health scale,
  bosses and constellation bonus follow it.
- `RunSummary.LearningStep`; `ProgressionService.ApplyRun` records `path_N` clears
  and per-step best waves (`PlayerSaveData.PathBestWaves`).
- Existing saves keep level, codex and growth and start at step 1.
