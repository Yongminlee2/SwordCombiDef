"""Generate small original procedural WAV assets for Swordstellation."""

from __future__ import annotations

import math
from pathlib import Path
import random
import struct
import wave


SAMPLE_RATE = 44_100
OUTPUT_DIRECTORY = (
    Path(__file__).resolve().parents[1] / "Assets" / "Resources" / "Audio"
)


def envelope(time: float, duration: float, attack: float = 0.02, release: float = 0.16) -> float:
    attack_gain = min(1.0, time / max(0.001, attack))
    release_gain = min(1.0, (duration - time) / max(0.001, release))
    return max(0.0, attack_gain * release_gain)


def tone(frequency: float, time: float, phase: float = 0.0) -> float:
    return math.sin(math.tau * frequency * time + phase)


def write_wav(filename: str, duration: float, sample_function) -> None:
    OUTPUT_DIRECTORY.mkdir(parents=True, exist_ok=True)
    frames = bytearray()
    sample_count = round(duration * SAMPLE_RATE)
    for index in range(sample_count):
        time = index / SAMPLE_RATE
        value = max(-1.0, min(1.0, sample_function(time, duration)))
        frames.extend(struct.pack("<h", round(value * 32767)))

    with wave.open(str(OUTPUT_DIRECTORY / filename), "wb") as output:
        output.setnchannels(1)
        output.setsampwidth(2)
        output.setframerate(SAMPLE_RATE)
        output.writeframes(frames)


def generate() -> None:
    rng = random.Random(0x51A7)
    noise = [rng.uniform(-1, 1) for _ in range(round(SAMPLE_RATE * 0.4))]

    write_wav(
        "ui_click.wav",
        0.10,
        lambda t, d: 0.20
        * envelope(t, d, 0.003, 0.07)
        * (tone(920 - t * 1800, t) + 0.35 * tone(1380, t)),
    )
    write_wav(
        "summon.wav",
        0.55,
        lambda t, d: 0.28
        * envelope(t, d, 0.01, 0.20)
        * (
            tone(330 + t * 560, t)
            + 0.55 * tone(495 + t * 780, t)
            + 0.25 * tone(990, t)
        ),
    )
    write_wav(
        "forge.wav",
        0.80,
        lambda t, d: envelope(t, d, 0.002, 0.34)
        * (
            0.27 * tone(220, t)
            + 0.18 * tone(660, t)
            + 0.12 * tone(1320, t)
            + 0.10 * noise[min(len(noise) - 1, int(t * SAMPLE_RATE / 2))]
        ),
    )
    write_wav(
        "ultimate.wav",
        1.35,
        lambda t, d: 0.30
        * envelope(t, d, 0.04, 0.38)
        * (
            tone(180 + t * 700, t)
            + 0.55 * tone(270 + t * 920, t)
            + 0.30 * tone(1080 - t * 260, t)
        ),
    )

    victory_notes = [523.25, 659.25, 783.99, 1046.50]
    write_wav(
        "victory.wav",
        2.40,
        lambda t, d: 0.25
        * envelope(t, d, 0.02, 0.38)
        * sum(
            tone(note, t) * max(0, 1 - abs(t - index * 0.38) / 0.48)
            for index, note in enumerate(victory_notes)
        ),
    )
    write_wav(
        "defeat.wav",
        1.80,
        lambda t, d: 0.25
        * envelope(t, d, 0.03, 0.55)
        * (tone(330 - t * 75, t) + 0.55 * tone(220 - t * 42, t)),
    )

    chords = [
        (220.00, 277.18, 329.63),
        (196.00, 246.94, 293.66),
        (174.61, 220.00, 261.63),
        (196.00, 246.94, 329.63),
    ]

    def music(time: float, duration: float) -> float:
        beat = 0.5
        chord = chords[int(time // 4) % len(chords)]
        step = int(time / beat)
        note = chord[step % len(chord)] * (2 if step % 6 == 5 else 1)
        pulse_time = time % beat
        pulse = math.exp(-pulse_time * 5.5)
        pad = sum(tone(freq / 2, time, index * 0.7) for index, freq in enumerate(chord))
        shimmer = tone(note, time) + 0.35 * tone(note * 2, time)
        edge = min(1.0, time / 0.5, (duration - time) / 0.5)
        return edge * (0.075 * pad + 0.11 * pulse * shimmer)

    write_wav("forge_ambient.wav", 16.0, music)
    print(f"Generated 7 original WAV files in {OUTPUT_DIRECTORY}.")


if __name__ == "__main__":
    generate()
