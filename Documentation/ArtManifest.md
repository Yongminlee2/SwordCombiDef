# Art Asset Manifest

All production art currently checked into this project was generated specifically for
**Swordstellation: Random Forge / 검별공방: 랜덤 디펜스** on 2026-07-29–30. The prompts
explicitly excluded text, logos, existing characters, and franchise references.

## Runtime assets

- `Assets/Art/Generated/AppIcon.png`
  - Original 1024×1024 icon master.
  - Motif: a gold star-forged sword, cyan constellation orbit, dimensional sparks.
- `Assets/Resources/Art/UI/MainMenuBackground.png`
  - Original portrait celestial forge illustration.
- `Assets/Resources/Art/Swords/*.png`
  - 60 transparent individual sword sprites matching every sword ID in `ContentCatalog`.
  - 12 common, 20 rare, 16 heroic, 8 legendary, and 4 mythic.
- `Assets/Resources/Art/Enemies/*.png`
  - 26 transparent individual enemy sprites matching every enemy ID in `ContentCatalog`.
  - 15 normal, 6 elite, and 5 boss enemies.
- `Assets/Resources/Art/Characters/HeroSwordsmith.png`
  - Original transparent 1024×1536 celestial swordsmith protagonist.
  - Navy, violet, gold, and cyan forge-core palette matched to the original boss art
    direction without copying any existing character.
- `StoreAssets/FeatureGraphic.png`
  - Original 1024×500 celestial forge feature graphic.
  - Exported without text so one asset can serve Korean and English listings.
- `StoreAssets/PlayIcon512.png`
  - 512×512 Play Console export derived from the original icon master.

## Source sheets and reproducibility

The chroma-key-removed generated sheets are preserved under
`Assets/Art/Generated/`. The deterministic splitting utilities are under `Tools/`:

- `process_sword_sheet.py`
- `process_fusion_sheets.py`
- `process_enemy_sheet.py`
- `process_elite_sheet.py`
- `process_boss_sheet.py`
- The protagonist source uses the repository-independent ImageGen chroma-key removal
  workflow and is preserved as `Assets/Art/Generated/HeroSwordsmith-source.png`.

They crop by the authored grid, trim transparent bounds, fit the art into a 512×512
canvas, and retain alpha. The Unity asset importer enforces sprite mode, alpha
transparency, no mipmaps, and mobile texture compression.

## Review policy

Before release, every asset should receive a final human review for visual defects,
unintended symbols, store-rating suitability, and consistency at phone scale. No
third-party franchise images are authorized for the shipping build.
