"""Split the approved 3x2 generated boss sheet into Unity-ready sprites."""

from pathlib import Path

from PIL import Image


PROJECT_ROOT = Path(__file__).resolve().parents[1]
SHEET_PATH = PROJECT_ROOT / "Assets" / "Art" / "Generated" / "BossEnemySheet.png"
OUTPUT_DIRECTORY = PROJECT_ROOT / "Assets" / "Resources" / "Art" / "Enemies"
BOSS_IDS = [
    "boss_forge_colossus",
    "boss_glacier_whale",
    "boss_clockwork_dragon",
    "boss_worldroot_guardian",
    "boss_constellation_eater",
]


def main() -> None:
    sheet = Image.open(SHEET_PATH).convert("RGBA")
    OUTPUT_DIRECTORY.mkdir(parents=True, exist_ok=True)

    for index, boss_id in enumerate(BOSS_IDS):
        column = index % 3
        row = index // 3
        left = round(column * sheet.width / 3)
        right = round((column + 1) * sheet.width / 3)
        top = round(row * sheet.height / 2)
        bottom = round((row + 1) * sheet.height / 2)
        tile = sheet.crop((left, top, right, bottom))
        content_bounds = tile.getbbox()
        if content_bounds:
            tile = tile.crop(content_bounds)

        scale = min(460 / tile.width, 460 / tile.height)
        tile = tile.resize(
            (max(1, round(tile.width * scale)), max(1, round(tile.height * scale))),
            Image.Resampling.LANCZOS,
        )
        canvas = Image.new("RGBA", (512, 512), (0, 0, 0, 0))
        canvas.alpha_composite(
            tile,
            ((canvas.width - tile.width) // 2, (canvas.height - tile.height) // 2),
        )
        canvas.save(OUTPUT_DIRECTORY / f"{boss_id}.png", optimize=True)

    print(f"Wrote {len(BOSS_IDS)} boss sprites to {OUTPUT_DIRECTORY}.")


if __name__ == "__main__":
    main()
