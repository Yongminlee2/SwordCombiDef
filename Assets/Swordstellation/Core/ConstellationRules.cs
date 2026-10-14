using System.Collections.Generic;

namespace Swordstellation.Core
{
    public readonly struct ConstellationLink
    {
        public readonly int First;
        public readonly int Second;
        public readonly SwordElement Element;

        public ConstellationLink(int first, int second, SwordElement element)
        {
            First = first;
            Second = second;
            Element = element;
        }
    }

    // Swords of one element touching up/down/left/right on the 3-wide board
    // form a constellation; every sword in it hits harder the bigger it is.
    public static class ConstellationRules
    {
        public const int Columns = 3;

        public static int GroupSize(
            RunInventory inventory,
            ContentCatalog catalog,
            int slotIndex)
        {
            SwordElement? element = ElementAt(inventory, catalog, slotIndex);
            if (element == null)
            {
                return 0;
            }

            HashSet<int> seen = new HashSet<int> { slotIndex };
            Stack<int> open = new Stack<int>();
            open.Push(slotIndex);
            while (open.Count > 0)
            {
                foreach (int next in Neighbours(open.Pop(), inventory.Capacity))
                {
                    if (!seen.Contains(next) &&
                        ElementAt(inventory, catalog, next) == element)
                    {
                        seen.Add(next);
                        open.Push(next);
                    }
                }
            }

            return seen.Count;
        }

        public static float GroupBonus(int size)
        {
            return size >= 5 ? 0.40f
                : size == 4 ? 0.30f
                : size == 3 ? 0.20f
                : size == 2 ? 0.10f
                : 0f;
        }

        public static IReadOnlyList<ConstellationLink> Links(
            RunInventory inventory,
            ContentCatalog catalog)
        {
            List<ConstellationLink> links = new List<ConstellationLink>();
            for (int slot = 0; slot < inventory.Capacity; slot++)
            {
                SwordElement? element = ElementAt(inventory, catalog, slot);
                if (element == null)
                {
                    continue;
                }

                foreach (int next in Neighbours(slot, inventory.Capacity))
                {
                    if (next > slot &&
                        ElementAt(inventory, catalog, next) == element)
                    {
                        links.Add(new ConstellationLink(slot, next, element.Value));
                    }
                }
            }

            return links;
        }

        private static IEnumerable<int> Neighbours(int slot, int capacity)
        {
            int column = slot % Columns;
            if (column > 0)
            {
                yield return slot - 1;
            }
            if (column < Columns - 1 && slot + 1 < capacity)
            {
                yield return slot + 1;
            }
            if (slot - Columns >= 0)
            {
                yield return slot - Columns;
            }
            if (slot + Columns < capacity)
            {
                yield return slot + Columns;
            }
        }

        private static SwordElement? ElementAt(
            RunInventory inventory,
            ContentCatalog catalog,
            int slot)
        {
            if (slot < 0 || slot >= inventory.Capacity)
            {
                return null;
            }

            SwordInstance sword = inventory.Get(slot);
            SwordDefinition definition = sword == null
                ? null
                : catalog.GetSword(sword.DefinitionId);
            return definition == null ||
                   definition.Element == SwordElement.Neutral
                ? (SwordElement?)null
                : definition.Element;
        }
    }
}
