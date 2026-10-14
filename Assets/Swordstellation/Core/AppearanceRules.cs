namespace Swordstellation.Core
{
    public enum AppearanceVariant
    {
        Standard,
        Aurora,
        Starforged,
        Voidglass
    }

    public sealed class AppearanceResult
    {
        public AppearanceVariant Variant { get; }
        public int AccentRgb { get; }
        public bool IsMutation => Variant != AppearanceVariant.Standard;

        public AppearanceResult(AppearanceVariant variant, int accentRgb)
        {
            Variant = variant;
            AccentRgb = accentRgb;
        }
    }

    public static class AppearanceRules
    {
        public static AppearanceResult Evaluate(ulong seed, SwordRarity rarity)
        {
            int threshold = MutationBasisPoints(rarity);
            int roll = (int)(seed % 10000UL);
            if (roll >= threshold)
            {
                return new AppearanceResult(AppearanceVariant.Standard, 0xFFFFFF);
            }

            int variantRoll = (int)((seed >> 17) % 1000UL);
            if (variantRoll < 700)
            {
                return new AppearanceResult(AppearanceVariant.Aurora, 0x71F2E6);
            }
            if (variantRoll < 950)
            {
                return new AppearanceResult(AppearanceVariant.Starforged, 0xFFD66E);
            }

            return new AppearanceResult(AppearanceVariant.Voidglass, 0xC597FF);
        }

        public static string CollectionId(SwordInstance instance)
        {
            return $"{instance.DefinitionId}:{instance.AppearanceSeed:x16}";
        }

        public static int MutationBasisPoints(SwordRarity rarity)
        {
            switch (rarity)
            {
                case SwordRarity.Mythic:
                    return 2000;
                case SwordRarity.Legendary:
                    return 1200;
                case SwordRarity.Heroic:
                    return 600;
                case SwordRarity.Rare:
                    return 300;
                default:
                    return 100;
            }
        }
    }
}
