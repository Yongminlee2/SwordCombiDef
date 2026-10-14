using System;
using System.Collections.Generic;
using System.Linq;
using Swordstellation.Core;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

namespace Swordstellation.Runtime
{
    public sealed class BattleController : MonoBehaviour
    {
        public const int EnemyLimit = 60;

        private readonly List<PrototypeSwordSlotView> _slotViews =
            new List<PrototypeSwordSlotView>(
                RunInventory.MaximumSlotCount);
        private readonly List<LoopEnemyPrototypeView> _loopEnemies =
            new List<LoopEnemyPrototypeView>(16);
        private readonly Dictionary<string, LoopEnemyPrototypeView>
            _enemyViews =
                new Dictionary<string, LoopEnemyPrototypeView>(
                    StringComparer.Ordinal);

        private GameBootstrap _bootstrap;
        private ContentCatalog _catalog;
        private StageDefinition _stage;
        private RunMode _mode;
        private SeededRandom _random;
        private ForgeRules _forgeRules;
        private RunInventory _inventory;
        private RecipeBookOverlay _recipeBook;
        private ConstellationForgeEffect _forgeEffect;
        private BattleAttackEffectView _attackEffect;
        private CombinationDefenseSession _session;
        private Canvas _canvas;
        private RectTransform _safeRoot;
        private RectTransform _arenaRoot;
        private RectTransform _boardRoot;
        private Text _waveText;
        private Text _goldText;
        private Text _enemyCountText;
        private Text _ultimateChargeText;
        private Text _toastText;
        private Text _forgeStatusText;
        private Text _summonTitleText;
        private Text _summonValueText;
        private Text _recipeTitleText;
        private Text _recipeValueText;
        private Text _codexTitleText;
        private Text _codexValueText;
        private Image _enemyGaugeFill;
        private RectTransform _bossGaugeRoot;
        private Image _bossHealthFill;
        private Text _bossNameText;
        private Button _summonButton;
        private Button _recipeButton;
        private Button _codexButton;
        private RectTransform _combinationGuideRoot;
        private readonly List<Image> _guideIcons =
            new List<Image>(3);
        private readonly List<Text> _guideProgressTexts =
            new List<Text>(3);
        private RectTransform _combinationLinkRoot;
        private readonly List<Image> _combinationLinks =
            new List<Image>();
        private readonly List<Image> _elementLinks =
            new List<Image>();
        private CombinationSnapshot _combinationSnapshot;
        private float _toastRemaining;
        private int _gold;
        private int _summonCount;
        private int _enemyCount;
        private string _firstSummonSwordId;
        private RecipeDefinition _demoRecipe;
        private bool _isCombining;
        private bool _resultShown;
        private bool _isPaused;
        private RectTransform _pauseOverlay;
        private RectTransform _battleCodexOverlay;
        private int _starUpgrades;
        private int _recipeForges;
        private int _forgeChain;
        private int _summonsSinceForge;
        private float _forgeChainRemaining;
        private bool _lastSummonWasResonant;
        private int _draggingSlot = -1;
        private int _freeSummons;
        private RectTransform _runeOverlay;
        private RectTransform _engravingListOverlay;
        private RectTransform _treasureOverlay;
        private RectTransform _introOverlay;
        private LearningStep _step;
        private Button _powerButton;
        private WaveMission _mission;
        private Text _missionText;
        private bool _treasurePending;
        private DamagePopupLayer _damagePopups;
        private float _shownPower;
        private Button _ultimateButton;
        private Image _ultimateFill;
        private Text _ultimateLabel;
        private LoopEnemyPrototypeView _focusedView;
        private SwordInstance _draggingSword;
        private ulong _runSeed;

        public RunInventory Inventory => _inventory;
        public RectTransform BoardRoot => _boardRoot;
        public CombinationDefenseSession Session => _session;
        public int Gold => _session?.Gold ?? _gold;
        public int SummonCost =>
            _freeSummons > 0 ? 0 : SummonCostWithChain(ActiveForgeChain);
        public int ReforgeCost =>
            SummonCostWithChain(ActiveForgeChain) * 3 / 2;
        public int ForgeChain => _forgeChain;
        public int ReadyRecipeCount =>
            _combinationSnapshot?.ReadyRecipeCount ?? 0;
        public CombinationSnapshot CombinationState =>
            _combinationSnapshot;
        private int Pressure => _session?.PressureCount ?? EnemyCount;
        public int EnemyCount =>
            _session?.AliveEnemyCount ?? _enemyCount;
        public int LoopEnemyViewCount => _loopEnemies.Count;
        public bool IsCombining => _isCombining;
        public bool IsPaused => _isPaused;
        public LearningStep Step => _step;

        // A learning-path step opens systems one at a time; null is a full run.
        private bool Has(GameFeature feature)
        {
            return _step == null || _step.Has(feature);
        }
        public WaveMission CurrentMission => _mission;
        // Kept for the app session so a player who likes 2x keeps it next run.
        private static int _gameSpeed = 1;
        public int GameSpeed => _gameSpeed;
        private Text _speedLabel;
        private readonly List<Image> _dangerEdges = new List<Image>();

        public bool RecipeBookIsOpen =>
            _recipeBook != null &&
            _recipeBook.IsOpen;
        public string LastCraftedRecipeId { get; private set; }
        public bool LastCraftWasFirstDiscovery { get; private set; }
        public RunMode Mode => _mode;
        public StageDefinition Stage => _stage;

        private int ActiveForgeChain =>
            _forgeChainRemaining > 0f ? _forgeChain : 0;
        private int ForgeChainPercent =>
            Math.Min(RunBalanceRules.MaximumForgeChain, ActiveForgeChain) * 10;

        private int SummonCostWithChain(int forgeChain)
        {
            return RunBalanceRules.SummonCost(
                _summonCount,
                _bootstrap?.SaveData.SmithingLevel ?? 0,
                _session?.SummonDiscount ?? 0,
                forgeChain);
        }

        public void Initialize(
            GameBootstrap bootstrap,
            StageDefinition stage,
            RunMode mode,
            LearningStep learningStep = null)
        {
            _step = learningStep;
            if (bootstrap == null)
            {
                throw new ArgumentNullException(nameof(bootstrap));
            }
            if (stage == null)
            {
                throw new ArgumentNullException(nameof(stage));
            }

            _bootstrap = bootstrap;
            _catalog = bootstrap.Catalog;
            _stage = stage;
            _mode = mode;
            _runSeed = mode == RunMode.Daily
                ? CreateDailySeed(stage.Id, DateTime.Now)
                : (ulong)DateTime.UtcNow.Ticks ^
                  0xB04D12UL;
            _random = new SeededRandom(_runSeed);
            _forgeRules = new ForgeRules(
                _catalog,
                _random);
            _inventory = new RunInventory(
                RunBalanceRules.BoardSlotCount(
                    bootstrap.SaveData
                        .GuardianshipLevel));
            _inventory.Changed += HandleInventoryChanged;
            _gold = RunBalanceRules.StartingGold(
                bootstrap.SaveData.SmithingLevel);

            CreateInterface();
            _session = new CombinationDefenseSession(
                _catalog,
                _stage,
                _mode,
                _inventory,
                _runSeed ^ 0xC0B471UL,
                bootstrap.SaveData.SwordControlLevel,
                _gold,
                ultimateId:
                    bootstrap.SaveData
                        .SelectedUltimateId,
                swordMasteryLevels:
                    bootstrap.SaveData
                        .SwordMasteries
                        .Where(record =>
                            record != null &&
                            !string.IsNullOrWhiteSpace(
                                record.SwordId))
                        .GroupBy(record =>
                            record.SwordId)
                        .ToDictionary(
                            group => group.Key,
                            group => group.Max(
                                record =>
                                    record.Level)),
                learningStep: _step);
            _session.EnemySpawned += HandleEnemySpawned;
            _session.SwordAttacked += HandleSwordAttacked;
            _session.EnemyDefeated += HandleEnemyDefeated;
            _session.WaveStarted += HandleWaveStarted;
            _session.StateChanged += HandleSessionStateChanged;
            _session.UltimateUsed += HandleUltimateUsed;
            _session.IntermissionStarted += ShowRuneChoice;
            _session.BossRampaged += HandleBossRampaged;
            _session.WaveCleared += HandleWaveCleared;
            _session.EnemyAbilityUsed +=
                HandleEnemyAbilityUsed;
            _forgeEffect =
                gameObject.AddComponent<
                    ConstellationForgeEffect>();
            _attackEffect =
                gameObject.AddComponent<
                    BattleAttackEffectView>();
            _attackEffect.Initialize(
                _arenaRoot,
                bootstrap.SaveData.ReducedMotion);
            _damagePopups = gameObject.AddComponent<DamagePopupLayer>();
            _damagePopups.Initialize(
                _arenaRoot,
                bootstrap.SaveData.ReducedMotion);
            _recipeBook =
                gameObject.AddComponent<
                    RecipeBookOverlay>();
            _recipeBook.Initialize(
                this,
                _bootstrap,
                _safeRoot);
            RefreshLocalization();
            ApplyLearningGates();
            RefreshBoard();
            RefreshHud();
            ShowLearningIntro();

            Debug.Log(
                $"Combination defense session ready: " +
                $"slots={_inventory.Capacity}, " +
                $"waves={_session.TotalWaves}, gold={Gold}");
        }

        private void OnDestroy()
        {
            if (_inventory != null)
            {
                _inventory.Changed -= HandleInventoryChanged;
            }
            if (_session != null)
            {
                _session.EnemySpawned -= HandleEnemySpawned;
                _session.SwordAttacked -= HandleSwordAttacked;
                _session.EnemyDefeated -= HandleEnemyDefeated;
                _session.WaveStarted -= HandleWaveStarted;
                _session.StateChanged -=
                    HandleSessionStateChanged;
                _session.UltimateUsed -=
                    HandleUltimateUsed;
                _session.IntermissionStarted -= ShowRuneChoice;
                _session.BossRampaged -= HandleBossRampaged;
                _session.WaveCleared -= HandleWaveCleared;
                _session.EnemyAbilityUsed -=
                    HandleEnemyAbilityUsed;
            }
        }

        private void Update()
        {
            if (_toastRemaining > 0f)
            {
                _toastRemaining -= Time.unscaledDeltaTime;
                if (_toastRemaining <= 0f && _toastText != null)
                {
                    _toastText.text = string.Empty;
                }
            }

            if (_forgeChainRemaining > 0f &&
                !_isPaused &&
                !RecipeBookIsOpen &&
                !(_session != null &&
                  _session.Modifiers.Has(RuneEffect.EndlessChain)))
            {
                _forgeChainRemaining -= Time.unscaledDeltaTime;
                if (_forgeChainRemaining <= 0f)
                {
                    _forgeChainRemaining = 0f;
                    _forgeChain = 0;
                    _session?.Modifiers.SetForgeChain(0);
                }
            }
            AnimateCombinationLinks();

            if (_session != null &&
                !_session.IsEnded &&
                !_isPaused &&
                !RecipeBookIsOpen)
            {
                _session.Advance(Time.deltaTime * _gameSpeed);
                _gold = _session.Gold;
                RefreshHud();
            }

            // Development builds only: F9 opens the boss chest for device checks.
            if (Debug.isDebugBuild && Input.GetKeyDown(KeyCode.F9))
            {
                _treasurePending = true;
            }

            if (_treasurePending &&
                _treasureOverlay == null &&
                _runeOverlay == null &&
                !_isPaused &&
                !_isCombining &&
                !RecipeBookIsOpen &&
                _session != null &&
                !_session.IsEnded)
            {
                OfferBossTreasure();
            }

            if (Input.GetKeyDown(KeyCode.Escape) &&
                _bootstrap != null &&
                !_resultShown &&
                _runeOverlay == null &&
                _treasureOverlay == null &&
                _introOverlay == null)
            {
                if (_engravingListOverlay != null)
                {
                    CloseEngravingList();
                }
                else if (_battleCodexOverlay != null)
                {
                    CloseBattleCodex();
                }
                else if (RecipeBookIsOpen)
                {
                    _recipeBook.Close();
                }
                else
                {
                    SetPaused(!_isPaused);
                }
            }
        }

        public bool TrySummonSword()
        {
            if (_inventory == null ||
                _isCombining ||
                _session == null ||
                _session.IsEnded)
            {
                return false;
            }
            if (_inventory.EmptyCount <= 0)
            {
                ShowToast(
                    _bootstrap.Localization.IsKorean
                        ? "보드가 가득 찼습니다."
                        : "THE BOARD IS FULL.");
                return false;
            }

            int cost = SummonCost;
            if (Gold < cost)
            {
                ShowToast(
                    _bootstrap.Localization.IsKorean
                        ? "골드가 부족합니다."
                        : "NOT ENOUGH GOLD.");
                return false;
            }

            string swordId = ChooseSummonSwordId();
            int summonStars = RunBalanceRules.SummonStars(_random.NextFloat());
            SwordInstance instance = new SwordInstance(
                swordId,
                summonStars,
                _random.NextUlong(),
                Guid.NewGuid().ToString("N"));
            if (!_inventory.TryAdd(instance, out _))
            {
                return false;
            }

            if (_session == null ||
                !_session.TrySpendGold(cost))
            {
                return false;
            }
            _gold = _session.Gold;
            if (_freeSummons > 0)
            {
                _freeSummons--;
            }
            _summonCount++;
            _summonsSinceForge++;
            bool summonMissionDone = _mission?.Record(WaveMissionKind.Summon) == true;
            TryRegisterRareAppearance(
                instance,
                _catalog.GetSword(swordId));
            if (summonStars > 1)
            {
                AudioService.Instance?.PlaySfx("legendary", 0.9f);
                ShowToast(
                    _bootstrap.Localization.IsKorean
                        ? "대박 소환!  ★2 검이 나왔습니다"
                        : "Jackpot summon!  A ★2 sword");
                PayMissionIf(summonMissionDone);
                RefreshHud();
                return true;
            }
            AudioService.Instance?.PlaySfx("summon", 0.75f);
            ShowToast(
                _bootstrap.Localization.IsKorean
                    ? (_lastSummonWasResonant
                        ? "공명이 조합 재료를 이끌었습니다."
                        : "새 검이 소환되었습니다.")
                    : (_lastSummonWasResonant
                        ? "RESONANCE FOUND A FORGE MATERIAL."
                        : "A NEW SWORD WAS SUMMONED."));
            PayMissionIf(summonMissionDone);
            RefreshHud();
            return true;
        }

        public bool TrySummonOrReforge()
        {
            if (_inventory == null || _inventory.EmptyCount > 0)
            {
                return TrySummonSword();
            }

            return TryReforgeDeadEnd();
        }

        public int SellValue(int slotIndex)
        {
            SwordInstance sword =
                _inventory == null ||
                slotIndex < 0 ||
                slotIndex >= _inventory.Capacity
                    ? null
                    : _inventory.Get(slotIndex);
            SwordDefinition definition = sword == null
                ? null
                : _catalog.GetSword(sword.DefinitionId);
            return definition == null
                ? 0
                : ScrapDealerPrice(
                    RunBalanceRules.SellValue(
                        definition.Rarity,
                        sword.Stars,
                        LastPaidSummonPrice));
        }

        // The summon price before the latest summon raised it, so a summon
        // sold straight back can never return more than it cost.
        private int LastPaidSummonPrice =>
            RunBalanceRules.SummonCost(
                Math.Max(0, _summonCount - 1),
                _bootstrap?.SaveData.SmithingLevel ?? 0,
                _session?.SummonDiscount ?? 0);

        private int ScrapDealerPrice(int value)
        {
            return _session != null &&
                   _session.Modifiers.Has(RuneEffect.SellDouble)
                ? value * 3 / 2
                : value;
        }

        public bool TrySellSlot(int slotIndex)
        {
            int value = SellValue(slotIndex);
            if (value <= 0 ||
                !Has(GameFeature.Sell) ||
                _isCombining ||
                _isPaused ||
                RecipeBookIsOpen ||
                _session == null ||
                _session.IsEnded ||
                !_inventory.TryRemove(
                    slotIndex,
                    out SwordInstance sold))
            {
                return false;
            }

            _session.AddGold(value);
            _gold = _session.Gold;
            SwordDefinition soldDefinition =
                _catalog.GetSword(sold.DefinitionId);
            if (_session.Modifiers.Has(RuneEffect.SellDiscount))
            {
                _session.Modifiers.AddSummonDiscount(1);
            }
            if (_session.Modifiers.Has(RuneEffect.SellCharge))
            {
                _session.AddUltimateCharge(
                    10f * ((int)soldDefinition.Rarity + 1));
            }
            string name = _bootstrap.Localization.SwordName(
                soldDefinition);
            AudioService.Instance?.PlaySfx("summon", 0.5f);
            ShowToast(
                _bootstrap.Localization.IsKorean
                    ? $"{name} 판매  +{value}"
                    : $"SOLD {name}  +{value}");
            RecordMission(WaveMissionKind.Sell);
            RefreshHud();
            return true;
        }

        private void HandleSwordDrag(
            int slotIndex,
            PointerEventData eventData,
            bool ended)
        {
            SwordInstance picked = _draggingSword;
            _draggingSlot = ended ? -1 : slotIndex;
            _draggingSword = ended ? null : _inventory.Get(slotIndex);
            if (ended &&
                picked != null &&
                _inventory.Get(slotIndex) == picked &&
                _summonButton != null &&
                RectTransformUtility.RectangleContainsScreenPoint(
                    _summonButton.transform as RectTransform,
                    eventData.position,
                    eventData.pressEventCamera))
            {
                TrySellSlot(slotIndex);
            }
            RefreshHud();
        }

        public bool TryMergeSlots(
            int sourceIndex,
            int destinationIndex)
        {
            if (_inventory == null || _isCombining)
            {
                return false;
            }

            if (!_inventory.TryMergeSame(
                    sourceIndex,
                    destinationIndex,
                    out SwordInstance result))
            {
                // Not a merge: rearrange the board so same-element swords can touch.
                if (!_inventory.TryMoveOrSwap(
                        sourceIndex,
                        destinationIndex))
                {
                    return false;
                }

                AudioService.PlayUiClick();
                return true;
            }

            AudioService.Instance?.PlaySfx("forge", 0.9f);
            _starUpgrades++;
            RegisterForgeChain();
            _session?.RegisterForge();
            TryRegisterRareAppearance(
                result,
                _catalog.GetSword(
                    result.DefinitionId));
            ShowToast(
                _bootstrap.Localization.IsKorean
                    ? $"등급 상승!  ★{result.Stars}"
                    : $"RANK UP!  ★{result.Stars}");
            RecordMission(WaveMissionKind.Merge);
            return true;
        }

        public void OpenRecipeBook()
        {
            if (_isCombining || !Has(GameFeature.Recipes))
            {
                return;
            }

            _recipeBook?.Open();
        }

        public void ShowBattleCodex()
        {
            if (_isCombining ||
                _session == null ||
                _session.IsEnded ||
                _battleCodexOverlay != null)
            {
                return;
            }

            _recipeBook?.Close();
            _isPaused = true;
            _battleCodexOverlay = UiFactory.CreatePanel(
                _safeRoot,
                "BattleCodexOverlay",
                new Color(0.005f, 0.008f, 0.035f, 0.96f),
                Vector2.zero,
                Vector2.one,
                Vector2.zero,
                Vector2.zero);
            _battleCodexOverlay.SetAsLastSibling();
            RectTransform card = UiFactory.CreatePanel(
                _battleCodexOverlay,
                "BattleCodexCard",
                Color.white,
                new Vector2(0.04f, 0.08f),
                new Vector2(0.96f, 0.94f),
                Vector2.zero,
                Vector2.zero);
            UiFactory.CreateText(
                card,
                "Title",
                _bootstrap.Localization.IsKorean
                    ? "수호 중 검 도감"
                    : "BATTLE SWORD CODEX",
                38,
                GameTheme.Gold,
                TextAnchor.MiddleCenter,
                new Vector2(0.12f, 0.855f),
                new Vector2(0.88f, 0.915f),
                Vector2.zero,
                Vector2.zero,
                FontStyle.Bold);
            UiFactory.CreateButton(
                card,
                "Close",
                _bootstrap.Localization.IsKorean
                    ? "닫기"
                    : "CLOSE",
                GameTheme.Violet,
                GameTheme.Text,
                28,
                new Vector2(0.32f, 0.045f),
                new Vector2(0.68f, 0.115f),
                Vector2.zero,
                Vector2.zero,
                CloseBattleCodex);

            RectTransform viewport = UiFactory.CreatePanel(
                card,
                "BattleCodexViewport",
                new Color(
                    GameTheme.Panel.r,
                    GameTheme.Panel.g,
                    GameTheme.Panel.b,
                    0.78f),
                new Vector2(0.06f, 0.135f),
                new Vector2(0.94f, 0.845f),
                Vector2.zero,
                Vector2.zero);
            Mask mask =
                viewport.gameObject.AddComponent<Mask>();
            mask.showMaskGraphic = true;
            ScrollRect scroll =
                viewport.gameObject
                    .AddComponent<ScrollRect>();
            scroll.horizontal = false;

            GameObject contentObject = new GameObject(
                "Content",
                typeof(RectTransform),
                typeof(VerticalLayoutGroup),
                typeof(ContentSizeFitter));
            contentObject.transform.SetParent(
                viewport,
                false);
            RectTransform content =
                contentObject
                    .GetComponent<RectTransform>();
            content.anchorMin = new Vector2(0f, 1f);
            content.anchorMax = new Vector2(1f, 1f);
            // Same width as the viewport (a new RectTransform is 100 wider).
            content.sizeDelta = Vector2.zero;
            content.pivot = new Vector2(0.5f, 1f);
            content.anchoredPosition = Vector2.zero;
            VerticalLayoutGroup layout =
                contentObject
                    .GetComponent<VerticalLayoutGroup>();
            layout.padding =
                new RectOffset(16, 16, 36, 24);
            layout.spacing = 10;
            layout.childControlHeight = true;
            layout.childForceExpandHeight = false;
            contentObject
                    .GetComponent<ContentSizeFitter>()
                    .verticalFit =
                ContentSizeFitter.FitMode.PreferredSize;
            scroll.viewport = viewport;
            scroll.content = content;

            foreach (SwordDefinition sword in
                     _catalog.Swords.Values
                         .OrderBy(item => item.Rarity)
                         .ThenBy(item => item.Id))
            {
                CreateBattleCodexRow(
                    content,
                    sword);
            }
        }

        private void ToggleGameSpeed()
        {
            _gameSpeed = _gameSpeed == 1 ? 2 : 1;
            RefreshSpeedLabel();
        }

        private void RefreshSpeedLabel()
        {
            if (_speedLabel != null)
            {
                _speedLabel.text = _gameSpeed == 2 ? "▶ 2x" : "▶ 1x";
                _speedLabel.color = _gameSpeed == 2 ? GameTheme.Cyan : GameTheme.Text;
            }
        }

        private void CreateDangerWarning()
        {
            Vector2[,] edges =
            {
                { new Vector2(0f, 0f), new Vector2(1f, 0.012f) },
                { new Vector2(0f, 0.988f), new Vector2(1f, 1f) },
                { new Vector2(0f, 0f), new Vector2(0.022f, 1f) },
                { new Vector2(0.978f, 0f), new Vector2(1f, 1f) }
            };
            for (int index = 0; index < 4; index++)
            {
                RectTransform edge = UiFactory.CreatePanel(
                    _canvas.transform,
                    $"DangerEdge{index}",
                    Color.clear,
                    edges[index, 0],
                    edges[index, 1],
                    Vector2.zero,
                    Vector2.zero);
                Image image = edge.GetComponent<Image>();
                image.raycastTarget = false;
                _dangerEdges.Add(image);
            }
        }

        // Past 45 of 60 the screen edges pulse red so the danger is felt.
        private void RefreshDanger()
        {
            if (_dangerEdges.Count == 0 || _session == null)
            {
                return;
            }

            bool danger = !_session.IsEnded && Pressure >= 45;
            float pulse = 0.5f + 0.5f * Mathf.Sin(Time.unscaledTime * 7f);
            Color edge = GameTheme.Coral;
            edge.a = danger ? Mathf.Lerp(0.25f, 0.75f, pulse) : 0f;
            foreach (Image image in _dangerEdges)
            {
                image.color = edge;
            }
            _enemyCountText.color = danger
                ? Color.Lerp(GameTheme.Coral, Color.white, pulse * 0.4f)
                : GameTheme.Gold;
            if (danger)
            {
                _enemyCountText.text =
                    (_bootstrap.Localization.IsKorean ? "위험 · " : "Danger · ") +
                    _enemyCountText.text;
            }
        }

        private void HandleBossRampaged(BattleEnemyState boss)
        {
            ShowToast(
                _bootstrap.Localization.IsKorean
                    ? "보스 폭주! 적 10마리 몫이 됩니다"
                    : "Boss rampage! It now counts as 10");
            AudioService.Instance?.PlaySfx("legendary", 0.8f);
        }

        private void HandleWaveCleared(int wave, int bonus)
        {
            ShowToast(
                _bootstrap.Localization.IsKorean
                    ? $"완벽 정리!  +✦ {bonus}"
                    : $"Wave cleared!  +✦ {bonus}");
            AudioService.Instance?.PlaySfx("forge", 0.7f);
            _gold = Gold;
        }

        private void ApplyLearningGates()
        {
            if (_ultimateButton != null)
            {
                _ultimateButton.gameObject.SetActive(Has(GameFeature.Ultimate));
            }
            if (_combinationGuideRoot != null)
            {
                _combinationGuideRoot.gameObject.SetActive(Has(GameFeature.Recipes));
            }
            bool engravings = Has(GameFeature.Engravings);
            if (_ultimateChargeText != null)
            {
                _ultimateChargeText.gameObject.SetActive(engravings);
            }
            if (_powerButton != null)
            {
                _powerButton.gameObject.SetActive(engravings);
            }
        }

        // Each step opens with one card about what it adds; the run waits for it.
        private void ShowLearningIntro()
        {
            if (_step == null || _introOverlay != null)
            {
                return;
            }

            bool korean = _bootstrap.Localization.IsKorean;
            _isPaused = true;
            RectTransform overlay = UiFactory.CreatePanel(
                _safeRoot,
                "LearningIntroOverlay",
                new Color(0.02f, 0.03f, 0.10f, 0.94f),
                Vector2.zero,
                Vector2.one,
                Vector2.zero,
                Vector2.zero);
            overlay.SetAsLastSibling();
            _introOverlay = overlay;
            RectTransform card = UiFactory.CreatePanel(
                overlay,
                "LearningIntroCard",
                Color.white,
                new Vector2(0.07f, 0.30f),
                new Vector2(0.93f, 0.74f),
                Vector2.zero,
                Vector2.zero);
            UiFactory.CreateText(
                card,
                "LearningIntroStep",
                korean
                    ? (_step.NewFeatures == GameFeature.None && _step.Number > 1
                        ? $"{_step.Number}단계"
                        : $"{_step.Number}단계 · 새 기능")
                    : $"Step {_step.Number}",
                24,
                GameTheme.Cyan,
                TextAnchor.MiddleCenter,
                new Vector2(0.10f, 0.76f),
                new Vector2(0.90f, 0.86f),
                Vector2.zero,
                Vector2.zero,
                FontStyle.Bold);
            UiFactory.UseTitleFont(UiFactory.CreateText(
                card,
                "LearningIntroTitle",
                korean ? _step.NameKo : _step.NameEn,
                42,
                GameTheme.Gold,
                TextAnchor.MiddleCenter,
                new Vector2(0.10f, 0.60f),
                new Vector2(0.90f, 0.76f),
                Vector2.zero,
                Vector2.zero));
            UiFactory.CreateText(
                card,
                "LearningIntroBody",
                korean ? _step.IntroKo : _step.IntroEn,
                26,
                GameTheme.Text,
                TextAnchor.MiddleCenter,
                new Vector2(0.12f, 0.30f),
                new Vector2(0.88f, 0.60f),
                Vector2.zero,
                Vector2.zero);
            UiFactory.CreateButton(
                card,
                "LearningIntroConfirm",
                korean ? "알겠어요" : "Got it",
                GameTheme.Violet,
                GameTheme.Text,
                28,
                new Vector2(0.24f, 0.10f),
                new Vector2(0.76f, 0.24f),
                Vector2.zero,
                Vector2.zero,
                () =>
                {
                    if (_introOverlay == null)
                    {
                        return;
                    }
                    Destroy(_introOverlay.gameObject);
                    _introOverlay = null;
                    _isPaused = false;
                });
        }

        public void OfferBossTreasure()
        {
            _treasurePending = false;
            if (_session == null ||
                _session.IsEnded ||
                _resultShown ||
                _treasureOverlay != null)
            {
                return;
            }

            bool korean = _bootstrap.Localization.IsKorean;
            _recipeBook?.Close();
            _isPaused = true;
            RectTransform overlay = UiFactory.CreatePanel(
                _safeRoot,
                "BossTreasureOverlay",
                new Color(0.02f, 0.03f, 0.10f, 0.97f),
                Vector2.zero,
                Vector2.one,
                Vector2.zero,
                Vector2.zero);
            overlay.SetAsLastSibling();
            _treasureOverlay = overlay;
            UiFactory.UseTitleFont(UiFactory.CreateText(
                overlay,
                "BossTreasureTitle",
                korean ? "보스 보물 상자" : "Boss treasure",
                46,
                GameTheme.Gold,
                TextAnchor.MiddleCenter,
                new Vector2(0.06f, 0.80f),
                new Vector2(0.94f, 0.88f),
                Vector2.zero,
                Vector2.zero));
            UiFactory.CreateText(
                overlay,
                "BossTreasureGuide",
                korean ? "보스를 쓰러뜨렸습니다! 보상 하나를 고르세요." : "Boss defeated! Pick one reward.",
                24,
                GameTheme.MutedText,
                TextAnchor.MiddleCenter,
                new Vector2(0.08f, 0.75f),
                new Vector2(0.92f, 0.80f),
                Vector2.zero,
                Vector2.zero);
            AudioService.Instance?.PlaySfx("legendary", 0.9f);

            IReadOnlyList<BossTreasure> options = BossTreasureRules.Draw(_random);
            for (int index = 0; index < options.Count; index++)
            {
                BossTreasure treasure = options[index];
                float top = 0.72f - index * 0.19f;
                Button button = UiFactory.CreateButton(
                    overlay,
                    $"BossTreasure_{index}",
                    string.Empty,
                    GameTheme.PanelLight,
                    GameTheme.Text,
                    24,
                    new Vector2(0.07f, top - 0.17f),
                    new Vector2(0.93f, top),
                    Vector2.zero,
                    Vector2.zero,
                    () => PickBossTreasure(overlay, treasure));
                RectTransform card = (RectTransform)button.transform;
                UiFactory.UseTitleFont(UiFactory.CreateText(
                    card,
                    "Name",
                    TreasureName(treasure, korean),
                    36,
                    GameTheme.Gold,
                    TextAnchor.MiddleCenter,
                    new Vector2(0.20f, 0.48f),
                    new Vector2(0.80f, 0.74f),
                    Vector2.zero,
                    Vector2.zero)).raycastTarget = false;
                UiFactory.CreateText(
                    card,
                    "Effect",
                    TreasureDescription(treasure, korean),
                    23,
                    GameTheme.Text,
                    TextAnchor.MiddleCenter,
                    new Vector2(0.27f, 0.20f),
                    new Vector2(0.73f, 0.46f),
                    Vector2.zero,
                    Vector2.zero).raycastTarget = false;
            }
        }

        private void PickBossTreasure(RectTransform overlay, BossTreasure treasure)
        {
            // Two fingers can land on two cards in one frame; take the first.
            if (_treasureOverlay == null || _treasureOverlay != overlay)
            {
                return;
            }

            _treasureOverlay = null;
            Destroy(overlay.gameObject);
            _isPaused = false;
            ApplyBossTreasure(treasure);
            ShowToast(TreasureName(treasure, _bootstrap.Localization.IsKorean));
        }

        public void ApplyBossTreasure(BossTreasure treasure)
        {
            if (_session == null || _inventory == null)
            {
                return;
            }

            switch (treasure)
            {
                case BossTreasure.StarBlessing:
                    if (!TryBlessStrongestSword())
                    {
                        _session.AddGold(BossTreasureRules.GoldHoard(_session.Wave));
                    }
                    break;
                case BossTreasure.GoldHoard:
                    _session.AddGold(BossTreasureRules.GoldHoard(_session.Wave));
                    break;
                case BossTreasure.UltimateCharge:
                    _session.AddUltimateCharge(100f);
                    break;
                case BossTreasure.FreeSummons:
                    _freeSummons += BossTreasureRules.FreeSummonCount;
                    break;
                case BossTreasure.RareSword:
                    if (!TrySummonRareSword())
                    {
                        _session.AddGold(BossTreasureRules.GoldHoard(_session.Wave));
                    }
                    break;
            }
            AudioService.Instance?.PlaySfx("forge", 0.9f);
            _gold = _session.Gold;
            RefreshBoard();
            RefreshHud();
        }

        // The best sword that can still rank up gets one more star.
        private bool TryBlessStrongestSword()
        {
            int best = -1;
            int bestScore = -1;
            for (int index = 0; index < _inventory.Capacity; index++)
            {
                SwordInstance sword = _inventory.Get(index);
                SwordDefinition definition = sword == null ? null : _catalog.GetSword(sword.DefinitionId);
                if (definition == null || sword.Stars >= RunBalanceRules.MaximumStars)
                {
                    continue;
                }
                int score = (int)definition.Rarity * 10 + sword.Stars;
                if (score > bestScore)
                {
                    bestScore = score;
                    best = index;
                }
            }
            if (best < 0)
            {
                return false;
            }

            SwordInstance chosen = _inventory.Get(best);
            _inventory.ReplaceSelection(
                new[] { best },
                new SwordInstance(
                    chosen.DefinitionId,
                    chosen.Stars + 1,
                    chosen.AppearanceSeed,
                    chosen.InstanceId));
            return true;
        }

        private bool TrySummonRareSword()
        {
            List<string> rares = _catalog.Swords.Values
                .Where(sword => sword.Rarity == SwordRarity.Rare)
                .Select(sword => sword.Id)
                .OrderBy(id => id)
                .ToList();
            if (rares.Count == 0 || _inventory.EmptyCount <= 0)
            {
                return false;
            }

            SwordInstance rare = new SwordInstance(
                rares[_random.NextInt(rares.Count)],
                1,
                _random.NextUlong(),
                Guid.NewGuid().ToString("N"));
            return _inventory.TryAdd(rare, out _);
        }

        private static string TreasureName(BossTreasure treasure, bool korean)
        {
            return treasure switch
            {
                BossTreasure.StarBlessing => korean ? "별의 축복" : "Star Blessing",
                BossTreasure.GoldHoard => korean ? "보물 금화" : "Gold Hoard",
                BossTreasure.UltimateCharge => korean ? "신성 충전" : "Holy Charge",
                BossTreasure.FreeSummons => korean ? "무료 소환권" : "Free Summons",
                _ => korean ? "희귀 검 상자" : "Rare Sword Chest"
            };
        }

        private string TreasureDescription(BossTreasure treasure, bool korean)
        {
            int gold = BossTreasureRules.GoldHoard(_session?.Wave ?? 1);
            return treasure switch
            {
                BossTreasure.StarBlessing => korean ? "가장 강한 검 ★+1" : "Your best sword gains a star",
                BossTreasure.GoldHoard => korean ? $"골드 +{gold}" : $"+{gold} gold",
                BossTreasure.UltimateCharge => korean ? "궁극기 즉시 100%" : "Ultimate fully charged",
                BossTreasure.FreeSummons => korean
                    ? $"다음 소환 {BossTreasureRules.FreeSummonCount}번 무료"
                    : $"Next {BossTreasureRules.FreeSummonCount} summons are free",
                _ => korean ? "희귀 검 하나를 빈칸에 소환" : "A rare sword joins your board"
            };
        }

        private void ShowEngravingList()
        {
            if (_isCombining ||
                _session == null ||
                _session.IsEnded ||
                _runeOverlay != null ||
                _engravingListOverlay != null)
            {
                return;
            }

            AudioService.PlayUiClick();
            bool korean = _bootstrap.Localization.IsKorean;
            _recipeBook?.Close();
            _isPaused = true;
            _engravingListOverlay = UiFactory.CreatePanel(
                _safeRoot,
                "EngravingListOverlay",
                new Color(0.02f, 0.03f, 0.10f, 0.97f),
                Vector2.zero,
                Vector2.one,
                Vector2.zero,
                Vector2.zero);
            _engravingListOverlay.SetAsLastSibling();
            List<RuneDefinition> owned = _session.SelectedRuneIds
                .Select(RunContentCatalog.GetRune)
                .Where(rune => rune != null)
                .ToList();
            UiFactory.CreateText(
                _engravingListOverlay,
                "EngravingListTitle",
                korean
                    ? $"새긴 각인 {owned.Count}"
                    : $"Engravings {owned.Count}",
                44,
                GameTheme.Text,
                TextAnchor.MiddleCenter,
                new Vector2(0.06f, 0.86f),
                new Vector2(0.94f, 0.94f),
                Vector2.zero,
                Vector2.zero);

            RectTransform viewport = UiFactory.CreatePanel(
                _engravingListOverlay,
                "EngravingListArea",
                Color.clear,
                new Vector2(0.06f, 0.16f),
                new Vector2(0.94f, 0.85f),
                Vector2.zero,
                Vector2.zero);
            viewport.gameObject.AddComponent<RectMask2D>();
            ScrollRect scroll = viewport.gameObject.AddComponent<ScrollRect>();
            scroll.horizontal = false;
            GameObject contentObject = new GameObject(
                "Content",
                typeof(RectTransform),
                typeof(VerticalLayoutGroup),
                typeof(ContentSizeFitter));
            contentObject.transform.SetParent(viewport, false);
            RectTransform content = contentObject.GetComponent<RectTransform>();
            content.anchorMin = new Vector2(0f, 1f);
            content.anchorMax = new Vector2(1f, 1f);
            // Same width as the viewport (a new RectTransform is 100 wider).
            content.sizeDelta = Vector2.zero;
            content.pivot = new Vector2(0.5f, 1f);
            content.anchoredPosition = Vector2.zero;
            VerticalLayoutGroup layout =
                contentObject.GetComponent<VerticalLayoutGroup>();
            layout.spacing = 10;
            layout.childControlHeight = true;
            layout.childForceExpandHeight = false;
            contentObject.GetComponent<ContentSizeFitter>().verticalFit =
                ContentSizeFitter.FitMode.PreferredSize;
            scroll.viewport = viewport;
            scroll.content = content;

            if (owned.Count == 0)
            {
                Text empty = UiFactory.CreateText(
                    content,
                    "EngravingListEmpty",
                    korean
                        ? "아직 새긴 각인이 없습니다.\n웨이브가 끝나면 고를 수 있어요."
                        : "No engravings yet.\nYou pick one after each wave.",
                    26,
                    GameTheme.MutedText,
                    TextAnchor.MiddleCenter,
                    Vector2.zero,
                    Vector2.one,
                    Vector2.zero,
                    Vector2.zero);
                empty.gameObject.AddComponent<LayoutElement>().preferredHeight = 160f;
            }

            foreach (RuneDefinition rune in owned)
            {
                GameObject row = new GameObject(
                    $"Engraving_{rune.Id}",
                    typeof(RectTransform),
                    typeof(Image),
                    typeof(LayoutElement));
                row.transform.SetParent(content, false);
                Image rowImage = row.GetComponent<Image>();
                rowImage.color = new Color(
                    GameTheme.Panel.r,
                    GameTheme.Panel.g,
                    GameTheme.Panel.b,
                    0.92f);
                UiFactory.AddOutline(
                    rowImage,
                    new Color(0.25f, 0.56f, 0.95f, 0.45f),
                    new Vector2(2f, -2f));
                row.GetComponent<LayoutElement>().preferredHeight = 150f;
                UiFactory.CreateText(
                    row.transform,
                    "Name",
                    korean ? rune.NameKo : rune.NameEn,
                    30,
                    rune.Rarity == RuneRarity.Common
                        ? GameTheme.Text
                        : RuneGradeColor(rune.Rarity),
                    TextAnchor.MiddleLeft,
                    new Vector2(0.05f, 0.36f),
                    new Vector2(0.95f, 0.70f),
                    Vector2.zero,
                    Vector2.zero);
                UiFactory.CreateText(
                    row.transform,
                    "Tag",
                    $"{RuneTagName(rune.Tag, korean)} · {RuneGradeName(rune.Rarity, korean)}",
                    20,
                    RuneGradeColor(rune.Rarity),
                    TextAnchor.MiddleRight,
                    new Vector2(0.05f, 0.70f),
                    new Vector2(0.95f, 0.95f),
                    Vector2.zero,
                    Vector2.zero);
                UiFactory.CreateText(
                    row.transform,
                    "Effect",
                    korean ? rune.DescriptionKo : rune.DescriptionEn,
                    22,
                    GameTheme.MutedText,
                    TextAnchor.MiddleLeft,
                    new Vector2(0.05f, 0.06f),
                    new Vector2(0.95f, 0.36f),
                    Vector2.zero,
                    Vector2.zero);
            }

            UiFactory.CreateButton(
                _engravingListOverlay,
                "EngravingListClose",
                korean ? "닫기" : "Close",
                GameTheme.PanelLight,
                GameTheme.Text,
                30,
                new Vector2(0.25f, 0.05f),
                new Vector2(0.75f, 0.125f),
                Vector2.zero,
                Vector2.zero,
                CloseEngravingList);
        }

        private void CloseEngravingList()
        {
            if (_engravingListOverlay == null)
            {
                return;
            }

            Destroy(_engravingListOverlay.gameObject);
            _engravingListOverlay = null;
            _isPaused = _runeOverlay != null || _battleCodexOverlay != null;
        }

        private void CloseBattleCodex()
        {
            if (_battleCodexOverlay != null)
            {
                Destroy(
                    _battleCodexOverlay.gameObject);
                _battleCodexOverlay = null;
            }
            _isPaused = false;
        }

        private void CreateBattleCodexRow(
            Transform parent,
            SwordDefinition sword)
        {
            bool unlocked =
                _bootstrap.SaveData.UnlockedSwordIds
                    .Contains(sword.Id);
            GameObject rowObject = new GameObject(
                $"BattleCodex_{sword.Id}",
                typeof(RectTransform),
                typeof(Image),
                typeof(LayoutElement));
            rowObject.transform.SetParent(parent, false);
            Image rowImage = rowObject.GetComponent<Image>();
            rowImage.color = new Color(0.025f, 0.045f, 0.12f, 0.94f);
            UiFactory.AddOutline(
                rowImage,
                new Color(0.25f, 0.56f, 0.95f, 0.42f),
                new Vector2(2f, -2f));
            rowObject.GetComponent<LayoutElement>()
                    .preferredHeight =
                110f;

            GameObject artObject = new GameObject(
                "SwordArt",
                typeof(RectTransform),
                typeof(Image));
            artObject.transform.SetParent(
                rowObject.transform,
                false);
            RectTransform artRect =
                artObject.GetComponent<RectTransform>();
            artRect.anchorMin =
                new Vector2(0.02f, 0.05f);
            artRect.anchorMax =
                new Vector2(0.18f, 0.95f);
            artRect.offsetMin = Vector2.zero;
            artRect.offsetMax = Vector2.zero;
            Image art =
                artObject.GetComponent<Image>();
            art.sprite = Resources.Load<Sprite>(
                $"Art/Swords/{sword.Id}");
            art.preserveAspect = true;
            art.raycastTarget = false;
            bool hiddenResult =
                _catalog.Recipes.Any(recipe =>
                    recipe.ResultSwordId == sword.Id &&
                    recipe.Visibility ==
                    RecipeVisibility.Hidden);
            art.color = unlocked
                ? Color.white
                : hiddenResult
                    ? new Color(0f, 0f, 0f, 0f)
                : new Color(
                    0.02f,
                    0.025f,
                    0.07f,
                    0.82f);

            SwordFamily family =
                SwordFamilyRules.Resolve(sword);
            string familyName =
                _bootstrap.Localization.IsKorean
                    ? SwordFamilyRules.NameKo(family)
                    : SwordFamilyRules.NameEn(family);
            UiFactory.CreateText(
                rowObject.transform,
                "Name",
                unlocked
                    ? _bootstrap.Localization
                        .SwordName(sword)
                    : "???",
                29,
                unlocked
                    ? RarityColor(sword.Rarity)
                    : GameTheme.MutedText,
                TextAnchor.MiddleLeft,
                new Vector2(0.21f, 0.44f),
                new Vector2(0.96f, 0.90f),
                Vector2.zero,
                Vector2.zero,
                FontStyle.Bold);
            UiFactory.CreateText(
                rowObject.transform,
                "Detail",
                unlocked
                    ? $"{familyName} · {_bootstrap.Localization.RarityName(sword.Rarity)}"
                    : (_bootstrap.Localization.IsKorean
                        ? "미발견"
                        : "UNDISCOVERED"),
                22,
                GameTheme.MutedText,
                TextAnchor.MiddleLeft,
                new Vector2(0.21f, 0.08f),
                new Vector2(0.96f, 0.46f),
                Vector2.zero,
                Vector2.zero);
        }

        public bool CanCraftRecipe(string recipeId)
        {
            RecipeDefinition recipe =
                _catalog?.GetRecipe(recipeId);
            if (recipe == null ||
                _inventory == null ||
                _isCombining ||
                !Has(GameFeature.Recipes))
            {
                return false;
            }

            bool discovered =
                _bootstrap.SaveData
                    .DiscoveredRecipeIds
                    .Contains(recipe.Id);
            if (recipe.Visibility ==
                    RecipeVisibility.Hidden &&
                !discovered)
            {
                return false;
            }

            return RecipeBoardRules
                .TryFindIngredientSlots(
                    _inventory,
                    recipe,
                    out _);
        }

        public bool TryCraftRecipe(string recipeId)
        {
            RecipeDefinition recipe =
                _catalog?.GetRecipe(recipeId);
            if (recipe == null ||
                !CanCraftRecipe(recipeId) ||
                !RecipeBoardRules
                    .TryFindIngredientSlots(
                        _inventory,
                        recipe,
                        out IReadOnlyList<int> slots))
            {
                return false;
            }

            StartCoroutine(
                CraftRecipeRoutine(
                    recipe,
                    slots,
                    false));
            return true;
        }

        public void RefreshLocalization()
        {
            if (_canvas == null || _bootstrap == null)
            {
                return;
            }

            RefreshBoard();
            RefreshHud();
            _recipeBook?.RefreshIfOpen();
        }

        private string ChooseSummonSwordId()
        {
            _lastSummonWasResonant = false;
            if (_summonCount == 0)
            {
                IReadOnlyList<string> baseIds =
                    _bootstrap.SaveData
                        .SelectedBaseSwordIds
                        .Where(id =>
                            _catalog.GetSword(id) != null)
                        .ToArray();
                if (baseIds.Count == 0)
                {
                    baseIds =
                        _catalog.BaseSwordIds;
                }
                _firstSummonSwordId =
                    baseIds[_random.NextInt(
                        baseIds.Count)];
                _demoRecipe = FindDemoRecipe(
                    _firstSummonSwordId);
                return _firstSummonSwordId;
            }
            if (_summonCount == 1 &&
                !string.IsNullOrWhiteSpace(
                    _firstSummonSwordId))
            {
                _lastSummonWasResonant = true;
                return _firstSummonSwordId;
            }
            if (_summonCount == 2 &&
                _demoRecipe != null)
            {
                _lastSummonWasResonant = true;
                return _demoRecipe
                    .IngredientSwordIds[0];
            }
            if (_summonCount == 3 &&
                _demoRecipe != null)
            {
                _lastSummonWasResonant = true;
                return _demoRecipe
                    .IngredientSwordIds[1];
            }

            IReadOnlyList<string> baseSwordIds =
                _bootstrap.SaveData
                    .SelectedBaseSwordIds
                    .Where(id =>
                        _catalog.GetSword(id) != null)
                    .ToArray();
            if (baseSwordIds.Count == 0)
            {
                baseSwordIds = _catalog.BaseSwordIds;
            }

            IReadOnlyList<string> resonanceCandidates =
                CombinationPlanner.FindResonanceCandidates(
                    _inventory,
                    _catalog,
                    _bootstrap.SaveData.DiscoveredRecipeIds,
                    baseSwordIds);
            bool guaranteedResonance = _summonsSinceForge >= 3;
            if (resonanceCandidates.Count > 0 &&
                (guaranteedResonance ||
                 _random.Chance(
                     _session.Modifiers.Has(RuneEffect.ResonanceBoost)
                         ? 0.70f
                         : 0.38f)))
            {
                _lastSummonWasResonant = true;
                return resonanceCandidates[
                    _random.NextInt(resonanceCandidates.Count)];
            }

            float advancedChance =
                RunBalanceRules.AdvancedSummonChance(
                    _bootstrap.SaveData.SmithingLevel);
            if (_random.Chance(advancedChance))
            {
                SwordRarity maximumRarity =
                    _bootstrap.SaveData.SmithingLevel >= 25
                        ? SwordRarity.Heroic
                        : SwordRarity.Rare;
                SwordDefinition[] advanced =
                    _bootstrap.SaveData.UnlockedSwordIds
                        .Select(_catalog.GetSword)
                        .Where(item =>
                            item != null &&
                            item.Rarity >=
                                SwordRarity.Rare &&
                            item.Rarity <= maximumRarity)
                        .ToArray();
                if (advanced.Length > 0)
                {
                    return advanced[
                        _random.NextInt(
                            advanced.Length)].Id;
                }
            }

            if (_session.Modifiers.Has(RuneEffect.EarlyStar) &&
                _random.Chance(0.10f))
            {
                SwordDefinition[] rares =
                    _bootstrap.SaveData.UnlockedSwordIds
                        .Select(_catalog.GetSword)
                        .Where(item =>
                            item != null &&
                            item.Rarity == SwordRarity.Rare)
                        .ToArray();
                if (rares.Length > 0)
                {
                    return rares[_random.NextInt(rares.Length)].Id;
                }
            }

            return baseSwordIds[
                _random.NextInt(baseSwordIds.Count)];
        }

        private RecipeDefinition FindDemoRecipe(
            string firstSwordId)
        {
            return _catalog.Recipes
                .FirstOrDefault(recipe =>
                    recipe.Visibility !=
                        RecipeVisibility.Hidden &&
                    recipe.IngredientSwordIds.Length == 2 &&
                    recipe.IngredientSwordIds
                        .Contains(firstSwordId) &&
                    recipe.IngredientSwordIds.All(id =>
                        _catalog.GetSword(id).Rarity ==
                        SwordRarity.Common))
                ?? _catalog.GetRecipe(
                    "recipe_scarlet_cyclone");
        }

        private bool TryReforgeDeadEnd()
        {
            if (_inventory == null ||
                _session == null ||
                _session.IsEnded ||
                _isCombining)
            {
                return false;
            }

            _combinationSnapshot ??=
                CombinationPlanner.Analyze(
                    _inventory,
                    _catalog,
                    _bootstrap.SaveData.DiscoveredRecipeIds);
            if (!CombinationPlanner.IsDeadlocked(
                    _inventory,
                    _combinationSnapshot))
            {
                ShowToast(
                    _bootstrap.Localization.IsKorean
                        ? "빛나는 검이나 조합서부터 확인하세요."
                        : "USE A GLOWING MERGE OR RECIPE FIRST.");
                return false;
            }
            if (Gold < ReforgeCost)
            {
                ShowToast(
                    _bootstrap.Localization.IsKorean
                        ? "재련할 골드가 부족합니다."
                        : "NOT ENOUGH GOLD TO REFORGE.");
                return false;
            }

            IReadOnlyList<string> allowed =
                _bootstrap.SaveData.SelectedBaseSwordIds
                    .Where(id => _catalog.GetSword(id) != null)
                    .ToArray();
            if (allowed.Count == 0)
            {
                allowed = _catalog.BaseSwordIds;
            }
            IReadOnlyList<string> candidates =
                CombinationPlanner.FindResonanceCandidates(
                    _inventory,
                    _catalog,
                    _bootstrap.SaveData.DiscoveredRecipeIds,
                    allowed);
            if (candidates.Count == 0)
            {
                candidates = allowed;
            }

            string targetId = candidates[
                _random.NextInt(candidates.Count)];
            int slotIndex = Enumerable
                .Range(0, _inventory.Capacity)
                .Where(index => _inventory.Get(index) != null)
                .OrderBy(index => _inventory.Get(index).Stars)
                .ThenBy(index =>
                    _inventory.Get(index).DefinitionId == targetId
                        ? 1
                        : 0)
                .ThenBy(index => index)
                .First();
            if (!_session.TrySpendGold(ReforgeCost))
            {
                return false;
            }

            SwordInstance result = new SwordInstance(
                targetId,
                1,
                _random.NextUlong(),
                Guid.NewGuid().ToString("N"));
            _inventory.ReplaceSelection(
                new[] { slotIndex },
                result);
            _gold = _session.Gold;
            _summonsSinceForge++;
            _lastSummonWasResonant = true;
            AudioService.Instance?.PlaySfx("forge", 0.72f);
            ShowToast(
                _bootstrap.Localization.IsKorean
                    ? "막힌 검을 공명 재련했습니다."
                    : "A DEAD-END SWORD WAS RESONANCE-REFORGED.");
            return true;
        }

        private void RegisterForgeChain()
        {
            _forgeChain = _forgeChainRemaining > 0f
                ? _forgeChain + 1
                : 1;
            _forgeChainRemaining = 10f;
            _summonsSinceForge = 0;
            _session?.Modifiers.SetForgeChain(_forgeChain);
        }

        private void CreateUltimateButton()
        {
            // Below the board, inside the enemy loop; beside it on 13+ slot boards.
            bool tallBoard = _inventory.Capacity > RunInventory.SlotCount;
            GameObject buttonObject = new GameObject(
                "UltimateButton",
                typeof(RectTransform),
                typeof(Image),
                typeof(Button));
            buttonObject.transform.SetParent(_safeRoot, false);
            RectTransform rect =
                buttonObject.GetComponent<RectTransform>();
            Vector2 anchor = tallBoard
                ? new Vector2(0.87f, 0.205f)
                : new Vector2(0.5f, 0.226f);
            rect.anchorMin = anchor;
            rect.anchorMax = anchor;
            rect.sizeDelta = tallBoard
                ? new Vector2(104f, 104f)
                : new Vector2(150f, 150f);
            Image background = buttonObject.GetComponent<Image>();
            background.sprite = UiFactory.CircleSprite;
            background.color = new Color(
                GameTheme.Background.r,
                GameTheme.Background.g,
                GameTheme.Background.b,
                0.88f);
            _ultimateButton = buttonObject.GetComponent<Button>();
            _ultimateButton.onClick.AddListener(UseUltimate);

            GameObject fillObject = new GameObject(
                "UltimateFill",
                typeof(RectTransform),
                typeof(Image));
            fillObject.transform.SetParent(rect, false);
            RectTransform fillRect =
                fillObject.GetComponent<RectTransform>();
            fillRect.anchorMin = Vector2.zero;
            fillRect.anchorMax = Vector2.one;
            fillRect.offsetMin = new Vector2(6f, 6f);
            fillRect.offsetMax = new Vector2(-6f, -6f);
            _ultimateFill = fillObject.GetComponent<Image>();
            _ultimateFill.sprite = UiFactory.CircleSprite;
            _ultimateFill.type = Image.Type.Filled;
            _ultimateFill.fillMethod = Image.FillMethod.Radial360;
            _ultimateFill.fillOrigin = (int)Image.Origin360.Top;
            _ultimateFill.raycastTarget = false;

            _ultimateLabel = UiFactory.CreateText(
                rect,
                "UltimateLabel",
                string.Empty,
                tallBoard ? 17 : 20,
                GameTheme.Text,
                TextAnchor.MiddleCenter,
                new Vector2(0.12f, 0.12f),
                new Vector2(0.88f, 0.88f),
                Vector2.zero,
                Vector2.zero);
            _ultimateLabel.raycastTarget = false;
            UiFactory.AddOutline(
                _ultimateLabel,
                new Color(0f, 0f, 0.06f, 0.95f),
                new Vector2(2f, -2f));
        }

        private void RefreshUltimateButton()
        {
            if (_ultimateButton == null || _session?.Ultimate == null)
            {
                return;
            }

            // Nothing to hit during a cleared break: don't invite a dead tap.
            bool ready = _session.UltimateReady && _session.AliveEnemyCount > 0;
            _ultimateFill.fillAmount = _session.UltimateCharge / 100f;
            _ultimateFill.color = ready
                ? GameTheme.Cyan
                : new Color(
                    GameTheme.Cyan.r,
                    GameTheme.Cyan.g,
                    GameTheme.Cyan.b,
                    0.38f);
            string name = _bootstrap.Localization.IsKorean
                ? _session.Ultimate.NameKo
                : _session.Ultimate.NameEn;
            _ultimateLabel.text = ready
                ? (_bootstrap.Localization.IsKorean
                    ? $"{name}\n발동!"
                    : $"{name}\nUSE!")
                : $"{name}\n{Mathf.FloorToInt(_session.UltimateCharge)}%";
            _ultimateLabel.color = ready
                ? GameTheme.Background
                : GameTheme.Text;
            // Dark text on the lit cyan disc reads cleanly without the dark outline.
            Outline labelOutline = _ultimateLabel.GetComponent<Outline>();
            if (labelOutline != null)
            {
                labelOutline.enabled = !ready;
            }
            float pulse = ready
                ? 1f + 0.06f * Mathf.Sin(Time.unscaledTime * 6f)
                : 1f;
            _ultimateButton.transform.localScale = Vector3.one * pulse;
            _ultimateButton.interactable = !_session.IsEnded;
        }

        private void CreateInterface()
        {
            _canvas = UiFactory.CreateCanvas(
                "CombinationDefenseCanvas",
                20);
            _canvas.transform.SetParent(transform, false);

            RectTransform background = UiFactory.CreatePanel(
                _canvas.transform,
                "StarlightCorridorBackground",
                Color.white,
                Vector2.zero,
                Vector2.one,
                Vector2.zero,
                Vector2.zero);
            Image backgroundImage =
                background.GetComponent<Image>();
            backgroundImage.sprite =
                Resources.Load<Sprite>(
                    "Art/UI/Battle/" +
                    "StarlightCorridorArena");
            backgroundImage.preserveAspect = false;
            backgroundImage.raycastTarget = false;
            background.SetAsFirstSibling();

            _safeRoot = UiFactory.CreatePanel(
                _canvas.transform,
                "BattleSafeArea",
                Color.clear,
                Vector2.zero,
                Vector2.one,
                Vector2.zero,
                Vector2.zero);
            _safeRoot.gameObject.AddComponent<SafeAreaFitter>();

            CreateTopHud();
            CreateArena();
            CreateCombinationGuide();
            CreateBottomControls();
            CreateUltimateButton();
            CreateDangerWarning();

            _toastText = UiFactory.CreateText(
                _safeRoot,
                "PrototypeToast",
                string.Empty,
                30,
                GameTheme.Gold,
                TextAnchor.MiddleCenter,
                new Vector2(0.08f, 0.125f),
                new Vector2(0.92f, 0.16f),
                Vector2.zero,
                Vector2.zero,
                FontStyle.Bold);
        }

        private void CreateCombinationGuide()
        {
            _combinationGuideRoot = UiFactory.CreatePanel(
                _safeRoot,
                "CombinationGuide",
                new Color(0.012f, 0.020f, 0.075f, 0.86f),
                new Vector2(0.075f, 0.805f),
                new Vector2(0.925f, 0.862f),
                Vector2.zero,
                Vector2.zero);
            Image guideBackground =
                _combinationGuideRoot.GetComponent<Image>();
            UiFactory.AddOutline(
                guideBackground,
                new Color(
                    GameTheme.Cyan.r,
                    GameTheme.Cyan.g,
                    GameTheme.Cyan.b,
                    0.42f),
                new Vector2(2f, -2f));

            _forgeStatusText = UiFactory.CreateText(
                _combinationGuideRoot,
                "ForgeStatus",
                string.Empty,
                23,
                GameTheme.Gold,
                TextAnchor.MiddleCenter,
                new Vector2(0.015f, 0.08f),
                new Vector2(0.235f, 0.92f),
                Vector2.zero,
                Vector2.zero,
                FontStyle.Bold);
            _forgeStatusText.raycastTarget = false;

            for (int index = 0; index < 3; index++)
            {
                float minX = 0.245f + index * 0.25f;
                RectTransform target = UiFactory.CreatePanel(
                    _combinationGuideRoot,
                    $"ForgeTarget{index}",
                    Color.clear,
                    new Vector2(minX, 0.08f),
                    new Vector2(minX + 0.24f, 0.92f),
                    Vector2.zero,
                    Vector2.zero);
                int targetIndex = index;
                target.gameObject.AddComponent<Button>()
                    .onClick.AddListener(() =>
                        HandleGuideTargetTapped(targetIndex));

                GameObject iconObject = new GameObject(
                    "ResultIcon",
                    typeof(RectTransform),
                    typeof(Image));
                iconObject.transform.SetParent(target, false);
                RectTransform iconRect =
                    iconObject.GetComponent<RectTransform>();
                iconRect.anchorMin = new Vector2(0.02f, 0.08f);
                iconRect.anchorMax = new Vector2(0.38f, 0.92f);
                iconRect.offsetMin = Vector2.zero;
                iconRect.offsetMax = Vector2.zero;
                Image icon = iconObject.GetComponent<Image>();
                icon.preserveAspect = true;
                icon.raycastTarget = false;
                _guideIcons.Add(icon);

                Text progress = UiFactory.CreateText(
                    target,
                    "Progress",
                    string.Empty,
                    22,
                    GameTheme.MutedText,
                    TextAnchor.MiddleLeft,
                    new Vector2(0.40f, 0.05f),
                    new Vector2(0.98f, 0.95f),
                    Vector2.zero,
                    Vector2.zero,
                    FontStyle.Bold);
                progress.raycastTarget = false;
                _guideProgressTexts.Add(progress);
            }
        }

        private void HandleGuideTargetTapped(int index)
        {
            if (_combinationSnapshot == null ||
                index >= _combinationSnapshot.SuggestedRecipes.Count)
            {
                return;
            }

            RecipeProgress progress =
                _combinationSnapshot.SuggestedRecipes[index];
            AudioService.PlayUiClick();
            if (!progress.IsReady ||
                !TryCraftRecipe(progress.Recipe.Id))
            {
                OpenRecipeBook();
            }
        }

        private void CreateTopHud()
        {
            RectTransform hud = UiFactory.CreatePanel(
                _safeRoot,
                "TopHud",
                Color.clear,
                new Vector2(0f, 0.895f),
                new Vector2(1f, 0.995f),
                Vector2.zero,
                Vector2.zero);
            hud.GetComponent<Image>().raycastTarget = false;

            _waveText = UiFactory.CreateText(
                hud,
                "Wave",
                string.Empty,
                32,
                GameTheme.Text,
                TextAnchor.MiddleLeft,
                new Vector2(0.035f, 0.60f),
                new Vector2(0.34f, 1f),
                Vector2.zero,
                Vector2.zero,
                FontStyle.Bold);
            UiFactory.AddOutline(
                _waveText,
                new Color(0f, 0f, 0.04f, 0.9f),
                new Vector2(2f, -2f));
            _ultimateChargeText = UiFactory.CreateText(
                hud,
                "UltimateCharge",
                string.Empty,
                27,
                GameTheme.Cyan,
                TextAnchor.MiddleLeft,
                new Vector2(0.035f, 0.46f),
                new Vector2(0.40f, 0.66f),
                Vector2.zero,
                Vector2.zero,
                FontStyle.Bold);
            _ultimateChargeText.raycastTarget = false;
            Button powerButton = _powerButton = UiFactory.CreatePanel(
                    hud,
                    "PowerButton",
                    Color.clear,
                    new Vector2(0.02f, 0.40f),
                    new Vector2(0.42f, 0.70f),
                    Vector2.zero,
                    Vector2.zero)
                .gameObject.AddComponent<Button>();
            powerButton.onClick.AddListener(ShowEngravingList);
            _missionText = UiFactory.CreateText(
                hud,
                "Mission",
                string.Empty,
                24,
                GameTheme.Cyan,
                TextAnchor.MiddleRight,
                new Vector2(0.44f, 0.46f),
                new Vector2(0.965f, 0.66f),
                Vector2.zero,
                Vector2.zero,
                FontStyle.Bold);
            _missionText.raycastTarget = false;
            UiFactory.AddOutline(
                _missionText,
                new Color(0f, 0f, 0.04f, 0.9f),
                new Vector2(2f, -2f));

            Button speedButton = UiFactory.CreateButton(
                hud,
                "SpeedButton",
                string.Empty,
                GameTheme.PanelLight,
                GameTheme.Text,
                24,
                new Vector2(0.60f, 0.58f),
                new Vector2(0.77f, 0.99f),
                Vector2.zero,
                Vector2.zero,
                ToggleGameSpeed);
            _speedLabel = speedButton.GetComponentInChildren<Text>();
            RefreshSpeedLabel();

            RectTransform gauge = UiFactory.CreatePanel(
                hud,
                "EnemyGauge",
                Color.clear,
                new Vector2(0.025f, 0.05f),
                new Vector2(0.975f, 0.53f),
                Vector2.zero,
                Vector2.zero);
            gauge.GetComponent<Image>().raycastTarget = false;
            RectTransform fill = UiFactory.CreatePanel(
                gauge,
                "EnemyGaugeFill",
                GameTheme.Coral,
                new Vector2(0.045f, 0.29f),
                new Vector2(0.955f, 0.71f),
                Vector2.zero,
                Vector2.zero);
            _enemyGaugeFill = fill.GetComponent<Image>();
            _enemyGaugeFill.raycastTarget = false;

            RectTransform gaugeFrame =
                UiFactory.CreatePanel(
                    gauge,
                    "EnemyGaugeOrnateFrame",
                    Color.white,
                    Vector2.zero,
                    Vector2.one,
                    Vector2.zero,
                    Vector2.zero);
            Image gaugeFrameImage =
                gaugeFrame.GetComponent<Image>();
            gaugeFrameImage.sprite =
                Resources.Load<Sprite>(
                    "Art/UI/Battle/" +
                    "EnemyGaugeFrame");
            gaugeFrameImage.preserveAspect = false;
            gaugeFrameImage.raycastTarget = false;
            gaugeFrame.SetAsFirstSibling();

            _enemyCountText = UiFactory.CreateText(
                hud,
                "EnemyCount",
                string.Empty,
                31,
                GameTheme.Gold,
                TextAnchor.MiddleCenter,
                new Vector2(0.24f, 0.55f),
                new Vector2(0.60f, 1f),
                Vector2.zero,
                Vector2.zero,
                FontStyle.Bold);
            UiFactory.AddOutline(
                _enemyCountText,
                new Color(0f, 0f, 0.04f, 0.95f),
                new Vector2(2f, -2f));

            _goldText = UiFactory.CreateText(
                hud,
                "Gold",
                string.Empty,
                33,
                GameTheme.Gold,
                TextAnchor.MiddleRight,
                new Vector2(0.70f, 0.60f),
                new Vector2(0.965f, 1f),
                Vector2.zero,
                Vector2.zero,
                FontStyle.Bold);
            UiFactory.AddOutline(
                _goldText,
                new Color(0f, 0f, 0.04f, 0.95f),
                new Vector2(2f, -2f));

            _bossGaugeRoot = UiFactory.CreatePanel(
                _safeRoot,
                "BossGauge",
                new Color(0.01f, 0.015f, 0.06f, 0.92f),
                new Vector2(0.16f, 0.865f),
                new Vector2(0.84f, 0.893f),
                Vector2.zero,
                Vector2.zero);
            UiFactory.AddOutline(
                _bossGaugeRoot.GetComponent<Image>(),
                new Color(1f, 0.58f, 0.16f, 0.88f),
                new Vector2(2f, -2f));
            RectTransform bossFill =
                UiFactory.CreatePanel(
                    _bossGaugeRoot,
                    "BossHealthFill",
                    new Color(0.88f, 0.22f, 0.30f, 0.96f),
                    new Vector2(0.01f, 0.12f),
                    new Vector2(0.99f, 0.88f),
                    Vector2.zero,
                    Vector2.zero);
            _bossHealthFill =
                bossFill.GetComponent<Image>();
            _bossHealthFill.raycastTarget = false;
            _bossNameText = UiFactory.CreateText(
                _bossGaugeRoot,
                "BossName",
                string.Empty,
                20,
                GameTheme.Text,
                TextAnchor.MiddleCenter,
                Vector2.zero,
                Vector2.one,
                Vector2.zero,
                Vector2.zero,
                FontStyle.Bold);
            _bossNameText.raycastTarget = false;
            _bossGaugeRoot.gameObject.SetActive(false);
        }

        private void CreateArena()
        {
            _arenaRoot = UiFactory.CreatePanel(
                _safeRoot,
                "LoopArena",
                Color.clear,
                new Vector2(0f, 0.10f),
                new Vector2(1f, 0.90f),
                Vector2.zero,
                Vector2.zero);
            _arenaRoot.GetComponent<Image>()
                .raycastTarget = false;

            _boardRoot = UiFactory.CreatePanel(
                _arenaRoot,
                "SwordBoard3x4",
                Color.clear,
                new Vector2(
                    0.185f,
                    _inventory.Capacity >
                    RunInventory.SlotCount
                        ? 0.10f
                        : 0.215f),
                new Vector2(0.815f, 0.815f),
                Vector2.zero,
                Vector2.zero);
            _boardRoot.GetComponent<Image>().raycastTarget = false;
            CreateBoardSlots();
            _combinationLinkRoot = UiFactory.CreatePanel(
                _boardRoot,
                "CombinationLinks",
                Color.clear,
                Vector2.zero,
                Vector2.one,
                Vector2.zero,
                Vector2.zero);
            _combinationLinkRoot.GetComponent<Image>()
                .raycastTarget = false;
            _combinationLinkRoot.SetAsFirstSibling();
        }

        private void CreatePathStrip(
            string name,
            Vector2 anchorMin,
            Vector2 anchorMax,
            Color color)
        {
            RectTransform strip = UiFactory.CreatePanel(
                _arenaRoot,
                name,
                color,
                anchorMin,
                anchorMax,
                Vector2.zero,
                Vector2.zero);
            strip.GetComponent<Image>().raycastTarget = false;
        }

        private void CreateBoardSlots()
        {
            for (int index = 0;
                 index < _inventory.Capacity;
                 index++)
            {
                int column = index % 3;
                int row = index / 3;
                const float columnSize = 1f / 3f;
                float rowSize =
                    1f /
                    Mathf.CeilToInt(
                        _inventory.Capacity / 3f);
                const float gapX = 0.025f;
                const float gapY = 0.025f;

                Vector2 anchorMin = new Vector2(
                    column * columnSize + gapX,
                    1f - (row + 1) * rowSize + gapY);
                Vector2 anchorMax = new Vector2(
                    (column + 1) * columnSize - gapX,
                    1f - row * rowSize - gapY);

                GameObject slotObject = new GameObject(
                    $"SwordBoardSlot{index}",
                    typeof(RectTransform),
                    typeof(Image),
                    typeof(CanvasGroup),
                    typeof(PrototypeSwordSlotView));
                slotObject.transform.SetParent(_boardRoot, false);
                RectTransform rect =
                    slotObject.GetComponent<RectTransform>();
                rect.anchorMin = anchorMin;
                rect.anchorMax = anchorMax;
                rect.offsetMin = Vector2.zero;
                rect.offsetMax = Vector2.zero;

                PrototypeSwordSlotView view =
                    slotObject.GetComponent<PrototypeSwordSlotView>();
                view.Initialize(
                    index,
                    _canvas,
                    TryMergeSlots,
                    HandleSwordDrag);
                _slotViews.Add(view);
            }
        }

        private void CreateBottomControls()
        {
            _summonButton = CreateBattleActionButton(
                "SummonButton",
                new Vector2(0.018f, 0.016f),
                new Vector2(0.326f, 0.105f),
                "ButtonFrameBlue",
                out _summonTitleText,
                out _summonValueText,
                () => TrySummonOrReforge());

            _recipeButton = CreateBattleActionButton(
                "RecipeBookButton",
                new Vector2(0.346f, 0.016f),
                new Vector2(0.654f, 0.105f),
                "ButtonFrameViolet",
                out _recipeTitleText,
                out _recipeValueText,
                OpenRecipeBook);

            _codexButton = CreateBattleActionButton(
                "CodexButton",
                new Vector2(0.674f, 0.016f),
                new Vector2(0.982f, 0.105f),
                "ButtonFrameBlue",
                out _codexTitleText,
                out _codexValueText,
                ShowBattleCodex);
        }

        private Button CreateBattleActionButton(
            string name,
            Vector2 anchorMin,
            Vector2 anchorMax,
            string frameSpriteName,
            out Text title,
            out Text value,
            Action action)
        {
            GameObject buttonObject = new GameObject(
                name,
                typeof(RectTransform),
                typeof(Image),
                typeof(Button));
            buttonObject.transform.SetParent(_safeRoot, false);
            RectTransform rect =
                buttonObject.GetComponent<RectTransform>();
            rect.anchorMin = anchorMin;
            rect.anchorMax = anchorMax;
            rect.offsetMin = Vector2.zero;
            rect.offsetMax = Vector2.zero;

            Image background = buttonObject.GetComponent<Image>();
            background.sprite = Resources.Load<Sprite>(
                $"Art/UI/Battle/{frameSpriteName}");
            background.type = Image.Type.Sliced;
            background.color = Color.white;
            // Keep the frame art's proportions instead of crushing its borders.
            buttonObject.AddComponent<SlicedFrameFitter>().Fit();

            Button button = buttonObject.GetComponent<Button>();
            ColorBlock colors = button.colors;
            colors.normalColor = Color.white;
            colors.highlightedColor =
                new Color(1.06f, 1.06f, 1.10f, 1f);
            colors.pressedColor =
                new Color(0.72f, 0.78f, 0.92f, 1f);
            colors.disabledColor =
                new Color(0.38f, 0.42f, 0.56f, 0.72f);
            button.colors = colors;
            button.onClick.AddListener(() =>
            {
                AudioService.PlayUiClick();
                action?.Invoke();
            });

            title = UiFactory.CreateText(
                buttonObject.transform,
                "ActionTitle",
                string.Empty,
                29,
                GameTheme.Text,
                TextAnchor.MiddleCenter,
                new Vector2(0.18f, 0.48f),
                new Vector2(0.82f, 0.80f),
                Vector2.zero,
                Vector2.zero,
                FontStyle.Bold);
            title.raycastTarget = false;
            UiFactory.AddOutline(
                title,
                new Color(0f, 0f, 0.08f, 0.92f),
                new Vector2(2f, -2f));

            value = UiFactory.CreateText(
                buttonObject.transform,
                "ActionValue",
                string.Empty,
                22,
                GameTheme.Gold,
                TextAnchor.MiddleCenter,
                new Vector2(0.26f, 0.20f),
                new Vector2(0.74f, 0.46f),
                Vector2.zero,
                Vector2.zero,
                FontStyle.Bold);
            value.raycastTarget = false;
            UiFactory.AddOutline(
                value,
                new Color(0f, 0f, 0.08f, 0.95f),
                new Vector2(2f, -2f));
            return button;
        }

        private void HandleEnemySpawned(
            BattleEnemyState enemy)
        {
            if (enemy == null || _arenaRoot == null)
            {
                return;
            }

            GameObject enemyObject = new GameObject(
                $"LoopEnemy_{enemy.RuntimeId}",
                typeof(RectTransform),
                typeof(Image),
                typeof(CanvasGroup),
                typeof(LoopEnemyPrototypeView));
            enemyObject.transform.SetParent(
                _arenaRoot,
                false);

            Image image = enemyObject.GetComponent<Image>();
            image.sprite =
                Resources.Load<Sprite>(
                    $"Art/Enemies/{enemy.Definition.Id}");
            image.color = Color.white;
            image.preserveAspect = true;
            image.raycastTarget = false;

            float size = enemy.Definition.IsBoss
                ? 150f
                : enemy.Definition.IsElite
                    ? 116f
                    : 84f +
                      (enemy.Wave % 3) * 5f;
            LoopEnemyPrototypeView view =
                enemyObject.GetComponent<
                    LoopEnemyPrototypeView>();
            view.Initialize(
                _arenaRoot,
                enemy,
                size);
            view.Tapped = FocusEnemy;
            _loopEnemies.Add(view);
            _enemyViews[enemy.RuntimeId] = view;
            _enemyCount = EnemyCount;

            _bootstrap.SaveData.SeenEnemyIds.Add(
                enemy.Definition.Id);
            _bootstrap.SaveData.SeenEnemyIds =
                _bootstrap.SaveData.SeenEnemyIds
                    .Distinct()
                    .ToList();
        }

        private void HandleSwordAttacked(
            BattleAttackEvent attack)
        {
            if (attack == null ||
                attack.SlotIndex < 0 ||
                attack.SlotIndex >= _slotViews.Count)
            {
                return;
            }

            PrototypeSwordSlotView source =
                _slotViews[attack.SlotIndex];
            source.PlayAttackPulse(
                attack.Definition.AttackStyle);

            List<LoopEnemyPrototypeView> targetViews =
                attack.Targets
                    .Select(target =>
                        _enemyViews.TryGetValue(
                            target.RuntimeId,
                            out LoopEnemyPrototypeView view)
                            ? view
                            : null)
                    .Where(view => view != null)
                    .ToList();
            foreach (LoopEnemyPrototypeView target in targetViews)
            {
                target.ShowDamage();
                _damagePopups?.Show(
                    (RectTransform)target.transform,
                    attack.DamagePerTarget,
                    attack.IsCritical);
            }

            _attackEffect?.Play(
                source.GetComponent<RectTransform>(),
                targetViews,
                attack);
            AudioService.Instance?.PlaySfx(
                attack.Definition.AttackStyle ==
                    AttackStyle.Area
                    ? "forge"
                    : "hit",
                0.20f);
        }

        private void HandleEnemyDefeated(
            BattleEnemyState enemy,
            int reward)
        {
            // Opens once nothing else is on screen (see Update).
            if (enemy != null && enemy.Definition.IsBoss && Has(GameFeature.Treasure))
            {
                _treasurePending = true;
            }
            if (enemy == null ||
                !_enemyViews.TryGetValue(
                    enemy.RuntimeId,
                    out LoopEnemyPrototypeView view))
            {
                return;
            }

            _enemyViews.Remove(enemy.RuntimeId);
            _loopEnemies.Remove(view);
            view.PlayDefeat();
            ShowGoldReward(view, reward);
            _gold = Gold;
            _enemyCount = EnemyCount;
            RefreshHud();
        }

        private void FocusEnemy(BattleEnemyState enemy)
        {
            if (!Has(GameFeature.Focus) ||
                _session == null ||
                _isPaused ||
                _session.IsEnded)
            {
                return;
            }

            _session.SetFocus(enemy);
            AudioService.PlayUiClick();
            RefreshFocusVisual();
        }

        private void RefreshFocusVisual()
        {
            BattleEnemyState focused = _session?.FocusedEnemy;
            LoopEnemyPrototypeView view =
                focused != null &&
                _enemyViews.TryGetValue(
                    focused.RuntimeId,
                    out LoopEnemyPrototypeView found)
                    ? found
                    : null;
            if (view == _focusedView)
            {
                return;
            }
            if (_focusedView != null)
            {
                _focusedView.SetFocused(false);
            }
            _focusedView = view;
            _focusedView?.SetFocused(true);
        }

        private void UseUltimate()
        {
            if (!Has(GameFeature.Ultimate) ||
                _session == null ||
                _isPaused ||
                !_session.TryUseUltimate())
            {
                return;
            }

            if (_session.Modifiers.Has(RuneEffect.UltimateFreeSummons))
            {
                _freeSummons = 3;
            }
            RecordMission(WaveMissionKind.Ultimate);
            RefreshHud();
        }

        private void HandleUltimateUsed(
            BattleUltimateEvent ultimate)
        {
            if (ultimate == null)
            {
                return;
            }

            foreach (BattleEnemyState target in
                     ultimate.Targets)
            {
                if (_enemyViews.TryGetValue(
                        target.RuntimeId,
                        out LoopEnemyPrototypeView view))
                {
                    view.ShowDamage();
                }
            }

            string name =
                _bootstrap.Localization.IsKorean
                    ? ultimate.Definition.NameKo
                    : ultimate.Definition.NameEn;
            StartCoroutine(
                UltimateVisualRoutine(
                    name,
                    ultimate.Definition.Effect));
            AudioService.Instance?.PlaySfx(
                "ultimate",
                0.92f);
            _gold = Gold;
        }

        private void HandleEnemyAbilityUsed(
            BattleEnemyAbilityEvent ability)
        {
            if (ability?.Caster == null ||
                ability.Definition == null)
            {
                return;
            }

            if (_enemyViews.TryGetValue(
                    ability.Caster.RuntimeId,
                    out LoopEnemyPrototypeView casterView))
            {
                casterView.ShowAbility();
            }
            foreach (BattleEnemyState affected in
                     ability.AffectedEnemies)
            {
                if (affected != ability.Caster &&
                    _enemyViews.TryGetValue(
                        affected.RuntimeId,
                        out LoopEnemyPrototypeView view))
                {
                    view.ShowAbility();
                }
            }

            string name =
                _bootstrap.Localization.IsKorean
                    ? ability.Definition.NameKo
                    : ability.Definition.NameEn;
            string drain = ability.GoldDrained > 0
                ? (_bootstrap.Localization.IsKorean
                    ? $" · 골드 -{ability.GoldDrained}"
                    : $" · GOLD -{ability.GoldDrained}")
                : string.Empty;
            ShowToast($"⚠ {name}{drain}");
            _gold = Gold;
            RefreshHud();
        }

        private void HandleWaveStarted(int wave)
        {
            ShowToast(
                wave == 6
                    ? (_bootstrap.Localization.IsKorean
                        ? "균열 갑주 출현 · 1★ 일반 검을 조합하세요!"
                        : "RIFT ARMOR · COMBINE 1★ COMMON SWORDS!")
                    : (_bootstrap.Localization.IsKorean
                        ? $"웨이브 {wave} 시작"
                        : $"WAVE {wave} START"));
            AudioService.Instance?.PlaySfx(
                wave % 5 == 0 ? "legendary" : "summon",
                wave % 5 == 0 ? 0.70f : 0.32f);
            StartMission(Has(GameFeature.Missions) ? WaveMissionRules.Create(wave, _random) : null);
        }

        public void StartMission(WaveMission mission)
        {
            _mission = mission;
            RefreshMissionText();
        }

        private void RecordMission(WaveMissionKind kind)
        {
            PayMissionIf(_mission?.Record(kind) == true);
            RefreshMissionText();
        }

        private void PayMissionIf(bool completed)
        {
            if (completed && _session != null)
            {
                _session.AddGold(_mission.Reward);
                _gold = _session.Gold;
                AudioService.Instance?.PlaySfx("legendary", 0.6f);
                ShowToast(
                    _bootstrap.Localization.IsKorean
                        ? $"미션 완료!  +✦ {_mission.Reward}"
                        : $"Mission complete!  +✦ {_mission.Reward}");
            }
            RefreshMissionText();
        }

        private void RefreshMissionText()
        {
            if (_missionText == null)
            {
                return;
            }
            if (_mission == null)
            {
                _missionText.text = string.Empty;
                return;
            }

            bool korean = _bootstrap.Localization.IsKorean;
            string action = _mission.Kind switch
            {
                WaveMissionKind.Summon => korean ? "소환" : "Summon",
                WaveMissionKind.Merge => korean ? "합성" : "Merge",
                WaveMissionKind.Forge => korean ? "제작" : "Forge",
                WaveMissionKind.Sell => korean ? "판매" : "Sell",
                _ => korean ? "궁극기" : "Ultimate"
            };
            _missionText.text = _mission.IsComplete
                ? (korean ? "미션 완료 ✓" : "Mission done ✓")
                : (korean
                    ? $"미션 {action} {_mission.Progress}/{_mission.Target} · ✦{_mission.Reward}"
                    : $"{action} {_mission.Progress}/{_mission.Target} · ✦{_mission.Reward}");
            _missionText.color = _mission.IsComplete ? GameTheme.Gold : GameTheme.Cyan;
        }

        private void HandleSessionStateChanged(
            CombinationDefenseState state)
        {
            if (state == CombinationDefenseState.Victory ||
                state == CombinationDefenseState.Defeat ||
                state == CombinationDefenseState.Abandoned)
            {
                ShowRunResult(state);
            }
            RefreshHud();
        }

        private void RefreshBoard()
        {
            if (_inventory == null ||
                _slotViews.Count != _inventory.Capacity)
            {
                return;
            }

            for (int index = 0;
                 index < _inventory.Capacity;
                 index++)
            {
                SwordInstance instance = _inventory.Get(index);
                SwordDefinition definition = instance == null
                    ? null
                    : _catalog.GetSword(instance.DefinitionId);
                string displayName = definition == null
                    ? string.Empty
                    : (_bootstrap.Localization.IsKorean
                        ? definition.NameKo
                        : definition.NameEn);
                _slotViews[index].Bind(
                    instance,
                    definition,
                    displayName);
            }

            _combinationSnapshot = CombinationPlanner.Analyze(
                _inventory,
                _catalog,
                _bootstrap.SaveData.DiscoveredRecipeIds);
            HashSet<int> mergeReadySlots =
                new HashSet<int>(
                    _combinationSnapshot.MergeOpportunities
                        .SelectMany(item => new[]
                        {
                            item.FirstSlot,
                            item.SecondSlot
                        }));
            for (int index = 0;
                 index < _slotViews.Count;
                 index++)
            {
                _slotViews[index].SetMergeReady(
                    mergeReadySlots.Contains(index));
            }
            RefreshCombinationLinks();
        }

        private void RefreshCombinationLinks()
        {
            if (_combinationLinkRoot == null)
            {
                return;
            }

            foreach (Image link in _combinationLinks)
            {
                if (link != null)
                {
                    Destroy(link.gameObject);
                }
            }
            _combinationLinks.Clear();
            foreach (Image link in _elementLinks)
            {
                if (link != null)
                {
                    Destroy(link.gameObject);
                }
            }
            _elementLinks.Clear();
            if (_combinationSnapshot == null)
            {
                return;
            }

            Canvas.ForceUpdateCanvases();
            foreach (ConstellationLink link in
                     Has(GameFeature.Constellations)
                         ? ConstellationRules.Links(_inventory, _catalog)
                         : Enumerable.Empty<ConstellationLink>())
            {
                Color color = ElementColor(link.Element);
                color.a = 0.55f;
                _elementLinks.Add(CreateSlotLine(
                    "ElementConstellationLink",
                    link.First,
                    link.Second,
                    7f,
                    color));
            }
            foreach (MergeOpportunity opportunity in
                     _combinationSnapshot.MergeOpportunities)
            {
                RectTransform first =
                    _slotViews[opportunity.FirstSlot]
                        .transform as RectTransform;
                RectTransform second =
                    _slotViews[opportunity.SecondSlot]
                        .transform as RectTransform;
                if (first == null || second == null)
                {
                    continue;
                }

                Vector2 start =
                    _combinationLinkRoot.InverseTransformPoint(
                        first.TransformPoint(first.rect.center));
                Vector2 end =
                    _combinationLinkRoot.InverseTransformPoint(
                        second.TransformPoint(second.rect.center));
                Vector2 direction = end - start;
                GameObject lineObject = new GameObject(
                    "MergeConstellationLink",
                    typeof(RectTransform),
                    typeof(Image));
                lineObject.transform.SetParent(
                    _combinationLinkRoot,
                    false);
                RectTransform lineRect =
                    lineObject.GetComponent<RectTransform>();
                lineRect.anchorMin = new Vector2(0.5f, 0.5f);
                lineRect.anchorMax = new Vector2(0.5f, 0.5f);
                lineRect.pivot = new Vector2(0.5f, 0.5f);
                lineRect.anchoredPosition = (start + end) * 0.5f;
                lineRect.sizeDelta =
                    new Vector2(direction.magnitude, 4.5f);
                lineRect.localRotation = Quaternion.Euler(
                    0f,
                    0f,
                    Mathf.Atan2(direction.y, direction.x) *
                    Mathf.Rad2Deg);
                Image line = lineObject.GetComponent<Image>();
                line.color = new Color(
                    GameTheme.Gold.r,
                    GameTheme.Gold.g,
                    GameTheme.Gold.b,
                    0.64f);
                line.raycastTarget = false;
                _combinationLinks.Add(line);
            }
        }

        private Image CreateSlotLine(
            string name,
            int firstSlot,
            int secondSlot,
            float thickness,
            Color color)
        {
            RectTransform first =
                _slotViews[firstSlot].transform as RectTransform;
            RectTransform second =
                _slotViews[secondSlot].transform as RectTransform;
            Vector2 start = _combinationLinkRoot.InverseTransformPoint(
                first.TransformPoint(first.rect.center));
            Vector2 end = _combinationLinkRoot.InverseTransformPoint(
                second.TransformPoint(second.rect.center));
            Vector2 direction = end - start;
            GameObject lineObject = new GameObject(
                name,
                typeof(RectTransform),
                typeof(Image));
            lineObject.transform.SetParent(_combinationLinkRoot, false);
            RectTransform lineRect =
                lineObject.GetComponent<RectTransform>();
            lineRect.anchorMin = new Vector2(0.5f, 0.5f);
            lineRect.anchorMax = new Vector2(0.5f, 0.5f);
            lineRect.pivot = new Vector2(0.5f, 0.5f);
            lineRect.anchoredPosition = (start + end) * 0.5f;
            lineRect.sizeDelta = new Vector2(direction.magnitude, thickness);
            lineRect.localRotation = Quaternion.Euler(
                0f,
                0f,
                Mathf.Atan2(direction.y, direction.x) * Mathf.Rad2Deg);
            Image line = lineObject.GetComponent<Image>();
            line.color = color;
            line.raycastTarget = false;
            return line;
        }

        private static Color ElementColor(SwordElement element)
        {
            return element switch
            {
                SwordElement.Flame => GameTheme.FromHex(0xF0643A),
                SwordElement.Frost => GameTheme.FromHex(0x73C8F2),
                SwordElement.Gale => GameTheme.FromHex(0x79D9B4),
                SwordElement.Thunder => GameTheme.FromHex(0xE8D84A),
                SwordElement.Earth => GameTheme.FromHex(0xB08A62),
                SwordElement.Nature => GameTheme.FromHex(0x6BCB63),
                SwordElement.Light => GameTheme.FromHex(0xFFE8A3),
                SwordElement.Shadow => GameTheme.FromHex(0x9A75D6),
                _ => GameTheme.MutedText
            };
        }

        private void AnimateCombinationLinks()
        {
            if (_combinationLinks.Count == 0)
            {
                return;
            }

            float pulse =
                0.5f +
                0.5f * Mathf.Sin(Time.unscaledTime * 4.8f);
            float alpha = Mathf.Lerp(0.28f, 0.88f, pulse);
            foreach (Image link in _combinationLinks)
            {
                if (link == null)
                {
                    continue;
                }
                Color color = link.color;
                color.a = alpha;
                link.color = color;
            }
        }

        private void RefreshCombinationGuide()
        {
            if (_combinationGuideRoot == null ||
                _combinationSnapshot == null)
            {
                return;
            }

            bool korean = _bootstrap.Localization.IsKorean;
            int mergeCount =
                _combinationSnapshot.MergeOpportunities.Count;
            if (mergeCount > 0)
            {
                _forgeStatusText.text = korean
                    ? $"드래그 합성\n{mergeCount}쌍 준비"
                    : $"DRAG TO MERGE\n{mergeCount} PAIR READY";
            }
            else if (_forgeChainRemaining > 0f)
            {
                _forgeStatusText.text = korean
                    ? $"조합 연쇄 ×{_forgeChain}\n소환 -{ForgeChainPercent}%"
                    : $"FORGE CHAIN ×{_forgeChain}\nSUMMON -{ForgeChainPercent}%";
            }
            else
            {
                _forgeStatusText.text = korean
                    ? $"조합 목표\n공명 {Math.Min(3, _summonsSinceForge)}/3"
                    : $"FORGE TARGETS\nRESONANCE {Math.Min(3, _summonsSinceForge)}/3";
            }

            for (int index = 0; index < _guideIcons.Count; index++)
            {
                bool hasRecipe =
                    index <
                    _combinationSnapshot.SuggestedRecipes.Count;
                _guideIcons[index].gameObject.SetActive(hasRecipe);
                _guideProgressTexts[index].gameObject.SetActive(hasRecipe);
                if (!hasRecipe)
                {
                    continue;
                }

                RecipeProgress progress =
                    _combinationSnapshot.SuggestedRecipes[index];
                SwordDefinition result = _catalog.GetSword(
                    progress.Recipe.ResultSwordId);
                _guideIcons[index].sprite = Resources.Load<Sprite>(
                    $"Art/Swords/{result.Id}");
                bool discovered =
                    _bootstrap.SaveData.DiscoveredRecipeIds
                        .Contains(progress.Recipe.Id);
                _guideIcons[index].color = discovered
                    ? Color.white
                    : new Color(0.36f, 0.42f, 0.78f, 1f);
                _guideProgressTexts[index].text = progress.IsReady
                    ? (korean ? "눌러서\n제작" : "TAP TO\nFORGE")
                    : $"{progress.MatchedIngredients}/{progress.RequiredIngredients}";
                _guideProgressTexts[index].color = progress.IsReady
                    ? GameTheme.Gold
                    : GameTheme.Cyan;
            }
        }

        private void HandleInventoryChanged()
        {
            RefreshBoard();
            RefreshHud();
            _recipeBook?.RefreshIfOpen();
            if (!_isCombining)
            {
                TryStartHiddenDiscovery();
            }
        }

        private void TryStartHiddenDiscovery()
        {
            if (_isCombining ||
                _inventory == null ||
                _bootstrap == null ||
                !Has(GameFeature.Recipes))
            {
                return;
            }

            foreach (RecipeDefinition recipe in
                     _catalog.Recipes)
            {
                if (recipe.Visibility !=
                        RecipeVisibility.Hidden ||
                    _bootstrap.SaveData
                        .DiscoveredRecipeIds
                        .Contains(recipe.Id) ||
                    !RecipeBoardRules
                        .TryFindIngredientSlots(
                            _inventory,
                            recipe,
                            out IReadOnlyList<int> slots) ||
                    !RecipeBoardRules
                        .MeetsHiddenCondition(
                            _inventory,
                            recipe,
                            slots))
                {
                    continue;
                }

                StartCoroutine(
                    CraftRecipeRoutine(
                        recipe,
                        slots,
                        true));
                return;
            }
        }

        private System.Collections.IEnumerator
            CraftRecipeRoutine(
                RecipeDefinition recipe,
                IReadOnlyList<int> slotIndices,
                bool hiddenTrigger)
        {
            if (_isCombining)
            {
                yield break;
            }

            IReadOnlyList<SwordInstance> ingredients =
                _inventory.GetSelection(slotIndices);
            ForgeResult forgeResult =
                _forgeRules.TryRecipe(ingredients);
            if (!forgeResult.Success)
            {
                yield break;
            }

            _isCombining = true;
            _recipeBook?.Close();
            List<RectTransform> slotRects =
                new List<RectTransform>();
            List<Sprite> materialSprites =
                new List<Sprite>();
            foreach (int slotIndex in slotIndices)
            {
                SwordInstance material =
                    _inventory.Get(slotIndex);
                slotRects.Add(
                    _slotViews[slotIndex]
                        .GetComponent<RectTransform>());
                materialSprites.Add(
                    Resources.Load<Sprite>(
                        $"Art/Swords/" +
                        $"{material.DefinitionId}"));
            }

            SwordDefinition resultDefinition =
                _catalog.GetSword(
                    recipe.ResultSwordId);
            bool firstDiscovery =
                !_bootstrap.SaveData
                    .DiscoveredRecipeIds
                    .Contains(recipe.Id);
            bool forged = false;
            yield return _forgeEffect.Play(
                _canvas,
                slotRects,
                materialSprites,
                Resources.Load<Sprite>(
                    $"Art/Swords/" +
                    $"{resultDefinition.Id}"),
                _bootstrap.Localization
                    .SwordName(resultDefinition),
                DiscoveryLabel(
                    firstDiscovery,
                    hiddenTrigger),
                RarityColor(resultDefinition.Rarity),
                firstDiscovery,
                () =>
                {
                    _inventory.ReplaceSelection(
                        slotIndices,
                        forgeResult.Result);
                    _bootstrap.SaveData
                        .DiscoverRecipe(recipe.Id);
                    _bootstrap.SaveData
                        .UnlockSword(
                            resultDefinition.Id);
                    _bootstrap.Save();
                    forged = true;
                });

            if (forged)
            {
                LastCraftedRecipeId = recipe.Id;
                LastCraftWasFirstDiscovery =
                    firstDiscovery;
                _recipeForges++;
                RegisterForgeChain();
                _session.RegisterForge();
                _session.Modifiers.RegisterCraftedResult(
                    recipe.ResultSwordId);
                if (_session.Modifiers.Has(RuneEffect.RecipeCharge))
                {
                    _session.AddUltimateCharge(25f);
                }
                AudioService.Instance?.PlaySfx(
                    hiddenTrigger
                        ? "legendary"
                        : "forge",
                    hiddenTrigger ? 1f : 0.9f);
                RecordMission(WaveMissionKind.Forge);
            }

            _isCombining = false;
            RefreshBoard();
            RefreshHud();
            _recipeBook?.RefreshIfOpen();
            if (forged)
            {
                TryRegisterRareAppearance(
                    forgeResult.Result,
                    resultDefinition);
            }
            TryStartHiddenDiscovery();
        }

        private bool TryRegisterRareAppearance(
            SwordInstance instance,
            SwordDefinition definition)
        {
            if (instance == null ||
                definition == null ||
                _bootstrap == null)
            {
                return false;
            }

            AppearanceResult appearance =
                AppearanceRules.Evaluate(
                    instance.AppearanceSeed,
                    definition.Rarity);
            if (!appearance.IsMutation)
            {
                return false;
            }

            string collectionId =
                AppearanceRules.CollectionId(instance);
            if (_bootstrap.SaveData.OwnedAppearanceIds
                    .Contains(collectionId))
            {
                return false;
            }

            _bootstrap.SaveData.OwnedAppearanceIds.Add(
                collectionId);
            _bootstrap.Save();
            if (!_isCombining)
            {
                StartCoroutine(
                    RareAppearanceRoutine(
                        instance,
                        definition,
                        appearance));
            }
            else
            {
                ShowToast(
                    _bootstrap.Localization.IsKorean
                        ? "희귀 외형이 도감에 등록되었습니다."
                        : "RARE APPEARANCE ADDED TO THE CODEX.");
            }
            return true;
        }

        private System.Collections.IEnumerator
            RareAppearanceRoutine(
                SwordInstance instance,
                SwordDefinition definition,
                AppearanceResult appearance)
        {
            _isCombining = true;
            Color accent =
                GameTheme.FromHex(
                    appearance.AccentRgb);
            RectTransform overlay = UiFactory.CreatePanel(
                _safeRoot,
                "RareAppearanceReveal",
                new Color(0.005f, 0.008f, 0.035f, 0.94f),
                Vector2.zero,
                Vector2.one,
                Vector2.zero,
                Vector2.zero);
            overlay.SetAsLastSibling();
            CanvasGroup group =
                overlay.gameObject
                    .AddComponent<CanvasGroup>();
            group.alpha = 0f;

            UiFactory.CreateText(
                overlay,
                "RareAppearanceTitle",
                _bootstrap.Localization.IsKorean
                    ? "희귀 외형 발견"
                    : "RARE APPEARANCE",
                46,
                accent,
                TextAnchor.MiddleCenter,
                new Vector2(0.08f, 0.76f),
                new Vector2(0.92f, 0.90f),
                Vector2.zero,
                Vector2.zero,
                FontStyle.Bold);

            GameObject swordObject = new GameObject(
                "RareSword",
                typeof(RectTransform),
                typeof(Image),
                typeof(Outline));
            swordObject.transform.SetParent(
                overlay,
                false);
            RectTransform swordRect =
                swordObject.GetComponent<RectTransform>();
            swordRect.anchorMin =
                new Vector2(0.18f, 0.27f);
            swordRect.anchorMax =
                new Vector2(0.82f, 0.76f);
            swordRect.offsetMin = Vector2.zero;
            swordRect.offsetMax = Vector2.zero;
            Image swordImage =
                swordObject.GetComponent<Image>();
            swordImage.sprite = Resources.Load<Sprite>(
                $"Art/Swords/{definition.Id}");
            swordImage.preserveAspect = true;
            swordImage.color =
                Color.Lerp(
                    Color.white,
                    accent,
                    0.20f);
            swordImage.raycastTarget = false;
            Outline glow =
                swordObject.GetComponent<Outline>();
            glow.effectColor = accent;
            glow.effectDistance =
                new Vector2(14f, -14f);

            string variant =
                appearance.Variant switch
                {
                    AppearanceVariant.Aurora =>
                        _bootstrap.Localization.IsKorean
                            ? "오로라"
                            : "AURORA",
                    AppearanceVariant.Starforged =>
                        _bootstrap.Localization.IsKorean
                            ? "별벼림"
                            : "STARFORGED",
                    AppearanceVariant.Voidglass =>
                        _bootstrap.Localization.IsKorean
                            ? "공허유리"
                            : "VOIDGLASS",
                    _ => string.Empty
                };
            UiFactory.CreateText(
                overlay,
                "RareSwordName",
                $"{_bootstrap.Localization.SwordName(definition)}\n{variant}",
                34,
                GameTheme.Text,
                TextAnchor.MiddleCenter,
                new Vector2(0.08f, 0.12f),
                new Vector2(0.92f, 0.28f),
                Vector2.zero,
                Vector2.zero,
                FontStyle.Bold);

            AudioService.Instance?.PlaySfx(
                "legendary",
                1f);
            float elapsed = 0f;
            float duration =
                _bootstrap.SaveData.ReducedMotion
                    ? 0.65f
                    : 1.25f;
            while (elapsed < duration)
            {
                elapsed += Time.unscaledDeltaTime;
                float progress =
                    Mathf.Clamp01(elapsed / duration);
                group.alpha =
                    Mathf.Clamp01(
                        Mathf.Min(
                            progress * 5f,
                            (1f - progress) * 5f));
                float pulse =
                    _bootstrap.SaveData.ReducedMotion
                        ? 1f
                        : 0.92f +
                          Mathf.Sin(
                              progress * Mathf.PI) *
                          0.12f;
                swordRect.localScale =
                    Vector3.one * pulse;
                yield return null;
            }

            Destroy(overlay.gameObject);
            _isCombining = false;
            RefreshBoard();
            RefreshHud();
        }

        private static Color RarityColor(
            SwordRarity rarity)
        {
            return rarity switch
            {
                SwordRarity.Rare =>
                    GameTheme.Cyan,
                SwordRarity.Heroic =>
                    GameTheme.Violet,
                SwordRarity.Legendary =>
                    GameTheme.Gold,
                SwordRarity.Mythic =>
                    new Color(0.62f, 0.90f, 1f, 1f),
                _ => GameTheme.MutedText
            };
        }

        private string DiscoveryLabel(
            bool firstDiscovery,
            bool hiddenLegend)
        {
            if (_bootstrap.Localization.IsKorean)
            {
                if (hiddenLegend)
                {
                    return "히든 전설 발견";
                }
                return firstDiscovery
                    ? "새 별자리 발견"
                    : "별자리 단조";
            }

            if (hiddenLegend)
            {
                return "HIDDEN LEGEND";
            }
            return firstDiscovery
                ? "NEW CONSTELLATION"
                : "CONSTELLATION FORGE";
        }

        private void RefreshHud()
        {
            if (_waveText == null)
            {
                return;
            }

            _waveText.text = WaveLabel();
            _goldText.text = $"✦ {Gold:N0}";
            _enemyCountText.text =
                (_bootstrap.Localization.IsKorean
                    ? $"적 {Pressure}/{EnemyLimit}"
                    : $"ENEMIES {Pressure}/{EnemyLimit}") +
                // On a step, the stars still reachable drop as the peak rises.
                (_step != null && _session != null
                    ? $"  {LearningPath.StarText(LearningPath.Stars(true, _session.PeakPressure))}"
                    : string.Empty);

            RectTransform fillRect =
                _enemyGaugeFill.rectTransform;
            fillRect.anchorMax = new Vector2(
                Mathf.Lerp(
                    0.045f,
                    0.955f,
                    Mathf.Clamp01(
                        Pressure /
                        (float)EnemyLimit)),
                0.71f);

            SetButtonLabel(
                _summonButton,
                string.Empty);
            bool korean = _bootstrap.Localization.IsKorean;
            bool boardFull = _inventory.EmptyCount <= 0;
            bool deadlocked = boardFull &&
                              _combinationSnapshot != null &&
                              (Has(GameFeature.Recipes)
                                  ? CombinationPlanner.IsDeadlocked(
                                      _inventory,
                                      _combinationSnapshot)
                                  // Before recipes open only merges can free a slot.
                                  : _combinationSnapshot.MergeOpportunities.Count == 0);
            int dragSellValue = Has(GameFeature.Sell) ? SellValue(_draggingSlot) : 0;
            bool selling = dragSellValue > 0 && !_isCombining;
            _summonTitleText.text = selling
                ? (korean ? "판매" : "SELL")
                : boardFull
                    ? deadlocked
                        ? (korean ? "공명 재련" : "REFORGE")
                        : (korean ? "보드 가득" : "BOARD FULL")
                    : (korean ? "소환" : "SUMMON");
            _summonValueText.text = selling
                ? $"+✦ {dragSellValue}"
                : boardFull
                    ? deadlocked
                        ? $"✦ {ReforgeCost}"
                        : !Has(GameFeature.Sell)
                            ? (korean ? "합성해서 비우기" : "MERGE TO FREE")
                            : _combinationSnapshot != null &&
                              _combinationSnapshot.HasImmediateAction
                                ? (korean ? "조합 또는 판매" : "FORGE OR SELL")
                                : (korean ? "검 끌어와 판매" : "DRAG HERE: SELL")
                    : $"✦ {SummonCost}";
            _recipeTitleText.text = korean ? "조합서" : "RECIPES";
            _recipeValueText.text = ReadyRecipeCount > 0
                ? (korean
                    ? $"제작 가능 {ReadyRecipeCount}"
                    : $"{ReadyRecipeCount} READY")
                : (korean ? "재료 힌트" : "MATERIAL HINTS");
            if (!Has(GameFeature.Recipes))
            {
                _recipeValueText.text = korean ? "2단계에 열림" : "Opens at step 2";
            }
            _codexTitleText.text = korean ? "도감" : "CODEX";
            _codexValueText.text = korean
                ? $"발견 {_bootstrap.SaveData.UnlockedSwordIds.Count}"
                : $"{_bootstrap.SaveData.UnlockedSwordIds.Count} FOUND";
            _summonButton.interactable =
                !_isCombining &&
                (_session == null || !_session.IsEnded) &&
                (selling ||
                 (boardFull
                     ? deadlocked && Gold >= ReforgeCost
                     : Gold >= SummonCost));
            _recipeButton.interactable =
                Has(GameFeature.Recipes) &&
                !_isCombining &&
                (_session == null ||
                 !_session.IsEnded);
            _codexButton.interactable =
                !_isCombining &&
                (_session == null ||
                 !_session.IsEnded);
            if (_recipeButton != null)
            {
                float pulse =
                    0.5f +
                    0.5f * Mathf.Sin(
                        Time.unscaledTime * 5f);
                _recipeButton.GetComponent<Image>().color =
                    ReadyRecipeCount > 0
                        ? Color.Lerp(
                            Color.white,
                            new Color(1.12f, 0.88f, 1.18f, 1f),
                            pulse * 0.55f)
                        : Color.white;
            }
            if (_ultimateChargeText != null && _session != null)
            {
                float power = _session.BoardPower();
                _shownPower = Mathf.Abs(power - _shownPower) < 1f
                    ? power
                    : Mathf.Lerp(
                        _shownPower,
                        power,
                        Mathf.Clamp01(Time.unscaledDeltaTime * 8f));
                _ultimateChargeText.text =
                    _bootstrap.Localization.IsKorean
                        ? $"전투력 {_shownPower:N0}"
                        : $"Power {_shownPower:N0}";
                _ultimateChargeText.color = GameTheme.Gold;
            }
            RefreshUltimateButton();
            RefreshFocusVisual();
            RefreshDanger();
            RefreshBossGauge();
            RefreshCombinationGuide();
            // The boss bar sits above the guide, so one-tap forging stays
            // available through long boss fights.
        }

        private string WaveLabel()
        {
            if (_session == null)
            {
                return "WAVE 1";
            }

            string total = _session.TotalWaves == int.MaxValue
                ? string.Empty
                : $"/{_session.TotalWaves}";
            return _session.State switch
            {
                CombinationDefenseState.Preparing =>
                    _bootstrap.Localization.IsKorean
                        ? $"준비 {Mathf.CeilToInt(_session.StateTimeRemaining)}"
                        : $"READY {Mathf.CeilToInt(_session.StateTimeRemaining)}",
                CombinationDefenseState.Intermission =>
                    _bootstrap.Localization.IsKorean
                        ? $"WAVE {_session.Wave}{total} · 다음 {Mathf.CeilToInt(_session.StateTimeRemaining)}"
                        : $"WAVE {_session.Wave}{total} · NEXT {Mathf.CeilToInt(_session.StateTimeRemaining)}",
                CombinationDefenseState.Victory =>
                    _bootstrap.Localization.IsKorean
                        ? "수호 성공"
                        : "VICTORY",
                CombinationDefenseState.Defeat =>
                    _bootstrap.Localization.IsKorean
                        ? "적 한계 초과"
                        : "ENEMY LIMIT REACHED",
                _ => $"WAVE {_session.Wave}{total}"
            };
        }

        private void RefreshBossGauge()
        {
            if (_bossGaugeRoot == null)
            {
                return;
            }

            BattleEnemyState boss =
                _session?.Enemies
                    .Where(enemy =>
                        enemy.Definition.IsBoss)
                    .OrderBy(enemy => enemy.Health)
                    .FirstOrDefault();
            _bossGaugeRoot.gameObject.SetActive(
                boss != null);
            if (boss == null)
            {
                return;
            }

            float ratio = Mathf.Clamp01(
                boss.Health / boss.MaxHealth);
            _bossHealthFill.rectTransform.anchorMax =
                new Vector2(
                    Mathf.Lerp(0.01f, 0.99f, ratio),
                    0.88f);
            bool korean = _bootstrap.Localization.IsKorean;
            int secondsLeft = Mathf.CeilToInt(_session.BossTimeRemaining(boss));
            _bossNameText.text = boss.Rampaging
                ? (korean
                    ? $"{boss.Definition.NameKo} 폭주 중!"
                    : $"{boss.Definition.NameEn} is rampaging!")
                : (korean
                    ? $"보스 · {boss.Definition.NameKo} · {secondsLeft}초"
                    : $"Boss · {boss.Definition.NameEn} · {secondsLeft}s");
            _bossNameText.color = boss.Rampaging || secondsLeft <= 10
                ? GameTheme.Coral
                : GameTheme.Text;
        }

        private void ShowGoldReward(
            LoopEnemyPrototypeView view,
            int reward)
        {
            if (view == null || _arenaRoot == null)
            {
                return;
            }

            Vector2 position =
                _arenaRoot.InverseTransformPoint(
                    view.transform.position);
            StartCoroutine(
                GoldRewardRoutine(position, reward));
        }

        private System.Collections.IEnumerator
            UltimateVisualRoutine(
                string ultimateName,
                UltimateEffect effect)
        {
            Color color = effect switch
            {
                UltimateEffect.ChronoSeal =>
                    new Color(0.42f, 0.82f, 1f, 1f),
                UltimateEffect.ForgeNova =>
                    new Color(1f, 0.58f, 0.22f, 1f),
                _ => new Color(0.74f, 0.48f, 1f, 1f)
            };
            RectTransform flash = UiFactory.CreatePanel(
                _safeRoot,
                "UltimateFlash",
                new Color(
                    color.r,
                    color.g,
                    color.b,
                    0f),
                Vector2.zero,
                Vector2.one,
                Vector2.zero,
                Vector2.zero);
            flash.SetAsLastSibling();
            flash.GetComponent<Image>().raycastTarget =
                false;
            Text mark = UiFactory.CreateText(
                flash,
                "UltimateMark",
                $"✦\n{ultimateName}",
                48,
                color,
                TextAnchor.MiddleCenter,
                new Vector2(0.08f, 0.34f),
                new Vector2(0.92f, 0.66f),
                Vector2.zero,
                Vector2.zero,
                FontStyle.Bold);
            mark.raycastTarget = false;
            UiFactory.AddOutline(
                mark,
                new Color(0f, 0f, 0.03f, 0.96f),
                new Vector2(3f, -3f));

            CanvasGroup group =
                flash.gameObject.AddComponent<CanvasGroup>();
            float elapsed = 0f;
            float duration =
                _bootstrap.SaveData.ReducedMotion
                    ? 0.22f
                    : 0.58f;
            while (elapsed < duration)
            {
                elapsed += Time.unscaledDeltaTime;
                float progress =
                    Mathf.Clamp01(elapsed / duration);
                float pulse =
                    Mathf.Sin(progress * Mathf.PI);
                group.alpha = pulse;
                mark.rectTransform.localScale =
                    Vector3.one *
                    Mathf.Lerp(0.72f, 1.15f, pulse);
                yield return null;
            }

            Destroy(flash.gameObject);
        }

        private System.Collections.IEnumerator
            GoldRewardRoutine(
                Vector2 position,
                int reward)
        {
            Text text = UiFactory.CreateText(
                _arenaRoot,
                "GoldReward",
                $"+{reward} ✦",
                23,
                GameTheme.Gold,
                TextAnchor.MiddleCenter,
                new Vector2(0.5f, 0.5f),
                new Vector2(0.5f, 0.5f),
                position - new Vector2(65f, 28f),
                position + new Vector2(65f, 28f),
                FontStyle.Bold);
            text.raycastTarget = false;
            UiFactory.AddOutline(
                text,
                new Color(0f, 0f, 0.03f, 0.95f),
                new Vector2(2f, -2f));
            CanvasGroup group =
                text.gameObject.AddComponent<CanvasGroup>();
            Vector2 start =
                text.rectTransform.anchoredPosition;
            float elapsed = 0f;
            const float duration = 0.62f;
            while (elapsed < duration)
            {
                elapsed += Time.deltaTime;
                float progress =
                    Mathf.Clamp01(elapsed / duration);
                text.rectTransform.anchoredPosition =
                    start +
                    Vector2.up * progress * 54f;
                group.alpha = 1f - progress;
                yield return null;
            }

            Destroy(text.gameObject);
        }

        private void SetPaused(bool paused)
        {
            if (_session == null ||
                _session.IsEnded)
            {
                return;
            }

            // An open engraving card or codex keeps the run paused even
            // when the pause menu closes.
            _isPaused =
                paused ||
                _runeOverlay != null ||
                _battleCodexOverlay != null ||
                _engravingListOverlay != null;
            _draggingSlot = -1;
            _draggingSword = null;
            if (!paused)
            {
                if (_pauseOverlay != null)
                {
                    _pauseOverlay.gameObject.SetActive(
                        false);
                }
                return;
            }

            if (_pauseOverlay == null)
            {
                _pauseOverlay = UiFactory.CreatePanel(
                    _safeRoot,
                    "PauseOverlay",
                    new Color(
                        0.005f,
                        0.008f,
                        0.035f,
                        0.88f),
                    Vector2.zero,
                    Vector2.one,
                    Vector2.zero,
                    Vector2.zero);
                RectTransform card =
                    UiFactory.CreatePanel(
                        _pauseOverlay,
                        "PauseCard",
                        Color.white,
                        new Vector2(0.10f, 0.31f),
                        new Vector2(0.90f, 0.69f),
                        Vector2.zero,
                        Vector2.zero);
                UiFactory.CreateText(
                    card,
                    "PauseTitle",
                    _bootstrap.Localization.IsKorean
                        ? "일시정지"
                        : "PAUSED",
                    44,
                    GameTheme.Gold,
                    TextAnchor.MiddleCenter,
                    new Vector2(0.08f, 0.70f),
                    new Vector2(0.92f, 0.92f),
                    Vector2.zero,
                    Vector2.zero,
                    FontStyle.Bold);
                UiFactory.CreateText(
                    card,
                    "PauseGuide",
                    _bootstrap.Localization.IsKorean
                        ? "전투와 웨이브 진행이 멈췄습니다."
                        : "COMBAT AND WAVES ARE PAUSED.",
                    22,
                    GameTheme.MutedText,
                    TextAnchor.MiddleCenter,
                    new Vector2(0.08f, 0.54f),
                    new Vector2(0.92f, 0.70f),
                    Vector2.zero,
                    Vector2.zero);
                UiFactory.CreateButton(
                    card,
                    "ResumeButton",
                    _bootstrap.Localization.IsKorean
                        ? "계속하기"
                        : "RESUME",
                    GameTheme.Violet,
                    GameTheme.Text,
                    29,
                    new Vector2(0.18f, 0.38f),
                    new Vector2(0.82f, 0.53f),
                    Vector2.zero,
                    Vector2.zero,
                    () => SetPaused(false));
                UiFactory.CreateButton(
                    card,
                    "AbandonButton",
                    _bootstrap.Localization.IsKorean
                        ? "이번 수호 종료"
                        : "END RUN",
                    GameTheme.PanelLight,
                    GameTheme.Text,
                    25,
                    new Vector2(0.18f, 0.18f),
                    new Vector2(0.82f, 0.33f),
                    Vector2.zero,
                    Vector2.zero,
                    () =>
                    {
                        SetPaused(false);
                        _session.Abandon();
                    });
            }

            _pauseOverlay.gameObject.SetActive(true);
            _pauseOverlay.SetAsLastSibling();
        }

        public void PauseFromLifecycle()
        {
            if (_session != null &&
                !_session.IsEnded &&
                !_resultShown)
            {
                SetPaused(true);
            }
        }

        private void ShowRuneChoice(int completedWave)
        {
            if (_session == null ||
                _session.IsEnded ||
                _resultShown ||
                !Has(GameFeature.Engravings))
            {
                return;
            }

            if (_runeOverlay != null)
            {
                return;
            }

            IReadOnlyList<RuneDefinition> choices =
                RunContentCatalog.DrawRunes(
                    _random,
                    _session.SelectedRuneIds,
                    completedWave % 5 == 0,
                    _bootstrap.SaveData.PlayerLevel);
            if (choices.Count == 0)
            {
                return;
            }

            bool korean = _bootstrap.Localization.IsKorean;
            _recipeBook?.Close();
            _isPaused = true;
            float powerBefore = _session.BoardPower();
            RectTransform overlay = UiFactory.CreatePanel(
                _safeRoot,
                "EngravingChoiceOverlay",
                new Color(0.02f, 0.03f, 0.10f, 0.97f),
                Vector2.zero,
                Vector2.one,
                Vector2.zero,
                Vector2.zero);
            overlay.SetAsLastSibling();
            _runeOverlay = overlay;
            UiFactory.UseTitleFont(UiFactory.CreateText(
                overlay,
                "EngravingTitle",
                korean
                    ? $"웨이브 {completedWave} 각인"
                    : $"Wave {completedWave} engraving",
                46,
                GameTheme.Text,
                TextAnchor.MiddleCenter,
                new Vector2(0.06f, 0.80f),
                new Vector2(0.94f, 0.88f),
                Vector2.zero,
                Vector2.zero));
            UiFactory.CreateText(
                overlay,
                "EngravingGuide",
                korean
                    ? "하나를 골라 이번 수호 내내 새깁니다."
                    : "Pick one. It lasts for the rest of this run.",
                24,
                GameTheme.MutedText,
                TextAnchor.MiddleCenter,
                new Vector2(0.08f, 0.75f),
                new Vector2(0.92f, 0.80f),
                Vector2.zero,
                Vector2.zero);

            for (int index = 0; index < choices.Count; index++)
            {
                RuneDefinition rune = choices[index];
                float top = 0.72f - index * 0.19f;
                Button button = UiFactory.CreateButton(
                    overlay,
                    $"EngravingChoice_{index}",
                    string.Empty,
                    GameTheme.PanelLight,
                    GameTheme.Text,
                    24,
                    new Vector2(0.07f, top - 0.17f),
                    new Vector2(0.93f, top),
                    Vector2.zero,
                    Vector2.zero,
                    () => SelectRune(overlay, rune, powerBefore));
                RectTransform card = (RectTransform)button.transform;
                Color gradeColor = RuneGradeColor(rune.Rarity);
                int sameTag = rune.Tag == RuneTag.Basic
                    ? 0
                    : _session.SelectedRuneIds
                        .Select(RunContentCatalog.GetRune)
                        .Count(owned =>
                            owned != null &&
                            owned.Tag == rune.Tag);
                UiFactory.CreateText(
                    card,
                    "Tag",
                    $"{RuneTagName(rune.Tag, korean)} · {RuneGradeName(rune.Rarity, korean)}" +
                    (sameTag > 0 ? $"   ×{sameTag + 1}" : string.Empty),
                    21,
                    gradeColor,
                    TextAnchor.MiddleCenter,
                    new Vector2(0.28f, 0.63f),
                    new Vector2(0.72f, 0.77f),
                    Vector2.zero,
                    Vector2.zero).raycastTarget = false;
                Text nameText = UiFactory.UseTitleFont(UiFactory.CreateText(
                    card,
                    "Name",
                    korean ? rune.NameKo : rune.NameEn,
                    36,
                    rune.Rarity == RuneRarity.Common
                        ? GameTheme.Text
                        : gradeColor,
                    TextAnchor.MiddleCenter,
                    new Vector2(0.12f, 0.40f),
                    new Vector2(0.88f, 0.64f),
                    Vector2.zero,
                    Vector2.zero));
                nameText.raycastTarget = false;
                UiFactory.CreateText(
                    card,
                    "Effect",
                    korean ? rune.DescriptionKo : rune.DescriptionEn,
                    23,
                    GameTheme.Text,
                    TextAnchor.MiddleCenter,
                    // Inside the corner ornaments; long effects wrap to two lines.
                    new Vector2(0.27f, 0.17f),
                    new Vector2(0.73f, 0.41f),
                    Vector2.zero,
                    Vector2.zero).raycastTarget = false;
            }
        }

        private void SelectRune(
            RectTransform overlay,
            RuneDefinition rune,
            float powerBefore)
        {
            // Two fingers can land on two cards in one frame; take the first.
            if (_session == null ||
                rune == null ||
                _runeOverlay == null ||
                _runeOverlay != overlay)
            {
                return;
            }

            _runeOverlay = null;
            _session.ApplyRune(rune);
            AudioService.Instance?.PlaySfx(
                rune.Rarity == RuneRarity.Common ? "forge" : "legendary",
                0.75f);
            if (!_bootstrap.SaveData.SeenRuneIds
                    .Contains(rune.Id))
            {
                _bootstrap.SaveData.SeenRuneIds.Add(
                    rune.Id);
            }
            _bootstrap.Save();
            _isPaused = false;
            if (overlay != null)
            {
                Destroy(overlay.gameObject);
            }
            bool korean = _bootstrap.Localization.IsKorean;
            string name = korean ? rune.NameKo : rune.NameEn;
            float powerAfter = _session.BoardPower();
            int gain = powerBefore > 0f
                ? Mathf.RoundToInt((powerAfter / powerBefore - 1f) * 100f)
                : 0;
            ShowToast(
                gain > 0
                    ? (korean
                        ? $"{name} · 전투력 +{gain}%"
                        : $"{name} · power +{gain}%")
                    : (korean
                        ? $"각인 · {name}"
                        : $"Engraved · {name}"));
            RefreshHud();
        }

        private static Color RuneGradeColor(RuneRarity rarity)
        {
            return rarity switch
            {
                RuneRarity.Legendary => GameTheme.Gold,
                RuneRarity.Rare => GameTheme.Cyan,
                _ => GameTheme.MutedText
            };
        }

        private static string RuneGradeName(RuneRarity rarity, bool korean)
        {
            return rarity switch
            {
                RuneRarity.Legendary => korean ? "전설" : "Legendary",
                RuneRarity.Rare => korean ? "희귀" : "Rare",
                _ => korean ? "일반" : "Common"
            };
        }

        private static string RuneTagName(RuneTag tag, bool korean)
        {
            return tag switch
            {
                RuneTag.Forge => korean ? "합성" : "Forge",
                RuneTag.Sell => korean ? "판매" : "Sell",
                RuneTag.Recipe => korean ? "조합서" : "Recipes",
                RuneTag.Element => korean ? "원소" : "Element",
                RuneTag.Hand => korean ? "손" : "Hands",
                RuneTag.Rule => korean ? "규칙" : "Rules",
                _ => korean ? "기본" : "Basic"
            };
        }

        private void ShowRunResult(
            CombinationDefenseState resultState)
        {
            if (_resultShown ||
                _session == null ||
                _bootstrap == null)
            {
                return;
            }

            _resultShown = true;
            _draggingSlot = -1;
            _draggingSword = null;
            bool victory =
                resultState ==
                CombinationDefenseState.Victory;
            bool abandoned =
                resultState ==
                CombinationDefenseState.Abandoned;
            RunSummary summary = new RunSummary
            {
                Seed = _runSeed,
                ContentVersion = "0.10.0",
                StageId = _stage.Id,
                Mode = _mode,
                LearningStep = _step?.Number ?? 0,
                PeakPressure = _session.PeakPressure,
                FinalWave = _session.Wave,
                Score = _session.Score,
                DefeatedEnemies =
                    _session.DefeatedEnemies,
                DefeatedElites =
                    _session.DefeatedElites,
                DefeatedBosses =
                    _session.DefeatedBosses,
                DurationSeconds =
                    _session.DurationSeconds,
                Victory = victory,
                SelectedUltimateId =
                    _bootstrap.SaveData
                        .SelectedUltimateId,
                DamageDealt = _session.DamageDealt,
                SwordsSummoned = _summonCount,
                SuccessfulForges =
                    _starUpgrades + _recipeForges,
                StarUpgrades = _starUpgrades,
                RecipeForges = _recipeForges,
                UltimatesUsed =
                    _session.UltimatesUsed,
                EnemyAbilitiesTriggered =
                    _session.EnemyAbilitiesTriggered,
                PeakSwordStars =
                    _inventory.Slots
                        .Where(item => item != null)
                        .Select(item => item.Stars)
                        .DefaultIfEmpty(0)
                        .Max(),
                DiscoveredRecipeIds =
                    new List<string>(
                        _bootstrap.SaveData
                            .DiscoveredRecipeIds),
                SelectedRuneIds =
                    new List<string>(
                        _session.SelectedRuneIds)
            };
            int dateKey =
                int.Parse(
                    DateTime.Now.ToString("yyyyMMdd"));
            int levelBefore = _bootstrap.SaveData.PlayerLevel;
            RunRewardResult reward =
                ProgressionService.ApplyRun(
                    _bootstrap.SaveData,
                    summary,
                    dateKey);
            _bootstrap.Save();
            string unlockLine = UnlockLine(
                levelBefore,
                _bootstrap.SaveData.PlayerLevel);

            RectTransform overlay = UiFactory.CreatePanel(
                _safeRoot,
                "RunResultOverlay",
                new Color(0.005f, 0.008f, 0.035f, 0.90f),
                Vector2.zero,
                Vector2.one,
                Vector2.zero,
                Vector2.zero);
            overlay.SetAsLastSibling();
            RectTransform panel = UiFactory.CreatePanel(
                overlay,
                "RunResultCard",
                Color.white,
                new Vector2(0.07f, 0.20f),
                new Vector2(0.93f, 0.82f),
                Vector2.zero,
                Vector2.zero);

            Text title = UiFactory.CreateText(
                panel,
                "ResultTitle",
                victory && _step != null
                    ? (_bootstrap.Localization.IsKorean
                        ? $"{_step.Number}단계 클리어!"
                        : $"Step {_step.Number} cleared!")
                    : victory
                    ? (_bootstrap.Localization.IsKorean
                        ? "차원로 수호 성공"
                        : "RIFT DEFENDED")
                    : abandoned
                        ? (_bootstrap.Localization.IsKorean
                            ? "수호를 중단했습니다"
                            : "RUN ABANDONED")
                        : (_bootstrap.Localization.IsKorean
                            ? "적이 60마리 쌓였습니다"
                            : "ENEMY LIMIT REACHED"),
                42,
                victory
                    ? GameTheme.Gold
                    : GameTheme.Coral,
                TextAnchor.MiddleCenter,
                new Vector2(0.08f, 0.76f),
                new Vector2(0.92f, 0.94f),
                Vector2.zero,
                Vector2.zero,
                FontStyle.Bold);
            UiFactory.AddOutline(
                title,
                new Color(0f, 0f, 0.03f, 0.95f),
                new Vector2(3f, -3f));

            string stats =
                _bootstrap.Localization.IsKorean
                    ? $"도달 웨이브  {_session.Wave}/{_session.TotalWaves}\n" +
                      $"처치  {_session.DefeatedEnemies:N0}    보스  {_session.DefeatedBosses}\n" +
                      $"조합  {_starUpgrades + _recipeForges}    피해  {_session.DamageDealt:N0}\n" +
                      $"점수  {_session.Score:N0}\n\n" +
                      $"획득  철가루 {reward.IronPowder:N0}   경험치 {reward.Experience:N0}\n" +
                      unlockLine
                    : $"WAVE  {_session.Wave}/{_session.TotalWaves}\n" +
                      $"DEFEATED  {_session.DefeatedEnemies:N0}    BOSSES  {_session.DefeatedBosses}\n" +
                      $"FORGES  {_starUpgrades + _recipeForges}    DAMAGE  {_session.DamageDealt:N0}\n" +
                      $"SCORE  {_session.Score:N0}\n\n" +
                      $"REWARD  IRON {reward.IronPowder:N0}   XP {reward.Experience:N0}\n" +
                      unlockLine;
            bool stepCleared = victory && _step != null;
            if (stepCleared)
            {
                bool korean = _bootstrap.Localization.IsKorean;
                int stars = LearningPath.Stars(true, _session.PeakPressure);
                UiFactory.CreateText(
                    panel,
                    "ResultStars",
                    LearningPath.StarText(stars),
                    54,
                    GameTheme.Gold,
                    TextAnchor.MiddleCenter,
                    new Vector2(0.10f, 0.66f),
                    new Vector2(0.90f, 0.77f),
                    Vector2.zero,
                    Vector2.zero,
                    FontStyle.Bold);
                string starLine = reward.NewStars > 0
                    ? (korean
                        ? $"새 별 {reward.NewStars}개!  철가루 +{reward.NewStars * LearningPath.IronPerStar}"
                        : $"{reward.NewStars} new star(s)!  Iron +{reward.NewStars * LearningPath.IronPerStar}")
                    : stars < 3
                        ? (korean
                            ? $"적을 {(stars < 2 ? LearningPath.TwoStarsBelow : LearningPath.ThreeStarsBelow)}마리 아래로 막으면 ★{stars + 1}"
                            : $"Hold enemies under {(stars < 2 ? LearningPath.TwoStarsBelow : LearningPath.ThreeStarsBelow)} for ★{stars + 1}")
                        : (korean ? "만점 클리어!" : "Perfect clear!");
                stats = starLine + "\n\n" + stats;
            }
            UiFactory.CreateText(
                panel,
                "ResultStats",
                stats,
                26,
                GameTheme.Text,
                TextAnchor.MiddleCenter,
                new Vector2(0.09f, 0.34f),
                new Vector2(0.91f, stepCleared ? 0.66f : 0.76f),
                Vector2.zero,
                Vector2.zero,
                FontStyle.Bold);

            bool nextStep = victory && _step != null;
            UiFactory.CreateButton(
                panel,
                nextStep ? "NextStepButton" : "RetryButton",
                nextStep
                    ? (_bootstrap.Localization.IsKorean
                        ? (_step.Number < LearningPath.Steps.Count ? "다음 단계" : "균열 목록으로")
                        : (_step.Number < LearningPath.Steps.Count ? "NEXT STEP" : "RIFT LIST"))
                    : (_bootstrap.Localization.IsKorean
                        ? "다시 도전"
                        : "RETRY"),
                GameTheme.Violet,
                GameTheme.Text,
                29,
                new Vector2(0.16f, 0.25f),
                new Vector2(0.84f, 0.36f),
                Vector2.zero,
                Vector2.zero,
                () =>
                {
                    if (!nextStep)
                    {
                        _bootstrap.StartBattle(_stage.Id, _mode, _step?.Number ?? 0);
                        return;
                    }
                    LearningStep next = LearningPath.Get(_step.Number + 1);
                    if (next == null)
                    {
                        _bootstrap.ShowStageSelect();
                    }
                    else if (next.Has(GameFeature.Ultimate))
                    {
                        _bootstrap.ShowLoadout(LearningPath.StageId, RunMode.Standard, next.Number);
                    }
                    else
                    {
                        _bootstrap.StartBattle(LearningPath.StageId, RunMode.Standard, next.Number);
                    }
                });
            UiFactory.CreateButton(
                panel,
                "HomeButton",
                _bootstrap.Localization.IsKorean
                    ? "대장간으로"
                    : "FORGE HOME",
                GameTheme.PanelLight,
                GameTheme.Text,
                27,
                new Vector2(0.16f, 0.11f),
                new Vector2(0.84f, 0.22f),
                Vector2.zero,
                Vector2.zero,
                _bootstrap.ShowMainMenu);
        }

        // Level-ups name the engravings they opened; otherwise show the next goal.
        private string UnlockLine(int levelBefore, int levelAfter)
        {
            bool korean = _bootstrap.Localization.IsKorean;
            IReadOnlyList<RuneDefinition> opened =
                RunContentCatalog.UnlockedBetween(levelBefore, levelAfter);
            if (opened.Count > 0)
            {
                string names = string.Join(
                    ", ",
                    opened.Select(rune => korean ? rune.NameKo : rune.NameEn));
                return korean
                    ? $"LV.{levelAfter} 달성 · 새 각인 해금: {names}"
                    : $"LV.{levelAfter} · new engraving: {names}";
            }

            RuneDefinition next = RunContentCatalog.NextUnlock(levelAfter);
            if (next == null)
            {
                return korean ? "모든 각인을 해금했습니다" : "Every engraving unlocked";
            }

            int remaining = Math.Max(
                0,
                RunBalanceRules.ExperienceForLevel(levelAfter) -
                _bootstrap.SaveData.PlayerExperience);
            return korean
                ? $"다음 해금 LV.{next.UnlockLevel} · {next.NameKo} (경험치 {remaining} 남음)"
                : $"Next unlock LV.{next.UnlockLevel} · {next.NameEn} ({remaining} XP to go)";
        }

        private void ShowToast(string message)
        {
            if (_toastText == null)
            {
                return;
            }

            _toastText.text = message;
            _toastRemaining = 1.4f;
        }

        private static void SetButtonLabel(
            Button button,
            string value)
        {
            if (button == null)
            {
                return;
            }

            Text label = button.GetComponentInChildren<Text>();
            if (label != null)
            {
                label.text = value;
            }
        }

        private static ulong CreateDailySeed(
            string stageId,
            DateTime localDate)
        {
            ulong hash = 1469598103934665603UL;
            string value =
                $"{localDate:yyyyMMdd}|" +
                $"{stageId ?? string.Empty}|" +
                "daily-v1";
            foreach (char character in value)
            {
                hash ^= character;
                hash *= 1099511628211UL;
            }

            return hash;
        }

        private static void SkinBottomButton(
            Button button,
            string frameSpriteName,
            string iconSpriteName)
        {
            Image background =
                button.GetComponent<Image>();
            background.sprite = Resources.Load<Sprite>(
                $"Art/UI/Battle/{frameSpriteName}");
            background.type = Image.Type.Sliced;
            background.preserveAspect = false;
            background.color = Color.white;

            ColorBlock colors = button.colors;
            colors.normalColor = Color.white;
            colors.highlightedColor =
                new Color(1.08f, 1.08f, 1.08f, 1f);
            colors.pressedColor =
                new Color(0.78f, 0.78f, 0.92f, 1f);
            colors.disabledColor = Color.white;
            button.colors = colors;

            Text label =
                button.GetComponentInChildren<Text>();
            if (label != null)
            {
                RectTransform labelRect =
                    label.rectTransform;
                labelRect.anchorMin =
                    new Vector2(0.35f, 0.08f);
                labelRect.anchorMax =
                    new Vector2(0.96f, 0.92f);
                labelRect.offsetMin = Vector2.zero;
                labelRect.offsetMax = Vector2.zero;
                UiFactory.AddOutline(
                    label,
                    new Color(0f, 0f, 0.08f, 0.95f),
                    new Vector2(2f, -2f));
            }

            GameObject iconObject = new GameObject(
                "ButtonIcon",
                typeof(RectTransform),
                typeof(Image));
            iconObject.transform.SetParent(
                button.transform,
                false);
            iconObject.transform.SetSiblingIndex(0);
            RectTransform iconRect =
                iconObject.GetComponent<RectTransform>();
            iconRect.anchorMin =
                new Vector2(0.045f, 0.08f);
            iconRect.anchorMax =
                new Vector2(0.36f, 0.92f);
            iconRect.offsetMin = Vector2.zero;
            iconRect.offsetMax = Vector2.zero;
            Image icon =
                iconObject.GetComponent<Image>();
            icon.sprite = Resources.Load<Sprite>(
                $"Art/UI/Battle/{iconSpriteName}");
            icon.preserveAspect = true;
            icon.raycastTarget = false;
        }
    }
}
