using System;
using System.Collections.Generic;
using System.Linq;

namespace Swordstellation.Core
{
    public enum UltimateEffect
    {
        BladeRain,
        ChronoSeal,
        ForgeNova
    }

    public enum RuneEffect
    {
        Damage,
        AttackSpeed,
        KillGold,
        SummonDiscount,
        ExtraTarget,
        Range,
        PressureBurst,
        UltimateCharge,
        UltimatePower,
        EliteDamage,
        BossDamage,
        Score,
        ForgeStack,
        StarredExtraTarget,
        EndlessChain,
        SellDouble,
        SellDiscount,
        SellCharge,
        RecipeCharge,
        DiscoveryStack,
        ResonanceBoost,
        Burn,
        ChainLightning,
        FrostSlow,
        ElementResonance,
        FocusDamage,
        UltimateFreeSummons,
        UltimateOvercharge,
        EmptySlotPower,
        EarlyStar,
        ConstellationMastery
    }

    [Serializable]
    public sealed class UltimateDefinition
    {
        public string Id;
        public string NameKo;
        public string NameEn;
        public string DescriptionKo;
        public string DescriptionEn;
        public UltimateEffect Effect;
        public float BasePower;

        public UltimateDefinition(
            string id,
            string nameKo,
            string nameEn,
            string descriptionKo,
            string descriptionEn,
            UltimateEffect effect,
            float basePower)
        {
            Id = id;
            NameKo = nameKo;
            NameEn = nameEn;
            DescriptionKo = descriptionKo;
            DescriptionEn = descriptionEn;
            Effect = effect;
            BasePower = basePower;
        }
    }

    public enum RuneRarity
    {
        Common,
        Rare,
        Legendary
    }

    public enum RuneTag
    {
        Basic,
        Forge,
        Sell,
        Recipe,
        Element,
        Hand,
        Rule
    }

    [Serializable]
    public sealed class RuneDefinition
    {
        public RuneRarity Rarity;
        public RuneTag Tag;
        public int UnlockLevel;
        public string Id;
        public string NameKo;
        public string NameEn;
        public string DescriptionKo;
        public string DescriptionEn;
        public RuneEffect Effect;
        public float Value;

        public RuneDefinition(
            string id,
            string nameKo,
            string nameEn,
            string descriptionKo,
            string descriptionEn,
            RuneEffect effect,
            float value,
            RuneRarity rarity = RuneRarity.Common,
            RuneTag tag = RuneTag.Basic,
            int unlockLevel = 0)
        {
            Id = id;
            NameKo = nameKo;
            NameEn = nameEn;
            DescriptionKo = descriptionKo;
            DescriptionEn = descriptionEn;
            Effect = effect;
            Value = value;
            Rarity = rarity;
            Tag = tag;
            UnlockLevel = unlockLevel;
        }
    }

    public sealed class RunModifierState
    {
        public float DamageMultiplier { get; private set; } = 1f;
        public float AttackSpeedMultiplier { get; private set; } = 1f;
        public float KillGoldMultiplier { get; private set; } = 1f;
        public int SummonDiscount { get; private set; }
        public int ExtraTargets { get; private set; }
        public float RangeMultiplier { get; private set; } = 1f;
        public float PendingPressureBurst { get; private set; }
        public float UltimateChargeMultiplier { get; private set; } = 1f;
        public float UltimatePowerMultiplier { get; private set; } = 1f;
        public float EliteDamageMultiplier { get; private set; } = 1f;
        public float BossDamageMultiplier { get; private set; } = 1f;
        public float ScoreMultiplier { get; private set; } = 1f;
        public int ForgeStacks { get; private set; }
        public float ForgeDamageBonus => ForgeStacks * 0.02f;
        public float DiscoveryDamageBonus => _craftedResults.Count * 0.04f;
        public float ChainDamageBonus =>
            Has(RuneEffect.EndlessChain)
                ? Math.Min(0.30f, _forgeChain * 0.05f)
                : 0f;

        private readonly HashSet<RuneEffect> _effects =
            new HashSet<RuneEffect>();
        private readonly HashSet<string> _craftedResults =
            new HashSet<string>(StringComparer.Ordinal);
        private int _forgeChain;

        public bool Has(RuneEffect effect)
        {
            return _effects.Contains(effect);
        }

        public void RegisterForge()
        {
            if (Has(RuneEffect.ForgeStack))
            {
                ForgeStacks++;
            }
        }

        public void RegisterCraftedResult(string swordId)
        {
            if (Has(RuneEffect.DiscoveryStack) &&
                !string.IsNullOrEmpty(swordId))
            {
                _craftedResults.Add(swordId);
            }
        }

        public void SetForgeChain(int chain)
        {
            _forgeChain = Math.Max(0, chain);
        }

        public void AddSummonDiscount(int amount)
        {
            SummonDiscount += Math.Max(0, amount);
        }

        public void Apply(RuneDefinition rune)
        {
            if (rune == null)
            {
                throw new ArgumentNullException(nameof(rune));
            }

            _effects.Add(rune.Effect);
            switch (rune.Effect)
            {
                case RuneEffect.Damage:
                    DamageMultiplier += rune.Value;
                    break;
                case RuneEffect.AttackSpeed:
                    AttackSpeedMultiplier += rune.Value;
                    break;
                case RuneEffect.KillGold:
                    KillGoldMultiplier += rune.Value;
                    break;
                case RuneEffect.SummonDiscount:
                    SummonDiscount += Math.Max(0, (int)Math.Round(rune.Value));
                    break;
                case RuneEffect.ExtraTarget:
                    ExtraTargets += Math.Max(0, (int)Math.Round(rune.Value));
                    break;
                case RuneEffect.Range:
                    RangeMultiplier += rune.Value;
                    break;
                case RuneEffect.PressureBurst:
                    PendingPressureBurst +=
                        Math.Max(0f, rune.Value);
                    break;
                case RuneEffect.UltimateCharge:
                    UltimateChargeMultiplier += rune.Value;
                    break;
                case RuneEffect.UltimatePower:
                    UltimatePowerMultiplier += rune.Value;
                    break;
                case RuneEffect.EliteDamage:
                    EliteDamageMultiplier += rune.Value;
                    break;
                case RuneEffect.BossDamage:
                    BossDamageMultiplier += rune.Value;
                    break;
                case RuneEffect.Score:
                    ScoreMultiplier += rune.Value;
                    break;
                case RuneEffect.UltimateOvercharge:
                    UltimatePowerMultiplier += rune.Value;
                    UltimateChargeMultiplier -= 0.2f;
                    break;
                case RuneEffect.ForgeStack:
                case RuneEffect.StarredExtraTarget:
                case RuneEffect.EndlessChain:
                case RuneEffect.SellDouble:
                case RuneEffect.SellDiscount:
                case RuneEffect.SellCharge:
                case RuneEffect.RecipeCharge:
                case RuneEffect.DiscoveryStack:
                case RuneEffect.ResonanceBoost:
                case RuneEffect.Burn:
                case RuneEffect.ChainLightning:
                case RuneEffect.FrostSlow:
                case RuneEffect.ElementResonance:
                case RuneEffect.FocusDamage:
                case RuneEffect.UltimateFreeSummons:
                case RuneEffect.EmptySlotPower:
                case RuneEffect.EarlyStar:
                case RuneEffect.ConstellationMastery:
                    // Flag effects: read through Has() where they apply.
                    break;
                default:
                    throw new ArgumentOutOfRangeException();
            }
        }

        public float ConsumePressureBurst()
        {
            float burst = PendingPressureBurst;
            PendingPressureBurst = 0f;
            return burst;
        }
    }

    public static class RunContentCatalog
    {
        private static readonly UltimateDefinition[] UltimateItems =
        {
            new UltimateDefinition(
                "blade_rain",
                "검우",
                "Blade Rain",
                "모든 적에게 별빛 검을 쏟아붓습니다.",
                "Rain starlight blades on every enemy.",
                UltimateEffect.BladeRain,
                780f),
            new UltimateDefinition(
                "chrono_seal",
                "시간 봉인",
                "Chrono Seal",
                "모든 적에게 피해를 주고 이동을 크게 늦춥니다.",
                "Damage every enemy and greatly slow their advance.",
                UltimateEffect.ChronoSeal,
                390f),
            new UltimateDefinition(
                "forge_nova",
                "대장간 신성",
                "Forge Nova",
                "모든 적을 태우고 소환 골드 75를 회수합니다.",
                "Burn every enemy and recover 75 summon gold.",
                UltimateEffect.ForgeNova,
                520f)
        };

        private static readonly RuneDefinition[] RuneItems =
        {
            Rune("keen_edge", "예리한 날", "Keen Edge", "검 피해 +12%", "Sword damage +12%", RuneEffect.Damage, 0.12f),
            Rune("star_honing", "별빛 연마", "Star Honing", "검 피해 +18%", "Sword damage +18%", RuneEffect.Damage, 0.18f),
            Rune("swift_sheath", "빠른 칼집", "Swift Sheath", "공격 속도 +10%", "Attack speed +10%", RuneEffect.AttackSpeed, 0.10f),
            Rune("gale_bearing", "질풍 베어링", "Gale Bearing", "공격 속도 +16%", "Attack speed +16%", RuneEffect.AttackSpeed, 0.16f),
            Rune("ether_spring", "황금 불씨", "Golden Ember", "처치 골드 +18%", "Kill gold +18%", RuneEffect.KillGold, 0.18f),
            Rune("resonant_bell", "풍요의 종", "Bell of Plenty", "처치 골드 +28%", "Kill gold +28%", RuneEffect.KillGold, 0.28f),
            Rune("frugal_hammer", "절약 망치", "Frugal Hammer", "소환 비용 -2", "Summon cost -2", RuneEffect.SummonDiscount, 2f),
            Rune("master_mold", "명장의 거푸집", "Master Mold", "소환 비용 -3", "Summon cost -3", RuneEffect.SummonDiscount, 3f),
            Rune("echo_fork", "메아리 갈래", "Echo Fork", "공격 대상 +1", "Attack targets +1", RuneEffect.ExtraTarget, 1f),
            Rune("constellation_link", "별자리 연결", "Constellation Link", "공격 대상 +1", "Attack targets +1", RuneEffect.ExtraTarget, 1f),
            Rune("ward_plate", "사거리 각인", "Range Inscription", "모든 검 사거리 +8%", "All sword range +8%", RuneEffect.Range, 0.08f),
            Rune("guardian_sigil", "천리안 문양", "Far-Sight Sigil", "모든 검 사거리 +13%", "All sword range +13%", RuneEffect.Range, 0.13f),
            Rune("repair_moss", "정화의 별가루", "Purging Stardust", "현재 적에게 최대 체력 22% 피해", "Deal 22% max health to current enemies", RuneEffect.PressureBurst, 0.22f),
            Rune("phoenix_ember", "불사조 섬광", "Phoenix Flash", "현재 적에게 최대 체력 36% 피해", "Deal 36% max health to current enemies", RuneEffect.PressureBurst, 0.36f),
            Rune("starlight_reservoir", "별빛 저장고", "Starlight Reservoir", "궁극기 충전 +20%", "Ultimate charge +20%", RuneEffect.UltimateCharge, 0.20f),
            Rune("resonant_heart", "공명의 심장", "Resonant Heart", "궁극기 충전 +30%", "Ultimate charge +30%", RuneEffect.UltimateCharge, 0.30f),
            Rune("falling_star_core", "낙성 핵", "Falling Star Core", "궁극기 위력 +22%", "Ultimate power +22%", RuneEffect.UltimatePower, 0.22f),
            Rune("comet_lens", "혜성 렌즈", "Comet Lens", "궁극기 위력 +32%", "Ultimate power +32%", RuneEffect.UltimatePower, 0.32f),
            Rune("elite_breaker", "정예 파쇄", "Elite Breaker", "정예 피해 +28%", "Elite damage +28%", RuneEffect.EliteDamage, 0.28f),
            Rune("champion_anvil", "투사의 모루", "Champion Anvil", "정예 피해 +42%", "Elite damage +42%", RuneEffect.EliteDamage, 0.42f),
            Rune("giant_slayer", "거신 사냥", "Giant Slayer", "보스 피해 +24%", "Boss damage +24%", RuneEffect.BossDamage, 0.24f),
            Rune("rift_piercer", "균열 관통", "Rift Piercer", "보스 피해 +36%", "Boss damage +36%", RuneEffect.BossDamage, 0.36f),
            Rune("smiths_record", "대장장이의 기록", "Smith's Record", "점수 획득 +15%", "Score gain +15%", RuneEffect.Score, 0.15f),
            Rune("celestial_abacus", "천공 주판", "Celestial Abacus", "점수 획득 +25%", "Score gain +25%", RuneEffect.Score, 0.25f),

            Rune("forge_memory", "담금질 기억", "Tempered Memory", "합성할 때마다 모든 검 피해 +2%씩 누적", "Every forge adds +2% sword damage for the rest of the run", RuneEffect.ForgeStack, 0.02f, RuneRarity.Rare, RuneTag.Forge),
            Rune("twin_stars", "쌍성", "Twin Stars", "★3 이상 검은 공격 대상 +1", "Swords at ★3 or higher hit one more target", RuneEffect.StarredExtraTarget, 1f, RuneRarity.Rare, RuneTag.Forge),
            Rune("constellation_chain", "별자리 연쇄", "Endless Constellation", "조합 연쇄가 끊기지 않고, 연쇄 1당 피해 +5% (최대 30%)", "Forge chains never expire; +5% damage per chain step (max 30%)", RuneEffect.EndlessChain, 0.05f, RuneRarity.Legendary, RuneTag.Forge, 11),
            Rune("scrap_dealer", "고물상", "Scrap Dealer", "판매 골드 1.5배", "Selling pays 1.5x gold", RuneEffect.SellDouble, 1.5f, RuneRarity.Rare, RuneTag.Sell),
            Rune("rewind", "되감기", "Rewind", "판매할 때마다 소환 비용 -1씩 누적", "Every sale lowers summon cost by 1 for the rest of the run", RuneEffect.SellDiscount, 1f, RuneRarity.Rare, RuneTag.Sell, 2),
            Rune("smelter_recycle", "용광로 재활용", "Smelter Recycling", "판매한 검 등급마다 궁극기 충전 +10%", "Sold swords charge the ultimate by 10% per grade", RuneEffect.SellCharge, 10f, RuneRarity.Legendary, RuneTag.Sell, 9),
            Rune("arcane_tome", "비전서", "Arcane Tome", "레시피로 제작하면 궁극기 충전 +25%", "Recipe forges charge the ultimate by 25%", RuneEffect.RecipeCharge, 25f, RuneRarity.Rare, RuneTag.Recipe),
            Rune("joy_of_discovery", "첫 발견의 환희", "Joy of Discovery", "이번 수호에서 처음 만든 검 종류마다 피해 +4%", "+4% damage for each new sword type forged this run", RuneEffect.DiscoveryStack, 0.04f, RuneRarity.Rare, RuneTag.Recipe, 4),
            Rune("resonant_certainty", "공명 확신", "Resonant Certainty", "소환이 조합 재료를 이끌 확률 38% → 70%", "Resonance summons find forge materials 70% of the time", RuneEffect.ResonanceBoost, 0.70f, RuneRarity.Legendary, RuneTag.Recipe, 12),
            Rune("searing_flame", "화상", "Searing Flame", "불꽃 검이 3초 동안 화상 피해를 남김", "Flame swords leave a 3-second burn", RuneEffect.Burn, 0.20f, RuneRarity.Rare, RuneTag.Element),
            Rune("chain_lightning", "연쇄 번개", "Chain Lightning", "번개 검이 처치하면 가까운 적에게 절반 피해가 튐", "Thunder kills arc half their damage to the nearest enemy", RuneEffect.ChainLightning, 0.50f, RuneRarity.Rare, RuneTag.Element, 5),
            Rune("frost_shackle", "서리 족쇄", "Frost Shackle", "서리 검에 맞은 적은 2초간 30% 느려짐", "Frost hits slow enemies by 30% for 2 seconds", RuneEffect.FrostSlow, 0.30f, RuneRarity.Rare, RuneTag.Element, 3),
            Rune("element_resonance", "원소 공명", "Elemental Resonance", "같은 원소 검이 3개 이상이면 그 원소 피해 +30%", "Three or more swords of one element deal +30% damage", RuneEffect.ElementResonance, 0.30f, RuneRarity.Legendary, RuneTag.Element, 10),
            Rune("hunters_mark", "사냥꾼의 표식", "Hunter's Mark", "집중 공격 중인 적이 받는 피해 +50%", "The focused enemy takes 50% more damage", RuneEffect.FocusDamage, 0.50f, RuneRarity.Rare, RuneTag.Hand),
            Rune("overflowing_starlight", "넘치는 별빛", "Overflowing Starlight", "궁극기를 쓰면 다음 소환 3번 무료", "Using the ultimate makes the next 3 summons free", RuneEffect.UltimateFreeSummons, 3f, RuneRarity.Rare, RuneTag.Hand, 6),
            Rune("falling_star_descent", "별똥 강림", "Starfall Descent", "궁극기 위력 +60%, 충전 속도 -20%", "Ultimate power +60%, charge speed -20%", RuneEffect.UltimateOvercharge, 0.60f, RuneRarity.Legendary, RuneTag.Hand, 13),
            Rune("empty_sky", "빈 하늘", "Empty Sky", "보드 빈칸 하나당 피해 +5%", "+5% damage for each empty board slot", RuneEffect.EmptySlotPower, 0.05f, RuneRarity.Rare, RuneTag.Rule, 7),
            Rune("early_star", "이른 별", "Early Star", "소환할 때 10% 확률로 희귀 검이 나옴", "Summons have a 10% chance to be a rare sword", RuneEffect.EarlyStar, 0.10f, RuneRarity.Rare, RuneTag.Rule, 8),
            Rune("constellation_mastery", "별자리 숙련", "Constellation Mastery", "같은 원소 검을 붙여 둔 별자리 보너스 2배", "Doubles the bonus for same-element swords placed side by side", RuneEffect.ConstellationMastery, 2f, RuneRarity.Legendary, RuneTag.Element, 14)
        };

        public static IReadOnlyList<UltimateDefinition> Ultimates => UltimateItems;
        public static IReadOnlyList<RuneDefinition> Runes => RuneItems;

        public static UltimateDefinition GetUltimate(string id)
        {
            return UltimateItems.FirstOrDefault(
                       item => string.Equals(item.Id, id, StringComparison.Ordinal)) ??
                   UltimateItems[0];
        }

        // Three distinct engravings the run does not own yet. Boss waves always
        // include one rare or legendary; otherwise grades roll 70/25/5.
        public static IReadOnlyList<RuneDefinition> DrawRunes(
            SeededRandom random,
            IReadOnlyCollection<string> owned,
            bool bossWave,
            int playerLevel = int.MaxValue)
        {
            if (random == null)
            {
                throw new ArgumentNullException(nameof(random));
            }

            List<RuneDefinition> pool = RuneItems
                .Where(rune =>
                    rune.UnlockLevel <= playerLevel &&
                    (owned == null ||
                     !owned.Contains(rune.Id)))
                .ToList();
            List<RuneDefinition> picks = new List<RuneDefinition>();
            if (bossWave)
            {
                TakeFrom(
                    pool.Where(rune =>
                            rune.Rarity >= RuneRarity.Rare)
                        .ToList(),
                    pool,
                    picks,
                    random);
            }

            while (picks.Count < 3 && pool.Count > 0)
            {
                float roll = random.NextFloat(0f, 100f);
                RuneRarity rarity = roll < 5f
                    ? RuneRarity.Legendary
                    : roll < 30f
                        ? RuneRarity.Rare
                        : RuneRarity.Common;
                List<RuneDefinition> tier = pool
                    .Where(rune => rune.Rarity == rarity)
                    .ToList();
                TakeFrom(
                    tier.Count > 0 ? tier : pool,
                    pool,
                    picks,
                    random);
            }

            return picks;
        }

        private static void TakeFrom(
            List<RuneDefinition> candidates,
            List<RuneDefinition> pool,
            List<RuneDefinition> picks,
            SeededRandom random)
        {
            if (candidates.Count == 0)
            {
                return;
            }

            RuneDefinition pick =
                candidates[random.NextInt(candidates.Count)];
            picks.Add(pick);
            pool.Remove(pick);
        }

        // The engraving the next smith level opens, or null when all are open.
        public static RuneDefinition NextUnlock(int playerLevel)
        {
            return RuneItems
                .Where(rune => rune.UnlockLevel > playerLevel)
                .OrderBy(rune => rune.UnlockLevel)
                .FirstOrDefault();
        }

        public static IReadOnlyList<RuneDefinition> UnlockedBetween(
            int fromLevelExclusive,
            int toLevelInclusive)
        {
            return RuneItems
                .Where(rune =>
                    rune.UnlockLevel > fromLevelExclusive &&
                    rune.UnlockLevel <= toLevelInclusive)
                .ToList();
        }

        public static RuneDefinition GetRune(string id)
        {
            return RuneItems.FirstOrDefault(
                item => string.Equals(item.Id, id, StringComparison.Ordinal));
        }

        private static RuneDefinition Rune(
            string id,
            string nameKo,
            string nameEn,
            string descriptionKo,
            string descriptionEn,
            RuneEffect effect,
            float value,
            RuneRarity rarity = RuneRarity.Common,
            RuneTag tag = RuneTag.Basic,
            int unlockLevel = 0)
        {
            return new RuneDefinition(
                id,
                nameKo,
                nameEn,
                descriptionKo,
                descriptionEn,
                effect,
                value,
                rarity,
                tag,
                unlockLevel);
        }
    }
}
