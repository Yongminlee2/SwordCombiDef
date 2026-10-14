using System;

namespace Swordstellation.Core
{
    /// <summary>
    /// Small deterministic generator with stable behavior across Unity versions.
    /// Separate instances should be used for summon, combat, mutation, and rewards.
    /// </summary>
    public sealed class SeededRandom
    {
        private ulong _state;

        public SeededRandom(ulong seed)
        {
            _state = seed == 0 ? 0x9E3779B97F4A7C15UL : seed;
        }

        public ulong NextUlong()
        {
            ulong value = _state;
            value ^= value >> 12;
            value ^= value << 25;
            value ^= value >> 27;
            _state = value;
            return value * 2685821657736338717UL;
        }

        public int NextInt(int exclusiveMax)
        {
            if (exclusiveMax <= 0)
            {
                throw new ArgumentOutOfRangeException(nameof(exclusiveMax));
            }

            return (int)(NextUlong() % (ulong)exclusiveMax);
        }

        public int NextInt(int inclusiveMin, int exclusiveMax)
        {
            if (exclusiveMax <= inclusiveMin)
            {
                throw new ArgumentOutOfRangeException(nameof(exclusiveMax));
            }

            return inclusiveMin + NextInt(exclusiveMax - inclusiveMin);
        }

        public float NextFloat()
        {
            return (NextUlong() >> 40) / (float)(1UL << 24);
        }

        public float NextFloat(float inclusiveMin, float exclusiveMax)
        {
            if (exclusiveMax <= inclusiveMin)
            {
                throw new ArgumentOutOfRangeException(nameof(exclusiveMax));
            }

            return inclusiveMin + (exclusiveMax - inclusiveMin) * NextFloat();
        }

        public bool Chance(float probability)
        {
            return NextFloat() < Math.Clamp(probability, 0f, 1f);
        }
    }
}
