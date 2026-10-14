using System;
using System.Collections.Generic;
using System.Linq;

namespace Swordstellation.Core
{
    public sealed class MergeOpportunity
    {
        public int FirstSlot { get; }
        public int SecondSlot { get; }
        public string SwordId { get; }
        public int Stars { get; }

        public MergeOpportunity(
            int firstSlot,
            int secondSlot,
            string swordId,
            int stars)
        {
            FirstSlot = firstSlot;
            SecondSlot = secondSlot;
            SwordId = swordId;
            Stars = stars;
        }
    }

    public sealed class RecipeProgress
    {
        public RecipeDefinition Recipe { get; }
        public int MatchedIngredients { get; }
        public int RequiredIngredients { get; }
        public IReadOnlyList<string> MissingIngredientIds { get; }
        public bool IsReady => MatchedIngredients >= RequiredIngredients;

        public RecipeProgress(
            RecipeDefinition recipe,
            int matchedIngredients,
            int requiredIngredients,
            IReadOnlyList<string> missingIngredientIds)
        {
            Recipe = recipe;
            MatchedIngredients = matchedIngredients;
            RequiredIngredients = requiredIngredients;
            MissingIngredientIds = missingIngredientIds;
        }
    }

    public sealed class CombinationSnapshot
    {
        public IReadOnlyList<MergeOpportunity> MergeOpportunities { get; }
        public IReadOnlyList<RecipeProgress> SuggestedRecipes { get; }
        public int ReadyRecipeCount { get; }
        public bool HasImmediateAction =>
            MergeOpportunities.Count > 0 || ReadyRecipeCount > 0;

        public CombinationSnapshot(
            IReadOnlyList<MergeOpportunity> mergeOpportunities,
            IReadOnlyList<RecipeProgress> suggestedRecipes,
            int readyRecipeCount)
        {
            MergeOpportunities = mergeOpportunities;
            SuggestedRecipes = suggestedRecipes;
            ReadyRecipeCount = readyRecipeCount;
        }
    }

    public static class CombinationPlanner
    {
        public static CombinationSnapshot Analyze(
            RunInventory inventory,
            ContentCatalog catalog,
            IReadOnlyCollection<string> discoveredRecipeIds,
            int suggestionCount = 3)
        {
            if (inventory == null)
            {
                throw new ArgumentNullException(nameof(inventory));
            }
            if (catalog == null)
            {
                throw new ArgumentNullException(nameof(catalog));
            }

            HashSet<string> discovered = discoveredRecipeIds == null
                ? new HashSet<string>(StringComparer.Ordinal)
                : new HashSet<string>(
                    discoveredRecipeIds,
                    StringComparer.Ordinal);
            List<MergeOpportunity> merges = FindMergeOpportunities(inventory);
            List<RecipeProgress> progress = catalog.Recipes
                .Where(recipe =>
                    recipe.Visibility != RecipeVisibility.Hidden ||
                    discovered.Contains(recipe.Id))
                .Select(recipe => MeasureRecipe(inventory, recipe))
                .ToList();
            int readyCount = progress.Count(item => item.IsReady);
            RecipeProgress[] suggested = progress
                .OrderByDescending(item => item.IsReady)
                .ThenBy(item => item.RequiredIngredients - item.MatchedIngredients)
                .ThenByDescending(item => item.MatchedIngredients)
                .ThenByDescending(item =>
                    catalog.GetSword(item.Recipe.ResultSwordId)?.Rarity ??
                    SwordRarity.Common)
                .ThenBy(item => item.Recipe.Id, StringComparer.Ordinal)
                .Take(Math.Max(0, suggestionCount))
                .ToArray();

            return new CombinationSnapshot(
                merges,
                suggested,
                readyCount);
        }

        public static IReadOnlyList<string> FindResonanceCandidates(
            RunInventory inventory,
            ContentCatalog catalog,
            IReadOnlyCollection<string> discoveredRecipeIds,
            IReadOnlyCollection<string> allowedSwordIds)
        {
            if (inventory == null)
            {
                throw new ArgumentNullException(nameof(inventory));
            }
            if (catalog == null)
            {
                throw new ArgumentNullException(nameof(catalog));
            }

            HashSet<string> allowed = allowedSwordIds == null
                ? new HashSet<string>(StringComparer.Ordinal)
                : new HashSet<string>(
                    allowedSwordIds,
                    StringComparer.Ordinal);
            CombinationSnapshot snapshot = Analyze(
                inventory,
                catalog,
                discoveredRecipeIds,
                Math.Max(6, catalog.Recipes.Count));
            List<string> candidates = new List<string>();
            HashSet<string> onBoard = new HashSet<string>(
                inventory.Slots
                    .Where(sword => sword != null)
                    .Select(sword => sword.DefinitionId),
                StringComparer.Ordinal);

            // Only recipes already started on the board steer the summon;
            // a missing crafted ingredient points at its own base materials.
            foreach (RecipeProgress recipe in snapshot.SuggestedRecipes
                         .Where(item => item.MatchedIngredients > 0))
            {
                foreach (string missingId in recipe.MissingIngredientIds)
                {
                    if (allowed.Contains(missingId))
                    {
                        AddCandidate(candidates, missingId);
                        continue;
                    }

                    foreach (RecipeDefinition source in catalog.Recipes
                                 .Where(item =>
                                     item.ResultSwordId == missingId &&
                                     item.Visibility != RecipeVisibility.Hidden))
                    {
                        foreach (string materialId in source.IngredientSwordIds)
                        {
                            if (allowed.Contains(materialId) &&
                                !onBoard.Contains(materialId))
                            {
                                AddCandidate(candidates, materialId);
                            }
                        }
                    }
                }
            }

            foreach (SwordInstance sword in inventory.Slots)
            {
                if (sword != null &&
                    sword.Stars == 1 &&
                    allowed.Contains(sword.DefinitionId) &&
                    !candidates.Contains(sword.DefinitionId))
                {
                    candidates.Add(sword.DefinitionId);
                }
            }

            return candidates;
        }

        private static void AddCandidate(
            List<string> candidates,
            string swordId)
        {
            if (!candidates.Contains(swordId))
            {
                candidates.Add(swordId);
            }
        }

        public static bool IsDeadlocked(
            RunInventory inventory,
            CombinationSnapshot snapshot)
        {
            if (inventory == null)
            {
                throw new ArgumentNullException(nameof(inventory));
            }
            if (snapshot == null)
            {
                throw new ArgumentNullException(nameof(snapshot));
            }

            return inventory.EmptyCount == 0 &&
                   !snapshot.HasImmediateAction;
        }

        private static List<MergeOpportunity> FindMergeOpportunities(
            RunInventory inventory)
        {
            List<MergeOpportunity> result = new List<MergeOpportunity>();
            IEnumerable<IGrouping<string, int>> groups = Enumerable
                .Range(0, inventory.Capacity)
                .Where(index =>
                    inventory.Get(index) != null &&
                    inventory.Get(index).Stars < RunBalanceRules.MaximumStars)
                .GroupBy(index =>
                {
                    SwordInstance sword = inventory.Get(index);
                    return $"{sword.DefinitionId}|{sword.Stars}";
                });

            foreach (IGrouping<string, int> group in groups)
            {
                int[] slots = group.OrderBy(index => index).ToArray();
                for (int index = 0; index + 1 < slots.Length; index += 2)
                {
                    SwordInstance sword = inventory.Get(slots[index]);
                    result.Add(new MergeOpportunity(
                        slots[index],
                        slots[index + 1],
                        sword.DefinitionId,
                        sword.Stars));
                }
            }

            return result;
        }

        private static RecipeProgress MeasureRecipe(
            RunInventory inventory,
            RecipeDefinition recipe)
        {
            Dictionary<string, int> available = inventory.Slots
                .Where(sword => sword != null)
                .GroupBy(sword => sword.DefinitionId)
                .ToDictionary(group => group.Key, group => group.Count());
            List<string> missing = new List<string>();
            int matched = 0;

            foreach (string ingredientId in recipe.IngredientSwordIds)
            {
                if (available.TryGetValue(ingredientId, out int count) && count > 0)
                {
                    matched++;
                    available[ingredientId] = count - 1;
                }
                else
                {
                    missing.Add(ingredientId);
                }
            }

            return new RecipeProgress(
                recipe,
                matched,
                recipe.IngredientSwordIds.Length,
                missing);
        }
    }
}
