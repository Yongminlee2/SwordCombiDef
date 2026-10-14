using System;
using System.Collections.Generic;

namespace Swordstellation.Core
{
    public enum SwordFamily
    {
        StraightSword,
        CurvedBlade,
        Greatsword,
        Rapier,
        TwinBlades,
        MagicBlade,
        HolyBlade,
        DragonBlade,
        ReaperBlade,
        AxeBlade,
        SpearBlade,
        SpiritBlade
    }

    public static class SwordFamilyRules
    {
        private static readonly IReadOnlyDictionary<string, SwordFamily>
            BaseFamilies =
                new Dictionary<string, SwordFamily>(
                    StringComparer.Ordinal)
                {
                    ["ember_blade"] =
                        SwordFamily.StraightSword,
                    ["frost_blade"] =
                        SwordFamily.CurvedBlade,
                    ["gale_blade"] =
                        SwordFamily.Greatsword,
                    ["thunder_blade"] =
                        SwordFamily.Rapier,
                    ["stone_blade"] =
                        SwordFamily.TwinBlades,
                    ["sprout_blade"] =
                        SwordFamily.MagicBlade,
                    ["radiant_blade"] =
                        SwordFamily.HolyBlade,
                    ["twilight_blade"] =
                        SwordFamily.DragonBlade,
                    ["mist_blade"] =
                        SwordFamily.ReaperBlade,
                    ["resonance_blade"] =
                        SwordFamily.AxeBlade,
                    ["iron_shard_blade"] =
                        SwordFamily.SpearBlade,
                    ["glass_blade"] =
                        SwordFamily.SpiritBlade
                };

        public static SwordFamily Resolve(
            SwordDefinition definition)
        {
            if (definition == null)
            {
                throw new ArgumentNullException(
                    nameof(definition));
            }
            if (BaseFamilies.TryGetValue(
                    definition.Id,
                    out SwordFamily family))
            {
                return family;
            }

            uint hash = 2166136261;
            foreach (char character in definition.Id)
            {
                hash ^= character;
                hash *= 16777619;
            }

            return (SwordFamily)(
                hash %
                (uint)Enum.GetValues(
                    typeof(SwordFamily)).Length);
        }

        public static float Range(
            SwordDefinition definition)
        {
            return Resolve(definition) switch
            {
                SwordFamily.Rapier => 0.47f,
                SwordFamily.SpearBlade => 0.49f,
                SwordFamily.HolyBlade => 0.46f,
                SwordFamily.SpiritBlade => 0.46f,
                SwordFamily.Greatsword => 0.37f,
                SwordFamily.AxeBlade => 0.36f,
                SwordFamily.ReaperBlade => 0.40f,
                _ => definition.AttackStyle switch
                {
                    AttackStyle.Dash => 0.39f,
                    AttackStyle.Projectile => 0.43f,
                    AttackStyle.Chain => 0.45f,
                    AttackStyle.Area => 0.38f,
                    _ => 0.40f
                }
            };
        }

        public static string NameKo(
            SwordFamily family)
        {
            return family switch
            {
                SwordFamily.StraightSword => "직검",
                SwordFamily.CurvedBlade => "곡도",
                SwordFamily.Greatsword => "대검",
                SwordFamily.Rapier => "세검",
                SwordFamily.TwinBlades => "쌍검",
                SwordFamily.MagicBlade => "마검",
                SwordFamily.HolyBlade => "성검",
                SwordFamily.DragonBlade => "용검",
                SwordFamily.ReaperBlade => "낫검",
                SwordFamily.AxeBlade => "도끼검",
                SwordFamily.SpearBlade => "창검",
                SwordFamily.SpiritBlade => "정령검",
                _ => family.ToString()
            };
        }

        public static string NameEn(
            SwordFamily family)
        {
            return family switch
            {
                SwordFamily.StraightSword => "Straight",
                SwordFamily.CurvedBlade => "Curved",
                SwordFamily.Greatsword => "Greatsword",
                SwordFamily.Rapier => "Rapier",
                SwordFamily.TwinBlades => "Twin",
                SwordFamily.MagicBlade => "Magic",
                SwordFamily.HolyBlade => "Holy",
                SwordFamily.DragonBlade => "Dragon",
                SwordFamily.ReaperBlade => "Reaper",
                SwordFamily.AxeBlade => "Axe",
                SwordFamily.SpearBlade => "Spear",
                SwordFamily.SpiritBlade => "Spirit",
                _ => family.ToString()
            };
        }
    }
}
