using System.Collections;
using System;
using System.IO;
using System.Linq;
using NUnit.Framework;
using Swordstellation.Core;
using Swordstellation.Runtime;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.SceneManagement;
using UnityEngine.TestTools;
using UnityEngine.UI;
using Object = UnityEngine.Object;

namespace Swordstellation.Tests.PlayMode
{
    public sealed class RuntimeViewPlayModeTests
    {
        [UnitySetUp]
        public IEnumerator SetUp()
        {
            Time.timeScale = 1f;
            GameBootstrap[] bootstraps =
                Object.FindObjectsByType<GameBootstrap>(
                    FindObjectsSortMode.None);
            foreach (GameBootstrap bootstrap in bootstraps)
            {
                Object.Destroy(bootstrap.gameObject);
            }
            yield return null;
        }

        [UnityTest]
        public IEnumerator UiFactoryButtonInvokesOneHandAction()
        {
            bool invoked = false;
            Canvas canvas = UiFactory.CreateCanvas(
                "UiSmokeCanvas");
            Button button = UiFactory.CreateButton(
                canvas.transform,
                "SummonButton",
                "SUMMON",
                Color.blue,
                Color.white,
                28,
                new Vector2(0.1f, 0.1f),
                new Vector2(0.9f, 0.2f),
                Vector2.zero,
                Vector2.zero,
                () => invoked = true);

            button.onClick.Invoke();
            yield return null;

            Assert.That(invoked, Is.True);
            Assert.That(
                button.GetComponentInChildren<Text>().text,
                Is.EqualTo("SUMMON"));
            Object.Destroy(canvas.gameObject);
        }

        [UnityTest]
        public IEnumerator LoopEnemyMovesAroundArena()
        {
            Canvas canvas = UiFactory.CreateCanvas(
                "LoopTestCanvas");
            GameObject arenaObject = new GameObject(
                "Arena",
                typeof(RectTransform));
            arenaObject.transform.SetParent(
                canvas.transform,
                false);
            RectTransform arena =
                arenaObject.GetComponent<RectTransform>();
            arena.sizeDelta = new Vector2(500f, 800f);

            GameObject enemyObject = new GameObject(
                "LoopEnemy",
                typeof(RectTransform),
                typeof(Image),
                typeof(LoopEnemyPrototypeView));
            enemyObject.transform.SetParent(arena, false);
            LoopEnemyPrototypeView enemy =
                enemyObject.GetComponent<LoopEnemyPrototypeView>();
            enemy.Initialize(arena, 0.1f, 0.5f);
            Vector2 initialPosition =
                enemyObject.GetComponent<RectTransform>()
                    .anchoredPosition;
            float initialProgress = enemy.Progress;

            yield return new WaitForSeconds(0.12f);

            Assert.That(
                enemy.Progress,
                Is.GreaterThan(initialProgress));
            Assert.That(
                enemyObject.GetComponent<RectTransform>()
                    .anchoredPosition,
                Is.Not.EqualTo(initialPosition));
            Object.Destroy(canvas.gameObject);
        }

        [UnityTest]
        public IEnumerator MainSceneBootstrapCreatesVisibleCanvas()
        {
            yield return LoadMainScene();

            GameBootstrap bootstrap =
                Object.FindFirstObjectByType<GameBootstrap>();
            Canvas[] canvases =
                Object.FindObjectsByType<Canvas>(
                    FindObjectsSortMode.None);

            Assert.That(bootstrap, Is.Not.Null);
            Assert.That(canvases, Is.Not.Empty);
            Assert.That(
                canvases,
                Has.Some.Matches<Canvas>(
                    canvas => canvas.enabled &&
                              canvas.gameObject
                                  .activeInHierarchy));

            GameObject safeArea = GameObject.Find("SafeArea");
            Assert.That(safeArea, Is.Not.Null);
            Rect safeRect =
                safeArea.GetComponent<RectTransform>().rect;
            Assert.That(
                safeRect.width,
                Is.GreaterThan(0f));
            Assert.That(
                safeRect.height,
                Is.GreaterThan(0f));

            GameObject background =
                GameObject.Find("Background");
            Assert.That(background, Is.Not.Null);
            Assert.That(
                background.transform.GetSiblingIndex(),
                Is.LessThan(
                    safeArea.transform.GetSiblingIndex()));
        }

        [UnityTest]
        public IEnumerator BattleCreatesRequiredBoardAndSpawningLoop()
        {
            BattleController battle = null;
            yield return LoadBattle(
                value => battle = value);
            battle.Session.Advance(1f);
            battle.Session.Advance(1f);
            battle.Session.Advance(1f);
            yield return null;

            Assert.That(battle, Is.Not.Null);
            Assert.That(
                battle.GetComponentsInChildren<
                    PrototypeSwordSlotView>(true).Length,
                Is.EqualTo(
                    battle.Inventory.Capacity));
            Assert.That(
                battle.Inventory.Capacity,
                Is.InRange(12, 15));
            Assert.That(
                battle.GetComponentsInChildren<
                    LoopEnemyPrototypeView>(true).Length,
                Is.GreaterThan(0));
            Assert.That(
                battle.GetComponentsInChildren<Button>(
                        true)
                    .Count(button =>
                        button.name ==
                            "SummonButton" ||
                        button.name ==
                            "RecipeBookButton" ||
                        button.name ==
                            "CodexButton"),
                Is.EqualTo(3));
            Assert.That(
                battle.BoardRoot,
                Is.Not.Null);
            Assert.That(
                battle.EnemyCount,
                Is.LessThan(BattleController.EnemyLimit));
        }

        [UnityTest]
        public IEnumerator SwordsAttackKillAndEarnGold()
        {
            BattleController battle = null;
            yield return LoadBattle(
                value => battle = value);
            Assert.That(battle.TrySummonSword(), Is.True);
            if (battle.IsCombining)
            {
                yield return new WaitForSecondsRealtime(
                    1.4f);
            }
            Assert.That(battle.TrySummonSword(), Is.True);
            int goldAfterSummons = battle.Gold;

            for (int index = 0;
                 index < 240 &&
                 !battle.Session.IsEnded;
                 index++)
            {
                battle.Session.Advance(0.1f);
            }
            yield return null;

            Assert.That(
                battle.Session.DamageDealt,
                Is.GreaterThan(0f));
            Assert.That(
                battle.Session.DefeatedEnemies,
                Is.GreaterThan(0));
            Assert.That(
                battle.Gold,
                Is.GreaterThan(goldAfterSummons));
        }

        [UnityTest]
        public IEnumerator SummonedPairMergesIntoHigherRank()
        {
            BattleController battle = null;
            yield return LoadBattle(
                value => battle = value);
            int goldBefore = battle.Gold;
            int firstCost = battle.SummonCost;

            Assert.That(battle.TrySummonSword(), Is.True);
            if (battle.IsCombining)
            {
                yield return new WaitForSecondsRealtime(
                    1.4f);
            }
            int secondCost = battle.SummonCost;
            Assert.That(secondCost, Is.GreaterThan(firstCost));
            Assert.That(battle.TrySummonSword(), Is.True);
            SwordInstance first = battle.Inventory.Get(0);
            SwordInstance second = battle.Inventory.Get(1);

            Assert.That(first, Is.Not.Null);
            Assert.That(second, Is.Not.Null);
            Assert.That(
                second.DefinitionId,
                Is.EqualTo(first.DefinitionId));
            // A jackpot summon may land at ★2; this test is about an equal pair.
            first.Stars = 1;
            second.Stars = 1;
            Assert.That(
                battle.TryMergeSlots(0, 1),
                Is.True);
            Assert.That(
                battle.Inventory.Get(0),
                Is.Null);
            Assert.That(
                battle.Inventory.Get(1).Stars,
                Is.EqualTo(2));
            Assert.That(
                battle.Inventory.EmptyCount,
                Is.EqualTo(11));
            Assert.That(
                battle.Gold,
                Is.EqualTo(goldBefore - firstCost - secondCost));
        }

        [UnityTest]
        public IEnumerator EveryWaveEndOffersEngravingsThatApplyOnTap()
        {
            BattleController battle = null;
            yield return LoadBattle(value => battle = value);
            for (int step = 0;
                 step < 600 && GameObject.Find("EngravingChoiceOverlay") == null;
                 step++)
            {
                battle.Session.Advance(0.1f);
            }
            yield return null;

            Assert.That(GameObject.Find("EngravingChoiceOverlay"), Is.Not.Null);
            Button first = GameObject.Find("EngravingChoice_0").GetComponent<Button>();
            first.onClick.Invoke();
            yield return null;

            Assert.That(GameObject.Find("EngravingChoiceOverlay"), Is.Null);
            Assert.That(battle.Session.SelectedRuneIds.Count, Is.EqualTo(1));
        }

        [UnityTest]
        public IEnumerator UltimateButtonFiresOnlyWhenFull()
        {
            BattleController battle = null;
            yield return LoadBattle(value => battle = value);
            for (int step = 0; step < 100 && battle.Session.Enemies.Count == 0; step++)
            {
                battle.Session.Advance(0.1f);
            }
            Button ultimate = GameObject.Find("UltimateButton").GetComponent<Button>();

            ultimate.onClick.Invoke();
            Assert.That(battle.Session.UltimatesUsed, Is.EqualTo(0));

            battle.Session.AddUltimateCharge(100f);
            yield return null;
            ultimate.onClick.Invoke();
            yield return null;

            Assert.That(battle.Session.UltimatesUsed, Is.EqualTo(1));
        }

        [UnityTest]
        public IEnumerator TappingAnEnemyFocusesIt()
        {
            BattleController battle = null;
            yield return LoadBattle(value => battle = value);
            for (int step = 0; step < 100 && battle.Session.Enemies.Count == 0; step++)
            {
                battle.Session.Advance(0.1f);
            }
            yield return null;
            LoopEnemyPrototypeView view =
                Object.FindFirstObjectByType<LoopEnemyPrototypeView>();

            ExecuteEvents.ExecuteHierarchy(
                view.transform.GetChild(view.transform.childCount - 1).gameObject,
                new PointerEventData(EventSystem.current),
                ExecuteEvents.pointerClickHandler);

            Assert.That(battle.Session.FocusedEnemy, Is.SameAs(view.State));
        }

        [UnityTest]
        public IEnumerator ScrapDealerRaisesTheSellPrice()
        {
            BattleController battle = null;
            yield return LoadBattle(value => battle = value);
            Assert.That(battle.TrySummonSword(), Is.True);
            int plain = battle.SellValue(0);

            battle.Session.ApplyRune(RunContentCatalog.GetRune("scrap_dealer"));

            Assert.That(battle.SellValue(0), Is.EqualTo(plain * 3 / 2));
        }

        [UnityTest]
        public IEnumerator SummonThenSellNeverMakesGold()
        {
            BattleController battle = null;
            yield return LoadBattle(value => battle = value);
            battle.Session.ApplyRune(RunContentCatalog.GetRune("scrap_dealer"));
            int goldBefore = battle.Gold;

            Assert.That(battle.TrySummonSword(), Is.True);
            while (battle.IsCombining)
            {
                yield return null;
            }
            // A 5% jackpot ★2 is a lucky gain; the rule covers ordinary summons.
            battle.Inventory.Get(0).Stars = 1;
            Assert.That(battle.TrySellSlot(0), Is.True);

            Assert.That(battle.Gold, Is.LessThanOrEqualTo(goldBefore));
        }

        [UnityTest]
        public IEnumerator FreeSummonsDoNotMakeReforgeFree()
        {
            BattleController battle = null;
            yield return LoadBattle(value => battle = value);
            battle.Session.ApplyRune(RunContentCatalog.GetRune("overflowing_starlight"));
            for (int step = 0; step < 100 && battle.Session.Enemies.Count == 0; step++)
            {
                battle.Session.Advance(0.1f);
            }
            battle.Session.AddUltimateCharge(100f);
            GameObject.Find("UltimateButton").GetComponent<Button>().onClick.Invoke();
            yield return null;

            Assert.That(battle.SummonCost, Is.EqualTo(0));
            Assert.That(battle.ReforgeCost, Is.GreaterThan(0));
        }

        [UnityTest]
        public IEnumerator EngravingCardsStayPausedThroughThePauseMenu()
        {
            BattleController battle = null;
            yield return LoadBattle(value => battle = value);
            for (int step = 0;
                 step < 600 && GameObject.Find("EngravingChoiceOverlay") == null;
                 step++)
            {
                battle.Session.Advance(0.1f);
            }
            Assert.That(GameObject.Find("EngravingChoiceOverlay"), Is.Not.Null);

            battle.PauseFromLifecycle();
            yield return null;
            GameObject.Find("ResumeButton").GetComponent<Button>().onClick.Invoke();
            yield return null;

            Assert.That(GameObject.Find("EngravingChoiceOverlay"), Is.Not.Null);
            Assert.That(battle.IsPaused, Is.True);
        }

        [UnityTest]
        public IEnumerator ExpiredChainLeavesNoConstellationBonus()
        {
            BattleController battle = null;
            yield return LoadBattle(value => battle = value);
            battle.Inventory.TryAdd(new SwordInstance("ember_blade", 1, 31, "chain-a"), out int first);
            battle.Inventory.TryAdd(new SwordInstance("ember_blade", 1, 32, "chain-b"), out int second);
            Assert.That(battle.TryMergeSlots(first, second), Is.True);
            Time.timeScale = 0f;
            yield return new WaitForSecondsRealtime(10.6f);
            Time.timeScale = 1f;

            battle.Session.ApplyRune(RunContentCatalog.GetRune("constellation_chain"));

            Assert.That(battle.Session.Modifiers.ChainDamageBonus, Is.EqualTo(0f));
        }

        [UnityTest]
        public IEnumerator EndlessChainStillShowsMergeHints()
        {
            BattleController battle = null;
            yield return LoadBattle(value => battle = value);
            battle.Session.ApplyRune(RunContentCatalog.GetRune("constellation_chain"));
            battle.Inventory.TryAdd(new SwordInstance("ember_blade", 1, 41, "pair-a"), out int first);
            battle.Inventory.TryAdd(new SwordInstance("ember_blade", 1, 42, "pair-b"), out int second);
            Assert.That(battle.TryMergeSlots(first, second), Is.True);
            battle.Inventory.TryAdd(new SwordInstance("frost_blade", 1, 43, "pair-c"), out _);
            battle.Inventory.TryAdd(new SwordInstance("frost_blade", 1, 44, "pair-d"), out _);
            yield return null;

            string status = GameObject.Find("ForgeStatus").GetComponent<Text>().text;
            Assert.That(
                status.Contains("합성") || status.Contains("MERGE"),
                Is.True,
                status);
        }

        [UnityTest]
        public IEnumerator OverflowingStarlightMakesThreeSummonsFree()
        {
            BattleController battle = null;
            yield return LoadBattle(value => battle = value);
            battle.Session.ApplyRune(RunContentCatalog.GetRune("overflowing_starlight"));
            for (int step = 0; step < 100 && battle.Session.Enemies.Count == 0; step++)
            {
                battle.Session.Advance(0.1f);
            }
            battle.Session.AddUltimateCharge(100f);
            GameObject.Find("UltimateButton").GetComponent<Button>().onClick.Invoke();
            yield return null;

            Assert.That(battle.SummonCost, Is.EqualTo(0));
            int gold = battle.Gold;
            for (int index = 0; index < 3; index++)
            {
                while (battle.IsCombining)
                {
                    yield return null;
                }
                Assert.That(battle.TrySummonSword(), Is.True);
            }
            Assert.That(battle.Gold, Is.GreaterThanOrEqualTo(gold));
            Assert.That(battle.SummonCost, Is.GreaterThan(0));
        }

        [UnityTest]
        public IEnumerator DroppingOnADifferentSwordSwapsThem()
        {
            BattleController battle = null;
            yield return LoadBattle(value => battle = value);
            battle.Inventory.TryAdd(new SwordInstance("ember_blade", 1, 51, "swap-a"), out int first);
            battle.Inventory.TryAdd(new SwordInstance("frost_blade", 1, 52, "swap-b"), out int second);

            Assert.That(battle.TryMergeSlots(first, second), Is.True);

            Assert.That(battle.Inventory.Get(first).DefinitionId, Is.EqualTo("frost_blade"));
            Assert.That(battle.Inventory.Get(second).DefinitionId, Is.EqualTo("ember_blade"));
        }

        [UnityTest]
        public IEnumerator DroppingOnAnEmptySlotMovesTheSword()
        {
            BattleController battle = null;
            yield return LoadBattle(value => battle = value);
            battle.Inventory.TryAdd(new SwordInstance("ember_blade", 1, 53, "move-a"), out int first);

            Assert.That(battle.TryMergeSlots(first, 7), Is.True);

            Assert.That(battle.Inventory.Get(first), Is.Null);
            Assert.That(battle.Inventory.Get(7).DefinitionId, Is.EqualTo("ember_blade"));
        }

        [UnityTest]
        public IEnumerator AdjacentSameElementSwordsShowAConstellationLink()
        {
            BattleController battle = null;
            yield return LoadBattle(value => battle = value);
            battle.Inventory.TryAdd(new SwordInstance("ember_blade", 1, 54, "link-a"), out _);
            battle.Inventory.TryAdd(new SwordInstance("scarlet_cyclone", 1, 55, "link-b"), out _);
            yield return null;

            Assert.That(GameObject.Find("ElementConstellationLink"), Is.Not.Null);
        }

        [UnityTest]
        public IEnumerator EngravingCardsOnlyOfferUnlockedEngravings()
        {
            BattleController battle = null;
            yield return LoadBattle(value => battle = value);
            Object.FindFirstObjectByType<GameBootstrap>().SaveData.PlayerLevel = 1;
            var allowed = RunContentCatalog.Runes
                .Where(rune => rune.UnlockLevel <= 1)
                .SelectMany(rune => new[] { rune.NameKo, rune.NameEn })
                .ToList();
            // Three draws: an empty board falls around wave 4 with the denser waves.
            for (int wave = 0; wave < 3; wave++)
            {
                for (int step = 0;
                     step < 600 && GameObject.Find("EngravingChoiceOverlay") == null;
                     step++)
                {
                    battle.Session.Advance(0.1f);
                }
                GameObject overlay = GameObject.Find("EngravingChoiceOverlay");
                Assert.That(overlay, Is.Not.Null);
                foreach (Text name in overlay.GetComponentsInChildren<Text>()
                             .Where(text => text.name == "Name"))
                {
                    Assert.That(allowed, Does.Contain(name.text));
                }
                GameObject.Find("EngravingChoice_0").GetComponent<Button>().onClick.Invoke();
                yield return null;
            }
        }

        [UnityTest]
        public IEnumerator HomeScreenShowsTheNextGoal()
        {
            yield return LoadMainScene();
            GameBootstrap bootstrap = Object.FindFirstObjectByType<GameBootstrap>();
            bootstrap.SaveData.PlayerLevel = 1;
            bootstrap.SaveData.CompletedStageIds.RemoveAll(
                id => id.StartsWith("path_") || id == LearningPath.StageId);
            bootstrap.SaveData.CompletedStageIds.Add(LearningPath.Get(1).Id);
            bootstrap.ShowMainMenu();
            yield return null;

            // While learning: the next step. Afterwards: the next engraving unlock.
            Text goal = GameObject.Find("NextUnlock").GetComponent<Text>();
            Assert.That(goal.text, Does.Contain("2"));
            Assert.That(goal.text, Does.Contain(bootstrap.Localization.IsKorean
                ? LearningPath.Get(2).NameKo
                : LearningPath.Get(2).NameEn));

            bootstrap.SaveData.CompletedStageIds.Add(LearningPath.StageId);
            bootstrap.ShowMainMenu();
            yield return null;
            goal = GameObject.Find("NextUnlock").GetComponent<Text>();
            Assert.That(goal.text, Does.Contain("LV.2"));
        }

        [UnityTest]
        public IEnumerator PowerButtonListsOwnedEngravings()
        {
            BattleController battle = null;
            yield return LoadBattle(value => battle = value);
            RuneDefinition first = RunContentCatalog.GetRune("forge_memory");
            RuneDefinition second = RunContentCatalog.GetRune("keen_edge");
            battle.Session.ApplyRune(first);
            battle.Session.ApplyRune(second);

            GameObject.Find("PowerButton").GetComponent<Button>().onClick.Invoke();
            yield return null;

            GameObject overlay = GameObject.Find("EngravingListOverlay");
            Assert.That(overlay, Is.Not.Null);
            Assert.That(battle.IsPaused, Is.True);
            string[] names = overlay.GetComponentsInChildren<Text>()
                .Select(text => text.text)
                .ToArray();
            Assert.That(names, Has.Some.EqualTo(first.NameKo).Or.Some.EqualTo(first.NameEn));
            Assert.That(names, Has.Some.EqualTo(second.NameKo).Or.Some.EqualTo(second.NameEn));

            GameObject.Find("EngravingListClose").GetComponent<Button>().onClick.Invoke();
            yield return null;

            Assert.That(GameObject.Find("EngravingListOverlay"), Is.Null);
            Assert.That(battle.IsPaused, Is.False);
        }

        [UnityTest]
        public IEnumerator SpeedButtonTogglesDoubleSpeed()
        {
            BattleController battle = null;
            yield return LoadBattle(value => battle = value);
            Button speed = GameObject.Find("SpeedButton").GetComponent<Button>();
            int before = battle.GameSpeed;

            speed.onClick.Invoke();
            Assert.That(battle.GameSpeed, Is.EqualTo(before == 1 ? 2 : 1));
            speed.onClick.Invoke();
            Assert.That(battle.GameSpeed, Is.EqualTo(before));
        }

        [UnityTest]
        public IEnumerator RetryReplacesThePreviousBattle()
        {
            BattleController battle = null;
            yield return LoadBattle(value => battle = value);
            GameBootstrap bootstrap =
                Object.FindFirstObjectByType<GameBootstrap>();

            bootstrap.StartBattle(battle.Stage.Id, battle.Mode);
            yield return null;

            Assert.That(
                Object.FindObjectsByType<BattleController>(
                    FindObjectsSortMode.None).Length,
                Is.EqualTo(1));
        }

        [UnityTest]
        public IEnumerator EnemyGaugeFillDrawsAboveItsFrame()
        {
            BattleController battle = null;
            yield return LoadBattle(value => battle = value);

            Transform fill = GameObject.Find("EnemyGaugeFill").transform;
            Transform frame = GameObject.Find("EnemyGaugeOrnateFrame").transform;
            Assert.That(fill.parent, Is.EqualTo(frame.parent));
            Assert.That(
                fill.GetSiblingIndex(),
                Is.GreaterThan(frame.GetSiblingIndex()));
        }

        [UnityTest]
        public IEnumerator SellingASwordFreesTheSlotAndRefundsGold()
        {
            BattleController battle = null;
            yield return LoadBattle(value => battle = value);
            Assert.That(battle.TrySummonSword(), Is.True);
            while (battle.IsCombining)
            {
                yield return null;
            }
            int goldBefore = battle.Gold;
            int value = battle.SellValue(0);

            Assert.That(value, Is.GreaterThan(0));
            Assert.That(battle.TrySellSlot(0), Is.True);
            yield return null;

            Assert.That(battle.Inventory.Get(0), Is.Null);
            Assert.That(battle.Gold, Is.EqualTo(goldBefore + value));
            Assert.That(battle.TrySellSlot(0), Is.False);
        }

        [UnityTest]
        public IEnumerator ReadyGuideTargetForgesWithOneTap()
        {
            BattleController battle = null;
            yield return LoadBattle(value => battle = value);
            for (int index = 0; index < 4; index++)
            {
                while (battle.IsCombining)
                {
                    yield return null;
                }
                Assert.That(battle.TrySummonSword(), Is.True);
            }
            while (battle.IsCombining)
            {
                yield return null;
            }

            Assert.That(battle.ReadyRecipeCount, Is.GreaterThan(0));
            Button target = GameObject.Find("ForgeTarget0")
                .GetComponent<Button>();
            target.onClick.Invoke();
            Assert.That(battle.IsCombining, Is.True);
            Assert.That(battle.RecipeBookIsOpen, Is.False);
            yield return new WaitForSecondsRealtime(2.5f);

            Assert.That(battle.IsCombining, Is.False);
            Assert.That(battle.LastCraftedRecipeId, Is.Not.Null);
        }

        [UnityTest]
        public IEnumerator StepOneShowsOnlySummonAndMerge()
        {
            yield return LoadMainScene();
            GameBootstrap bootstrap = Object.FindFirstObjectByType<GameBootstrap>();
            bootstrap.StartBattle(LearningPath.StageId, RunMode.Standard, 1);
            yield return null;
            yield return null;
            BattleController battle = Object.FindFirstObjectByType<BattleController>();

            Assert.That(GameObject.Find("LearningIntroOverlay"), Is.Not.Null);
            Assert.That(battle.IsPaused, Is.True);
            GameObject.Find("LearningIntroConfirm").GetComponent<Button>().onClick.Invoke();
            yield return null;
            Assert.That(battle.IsPaused, Is.False);

            Assert.That(GameObject.Find("UltimateButton"), Is.Null);
            Assert.That(GameObject.Find("RecipeBookButton").GetComponent<Button>().interactable, Is.False);
            Assert.That(battle.CurrentMission, Is.Null);
            Assert.That(battle.TrySummonSword(), Is.True);
            Assert.That(battle.TrySellSlot(0), Is.False);
            Assert.That(battle.Session.TotalWaves, Is.EqualTo(5));
        }

        [UnityTest]
        public IEnumerator RiftSelectShowsWhichStepsAreCleared()
        {
            yield return LoadMainScene();
            GameBootstrap bootstrap = Object.FindFirstObjectByType<GameBootstrap>();
            bootstrap.SaveData.CompletedStageIds.RemoveAll(id => id.StartsWith("path_"));
            bootstrap.SaveData.CompletedStageIds.Add(LearningPath.Get(1).Id);
            bootstrap.ShowStageSelect();
            yield return null;

            Assert.That(GameObject.Find("Step_1_Status").GetComponent<Text>().text, Does.Contain("✓"));
            Assert.That(GameObject.Find("Step_2_Status").GetComponent<Text>().text, Does.Contain("▶"));
            Assert.That(GameObject.Find("Step_2").GetComponent<Button>().interactable, Is.True);
            Assert.That(GameObject.Find("Step_3").GetComponent<Button>().interactable, Is.False);
        }

        [UnityTest]
        public IEnumerator CompletingTheWaveMissionPaysGold()
        {
            BattleController battle = null;
            yield return LoadBattle(value => battle = value);
            battle.StartMission(new WaveMission(WaveMissionKind.Summon, 2, 50));
            Assert.That(GameObject.Find("Mission").GetComponent<Text>().text, Is.Not.Empty);

            Assert.That(battle.TrySummonSword(), Is.True);
            int goldAfterFirst = battle.Gold;
            int secondCost = battle.SummonCost;
            Assert.That(battle.TrySummonSword(), Is.True);

            // Same frame: no kill gold can slip in between.
            Assert.That(battle.CurrentMission.IsComplete, Is.True);
            Assert.That(battle.Gold, Is.EqualTo(goldAfterFirst - secondCost + 50));
        }

        [UnityTest]
        public IEnumerator BossTreasureLetsThePlayerPickOneReward()
        {
            BattleController battle = null;
            yield return LoadBattle(value => battle = value);
            battle.OfferBossTreasure();
            yield return null;

            Assert.That(GameObject.Find("BossTreasureOverlay"), Is.Not.Null);
            Assert.That(battle.IsPaused, Is.True);
            Button first = GameObject.Find("BossTreasure_0").GetComponent<Button>();
            Assert.That(GameObject.Find("BossTreasure_2"), Is.Not.Null);
            first.onClick.Invoke();
            yield return null;

            Assert.That(GameObject.Find("BossTreasureOverlay"), Is.Null);
            Assert.That(battle.IsPaused, Is.False);
        }

        [UnityTest]
        public IEnumerator HitsShowFloatingDamageNumbers()
        {
            BattleController battle = null;
            yield return LoadBattle(value => battle = value);
            for (int index = 0; index < 4; index++)
            {
                battle.Inventory.TryAdd(
                    new SwordInstance("ember_blade", 3, (ulong)(index + 1), $"hit-{index}"),
                    out _);
            }

            GameObject popup = null;
            float waited = 0f;
            while (popup == null && waited < 12f)
            {
                waited += Time.unscaledDeltaTime;
                popup = GameObject.Find("DamagePopup");
                yield return null;
            }

            Assert.That(popup, Is.Not.Null);
            Assert.That(popup.GetComponent<Text>().text, Is.Not.Empty);
        }

        [UnityTest]
        public IEnumerator BossTreasureRewardsTakeEffect()
        {
            BattleController battle = null;
            yield return LoadBattle(value => battle = value);
            battle.Inventory.TryAdd(new SwordInstance("ember_blade", 1, 11, "ember-test"), out int slot);
            int goldBefore = battle.Gold;

            battle.ApplyBossTreasure(BossTreasure.StarBlessing);
            battle.ApplyBossTreasure(BossTreasure.GoldHoard);
            battle.ApplyBossTreasure(BossTreasure.UltimateCharge);
            yield return null;

            Assert.That(battle.Inventory.Get(slot).Stars, Is.EqualTo(2));
            Assert.That(battle.Gold, Is.GreaterThan(goldBefore));
            Assert.That(battle.Session.UltimateCharge, Is.EqualTo(100f));
        }

        [UnityTest]
        public IEnumerator BattleKeepsRunningWhileForging()
        {
            BattleController battle = null;
            yield return LoadBattle(value => battle = value);
            battle.Inventory.TryAdd(new SwordInstance("ember_blade", 1, 11, "ember-test"), out _);
            battle.Inventory.TryAdd(new SwordInstance("gale_blade", 1, 12, "gale-test"), out _);

            Assert.That(battle.TryCraftRecipe("recipe_scarlet_cyclone"), Is.True);
            float before = battle.Session.DurationSeconds;
            float minTimeScale = Time.timeScale;
            while (battle.IsCombining)
            {
                minTimeScale = Mathf.Min(minTimeScale, Time.timeScale);
                yield return null;
            }

            Assert.That(minTimeScale, Is.EqualTo(1f));
            Assert.That(battle.Session.DurationSeconds, Is.GreaterThan(before + 0.5f));
        }

        [UnityTest]
        public IEnumerator CombinationOpportunitiesAreVisibleOnTheBoard()
        {
            BattleController battle = null;
            yield return LoadBattle(value => battle = value);

            // A fixed equal pair (a summon could land as a jackpot ★2).
            battle.Inventory.TryAdd(new SwordInstance("ember_blade", 1, 11, "pair-a"), out _);
            battle.Inventory.TryAdd(new SwordInstance("ember_blade", 1, 12, "pair-b"), out _);
            yield return null;

            Assert.That(battle.CombinationState, Is.Not.Null);
            Assert.That(
                battle.CombinationState.MergeOpportunities.Count,
                Is.EqualTo(1));
            Assert.That(
                battle.GetComponentsInChildren<PrototypeSwordSlotView>(true)
                    .Count(view => view.IsMergeReady),
                Is.EqualTo(2));
            Assert.That(
                GameObject.Find("CombinationGuide"),
                Is.Not.Null);
            Assert.That(
                GameObject.Find("MergeConstellationLink"),
                Is.Not.Null);
        }

        [UnityTest]
        public IEnumerator BattleActionsUseSeparatedTitleAndValueRows()
        {
            BattleController battle = null;
            yield return LoadBattle(value => battle = value);
            yield return null;

            foreach (string buttonName in new[]
                     {
                         "SummonButton",
                         "RecipeBookButton",
                         "CodexButton"
                     })
            {
                GameObject button = GameObject.Find(buttonName);
                Assert.That(button, Is.Not.Null);
                RectTransform title = button.transform
                    .Find("ActionTitle")
                    .GetComponent<RectTransform>();
                RectTransform value = button.transform
                    .Find("ActionValue")
                    .GetComponent<RectTransform>();
                Assert.That(
                    value.anchorMax.y,
                    Is.LessThan(title.anchorMin.y));
            }
        }

        [UnityTest]
        public IEnumerator FullDeadEndBoardCanResonanceReforge()
        {
            BattleController battle = null;
            yield return LoadBattle(value => battle = value);
            for (int index = 0;
                 index < battle.Inventory.Capacity;
                 index++)
            {
                Assert.That(
                    battle.Inventory.TryAdd(
                        new SwordInstance(
                            $"dead-end-{index}",
                            1,
                            (ulong)(700 + index),
                            $"dead-end-instance-{index}"),
                        out _),
                    Is.True);
            }
            int goldBefore = battle.Gold;

            Assert.That(battle.Inventory.EmptyCount, Is.EqualTo(0));
            Assert.That(
                CombinationPlanner.IsDeadlocked(
                    battle.Inventory,
                    battle.CombinationState),
                Is.True);
            Assert.That(battle.TrySummonOrReforge(), Is.True);
            yield return null;

            Assert.That(battle.Inventory.EmptyCount, Is.EqualTo(0));
            Assert.That(battle.Gold, Is.LessThan(goldBefore));
            Assert.That(
                battle.Inventory.Slots.Count(sword =>
                    sword != null &&
                    !sword.DefinitionId.StartsWith("dead-end-")),
                Is.EqualTo(1));
        }

        [UnityTest]
        public IEnumerator RecipeBookOpensWithoutLeavingBattle()
        {
            BattleController battle = null;
            yield return LoadBattle(
                value => battle = value);
            Scene sceneBefore =
                SceneManager.GetActiveScene();

            battle.OpenRecipeBook();
            yield return null;

            Assert.That(
                battle.RecipeBookIsOpen,
                Is.True);
            Assert.That(
                GameObject.Find("RecipeBookOverlay"),
                Is.Not.Null);
            Assert.That(
                SceneManager.GetActiveScene(),
                Is.EqualTo(sceneBefore));
            RectTransform headerTitle = GameObject.Find(
                    "RecipeBookTitle")
                .GetComponent<RectTransform>();
            // Below the frame's top ornament, centred.
            Assert.That(headerTitle.anchorMax.y, Is.LessThanOrEqualTo(0.91f));
            Assert.That(
                headerTitle.anchorMin.x + headerTitle.anchorMax.x,
                Is.EqualTo(1f).Within(0.001f));
            Image firstRecipeRow = GameObject.Find(
                    "recipe_scarlet_cyclone")
                ?.GetComponent<Image>();
            Assert.That(firstRecipeRow, Is.Not.Null);
            Assert.That(firstRecipeRow.sprite, Is.Null);
        }

        [UnityTest]
        public IEnumerator BattleCodexOpensWithoutEndingRun()
        {
            BattleController battle = null;
            yield return LoadBattle(
                value => battle = value);
            CombinationDefenseSession session =
                battle.Session;

            battle.ShowBattleCodex();
            yield return null;

            Assert.That(
                GameObject.Find(
                    "BattleCodexOverlay"),
                Is.Not.Null);
            Assert.That(
                battle.Session,
                Is.SameAs(session));
            Assert.That(
                battle.Session.IsEnded,
                Is.False);
        }

        [UnityTest]
        public IEnumerator FormalRecipeConsumesMaterialsAndRevealsResult()
        {
            BattleController battle = null;
            yield return LoadBattle(
                value => battle = value);
            battle.Inventory.TryAdd(
                new SwordInstance(
                    "ember_blade",
                    1,
                    11,
                    "ember-test"),
                out _);
            battle.Inventory.TryAdd(
                new SwordInstance(
                    "gale_blade",
                    1,
                    12,
                    "gale-test"),
                out _);

            Assert.That(
                battle.CanCraftRecipe(
                    "recipe_scarlet_cyclone"),
                Is.True);
            Assert.That(
                battle.TryCraftRecipe(
                    "recipe_scarlet_cyclone"),
                Is.True);
            yield return new WaitForSecondsRealtime(2.2f);

            Assert.That(
                battle.LastCraftedRecipeId,
                Is.EqualTo(
                    "recipe_scarlet_cyclone"));
            Assert.That(
                battle.Inventory.Slots.Count(item =>
                    item != null &&
                    item.DefinitionId ==
                        "scarlet_cyclone"),
                Is.EqualTo(1));
            Assert.That(
                battle.Inventory.EmptyCount,
                Is.EqualTo(11));
        }

        [UnityTest]
        public IEnumerator HiddenLegendAutoDiscoversWhenMaterialsMeet()
        {
            BattleController battle = null;
            yield return LoadBattle(
                value => battle = value);
            GameBootstrap bootstrap =
                Object.FindFirstObjectByType<
                    GameBootstrap>();
            bootstrap.SaveData.DiscoveredRecipeIds
                .Remove("recipe_eclipse_twins");
            battle.Inventory.TryAdd(
                new SwordInstance(
                    "solar_guard",
                    1,
                    21,
                    "solar-test"),
                out _);
            battle.Inventory.TryAdd(
                new SwordInstance(
                    "lunar_echo",
                    1,
                    22,
                    "lunar-test"),
                out _);

            yield return new WaitForSecondsRealtime(2.2f);

            Assert.That(
                battle.LastCraftedRecipeId,
                Is.EqualTo(
                    "recipe_eclipse_twins"));
            Assert.That(
                battle.LastCraftWasFirstDiscovery,
                Is.True);
            Assert.That(
                bootstrap.SaveData
                    .DiscoveredRecipeIds,
                Does.Contain(
                    "recipe_eclipse_twins"));
            Assert.That(
                battle.Inventory.Slots.Count(item =>
                    item != null &&
                    item.DefinitionId ==
                        "eclipse_twins"),
                Is.EqualTo(1));
        }

        [UnityTest]
        public IEnumerator CorruptSaveRecoversFromAtomicBackup()
        {
            string directory = Path.Combine(
                Application.temporaryCachePath,
                "swordstellation-save-test-" +
                Guid.NewGuid().ToString("N"));
            Directory.CreateDirectory(directory);
            try
            {
                LocalSaveService service =
                    new LocalSaveService(directory);
                PlayerSaveData first =
                    PlayerSaveData.CreateNew();
                first.IronPowder = 321;
                service.Save(first);
                PlayerSaveData second =
                    service.Load();
                second.IronPowder = 654;
                service.Save(second);
                File.WriteAllText(
                    Path.Combine(
                        directory,
                        "player-save-v1.json"),
                    "{corrupt-json");

                PlayerSaveData recovered =
                    service.Load();

                Assert.That(
                    recovered.IronPowder,
                    Is.EqualTo(321));
            }
            finally
            {
                if (Directory.Exists(directory))
                {
                    Directory.Delete(
                        directory,
                        true);
                }
            }

            yield return null;
        }

        private static IEnumerator LoadMainScene()
        {
            AsyncOperation load =
                SceneManager.LoadSceneAsync("Main");
            Assert.That(load, Is.Not.Null);
            yield return load;
            yield return null;
        }

        private static IEnumerator LoadBattle(
            System.Action<BattleController> assign)
        {
            yield return LoadMainScene();
            GameBootstrap bootstrap =
                Object.FindFirstObjectByType<GameBootstrap>();
            Assert.That(bootstrap, Is.Not.Null);
            bootstrap.StartBattle();
            yield return null;
            yield return null;
            assign(
                Object.FindFirstObjectByType<
                    BattleController>());
        }
    }
}
