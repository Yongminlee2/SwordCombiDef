using System;
using NUnit.Framework;
using Swordstellation.Core;

namespace Swordstellation.Tests.Editor
{
    public sealed class CombinationPlannerTests
    {
        [Test]
        public void AnalyzeFindsMergeAndReadyRecipeActions()
        {
            ContentCatalog catalog = new ContentCatalog();
            RunInventory inventory = new RunInventory();
            Add(inventory, "ember_blade", 1, 1);
            Add(inventory, "ember_blade", 1, 2);
            Add(inventory, "gale_blade", 1, 3);

            CombinationSnapshot snapshot = CombinationPlanner.Analyze(
                inventory,
                catalog,
                Array.Empty<string>());

            Assert.That(snapshot.MergeOpportunities.Count, Is.EqualTo(1));
            Assert.That(snapshot.ReadyRecipeCount, Is.GreaterThanOrEqualTo(1));
            Assert.That(snapshot.HasImmediateAction, Is.True);
        }

        [Test]
        public void ResonanceCandidatesAdvanceNearRecipeOrPair()
        {
            ContentCatalog catalog = new ContentCatalog();
            RunInventory inventory = new RunInventory();
            Add(inventory, "ember_blade", 1, 1);

            var candidates = CombinationPlanner.FindResonanceCandidates(
                inventory,
                catalog,
                Array.Empty<string>(),
                catalog.BaseSwordIds);

            Assert.That(candidates, Does.Contain("ember_blade"));
            Assert.That(candidates, Does.Contain("gale_blade"));
        }

        [Test]
        public void ResonanceIgnoresRecipesNotStartedOnTheBoard()
        {
            ContentCatalog catalog = new ContentCatalog();
            RunInventory inventory = new RunInventory();
            Add(inventory, "ember_blade", 1, 1);

            var candidates = CombinationPlanner.FindResonanceCandidates(
                inventory,
                catalog,
                Array.Empty<string>(),
                catalog.BaseSwordIds);

            Assert.That(candidates, Does.Not.Contain("glass_blade"));
        }

        [Test]
        public void ResonanceReachesBaseMaterialsOfMissingRareIngredient()
        {
            ContentCatalog catalog = new ContentCatalog();
            RunInventory inventory = new RunInventory();
            Add(inventory, "sunshard_blade", 1, 1);

            var candidates = CombinationPlanner.FindResonanceCandidates(
                inventory,
                catalog,
                Array.Empty<string>(),
                catalog.BaseSwordIds);

            // Starfall needs Shard Sky, which is forged from iron and glass.
            Assert.That(candidates, Does.Contain("glass_blade"));
            Assert.That(candidates, Does.Contain("iron_shard_blade"));
        }

        [Test]
        public void SellingASwordFreesItsSlot()
        {
            RunInventory inventory = new RunInventory();
            Add(inventory, "ember_blade", 2, 1);

            Assert.That(inventory.TryRemove(0, out SwordInstance removed), Is.True);
            Assert.That(removed.DefinitionId, Is.EqualTo("ember_blade"));
            Assert.That(inventory.EmptyCount, Is.EqualTo(inventory.Capacity));
            Assert.That(inventory.TryRemove(0, out _), Is.False);
        }

        [Test]
        public void FullBoardWithoutActionIsDeadlocked()
        {
            RunInventory inventory = new RunInventory();
            for (int index = 0; index < inventory.Capacity; index++)
            {
                Add(inventory, $"unique-{index}", 1, index + 1);
            }
            CombinationSnapshot snapshot = new CombinationSnapshot(
                Array.Empty<MergeOpportunity>(),
                Array.Empty<RecipeProgress>(),
                0);

            Assert.That(
                CombinationPlanner.IsDeadlocked(inventory, snapshot),
                Is.True);
        }

        private static void Add(
            RunInventory inventory,
            string id,
            int stars,
            int seed)
        {
            Assert.That(
                inventory.TryAdd(
                    new SwordInstance(
                        id,
                        stars,
                        (ulong)seed,
                        $"test-{seed}"),
                    out _),
                Is.True);
        }
    }
}
