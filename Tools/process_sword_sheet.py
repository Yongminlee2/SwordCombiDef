"""Split the approved 4x3 generated base-sword sheet into Unity-ready sprites."""

from pathlib import Path

from PIL import Image


PROJECT_ROOT = Path(__file__).resolve().parents[1]
SHEET_PATH = PROJECT_ROOT / "Assets" / "Art" / "Generated" / "BaseSwordSheet.png"
OUTPUT_DIRECTORY = PROJECT_ROOT / "Assets" / "Resources" / "Art" / "Swords"
SWORD_IDS = [
    "ember_blade",
    "frost_blade",
    "gale_blade",
    "thunder_blade",
    "stone_blade",
    "sprout_blade",
    "radiant_blade",
    "twilight_blade",
    "mist_blade",
    "resonance_blade",
    "iron_shard_blade",
    "glass_blade",
]


def main() -> None:
    sheet = Image.open(SHEET_PATH).convert("RGBA")
    if sheet.size[0] % 4 or sheet.size[1] % 3:
        raise ValueError(f"Expected an exact 4x3 sheet, got {sheet.size}.")

    cell_width = sheet.size[0] // 4
    cell_height = sheet.size[1] // 3
    OUTPUT_DIRECTORY.mkdir(parents=True, exist_ok=True)

    for index, sword_id in enumerate(SWORD_IDS):
        x = (index % 4) * cell_width
        y = (index // 4) * cell_height
        tile = sheet.crop((x, y, x + cell_width, y + cell_height))
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

    print(f"Wrote {len(SWORD_IDS)} sword sprites to {OUTPUT_DIRECTORY}.")


if __name__ == "__main__":
    main()
