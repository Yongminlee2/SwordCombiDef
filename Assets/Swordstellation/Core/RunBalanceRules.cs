using System;

namespace Swordstellation.Core
{
    public sealed class WavePlan
    {
        public int Wave;
        public int SpawnCount;
        public float SpawnInterval;
        public float NormalHealthMultiplier;
        public float EliteHealthMultiplier;
        public float BossHealthMultiplier;
        public float SpeedMultiplier;
        public bool HasElite;
        public bool HasBoss;
    }

    public static class RunBalanceRules
    {
        public const int MaximumStars = 5;

        // Critical hits: 10% at one star, +2% per extra star, double damage.
        public const float CriticalMultiplier = 2f;
        // A paid summon sometimes arrives already ranked up.
        public const float JackpotSummonChance = 0.05f;

        public static float CriticalChance(int stars)
        {
            return 0.10f + 0.02f * (Math.Max(1, stars) - 1);
        }

        public static int SummonStars(float roll)
        {
            return roll < JackpotSummonChance ? 2 : 1;
        }
        public const int MaximumUpgradeLevel = 30;
        public const int MaximumSwordMasteryLevel = 20;
        public const float MaximumEnergy = 140f;

        public static int TotalWaves(RunMode mode)
        {
            return mode switch
            {
                RunMode.Daily => 15,
                RunMode.Endless => int.MaxValue,
                _ => 15
            };
        }

        public static WavePlan CreateWavePlan(int wave)
        {
            int safeWave = Math.Max(1, wave);
            // A run is 15 waves that walk the old 30-wave curve two steps at a
            // time: no dull early stretch, the climb is felt every wave.
            int depth = safeWave * 2;
            float growth = LateWaveHealthGrowth(depth);
            return new WavePlan
            {
                Wave = safeWave,
                SpawnCount = (int)Math.Round(
                    (9 +
                     Math.Ceiling(
                         Math.Min(depth, 30) * 0.70f)) *
                    SpawnScale),
                SpawnInterval = Math.Max(
                    0.24f,
                    SpawnIntervalStart - depth * 0.0075f),
                NormalHealthMultiplier =
                    (0.50f +
                     depth * 0.085f +
                     Math.Max(0, depth - 20) * 0.012f) *
                    growth,
                EliteHealthMultiplier =
                    (0.70f + depth * 0.09f) * growth,
                BossHealthMultiplier =
                    (0.22f + depth * 0.027f) *
                    (1f + (growth - 1f) * 0.5f),
                SpeedMultiplier = 1.15f + depth * 0.011f,
                HasElite = safeWave % 3 == 0 &&
                           safeWave % 5 != 0,
                HasBoss = safeWave % 5 == 0
            };
        }

        // Faster summoning lets a good board reach heroic and legendary swords,
        // so health climbs after the early waves to keep those forges needed.
        public static float LateWaveHealthGrowth(int wave)
        {
            int late = Math.Max(0, Math.Min(wave, 30) - LateWaveStart);
            return 1f + late * late * LateWaveHealthCurve;
        }

        // A boss left alive this long goes on a rampage and weighs as ten enemies.
        public const float BossTimeLimit = 40f;
        public const int RampagePressure = 10;
        // Tempo knobs for the 15-wave run (tuned with Tools/CoreSmoke).
        public const float SpawnScale = 1.5f;
        public const float SpawnIntervalStart = 0.46f;
        public const float KillGoldScale = 2.0f;
        public const int LateWaveStart = 6;
        public const float LateWaveHealthCurve = 0.014f;

        public static int CoreHealth(int guardianshipLevel)
        {
            return 20 + Math.Max(0, guardianshipLevel) * 2;
        }

        public const int BaseSummonCost = 30;
        public const int SummonCostStep = 2;
        public const int MaximumForgeChain = 3;

        public static int StartingGold(int smithingLevel)
        {
            return 150 + Math.Max(0, smithingLevel) * 5;
        }

        // Cheap, slowly rising summons keep swords flowing; the board's slot
        // limit, not gold, is what pushes the player to combine. Each forge
        // chain step takes 10% off, up to 30%.
        public static int SummonCost(
            int summonCount,
            int smithingLevel = 0,
            int runeDiscount = 0,
            int forgeChain = 0)
        {
            int cost = Math.Max(
                15,
                BaseSummonCost +
                SummonCostStep * Math.Max(0, summonCount) -
                Math.Max(0, smithingLevel) -
                Math.Max(0, runeDiscount));
            int chain = Math.Max(
                0,
                Math.Min(MaximumForgeChain, forgeChain));
            return cost - cost * chain / 10;
        }

        // Selling returns half of the summons a sword represents, so it frees
        // a blocked board without ever paying more than re-summoning costs.
        public static int SellValue(
            SwordRarity rarity,
            int stars,
            int currentSummonCost)
        {
            int tier = Math.Max(0, Math.Min(4, (int)rarity));
            int rank = Math.Max(1, Math.Min(MaximumStars, stars));
            int summons = (1 << tier) * (1 << (rank - 1));
            return Math.Max(1, currentSummonCost * summons / 2);
        }

        public static int BoardSlotCount(int guardianshipLevel)
        {
            return Math.Min(
                RunInventory.MaximumSlotCount,
                RunInventory.SlotCount +
                Math.Max(0, guardianshipLevel) / 10);
        }

        public static float AdvancedSummonChance(
            int smithingLevel)
        {
            return Math.Min(
                0.12f,
                Math.Max(0, smithingLevel) * 0.004f);
        }

        public static float EnergyRegeneration(int smithingLevel, float runeMultiplier = 1f)
        {
            float baseRegeneration = 2.1f + Math.Max(0, smithingLevel) * 0.06f;
            return baseRegeneration * Math.Max(0.1f, runeMultiplier);
        }

        public static float StarDamageMultiplier(int stars)
        {
            return 1f + (Math.Max(1, stars) - 1) * 0.78f;
        }

        public static float CombinationPressureMultiplier(
            int stars,
            SwordRarity rarity,
            int wave)
        {
            if (stars > 1 ||
                rarity > SwordRarity.Common ||
                wave <= 5)
            {
                return 1f;
            }

            return Math.Max(
                0.20f,
                1f - (wave - 5) * 0.20f);
        }

        public static float SwordControlDamageMultiplier(int swordControlLevel)
        {
            return 1f + Math.Max(0, swordControlLevel) * 0.04f;
        }

        public static float SwordControlSpeedMultiplier(int swordControlLevel)
        {
            return 1f + Math.Max(0, swordControlLevel) * 0.025f;
        }

        public static float SwordMasteryDamageMultiplier(
            int masteryLevel)
        {
            int level = Math.Max(
                0,
                Math.Min(
                    MaximumSwordMasteryLevel,
                    masteryLevel));
            return 1f + level * 0.025f;
        }

        public static int SwordMasteryCost(
            int currentLevel)
        {
            int level = Math.Max(
                0,
                Math.Min(
                    MaximumSwordMasteryLevel,
                    currentLevel));
            return 60 * (level + 1);
        }

        public static int UpgradeCost(int currentLevel)
        {
            return 100 * (Math.Max(0, currentLevel) + 1);
        }

        public static int UpgradeCatalystCost(int currentLevel)
        {
            int safeLevel = Math.Max(0, currentLevel);
            return safeLevel < 10 ? 0 : 1 + (safeLevel - 10) / 5;
        }

        public static int UpgradeBossCoreCost(int currentLevel)
        {
            return Math.Max(0, currentLevel) < 20 ? 0 : 1;
        }

        public static int ExperienceForLevel(int level)
        {
            return 80 + Math.Max(1, level) * 35;
        }
    }
}
