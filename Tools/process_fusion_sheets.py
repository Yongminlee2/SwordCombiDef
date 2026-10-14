"""Split generated fusion-sword sheets into 48 Unity-ready individual sprites."""

from pathlib import Path

from PIL import Image


PROJECT_ROOT = Path(__file__).resolve().parents[1]
GENERATED_DIRECTORY = PROJECT_ROOT / "Assets" / "Art" / "Generated"
OUTPUT_DIRECTORY = PROJECT_ROOT / "Assets" / "Resources" / "Art" / "Swords"

SHEETS = [
    (
        "RareSwordSheet.png",
        4,
        5,
        [
            "scarlet_cyclone",
            "frost_thunder_blade",
            "magma_greatsword",
            "storm_flying_blade",
            "snowbloom_woodblade",
            "life_saint_blade",
            "mountain_cleaver",
            "prismatic_blade",
            "moonless_mirror",
            "azure_mist_blade",
            "white_mist_blade",
            "thunder_resonance",
            "soundless_gale",
            "shard_sky_blade",
            "steam_echo_blade",
            "dawnroot_blade",
            "dusk_gale_blade",
            "stormstone_blade",
            "sunshard_blade",
            "ember_echo_blade",
        ],
    ),
    (
        "HeroicSwordSheet.png",
        4,
        4,
        [
            "starfall_blade",
            "aurora_edge",
            "verdant_tempest",
            "twilight_thunder",
            "crystal_winter",
            "solar_guard",
            "lunar_echo",
            "dragonbreath_blade",
            "terra_bloom",
            "phantom_gale",
            "celestial_shard",
            "storm_prism",
            "frozen_mountain",
            "forest_chorus",
            "sunset_mirage",
            "magnetic_anvil",
        ],
    ),
    (
        "EndgameSwordSheet.png",
        4,
        3,
        [
            "eclipse_twins",
            "genesis_forgeblade",
            "astral_chorus",
            "void_blossom",
            "heavenfall_blade",
            "worldroot_blade",
            "aurora_sovereign",
            "dimensional_edge",
            "infinite_orbit",
            "first_anvil",
            "sword_constellation",
            "forge_dream",
        ],
    ),
]


def split_sheet(filename: str, columns: int, rows: int, sword_ids: list[str]) -> None:
    sheet = Image.open(GENERATED_DIRECTORY / filename).convert("RGBA")
    if len(sword_ids) != columns * rows:
        raise ValueError(f"{filename}: grid and ID count do not match.")

    for index, sword_id in enumerate(sword_ids):
        column = index % columns
        row = index // columns
        left = round(column * sheet.width / columns)
        right = round((column + 1) * sheet.width / columns)
        top = round(row * sheet.height / rows)
        bottom = round((row + 1) * sheet.height / rows)
        tile = sheet.crop((left, top, right, bottom))
        content_bounds = tile.getbbox()
        if content_bounds:
            tile = tile.crop(content_bounds)

        scale = min(430 / tile.width, 470 / tile.height)
        tile = tile.resize(
            (max(1, round(tile.width * scale)), max(1, round(tile.height * scale))),
            Image.Resampling.LANCZOS,
        )
        canvas = Image.new("RGBA", (512, 512), (0, 0, 0, 0))
        canvas.alpha_composite(
            tile,
            ((canvas.width - tile.width) // 2, (canvas.height - tile.height) // 2),
        )
        canvas.save(OUTPUT_DIRECTORY / f"{sword_id}.png", optimize=True)


def main() -> None:
    OUTPUT_DIRECTORY.mkdir(parents=True, exist_ok=True)
    for sheet in SHEETS:
        split_sheet(*sheet)
    print(f"Wrote {sum(len(sheet[3]) for sheet in SHEETS)} fusion sword sprites.")


if __name__ == "__main__":
    main()
