using System;
using System.Collections.Generic;
using System.Linq;

namespace Swordstellation.Core
{
    [Serializable]
    public sealed class BlueprintFragmentRecord
    {
        public string SwordId;
        public int Fragments;
    }

    public sealed class GachaPullResult
    {
        public bool Success { get; }
        public string MessageKey { get; }
        public string SwordId { get; }
        public SwordRarity Rarity { get; }
        public int Fragments { get; }
        public bool Unlocked { get; }

        public GachaPullResult(
            bool success,
            string messageKey,
            string swordId = null,
            SwordRarity rarity = SwordRarity.Rare,
            int fragments = 0,
            bool unlocked = false)
        {
            Success = success;
            MessageKey = messageKey;
            SwordId = swordId;
            Rarity = rarity;
            Fragments = fragments;
            Unlocked = unlocked;
        }
    }

    public sealed class GachaService
    {
        public const int PullCost = 10;
        public const int UnlockRequirement = 30;
        public const int HighRarityPityThreshold = 30;

        private readonly ContentCatalog _catalog;
        private readonly SeededRandom _random;

        public GachaService(ContentCatalog catalog, SeededRandom random)
        {
            _catalog = catalog;
            _random = random;
        }

        public static IReadOnlyDictionary<SwordRarity, float> Odds { get; } =
            new Dictionary<SwordRarity, float>
            {
                [SwordRarity.Rare] = 70f,
                [SwordRarity.Heroic] = 22f,
                [SwordRarity.Legendary] = 7f,
                [SwordRarity.Mythic] = 1f
            };

        public GachaPullResult TryPull(PlayerSaveData save)
        {
            if (save == null)
            {
                throw new ArgumentNullException(nameof(save));
            }
            save.Normalize();
            if (save.Starsteel < PullCost)
            {
                return new GachaPullResult(false, "shop.not_enough_starsteel");
            }

            save.Starsteel -= PullCost;
            save.GachaPullsSinceHighRarity++;

            bool pityTriggered =
                save.GachaPullsSinceHighRarity >= HighRarityPityThreshold;
            SwordRarity rarity = RollRarity(pityTriggered);
            if (rarity >= SwordRarity.Legendary)
            {
                save.GachaPullsSinceHighRarity = 0;
            }

            List<SwordDefinition> candidates = _catalog.Swords.Values
                .Where(sword => sword.Rarity == rarity)
                .ToList();
            SwordDefinition selected = candidates[_random.NextInt(0, candidates.Count)];
            int fragments = rarity switch
            {
                SwordRarity.Rare => 10,
                SwordRarity.Heroic => 10,
                SwordRarity.Legendary => 12,
                SwordRarity.Mythic => 15,
                _ => 10
            };

            if (save.UnlockedSwordIds.Contains(selected.Id))
            {
                save.UniversalBlueprintFragments += fragments;
                return new GachaPullResult(
                    true,
                    "shop.pull_duplicate",
                    selected.Id,
                    rarity,
                    fragments,
                    false);
            }

            BlueprintFragmentRecord record = save.BlueprintFragments
                .FirstOrDefault(item => item.SwordId == selected.Id);
            if (record == null)
            {
                record = new BlueprintFragmentRecord { SwordId = selected.Id };
                save.BlueprintFragments.Add(record);
            }

            record.Fragments += fragments;
            bool unlocked = false;
            if (record.Fragments >= UnlockRequirement)
            {
                record.Fragments -= UnlockRequirement;
                unlocked = save.UnlockSword(selected.Id);
            }

            return new GachaPullResult(
                true,
                "shop.pull_success",
                selected.Id,
                rarity,
                fragments,
                unlocked);
        }

        public GachaPullResult TryUnlockWithUniversalFragments(
            PlayerSaveData save)
        {
            if (save == null)
            {
                throw new ArgumentNullException(nameof(save));
            }
            save.Normalize();
            if (save.UniversalBlueprintFragments < UnlockRequirement)
            {
                return new GachaPullResult(
                    false,
                    "shop.not_enough_universal");
            }

            List<SwordDefinition> candidates = _catalog.Swords.Values
                .Where(sword =>
                    sword.Rarity >= SwordRarity.Rare &&
                    !save.UnlockedSwordIds.Contains(sword.Id))
                .ToList();
            if (candidates.Count == 0)
            {
                return new GachaPullResult(
                    false,
                    "shop.all_unlocked");
            }

            SwordDefinition selected =
                candidates[_random.NextInt(0, candidates.Count)];
            save.UniversalBlueprintFragments -= UnlockRequirement;
            bool unlocked = save.UnlockSword(selected.Id);
            return new GachaPullResult(
                true,
                "shop.universal_unlocked",
                selected.Id,
                selected.Rarity,
                UnlockRequirement,
                unlocked);
        }

        private SwordRarity RollRarity(bool guaranteedHighRarity)
        {
            if (guaranteedHighRarity)
            {
                return _random.NextFloat(0f, 100f) < 12.5f
                    ? SwordRarity.Mythic
                    : SwordRarity.Legendary;
            }

            float roll = _random.NextFloat(0, 100);
            if (roll < Odds[SwordRarity.Mythic])
            {
                return SwordRarity.Mythic;
            }
            if (roll < Odds[SwordRarity.Mythic] + Odds[SwordRarity.Legendary])
            {
                return SwordRarity.Legendary;
            }
            if (roll < Odds[SwordRarity.Mythic] + Odds[SwordRarity.Legendary] + Odds[SwordRarity.Heroic])
            {
                return SwordRarity.Heroic;
            }
            return SwordRarity.Rare;
        }
    }
}
