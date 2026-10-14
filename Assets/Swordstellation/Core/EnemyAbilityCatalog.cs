using System;
using System.Collections.Generic;

namespace Swordstellation.Core
{
    public enum EnemyAbilityType
    {
        None,
        ShieldPulse,
        StormCharge,
        MirrorWard,
        RootRegeneration,
        SolarDrain,
        MoonBlink,
        ForgeQuake,
        GlacierStorm,
        ClockworkRush,
        WorldrootBloom,
        ConstellationDevour
    }

    [Serializable]
    public sealed class EnemyAbilityDefinition
    {
        public string EnemyId;
        public EnemyAbilityType Type;
        public string NameKo;
        public string NameEn;
        public string DescriptionKo;
        public string DescriptionEn;
        public float InitialDelay;
        public float Cooldown;
        public float Strength;
        public float Duration;

        public EnemyAbilityDefinition(
            string enemyId,
            EnemyAbilityType type,
            string nameKo,
            string nameEn,
            string descriptionKo,
            string descriptionEn,
            float initialDelay,
            float cooldown,
            float strength,
            float duration = 0f)
        {
            EnemyId = enemyId;
            Type = type;
            NameKo = nameKo;
            NameEn = nameEn;
            DescriptionKo = descriptionKo;
            DescriptionEn = descriptionEn;
            InitialDelay = initialDelay;
            Cooldown = cooldown;
            Strength = strength;
            Duration = duration;
        }
    }

    public static class EnemyAbilityCatalog
    {
        private static readonly IReadOnlyDictionary<string, EnemyAbilityDefinition>
            Definitions = new Dictionary<string, EnemyAbilityDefinition>
            {
                ["elite_iron_golem"] = Ability(
                    "elite_iron_golem",
                    EnemyAbilityType.ShieldPulse,
                    "철갑 전개",
                    "Iron Bulwark",
                    "최대 체력의 14%만큼 보호막을 얻습니다.",
                    "Gains a shield equal to 14% of maximum health.",
                    2.8f,
                    7.5f,
                    0.14f),
                ["elite_storm_ram"] = Ability(
                    "elite_storm_ram",
                    EnemyAbilityType.StormCharge,
                    "폭풍 돌진",
                    "Storm Charge",
                    "잠시 이동 속도가 80% 증가합니다.",
                    "Briefly gains 80% movement speed.",
                    3.2f,
                    7.2f,
                    0.80f,
                    1.6f),
                ["elite_mirror_mage"] = Ability(
                    "elite_mirror_mage",
                    EnemyAbilityType.MirrorWard,
                    "거울 장막",
                    "Mirror Ward",
                    "피해를 흡수하는 거울 보호막을 얻습니다.",
                    "Raises a mirror ward that absorbs incoming damage.",
                    3.8f,
                    8.4f,
                    0.11f),
                ["elite_root_guard"] = Ability(
                    "elite_root_guard",
                    EnemyAbilityType.RootRegeneration,
                    "뿌리 재생",
                    "Root Regeneration",
                    "잃은 체력 일부를 회복합니다.",
                    "Regenerates a portion of missing health.",
                    4.2f,
                    8.8f,
                    0.09f),
                ["elite_sun_beetle"] = Ability(
                    "elite_sun_beetle",
                    EnemyAbilityType.SolarDrain,
                    "태양 흡수",
                    "Solar Drain",
                    "단조력 8을 태워 보호막으로 바꿉니다.",
                    "Burns 8 forge energy and converts it into a shield.",
                    3.5f,
                    8.0f,
                    8f),
                ["elite_moon_owl"] = Ability(
                    "elite_moon_owl",
                    EnemyAbilityType.MoonBlink,
                    "달빛 도약",
                    "Moon Blink",
                    "경로를 건너뛰어 검의 사거리에서 벗어납니다.",
                    "Skips ahead on the loop to evade sword ranges.",
                    3.0f,
                    7.7f,
                    0.55f),
                ["boss_forge_colossus"] = Ability(
                    "boss_forge_colossus",
                    EnemyAbilityType.ForgeQuake,
                    "용광로 진동",
                    "Forge Quake",
                    "검의 공격을 늦추고 강철 보호막을 얻습니다.",
                    "Delays sword attacks and gains a molten shield.",
                    3.5f,
                    8.2f,
                    0.12f),
                ["boss_glacier_whale"] = Ability(
                    "boss_glacier_whale",
                    EnemyAbilityType.GlacierStorm,
                    "빙하 폭풍",
                    "Glacier Storm",
                    "모든 검의 다음 공격을 늦춥니다.",
                    "Delays the next attack of every sword.",
                    4.0f,
                    9.0f,
                    1.25f,
                    2.5f),
                ["boss_clockwork_dragon"] = Ability(
                    "boss_clockwork_dragon",
                    EnemyAbilityType.ClockworkRush,
                    "태엽 폭주",
                    "Clockwork Rush",
                    "태엽을 풀어 빠르게 전진합니다.",
                    "Unwinds its spring and rushes forward.",
                    3.6f,
                    7.4f,
                    1.05f,
                    2.0f),
                ["boss_worldroot_guardian"] = Ability(
                    "boss_worldroot_guardian",
                    EnemyAbilityType.WorldrootBloom,
                    "세계수 개화",
                    "Worldroot Bloom",
                    "자신과 살아있는 적들의 체력을 회복합니다.",
                    "Restores health to itself and living allies.",
                    4.4f,
                    9.4f,
                    0.07f),
                ["boss_constellation_eater"] = Ability(
                    "boss_constellation_eater",
                    EnemyAbilityType.ConstellationDevour,
                    "별자리 포식",
                    "Constellation Devour",
                    "단조력과 궁극기 충전을 삼킵니다.",
                    "Devours forge energy and ultimate charge.",
                    3.3f,
                    7.8f,
                    12f)
            };

        public static int Count => Definitions.Count;

        public static EnemyAbilityDefinition Get(string enemyId)
        {
            return !string.IsNullOrWhiteSpace(enemyId) &&
                   Definitions.TryGetValue(enemyId, out EnemyAbilityDefinition value)
                ? value
                : null;
        }

        public static IEnumerable<EnemyAbilityDefinition> All =>
            Definitions.Values;

        private static EnemyAbilityDefinition Ability(
            string enemyId,
            EnemyAbilityType type,
            string nameKo,
            string nameEn,
            string descriptionKo,
            string descriptionEn,
            float initialDelay,
            float cooldown,
            float strength,
            float duration = 0f)
        {
            return new EnemyAbilityDefinition(
                enemyId,
                type,
                nameKo,
                nameEn,
                descriptionKo,
                descriptionEn,
                initialDelay,
                cooldown,
                strength,
                duration);
        }
    }
}
