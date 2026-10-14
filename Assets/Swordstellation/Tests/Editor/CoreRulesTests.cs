using System.Linq;
using NUnit.Framework;
using Swordstellation.Core;
using UnityEngine;

namespace Swordstellation.Tests
{
    public sealed class CoreRulesTests
    {
        [Test]
        public void Catalog_HasReleaseCandidateContentCounts()
        {
            ContentCatalog catalog = new ContentCatalog();

            Assert.That(catalog.Swords.Count, Is.EqualTo(60));
            Assert.That(catalog.Recipes.Count, Is.EqualTo(50));
            Assert.That(catalog.NormalEnemyIds.Count, Is.EqualTo(15));
            Assert.That(catalog.EliteEnemyIds.Count, Is.EqualTo(6));
            Assert.That(catalog.BossEnemyIds.Count, Is.EqualTo(5));
            Assert.That(catalog.Stages.Count, Is.EqualTo(3));
        }

        [Test]
        public void EveryRuntimeContentDefinitionHasArtwork()
        {
            ContentCatalog catalog = new ContentCatalog();
            foreach (SwordDefinition sword in
                     catalog.Swords.Values)
            {
                Assert.That(
                    Resources.Load<Sprite>(
                        $"Art/Swords/{sword.Id}"),
                    Is.Not.Null,
                    sword.Id);
            }
            foreach (EnemyDefinition enemy in
                     catalog.Enemies.Values)
            {
                Assert.That(
                    Resources.Load<Sprite>(
                        $"Art/Enemies/{enemy.Id}"),
                    Is.Not.Null,
                    enemy.Id);
            }

            string[] requiredUi =
            {
                "StarlightCorridorArena",
                "ButtonFrameBlue",
                "ButtonFrameViolet",
                "EnemyGaugeFrame",
                "PanelFrameBlueGold",
                "SummonIcon",
                "RecipeIcon",
                "CodexIcon"
            };
            foreach (string asset in requiredUi)
            {
                Assert.That(
                    Resources.Load<Sprite>(
                        $"Art/UI/Battle/{asset}"),
                    Is.Not.Null,
                    asset);
            }
            foreach (string fontPath in new[]
                     {
                         "Fonts/Pretendard-Bold",
                         "Fonts/Hahmlet-Bold"
                     })
            {
                Font font = Resources.Load<Font>(fontPath);
                Assert.That(font, Is.Not.Null, fontPath);
                Assert.That(font.HasCharacter('검'), Is.True, fontPath);
            }
        }

        [Test]
        public void EveryContentEntryHasKoreanAndEnglishCopy()
        {
            ContentCatalog catalog = new ContentCatalog();
            foreach (SwordDefinition sword in
                     catalog.Swords.Values)
            {
                Assert.That(sword.NameKo, Is.Not.Empty, sword.Id);
                Assert.That(sword.NameEn, Is.Not.Empty, sword.Id);
            }
            foreach (EnemyDefinition enemy in
                     catalog.Enemies.Values)
            {
                Assert.That(enemy.NameKo, Is.Not.Empty, enemy.Id);
                Assert.That(enemy.NameEn, Is.Not.Empty, enemy.Id);
            }
            foreach (RecipeDefinition recipe in
                     catalog.Recipes)
            {
                Assert.That(recipe.HintKo, Is.Not.Empty, recipe.Id);
                Assert.That(recipe.HintEn, Is.Not.Empty, recipe.Id);
            }
            foreach (StageDefinition stage in
                     catalog.Stages.Values)
            {
                Assert.That(stage.NameKo, Is.Not.Empty, stage.Id);
                Assert.That(stage.NameEn, Is.Not.Empty, stage.Id);
                Assert.That(stage.DescriptionKo, Is.Not.Empty, stage.Id);
                Assert.That(stage.DescriptionEn, Is.Not.Empty, stage.Id);
            }
            foreach (RuneDefinition rune in
                     RunContentCatalog.Runes)
            {
                Assert.That(rune.NameKo, Is.Not.Empty, rune.Id);
                Assert.That(rune.NameEn, Is.Not.Empty, rune.Id);
                Assert.That(rune.DescriptionKo, Is.Not.Empty, rune.Id);
                Assert.That(rune.DescriptionEn, Is.Not.Empty, rune.Id);
            }
        }

        [Test]
        public void TwoIdenticalSwords_IncreaseStarLevel()
        {
            ContentCatalog catalog = new ContentCatalog();
            ForgeRules forge = new ForgeRules(catalog, new SeededRandom(123));
            SwordInstance[] ingredients =
            {
                forge.NewInstance("ember_blade", 2),
                forge.NewInstance("ember_blade", 2)
            };

            ForgeResult result = forge.TryCombineIdentical(ingredients);

            Assert.That(result.Success, Is.True);
            Assert.That(result.Result.DefinitionId, Is.EqualTo("ember_blade"));
            Assert.That(result.Result.Stars, Is.EqualTo(3));
        }

        [Test]
        public void RecipeMatching_IsIndependentOfIngredientOrder()
        {
            ContentCatalog catalog = new ContentCatalog();
            RecipeDefinition recipe = catalog.FindRecipe(new[] { "gale_blade", "ember_blade" });

            Assert.That(recipe, Is.Not.Null);
            Assert.That(recipe.ResultSwordId, Is.EqualTo("scarlet_cyclone"));
        }

        [Test]
        public void EveryRecipe_ReferencesExistingSwords()
        {
            ContentCatalog catalog = new ContentCatalog();

            foreach (RecipeDefinition recipe in catalog.Recipes)
            {
                Assert.That(
                    recipe.IngredientSwordIds.Length,
                    Is.InRange(2, 3),
                    recipe.Id);
                Assert.That(catalog.Swords.ContainsKey(recipe.ResultSwordId), Is.True, recipe.Id);
                Assert.That(
                    recipe.IngredientSwordIds.All(catalog.Swords.ContainsKey),
                    Is.True,
                    recipe.Id);
            }
        }

        [Test]
        public void RecipeBoardRules_FindMaterialsRegardlessOfBoardOrder()
        {
            ContentCatalog catalog = new ContentCatalog();
            ForgeRules forge =
                new ForgeRules(
                    catalog,
                    new SeededRandom(44));
            RunInventory inventory = new RunInventory();
            inventory.TryAdd(
                forge.NewInstance("stone_blade"),
                out _);
            inventory.TryAdd(
                forge.NewInstance("gale_blade"),
                out int galeSlot);
            inventory.TryAdd(
                forge.NewInstance("ember_blade"),
                out int emberSlot);

            RecipeDefinition recipe =
                catalog.GetRecipe(
                    "recipe_scarlet_cyclone");
            bool found =
                RecipeBoardRules.TryFindIngredientSlots(
                    inventory,
                    recipe,
                    out System.Collections.Generic
                        .IReadOnlyList<int> slots);

            Assert.That(found, Is.True);
            Assert.That(
                slots,
                Is.EquivalentTo(
                    new[] { emberSlot, galeSlot }));
        }

        [Test]
        public void ExpandedBoardRecipesUseSlotsBeyondTwelve()
        {
            ContentCatalog catalog = new ContentCatalog();
            ForgeRules forge = new ForgeRules(
                catalog,
                new SeededRandom(45));
            RunInventory inventory =
                new RunInventory(15);
            for (int index = 0; index < 12; index++)
            {
                inventory.TryAdd(
                    forge.NewInstance("stone_blade"),
                    out _);
            }
            inventory.TryAdd(
                forge.NewInstance("ember_blade"),
                out int emberSlot);
            inventory.TryAdd(
                forge.NewInstance("gale_blade"),
                out int galeSlot);

            bool found =
                RecipeBoardRules.TryFindIngredientSlots(
                    inventory,
                    catalog.GetRecipe(
                        "recipe_scarlet_cyclone"),
                    out System.Collections.Generic
                        .IReadOnlyList<int> slots);

            Assert.That(found, Is.True);
            Assert.That(emberSlot, Is.GreaterThanOrEqualTo(12));
            Assert.That(galeSlot, Is.GreaterThanOrEqualTo(12));
            Assert.That(
                slots,
                Is.EquivalentTo(
                    new[] { emberSlot, galeSlot }));
        }

        [Test]
        public void HiddenLegend_RevealsWhenAllMaterialsShareTheBoard()
        {
            ContentCatalog catalog = new ContentCatalog();
            ForgeRules forge =
                new ForgeRules(
                    catalog,
                    new SeededRandom(88));
            RecipeDefinition recipe =
                catalog.GetRecipe(
                    "recipe_eclipse_twins");
            RunInventory partial = new RunInventory();
            partial.TryAdd(
                forge.NewInstance("solar_guard", 1),
                out _);

            Assert.That(
                RecipeBoardRules.TryFindIngredientSlots(
                    partial,
                    recipe,
                    out _),
                Is.False);

            RunInventory ready = new RunInventory();
            ready.TryAdd(
                forge.NewInstance("solar_guard", 1),
                out _);
            ready.TryAdd(
                forge.NewInstance("lunar_echo", 1),
                out _);
            RecipeBoardRules.TryFindIngredientSlots(
                ready,
                recipe,
                out System.Collections.Generic
                    .IReadOnlyList<int> readySlots);

            Assert.That(
                RecipeBoardRules.MeetsHiddenCondition(
                    ready,
                    recipe,
                    readySlots),
                Is.True);
        }

        [Test]
        public void Recipe_SpendsTheLowestRankCopyOfAnIngredient()
        {
            ContentCatalog catalog = new ContentCatalog();
            ForgeRules forge = new ForgeRules(catalog, new SeededRandom(5));
            RunInventory inventory = new RunInventory();
            inventory.TryAdd(forge.NewInstance("solar_guard", 4), out _);
            inventory.TryAdd(forge.NewInstance("lunar_echo", 1), out _);
            inventory.TryAdd(forge.NewInstance("solar_guard", 1), out int cheapSlot);

            Assert.That(
                RecipeBoardRules.TryFindIngredientSlots(
                    inventory,
                    catalog.GetRecipe("recipe_eclipse_twins"),
                    out System.Collections.Generic.IReadOnlyList<int> slots),
                Is.True);
            Assert.That(slots, Has.Member(cheapSlot));
            Assert.That(slots, Has.No.Member(0));
        }

        [Test]
        public void MythicHiddenLegend_RejectsMaterialsBelowMaximumRank()
        {
            ContentCatalog catalog = new ContentCatalog();
            ForgeRules forge = new ForgeRules(catalog, new SeededRandom(6));
            RecipeDefinition recipe = catalog.GetRecipe("recipe_forge_dream");
            RunInventory inventory = new RunInventory();
            inventory.TryAdd(forge.NewInstance("first_anvil", 4), out _);
            inventory.TryAdd(forge.NewInstance("sword_constellation", 4), out _);
            RecipeBoardRules.TryFindIngredientSlots(
                inventory,
                recipe,
                out System.Collections.Generic.IReadOnlyList<int> slots);

            Assert.That(
                RecipeBoardRules.MeetsHiddenCondition(inventory, recipe, slots),
                Is.False);
        }

        [Test]
        public void MythicHiddenLegend_RequiresMaximumRankMaterial()
        {
            ContentCatalog catalog = new ContentCatalog();
            ForgeRules forge =
                new ForgeRules(
                    catalog,
                    new SeededRandom(99));
            RecipeDefinition recipe =
                catalog.GetRecipe(
                    "recipe_forge_dream");
            RunInventory inventory = new RunInventory();
            inventory.TryAdd(
                forge.NewInstance(
                    "first_anvil",
                    RunBalanceRules.MaximumStars),
                out _);
            inventory.TryAdd(
                forge.NewInstance(
                    "sword_constellation",
                    2),
                out _);
            RecipeBoardRules.TryFindIngredientSlots(
                inventory,
                recipe,
                out System.Collections.Generic
                    .IReadOnlyList<int> slots);

            Assert.That(
                RecipeBoardRules.MeetsHiddenCondition(
                    inventory,
                    recipe,
                    slots),
                Is.True);
        }

        [Test]
        public void SeededRandom_ReplaysSameSequence()
        {
            SeededRandom first = new SeededRandom(987654321);
            SeededRandom second = new SeededRandom(987654321);

            for (int index = 0; index < 50; index++)
            {
                Assert.That(first.NextUlong(), Is.EqualTo(second.NextUlong()));
            }
        }

        [Test]
        public void AppearanceMutation_IsDeterministicAndRarityWeighted()
        {
            AppearanceResult first = AppearanceRules.Evaluate(0UL, SwordRarity.Mythic);
            AppearanceResult second = AppearanceRules.Evaluate(0UL, SwordRarity.Mythic);

            Assert.That(first.Variant, Is.EqualTo(second.Variant));
            Assert.That(first.IsMutation, Is.True);
            Assert.That(
                AppearanceRules.MutationBasisPoints(SwordRarity.Mythic),
                Is.GreaterThan(AppearanceRules.MutationBasisPoints(SwordRarity.Rare)));
        }
    }
}
