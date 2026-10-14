using System;
using System.Collections.Generic;
using System.Linq;

namespace Swordstellation.Core
{
    public sealed class ForgeRules
    {
        private readonly ContentCatalog _catalog;
        private readonly SeededRandom _mutationRandom;

        public ForgeRules(ContentCatalog catalog, SeededRandom mutationRandom)
        {
            _catalog = catalog ?? throw new ArgumentNullException(nameof(catalog));
            _mutationRandom = mutationRandom ??
                throw new ArgumentNullException(nameof(mutationRandom));
        }

        public ForgeResult TryCombineIdentical(IReadOnlyList<SwordInstance> swords)
        {
            if (swords == null || swords.Count != 2)
            {
                return ForgeResult.Failed("forge.need_two");
            }

            SwordInstance first = swords[0];
            if (first == null ||
                !_catalog.Swords.ContainsKey(first.DefinitionId) ||
                swords.Any(sword => sword == null))
            {
                return ForgeResult.Failed("forge.invalid_selection");
            }
            bool allMatch = swords.All(
                sword => sword.DefinitionId == first.DefinitionId && sword.Stars == first.Stars);
            if (!allMatch)
            {
                return ForgeResult.Failed("forge.two_identical_required");
            }

            if (first.Stars < 1 || first.Stars >= RunBalanceRules.MaximumStars)
            {
                return ForgeResult.Failed("forge.max_stars");
            }

            SwordInstance result = NewInstance(first.DefinitionId, first.Stars + 1);
            return ForgeResult.Succeeded("forge.star_up", result);
        }

        public ForgeResult TryRecipe(IReadOnlyList<SwordInstance> swords)
        {
            if (swords == null || swords.Count < 2 || swords.Count > 3)
            {
                return ForgeResult.Failed("forge.select_recipe_items");
            }

            if (swords.Any(sword =>
                    sword == null ||
                    !_catalog.Swords.ContainsKey(sword.DefinitionId)))
            {
                return ForgeResult.Failed("forge.invalid_selection");
            }

            string[] ingredientIds = swords.Select(sword => sword.DefinitionId).ToArray();
            RecipeDefinition recipe = _catalog.FindRecipe(ingredientIds);
            if (recipe == null)
            {
                return ForgeResult.Failed("forge.unknown_recipe");
            }

            int inheritedStars = Math.Max(1, swords.Min(sword => sword.Stars));
            SwordInstance result = NewInstance(recipe.ResultSwordId, inheritedStars);
            return ForgeResult.Succeeded(recipe.Id, result);
        }

        public SwordInstance NewInstance(string definitionId, int stars = 1)
        {
            if (string.IsNullOrWhiteSpace(definitionId) ||
                !_catalog.Swords.ContainsKey(definitionId))
            {
                throw new ArgumentException(
                    "A known sword definition is required.",
                    nameof(definitionId));
            }
            if (stars < 1 || stars > RunBalanceRules.MaximumStars)
            {
                throw new ArgumentOutOfRangeException(nameof(stars));
            }

            ulong appearanceSeed = _mutationRandom.NextUlong();
            string instanceId = $"{definitionId}-{appearanceSeed:x16}";
            return new SwordInstance(definitionId, stars, appearanceSeed, instanceId);
        }
    }
}
