"""Split the approved 5x3 generated normal-enemy sheet into Unity-ready sprites."""

from pathlib import Path

from PIL import Image


PROJECT_ROOT = Path(__file__).resolve().parents[1]
SHEET_PATH = PROJECT_ROOT / "Assets" / "Art" / "Generated" / "NormalEnemySheet.png"
OUTPUT_DIRECTORY = PROJECT_ROOT / "Assets" / "Resources" / "Art" / "Enemies"
ENEMY_IDS = [
    "slime_mint",
    "slime_berry",
    "paper_golem",
    "clockwork_bug",
    "cloud_puff",
    "moss_turtle",
    "lantern_imp",
    "crystal_moth",
    "acorn_knight",
    "bubble_wisp",
    "shadow_plush",
    "stone_sprout",
    "star_mouse",
    "rune_box",
    "wind_sock",
]


def main() -> None:
    sheet = Image.open(SHEET_PATH).convert("RGBA")
    OUTPUT_DIRECTORY.mkdir(parents=True, exist_ok=True)

    for index, enemy_id in enumerate(ENEMY_IDS):
        column = index % 5
        row = index // 5
        left = round(column * sheet.width / 5)
        right = round((column + 1) * sheet.width / 5)
        top = round(row * sheet.height / 3)
        bottom = round((row + 1) * sheet.height / 3)
        tile = sheet.crop((left, top, right, bottom))
        content_bounds = tile.getbbox()
        if content_bounds:
            tile = tile.crop(content_bounds)

        scale = min(440 / tile.width, 440 / tile.height)
        tile = tile.resize(
            (max(1, round(tile.width * scale)), max(1, round(tile.height * scale))),
            Image.Resampling.LANCZOS,
        )
        canvas = Image.new("RGBA", (512, 512), (0, 0, 0, 0))
        canvas.alpha_composite(
            tile,
            ((canvas.width - tile.width) // 2, (canvas.height - tile.height) // 2),
        )
        canvas.save(OUTPUT_DIRECTORY / f"{enemy_id}.png", optimize=True)

    print(f"Wrote {len(ENEMY_IDS)} enemy sprites to {OUTPUT_DIRECTORY}.")


if __name__ == "__main__":
    main()
