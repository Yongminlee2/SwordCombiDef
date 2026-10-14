using System;
using System.Collections.Generic;
using System.Linq;

namespace Swordstellation.Core
{
    public enum BossTreasure
    {
        StarBlessing,
        GoldHoard,
        UltimateCharge,
        FreeSummons,
        RareSword
    }

    // A defeated boss drops a chest: pick one of three different rewards.
    public static class BossTreasureRules
    {
        public const int FreeSummonCount = 3;

        public static IReadOnlyList<BossTreasure> Draw(SeededRandom random, int count = 3)
        {
            List<BossTreasure> pool = Enum.GetValues(typeof(BossTreasure))
                .Cast<BossTreasure>()
                .ToList();
            List<BossTreasure> picked = new List<BossTreasure>();
            while (picked.Count < count && pool.Count > 0)
            {
                int index = random.NextInt(pool.Count);
                picked.Add(pool[index]);
                pool.RemoveAt(index);
            }
            return picked;
        }

        public static int GoldHoard(int wave)
        {
            return 60 + Math.Max(1, wave) * 12;
        }
    }
}
