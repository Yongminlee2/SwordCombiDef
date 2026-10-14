using System;
using System.Collections.Generic;
using System.Linq;
using Swordstellation.Core;

internal static class Program
{
    private static void Main()
    {
        ContentCatalog catalog = new ContentCatalog();
        Require(catalog.Swords.Count == 60, "Expected 60 swords.");
        Require(catalog.Recipes.Count == 50, "Expected 50 recipes.");
        Require(catalog.NormalEnemyIds.Count == 15, "Expected 15 normal enemies.");
        Require(catalog.EliteEnemyIds.Count == 6, "Expected 6 elite enemies.");
        Require(catalog.BossEnemyIds.Count == 5, "Expected 5 bosses.");
        Require(catalog.Stages.Count == 3, "Expected 3 stages.");

        foreach (RecipeDefinition recipe in catalog.Recipes)
        {
            Require(catalog.GetSword(recipe.ResultSwordId) != null, $"Missing result: {recipe.Id}");
            Require(
                recipe.IngredientSwordIds.All(id => catalog.GetSword(id) != null),
                $"Missing ingredient: {recipe.Id}");
        }

        ForgeRules forge = new ForgeRules(catalog, new SeededRandom(42));
        SwordInstance[] identical =
        {
            forge.NewInstance("ember_blade", 2),
            forge.NewInstance("ember_blade", 2)
        };
        ForgeResult upgraded = forge.TryCombineIdentical(identical);
        Require(upgraded.Success && upgraded.Result.Stars == 3, "Star forging failed.");

        ForgeResult recipeResult = forge.TryRecipe(new[]
        {
            forge.NewInstance("gale_blade"),
            forge.NewInstance("ember_blade")
        });
        Require(
            recipeResult.Success && recipeResult.Result.DefinitionId == "scarlet_cyclone",
            "Order-independent recipe failed.");

        float oddsTotal = GachaService.Odds.Values.Sum();
        Require(Math.Abs(oddsTotal - 100f) < 0.001f, "Gacha odds must sum to 100%.");

        AppearanceResult mutation = AppearanceRules.Evaluate(0UL, SwordRarity.Mythic);
        Require(mutation.IsMutation, "A deterministic mutation seed should be recognized.");
        Require(
            AppearanceRules.Evaluate(9999UL, SwordRarity.Common).Variant ==
            AppearanceVariant.Standard,
            "A common non-mutation seed should remain standard.");

        RunInventory inventory = new RunInventory();
        Require(
            inventory.EmptyCount == 12,
            "The combination-defense board must start with 12 slots.");
        Require(
            inventory.TryAdd(forge.NewInstance("ember_blade", 1), out int firstSlot),
            "The board should accept a summoned sword.");
        Require(
            inventory.TryAdd(forge.NewInstance("ember_blade", 1), out int secondSlot),
            "The board should accept a second summoned sword.");
        Require(
            inventory.TryMergeSame(firstSlot, secondSlot, out SwordInstance merged) &&
            merged.Stars == 2 &&
            inventory.EmptyCount == 11,
            "Two-sword drag merge failed.");

        BalanceResult beginner = SimulateRuns(
            catalog,
            300,
            0,
            0xB361UL);
        BalanceResult growth = SimulateRuns(
            catalog,
            300,
            12,
            0x9A07UL);
        // The learning path: a first-time player should clear the early steps.
        foreach (LearningStep step in LearningPath.Steps)
        {
            BalanceResult path = SimulateRuns(catalog, 100, 0, 0x7A7EUL + (ulong)step.Number, LearningPath.StageId, step);
            Console.WriteLine(
                $"Step {step.Number}: {path.Wins}/{path.RunCount} wins, " +
                $"wave {path.AverageFinalWave:0.0}/{step.Waves}, {path.AverageDuration / 60f:0.0} min.");
            if (step.Number <= 4)
            {
                Require(path.Wins >= 85, $"Learning step {step.Number} is too hard for a first-time player.");
            }
        }
        int staticBoardFailureWave =
            SimulateStaticOneStarBoard(catalog);
        // Later rifts are reached with some growth, so they are probed at level 6.
        foreach (string later in new[] { "stage_frozen_clock", "stage_worldroot_void" })
        {
            BalanceResult rift = SimulateRuns(catalog, 150, 6, 0x5EEDUL, later);
            Console.WriteLine(
                $"Rift {later}: {rift.Wins}/{rift.RunCount} wins, " +
                $"wave {rift.AverageFinalWave:0.0}, {rift.AverageDuration / 60f:0.0} min.");
        }
        Console.WriteLine(
            $"Balance diagnostics: beginner {beginner.Wins}/{beginner.RunCount}, " +
            $"growth {growth.Wins}/{growth.RunCount}, " +
            $"average {beginner.AverageDuration / 60f:0.0} min, " +
            $"wave {beginner.AverageFinalWave:0.0}, " +
            $"gold earned {beginner.AverageGoldEarned:0}, " +
            $"static one-star failure wave {staticBoardFailureWave}.");
        Require(
            beginner.Attacks > 0 &&
            beginner.Kills > 0,
            "Combat simulation did not produce attacks and kills.");
        Require(
            beginner.AverageGoldEarned > 0,
            "Defeated enemies did not grant gold.");
        Require(
            beginner.Wins >= 105 &&
            beginner.Wins <= 240,
            "Beginner balance is outside the intended 35-80% win band.");
        Require(
            growth.Wins >= 270,
            "Growth build win rate is below 90%.");
        Require(
            beginner.AverageDuration >= 150 &&
            beginner.AverageDuration <= 330,
            "Average run duration is outside 2.5-5.5 minutes.");
        Require(
            staticBoardFailureWave >= 5 &&
            staticBoardFailureWave <= 12,
            "A full one-star board does not hit the intended combination wall.");

        Console.WriteLine(
            $"Core smoke passed: {catalog.Swords.Count} swords, {catalog.Recipes.Count} recipes, " +
            $"{catalog.Enemies.Count} enemies, {catalog.Stages.Count} stages.");
        Console.WriteLine(
            "Combination-defense smoke passed: 12 slots and two-sword merge.");
        Console.WriteLine(
            $"Balance sample: beginner {beginner.Wins}/{beginner.RunCount}, " +
            $"growth {growth.Wins}/{growth.RunCount}, " +
            $"average {beginner.AverageDuration / 60f:0.0} min, " +
            $"wave {beginner.AverageFinalWave:0.0}, " +
            $"gold earned {beginner.AverageGoldEarned:0}.");
    }

    private static void Require(bool condition, string message)
    {
        if (!condition)
        {
            throw new InvalidOperationException(message);
        }
    }

    private static BalanceResult SimulateRuns(
        ContentCatalog catalog,
        int count,
        int growthLevel,
        ulong seedBase,
        string stageId = "stage_starlit_forge",
        LearningStep step = null)
    {
        bool Has(GameFeature feature) => step == null || step.Has(feature);
        BalanceResult result = new BalanceResult();
        string[] summonPool =
        {
            "ember_blade",
            "frost_blade",
            "gale_blade",
            "thunder_blade",
            "stone_blade",
            "sprout_blade"
        };

        for (int run = 0; run < count; run++)
        {
            ulong seed = seedBase + (ulong)run * 7919UL;
            SeededRandom random =
                new SeededRandom(seed);
            ForgeRules forge =
                new ForgeRules(catalog, random);
            RunInventory board = new RunInventory(
                RunBalanceRules.BoardSlotCount(
                    growthLevel));
            int startingGold =
                RunBalanceRules.StartingGold(growthLevel);
            CombinationDefenseSession session =
                new CombinationDefenseSession(
                    catalog,
                    catalog.GetStage(stageId),
                    RunMode.Standard,
                    board,
                    seed ^ 0xC0B471UL,
                    growthLevel,
                    startingGold,
                    learningStep: step);
            int summons = 0;
            int spent = 0;
            int sold = 0;
            int attacks = 0;
            session.SwordAttacked += _ => attacks++;
            // Every wave end offers three engravings; the bot takes the best.
            session.IntermissionStarted += wave =>
            {
                if (!Has(GameFeature.Engravings))
                {
                    return;
                }
                RuneDefinition rune = RunContentCatalog
                    .DrawRunes(
                        random,
                        session.SelectedRuneIds,
                        wave % 5 == 0,
                        growthLevel == 0 ? 1 : 14)
                    .OrderByDescending(RuneUtility)
                    .ThenBy(item => item.Id)
                    .FirstOrDefault();
                if (rune != null)
                {
                    session.ApplyRune(rune);
                }
            };

            for (int tick = 0;
                 tick < 7200 && !session.IsEnded;
                 tick++)
            {
                if (Has(GameFeature.Ultimate) &&
                    session.UltimateReady &&
                    (session.AliveEnemyCount >= 20 ||
                     session.Enemies.Any(enemy =>
                         enemy.Definition.IsBoss)))
                {
                    session.TryUseUltimate();
                }
                if (tick % 2 == 0)
                {
                    ManageBoard(
                        session,
                        catalog,
                        forge,
                        board,
                        Has(GameFeature.Recipes));
                    int summonCost =
                        RunBalanceRules.SummonCost(
                            summons,
                            growthLevel,
                            session.SummonDiscount);
                    if (board.EmptyCount == 0 && Has(GameFeature.Sell))
                    {
                        int slot = LeastUsefulSlot(
                            catalog,
                            board);
                        SwordInstance sword = board.Get(slot);
                        int value = RunBalanceRules.SellValue(
                            catalog.GetSword(
                                sword.DefinitionId).Rarity,
                            sword.Stars,
                            summonCost);
                        if (session.Gold + value >= summonCost)
                        {
                            board.TryRemove(slot, out _);
                            session.AddGold(value);
                            sold += value;
                        }
                    }
                    if (board.EmptyCount > 0 &&
                        session.TrySpendGold(summonCost))
                    {
                        string id = summonPool[
                            random.NextInt(
                                summonPool.Length)];
                        board.TryAdd(
                            forge.NewInstance(id),
                            out _);
                        summons++;
                        spent += summonCost;
                    }
                }

                session.Advance(0.1f);
            }

            result.Wins +=
                session.State ==
                CombinationDefenseState.Victory
                    ? 1
                    : 0;
            result.Attacks += attacks;
            result.Kills += session.DefeatedEnemies;
            result.TotalDuration += session.DurationSeconds;
            result.TotalFinalWave += session.Wave;
            result.TotalGoldEarned +=
                session.Gold +
                spent -
                sold -
                startingGold;
        }

        result.RunCount = count;
        return result;
    }

    private static int SimulateStaticOneStarBoard(
        ContentCatalog catalog)
    {
        RunInventory board = new RunInventory();
        ForgeRules forge = new ForgeRules(
            catalog,
            new SeededRandom(0x51A71CUL));
        foreach (string swordId in catalog.BaseSwordIds)
        {
            board.TryAdd(
                forge.NewInstance(swordId),
                out _);
        }
        CombinationDefenseSession session =
            new CombinationDefenseSession(
                catalog,
                catalog.GetStage(
                    "stage_starlit_forge"),
                RunMode.Standard,
                board,
                0x51A71CUL,
                0,
                RunBalanceRules.StartingGold(0));
        for (int tick = 0;
             tick < 7200 &&
             !session.IsEnded;
             tick++)
        {
            session.Advance(0.1f);
        }

        return session.Wave;
    }

    private static float RuneUtility(
        RuneDefinition rune)
    {
        // Economy and hands-on engravings act in the battle screen, which the
        // bot does not drive, so they would be wasted picks here.
        switch (rune.Effect)
        {
            case RuneEffect.SellDouble:
            case RuneEffect.SellDiscount:
            case RuneEffect.SellCharge:
            case RuneEffect.RecipeCharge:
            case RuneEffect.ResonanceBoost:
            case RuneEffect.FocusDamage:
            case RuneEffect.UltimateFreeSummons:
            case RuneEffect.EarlyStar:
            case RuneEffect.EndlessChain:
                return 1f;
        }
        if (rune.Rarity != RuneRarity.Common)
        {
            return 120f + (int)rune.Rarity * 10f;
        }

        return rune.Effect switch
        {
            RuneEffect.PressureBurst =>
                100f + rune.Value,
            RuneEffect.Damage =>
                90f + rune.Value,
            RuneEffect.AttackSpeed =>
                82f + rune.Value,
            RuneEffect.ExtraTarget =>
                80f + rune.Value,
            RuneEffect.Range =>
                74f + rune.Value,
            RuneEffect.UltimatePower =>
                68f + rune.Value,
            RuneEffect.BossDamage =>
                64f + rune.Value,
            RuneEffect.EliteDamage =>
                58f + rune.Value,
            RuneEffect.UltimateCharge =>
                54f + rune.Value,
            RuneEffect.SummonDiscount =>
                48f + rune.Value,
            RuneEffect.KillGold =>
                44f + rune.Value,
            _ => rune.Value
        };
    }

    private static void ManageBoard(
        CombinationDefenseSession session,
        ContentCatalog catalog,
        ForgeRules forge,
        RunInventory board,
        bool recipes = true)
    {
        bool changed;
        do
        {
            changed = false;
            foreach (RecipeDefinition recipe in
                     catalog.Recipes.Where(_ => recipes)
                         .OrderByDescending(item =>
                             catalog.GetSword(
                                 item.ResultSwordId)
                                 .Rarity)
                         .ThenByDescending(item =>
                             catalog.GetSword(
                                 item.ResultSwordId)
                                 .BaseDamage))
            {
                if (!RecipeBoardRules.TryFindIngredientSlots(
                        board,
                        recipe,
                        out IReadOnlyList<int> slots) ||
                    (recipe.Visibility ==
                         RecipeVisibility.Hidden &&
                     !RecipeBoardRules.MeetsHiddenCondition(
                         board,
                         recipe,
                         slots)))
                {
                    continue;
                }

                ForgeResult forged =
                    forge.TryRecipe(
                        board.GetSelection(slots));
                if (forged.Success)
                {
                    board.ReplaceSelection(
                        slots,
                        forged.Result);
                    session.RegisterForge();
                    session.Modifiers.RegisterCraftedResult(
                        recipe.ResultSwordId);
                    changed = true;
                    break;
                }
            }

            if (changed)
            {
                continue;
            }

            for (int source = 0;
                 source < board.Capacity;
                 source++)
            {
                SwordInstance first = board.Get(source);
                if (first == null)
                {
                    continue;
                }
                for (int destination = source + 1;
                     destination < board.Capacity;
                     destination++)
                {
                    if (board.TryMergeSame(
                            source,
                            destination,
                            out _))
                    {
                        session.RegisterForge();
                        changed = true;
                        break;
                    }
                }
                if (changed)
                {
                    break;
                }
            }
        } while (changed);
    }

    // Sell the cheapest sword that no rank pair or recipe on the board wants.
    private static int LeastUsefulSlot(
        ContentCatalog catalog,
        RunInventory board)
    {
        List<string> ids = board.Slots
            .Where(sword => sword != null)
            .Select(sword => sword.DefinitionId)
            .ToList();
        return Enumerable
            .Range(0, board.Capacity)
            .Where(index => board.Get(index) != null)
            .OrderBy(index =>
            {
                SwordInstance sword = board.Get(index);
                int units =
                    (1 << (int)catalog.GetSword(
                        sword.DefinitionId).Rarity) *
                    (1 << (sword.Stars - 1));
                bool twin = board.Slots.Any(other =>
                    other != null &&
                    other != sword &&
                    other.DefinitionId == sword.DefinitionId &&
                    other.Stars == sword.Stars);
                int partners = catalog.Recipes
                    .Where(recipe =>
                        recipe.IngredientSwordIds.Contains(
                            sword.DefinitionId))
                    .Select(recipe =>
                        recipe.IngredientSwordIds.Count(id =>
                            id != sword.DefinitionId &&
                            ids.Contains(id)))
                    .DefaultIfEmpty(0)
                    .Max();
                return units * 4 +
                       (twin ? 6 : 0) +
                       partners * 3;
            })
            .First();
    }

    private sealed class BalanceResult
    {
        public int RunCount;
        public int Wins;
        public long Attacks;
        public long Kills;
        public double TotalDuration;
        public double TotalFinalWave;
        public double TotalGoldEarned;

        public double AverageDuration =>
            RunCount <= 0
                ? 0
                : TotalDuration / RunCount;
        public double AverageGoldEarned =>
            RunCount <= 0
                ? 0
                : TotalGoldEarned / RunCount;
        public double AverageFinalWave =>
            RunCount <= 0
                ? 0
                : TotalFinalWave / RunCount;
    }
}
