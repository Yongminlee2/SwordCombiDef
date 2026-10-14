using System;
using System.Collections.Generic;

namespace Swordstellation.Core
{
    public static class RecipeBoardRules
    {
        public static bool TryFindIngredientSlots(
            RunInventory inventory,
            RecipeDefinition recipe,
            out IReadOnlyList<int> slotIndices)
        {
            if (inventory == null)
            {
                throw new ArgumentNullException(nameof(inventory));
            }
            if (recipe == null)
            {
                throw new ArgumentNullException(nameof(recipe));
            }

            List<int> matches = new List<int>();
            bool[] claimed = new bool[inventory.Capacity];
            foreach (string ingredientId in
                     recipe.IngredientSwordIds)
            {
                // Spend the lowest-rank copy so an automatic hidden-legend
                // reveal never eats a ★4 while a ★1 duplicate sits nearby.
                int match = -1;
                for (int slotIndex = 0;
                     slotIndex < inventory.Capacity;
                     slotIndex++)
                {
                    SwordInstance sword =
                        inventory.Get(slotIndex);
                    if (!claimed[slotIndex] &&
                        sword != null &&
                        sword.DefinitionId == ingredientId &&
                        (match < 0 ||
                         sword.Stars < inventory.Get(match).Stars))
                    {
                        match = slotIndex;
                    }
                }

                if (match < 0)
                {
                    slotIndices = Array.Empty<int>();
                    return false;
                }

                claimed[match] = true;
                matches.Add(match);
            }

            slotIndices = matches;
            return true;
        }

        public static bool MeetsHiddenCondition(
            RunInventory inventory,
            RecipeDefinition recipe,
            IReadOnlyList<int> slotIndices)
        {
            if (inventory == null)
            {
                throw new ArgumentNullException(nameof(inventory));
            }
            if (recipe == null)
            {
                throw new ArgumentNullException(nameof(recipe));
            }
            if (slotIndices == null ||
                slotIndices.Count !=
                recipe.IngredientSwordIds.Length)
            {
                return false;
            }
            if (recipe.Visibility != RecipeVisibility.Hidden)
            {
                return true;
            }

            switch (recipe.HiddenCondition)
            {
                // Legends reveal themselves the moment every heroic material
                // shares the board; a rank requirement made them unreachable
                // within one run.
                case HiddenRecipeCondition.AllIngredientsTogether:
                    return true;

                case HiddenRecipeCondition.AnyIngredientMaximumRank:
                    foreach (int slotIndex in slotIndices)
                    {
                        SwordInstance sword =
                            inventory.Get(slotIndex);
                        if (sword != null &&
                            sword.Stars >=
                            RunBalanceRules.MaximumStars)
                        {
                            return true;
                        }
                    }
                    return false;

                default:
                    return false;
            }
        }
    }
}
