# 검별공방: 랜덤 디펜스

**Swordstellation: Random Forge** is a portrait Android sword-combination game with
defense pressure. Swords occupy a central board while enemies circulate around its
outer path; combining swords is the main action and progression mechanism.

The current target is an offline single-player development candidate. Store publishing,
live advertising, real-money billing, signing, and online competition are deliberately
deferred.

## Product identity

- Developer: YMLEE
- In-game credit: YMLEE Games
- Android application ID: `com.ymlee.swordstellation`
- Engine: Unity 6.3 LTS
- Primary languages: Korean and English
- First release: single-player, all-ages-content target

## Repository layout

- `Assets/Swordstellation/Core`: deterministic game rules and content
- `Assets/Swordstellation/Runtime`: Unity-facing gameplay and presentation
- `Assets/Swordstellation/Editor`: project setup and build automation
- `Assets/Swordstellation/Tests`: automated tests
- `Assets/Art`: original production art
- `Assets/Resources/Art`: 60 sword, 26 enemy, and portrait UI runtime sprites
- `Documentation`: game design, development log, architecture, release, and policy notes
- `Documentation/ArtManifest.md`: generated-art inventory and review policy
- `Documentation/BuildAndTest.md`: reproducible Android build and verification steps
- `Tools/build_android.ps1`: Windows Android build wrapper
- `Tools/run_tests.ps1`: Unity rules, progression, and balance regression suite

## Build principle

Gameplay data and rules are kept independent from scene presentation so that balance
changes, save migrations, replays, and a later authoritative online mode remain possible.

## Current prototype

- Approved **Starlight Corridor** portrait layout
- Closed outer loop with circulating prototype enemies
- Central 3×4 board with 12 sword slots
- Cheap gold summon (+2 per summon) and drag-to-Summon-button selling, so a full
  board keeps cycling instead of freezing
- Drag two matching equal-rank swords together to rank up; drag onto another slot
  to move or swap, so same-element neighbours form constellation bonuses
- One engraving pick (1 of 3) after every wave builds each run; engravings unlock
  with smith level
- Ultimate fired by the player from a charge button; tap an enemy to focus it
- In-battle recipe-book overlay with automatic material matching
- Two-to-three-sword formal recipes and condition-based hidden legends
- Constellation gathering, flash, hit-stop, and first-discovery sword reveal
- Enemy population gauge with a 60-enemy limit
- Bottom actions limited to Summon, Recipe Book, and Codex
- Live combination guide, matching-pair glow/link, and craftable-recipe count
- Resonance-biased summons, ten-second forge chains, summon discounts, and
  full-board deadlock reforge
- Recipe-first sorting with explicit material ownership and one-tap crafting
- Existing 60-sword catalog, 50 recipes, collection data, Korean/English
  localization, and local-save foundation retained

- Seven-step learning path on the first rift: each cleared step opens one more
  system, with a ★1–★3 rating per step
- Fifteen-wave runs with bosses every five waves, critical hits, jackpot summons,
  boss treasure chests, and a small mission every wave

Development history (Korean): [Documentation/DevLog.md](Documentation/DevLog.md).
Detailed status: [Documentation/ImplementationStatus.md](Documentation/ImplementationStatus.md).
