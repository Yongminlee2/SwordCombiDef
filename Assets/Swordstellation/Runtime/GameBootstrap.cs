using System;
using System.Collections.Generic;
using System.Linq;
using Swordstellation.Core;
using UnityEngine;
using UnityEngine.UI;

namespace Swordstellation.Runtime
{
    public sealed class GameBootstrap : MonoBehaviour
    {
        private static GameBootstrap _instance;
        private Canvas _canvas;
        private RectTransform _safeRoot;
        private LocalSaveService _saveService;
        private AudioService _audioService;
        private BattleController _battle;
        // Android back on menu screens does what the screen's back button does.
        private Action _backAction;
        private string _shopFeedback;
        private string _upgradeFeedback;
        private int _tutorialPage;

        public ContentCatalog Catalog { get; private set; }
        public PlayerSaveData SaveData { get; private set; }
        public LocalizationService Localization { get; private set; }
        public GachaService Gacha { get; private set; }

        private void Awake()
        {
            if (_instance != null && _instance != this)
            {
                Destroy(gameObject);
                return;
            }
            _instance = this;

            DontDestroyOnLoad(gameObject);
            Application.targetFrameRate = 60;
            Screen.orientation = ScreenOrientation.Portrait;
            _audioService = GetComponent<AudioService>();
            if (_audioService == null)
            {
                _audioService = gameObject.AddComponent<AudioService>();
            }
            _audioService.Initialize();

            Catalog = new ContentCatalog();
            _saveService = new LocalSaveService();
            SaveData = _saveService.Load();
            _audioService.SetVolumes(
                SaveData.MusicVolume,
                SaveData.SfxVolume);
            Gacha = new GachaService(Catalog, new SeededRandom((ulong)DateTime.UtcNow.Ticks ^ 0xA11CE55UL));
            Localization = new LocalizationService(SaveData.Locale);
            Localization.LocaleChanged += OnLocaleChanged;

            EnsureCamera();
            // Learning-path steps introduce each system; the five-page guide
            // stays under Settings.
            ShowMainMenu();
            Debug.Log(
                $"Swordstellation bootstrap ready: " +
                $"locale={SaveData.Locale}, tutorial={SaveData.TutorialCompleted}");
        }

        private void OnDestroy()
        {
            if (_instance == this)
            {
                _instance = null;
            }
            if (Localization != null)
            {
                Localization.LocaleChanged -= OnLocaleChanged;
            }
        }

        private void Update()
        {
            if (_battle == null &&
                _backAction != null &&
                Input.GetKeyDown(KeyCode.Escape))
            {
                AudioService.PlayUiClick();
                _backAction();
            }
        }

        private void OnApplicationPause(bool paused)
        {
            if (!paused)
            {
                return;
            }

            _battle?.PauseFromLifecycle();
            SaveIfReady();
        }

        private void OnApplicationFocus(bool hasFocus)
        {
            if (!hasFocus)
            {
                _battle?.PauseFromLifecycle();
                SaveIfReady();
            }
        }

        private void SaveIfReady()
        {
            if (_saveService == null ||
                SaveData == null ||
                Localization == null)
            {
                return;
            }

            try
            {
                Save();
            }
            catch (Exception exception)
            {
                Debug.LogWarning(
                    $"Lifecycle save failed: {exception.Message}");
            }
        }

        public void ShowMainMenu()
        {
            DisposeBattle();
            CreateScreenCanvas("MainMenuCanvas");

            RectTransform background = UiFactory.CreatePanel(
                _canvas.transform,
                "Background",
                GameTheme.Background,
                Vector2.zero,
                Vector2.one,
                Vector2.zero,
                Vector2.zero);
            background.SetAsFirstSibling();
            Sprite backgroundSprite = Resources.Load<Sprite>("Art/UI/MainMenuBackground");
            if (backgroundSprite != null)
            {
                Image backgroundImage = background.GetComponent<Image>();
                backgroundImage.sprite = backgroundSprite;
                backgroundImage.color = Color.white;
            }

            CreateDecorativeBlades();

            RectTransform titlePlate = UiFactory.CreatePanel(
                _safeRoot,
                "TitlePlate",
                new Color(GameTheme.Panel.r, GameTheme.Panel.g, GameTheme.Panel.b, 0.94f),
                new Vector2(0.08f, 0.665f),
                new Vector2(0.92f, 0.92f),
                Vector2.zero,
                Vector2.zero);
            CreateHeroPortrait(
                titlePlate,
                new Vector2(0.70f, 0.02f),
                new Vector2(1.0f, 0.98f),
                0.32f);

            Text title = UiFactory.CreateText(
                titlePlate,
                "Title",
                Localization.Get("app.title"),
                Localization.IsKorean ? 78 : 58,
                GameTheme.Gold,
                TextAnchor.MiddleCenter,
                new Vector2(0.04f, 0.37f),
                new Vector2(0.96f, 0.86f),
                Vector2.zero,
                Vector2.zero,
                FontStyle.Bold);
            UiFactory.AddOutline(title, new Color(0, 0, 0, 0.6f), new Vector2(3, -3));

            UiFactory.CreateText(
                titlePlate,
                "Subtitle",
                Localization.Get("app.subtitle"),
                30,
                GameTheme.Cyan,
                TextAnchor.UpperCenter,
                new Vector2(0.05f, 0.10f),
                new Vector2(0.95f, 0.42f),
                Vector2.zero,
                Vector2.zero,
                FontStyle.Bold);

            CreateMenuButton("StartButton", "menu.start", 0.46f, 0.57f, GameTheme.Gold, GameTheme.Background, ShowStageSelect);
            CreateMenuButton("CodexButton", "menu.codex", 0.345f, 0.445f, GameTheme.PanelLight, GameTheme.Text, ShowCodex);
            CreateMenuButton("UpgradeButton", "menu.upgrades", 0.225f, 0.325f, GameTheme.PanelLight, GameTheme.Text, ShowUpgrades);
            CreateMenuButton("ShopButton", "menu.shop", 0.105f, 0.205f, GameTheme.Violet, GameTheme.Text, ShowShop);

            UiFactory.CreateText(
                _safeRoot,
                "PlayerStatus",
                $"{Localization.Get("menu.level")} LV.{SaveData.PlayerLevel}    " +
                $"⚒ {SaveData.IronPowder:N0}    ✦ {SaveData.Starsteel:N0}",
                30,
                GameTheme.Text,
                TextAnchor.MiddleCenter,
                new Vector2(0.08f, 0.618f),
                new Vector2(0.92f, 0.66f),
                Vector2.zero,
                Vector2.zero,
                FontStyle.Bold);

            RuneDefinition nextUnlock =
                RunContentCatalog.NextUnlock(SaveData.PlayerLevel);
            LearningStep nextStep = LearningPath.Get(LearningPath.NextStep(SaveData));
            UiFactory.CreateText(
                _safeRoot,
                "NextUnlock",
                nextStep != null
                    ? (Localization.IsKorean
                        ? $"다음 목표  {nextStep.Number}단계 · {nextStep.NameKo}"
                        : $"Next goal  Step {nextStep.Number} · {nextStep.NameEn}")
                    : nextUnlock == null
                    ? (Localization.IsKorean
                        ? "모든 각인 해금 완료"
                        : "Every engraving unlocked")
                    : (Localization.IsKorean
                        ? $"다음 각인 해금  LV.{nextUnlock.UnlockLevel} · {nextUnlock.NameKo}"
                        : $"Next engraving  LV.{nextUnlock.UnlockLevel} · {nextUnlock.NameEn}"),
                25,
                GameTheme.Cyan,
                TextAnchor.MiddleCenter,
                new Vector2(0.08f, 0.582f),
                new Vector2(0.92f, 0.615f),
                Vector2.zero,
                Vector2.zero);

            UiFactory.CreateButton(
                _safeRoot,
                "LanguageButton",
                Localization.Get("menu.language"),
                new Color(1, 1, 1, 0.10f),
                GameTheme.Text,
                26,
                new Vector2(0.69f, 0.055f),
                new Vector2(0.92f, 0.11f),
                Vector2.zero,
                Vector2.zero,
                Localization.Toggle);

            UiFactory.CreateButton(
                _safeRoot,
                "SettingsButton",
                Localization.Get("menu.settings"),
                new Color(1, 1, 1, 0.10f),
                GameTheme.Text,
                26,
                new Vector2(0.08f, 0.055f),
                new Vector2(0.31f, 0.11f),
                Vector2.zero,
                Vector2.zero,
                ShowSettings);

            UiFactory.CreateButton(
                _safeRoot,
                "StatisticsButton",
                Localization.Get("menu.statistics"),
                new Color(1, 1, 1, 0.10f),
                GameTheme.Cyan,
                26,
                new Vector2(0.385f, 0.055f),
                new Vector2(0.615f, 0.11f),
                Vector2.zero,
                Vector2.zero,
                ShowStatistics);
        }

        public void ShowTutorial()
        {
            _tutorialPage = 0;
            ShowTutorialPage();
        }

        private void ShowTutorialPage()
        {
            CreateStandardScreen("tutorial.title");
            string[] pages =
            {
                "tutorial.page1",
                "tutorial.page2",
                "tutorial.page3",
                "tutorial.page5",
                "tutorial.page4"
            };
            Color[] accents =
            {
                GameTheme.Gold,
                GameTheme.Cyan,
                GameTheme.Mint,
                GameTheme.Coral,
                GameTheme.Violet
            };

            RectTransform card = UiFactory.CreatePanel(
                _safeRoot,
                "TutorialCard",
                GameTheme.Panel,
                new Vector2(0.08f, 0.27f),
                new Vector2(0.92f, 0.79f),
                Vector2.zero,
                Vector2.zero);
            string[] lines = Localization.Get(pages[_tutorialPage])
                .Split(new[] { '\n' }, 2);
            UiFactory.CreateText(
                card,
                "Page",
                $"{_tutorialPage + 1} / {pages.Length}",
                28,
                GameTheme.MutedText,
                TextAnchor.MiddleCenter,
                new Vector2(0.2f, 0.10f),
                new Vector2(0.8f, 0.18f),
                Vector2.zero,
                Vector2.zero,
                FontStyle.Bold);
            UiFactory.CreateText(
                card,
                "Heading",
                lines[0],
                50,
                accents[_tutorialPage],
                TextAnchor.MiddleCenter,
                new Vector2(0.1f, 0.68f),
                new Vector2(0.9f, 0.84f),
                Vector2.zero,
                Vector2.zero,
                FontStyle.Bold);
            UiFactory.CreateText(
                card,
                "Body",
                lines.Length > 1 ? lines[1] : string.Empty,
                38,
                GameTheme.Text,
                TextAnchor.MiddleCenter,
                new Vector2(0.12f, 0.22f),
                new Vector2(0.88f, 0.66f),
                Vector2.zero,
                Vector2.zero,
                FontStyle.Bold).lineSpacing = 1.25f;

            bool isLastPage = _tutorialPage == pages.Length - 1;
            UiFactory.CreateButton(
                _safeRoot,
                "Continue",
                Localization.Get(
                    isLastPage ? "tutorial.begin" : "tutorial.next"),
                accents[_tutorialPage],
                GameTheme.Background,
                31,
                new Vector2(0.18f, 0.14f),
                new Vector2(0.82f, 0.22f),
                Vector2.zero,
                Vector2.zero,
                () =>
                {
                    if (!isLastPage)
                    {
                        _tutorialPage++;
                        ShowTutorialPage();
                        return;
                    }

                    SaveData.TutorialCompleted = true;
                    Save();
                    ShowMainMenu();
                });
        }

        public void ShowSettings()
        {
            CreateStandardScreen("settings.title");
            CreateSettingRow(
                "Music",
                "settings.music",
                0.66f,
                SaveData.MusicVolume,
                value =>
                {
                    SaveData.MusicVolume = value;
                    ApplyAudioSettings();
                });
            CreateSettingRow(
                "Sfx",
                "settings.sfx",
                0.49f,
                SaveData.SfxVolume,
                value =>
                {
                    SaveData.SfxVolume = value;
                    ApplyAudioSettings();
                });

            UiFactory.CreateButton(
                _safeRoot,
                "ReducedMotion",
                $"{Localization.Get("settings.reduced_motion")}\n" +
                $"{Localization.Get(SaveData.ReducedMotion ? "settings.on" : "settings.off")}",
                SaveData.ReducedMotion ? GameTheme.Mint : GameTheme.PanelLight,
                SaveData.ReducedMotion ? GameTheme.Background : GameTheme.Text,
                25,
                new Vector2(0.13f, 0.34f),
                new Vector2(0.87f, 0.43f),
                Vector2.zero,
                Vector2.zero,
                () =>
                {
                    SaveData.ReducedMotion = !SaveData.ReducedMotion;
                    Save();
                    ShowSettings();
                });
            UiFactory.CreateButton(
                _safeRoot,
                "Tutorial",
                Localization.Get("settings.tutorial"),
                GameTheme.Violet,
                GameTheme.Text,
                27,
                new Vector2(0.13f, 0.22f),
                new Vector2(0.87f, 0.30f),
                Vector2.zero,
                Vector2.zero,
                ShowTutorial);
            CreateBackButton();
        }

        private void CreateSettingRow(
            string name,
            string labelKey,
            float minY,
            float value,
            Action<float> setValue)
        {
            RectTransform row = UiFactory.CreatePanel(
                _safeRoot,
                name,
                GameTheme.Panel,
                new Vector2(0.10f, minY),
                new Vector2(0.90f, minY + 0.12f),
                Vector2.zero,
                Vector2.zero);
            UiFactory.CreateText(
                row,
                "Label",
                $"{Localization.Get(labelKey)}  {Mathf.RoundToInt(value * 100f)}%",
                28,
                GameTheme.Text,
                TextAnchor.MiddleLeft,
                new Vector2(0.06f, 0),
                new Vector2(0.54f, 1),
                Vector2.zero,
                Vector2.zero,
                FontStyle.Bold);
            UiFactory.CreateButton(
                row,
                "Minus",
                "−",
                GameTheme.PanelLight,
                GameTheme.Text,
                34,
                new Vector2(0.57f, 0.22f),
                new Vector2(0.75f, 0.78f),
                Vector2.zero,
                Vector2.zero,
                () =>
                {
                    setValue(Mathf.Clamp01(value - 0.1f));
                    ShowSettings();
                });
            UiFactory.CreateButton(
                row,
                "Plus",
                "+",
                GameTheme.Gold,
                GameTheme.Background,
                32,
                new Vector2(0.78f, 0.22f),
                new Vector2(0.96f, 0.78f),
                Vector2.zero,
                Vector2.zero,
                () =>
                {
                    setValue(Mathf.Clamp01(value + 0.1f));
                    ShowSettings();
                });
        }

        private void ApplyAudioSettings()
        {
            _audioService.SetVolumes(
                SaveData.MusicVolume,
                SaveData.SfxVolume);
            Save();
        }

        public void ShowCodex()
        {
            CreateStandardScreen("codex.title");

            int unlockedCount = Catalog.Swords.Keys.Count(id => SaveData.UnlockedSwordIds.Contains(id));
            UiFactory.CreateText(
                _safeRoot,
                "Progress",
                $"{Localization.Get("codex.progress")}  {unlockedCount} / {Catalog.Swords.Count}" +
                $"    {Localization.Get("codex.appearances")}  {SaveData.OwnedAppearanceIds.Count}" +
                $"    ⚒ {SaveData.IronPowder:N0}",
                23,
                GameTheme.Cyan,
                TextAnchor.MiddleCenter,
                new Vector2(0.06f, 0.835f),
                new Vector2(0.94f, 0.875f),
                Vector2.zero,
                Vector2.zero,
                FontStyle.Bold);
            UiFactory.CreateButton(
                _safeRoot,
                "RecipesTab",
                Localization.Get("codex.recipes"),
                GameTheme.Violet,
                GameTheme.Text,
                24,
                new Vector2(0.1f, 0.775f),
                new Vector2(0.48f, 0.83f),
                Vector2.zero,
                Vector2.zero,
                ShowRecipeCodex);
            UiFactory.CreateButton(
                _safeRoot,
                "EnemiesTab",
                Localization.Get("codex.enemies"),
                GameTheme.PanelLight,
                GameTheme.Text,
                24,
                new Vector2(0.52f, 0.775f),
                new Vector2(0.9f, 0.83f),
                Vector2.zero,
                Vector2.zero,
                ShowEnemyCodex);

            RectTransform viewport = UiFactory.CreatePanel(
                _safeRoot,
                "CodexViewport",
                new Color(GameTheme.Panel.r, GameTheme.Panel.g, GameTheme.Panel.b, 0.86f),
                new Vector2(0.06f, 0.12f),
                new Vector2(0.94f, 0.765f),
                Vector2.zero,
                Vector2.zero);
            Mask mask = viewport.gameObject.AddComponent<Mask>();
            mask.showMaskGraphic = true;
            ScrollRect scrollRect = viewport.gameObject.AddComponent<ScrollRect>();
            scrollRect.horizontal = false;

            GameObject contentObject = new GameObject(
                "Content",
                typeof(RectTransform),
                typeof(VerticalLayoutGroup),
                typeof(ContentSizeFitter));
            contentObject.transform.SetParent(viewport, false);
            RectTransform content = contentObject.GetComponent<RectTransform>();
            content.anchorMin = new Vector2(0, 1);
            content.anchorMax = new Vector2(1, 1);
            // Same width as the viewport (a new RectTransform is 100 wider).
            content.sizeDelta = Vector2.zero;
            content.pivot = new Vector2(0.5f, 1);
            content.anchoredPosition = Vector2.zero;

            VerticalLayoutGroup layout = contentObject.GetComponent<VerticalLayoutGroup>();
            layout.padding = new RectOffset(22, 22, 44, 28);
            layout.spacing = 12;
            layout.childControlHeight = true;
            layout.childForceExpandHeight = false;

            ContentSizeFitter fitter = contentObject.GetComponent<ContentSizeFitter>();
            fitter.verticalFit = ContentSizeFitter.FitMode.PreferredSize;
            scrollRect.viewport = viewport;
            scrollRect.content = content;

            foreach (SwordDefinition sword in Catalog.Swords.Values.OrderBy(item => item.Rarity).ThenBy(item => item.Id))
            {
                CreateCodexRow(content, sword);
            }

            CreateRuneCodexButton();
            CreateBackButton();
        }

        public void ShowRecipeCodex()
        {
            CreateStandardScreen("codex.title");

            int discovered = SaveData.DiscoveredRecipeIds.Count(
                id => Catalog.GetRecipe(id) != null);
            UiFactory.CreateText(
                _safeRoot,
                "Progress",
                $"{Localization.Get("codex.recipe_progress")}  {discovered} / {Catalog.Recipes.Count}",
                28,
                GameTheme.Cyan,
                TextAnchor.MiddleCenter,
                new Vector2(0.06f, 0.835f),
                new Vector2(0.94f, 0.875f),
                Vector2.zero,
                Vector2.zero,
                FontStyle.Bold);
            UiFactory.CreateButton(
                _safeRoot,
                "SwordsTab",
                Localization.Get("codex.swords"),
                GameTheme.PanelLight,
                GameTheme.Text,
                24,
                new Vector2(0.1f, 0.775f),
                new Vector2(0.48f, 0.83f),
                Vector2.zero,
                Vector2.zero,
                ShowCodex);
            UiFactory.CreateButton(
                _safeRoot,
                "EnemiesTab",
                Localization.Get("codex.enemies"),
                GameTheme.PanelLight,
                GameTheme.Text,
                24,
                new Vector2(0.52f, 0.775f),
                new Vector2(0.9f, 0.83f),
                Vector2.zero,
                Vector2.zero,
                ShowEnemyCodex);

            RectTransform viewport = UiFactory.CreatePanel(
                _safeRoot,
                "RecipeViewport",
                new Color(GameTheme.Panel.r, GameTheme.Panel.g, GameTheme.Panel.b, 0.86f),
                new Vector2(0.06f, 0.12f),
                new Vector2(0.94f, 0.765f),
                Vector2.zero,
                Vector2.zero);
            Mask mask = viewport.gameObject.AddComponent<Mask>();
            mask.showMaskGraphic = true;
            ScrollRect scrollRect = viewport.gameObject.AddComponent<ScrollRect>();
            scrollRect.horizontal = false;

            GameObject contentObject = new GameObject(
                "Content",
                typeof(RectTransform),
                typeof(VerticalLayoutGroup),
                typeof(ContentSizeFitter));
            contentObject.transform.SetParent(viewport, false);
            RectTransform content = contentObject.GetComponent<RectTransform>();
            content.anchorMin = new Vector2(0, 1);
            content.anchorMax = new Vector2(1, 1);
            // Same width as the viewport (a new RectTransform is 100 wider).
            content.sizeDelta = Vector2.zero;
            content.pivot = new Vector2(0.5f, 1);
            content.anchoredPosition = Vector2.zero;

            VerticalLayoutGroup layout = contentObject.GetComponent<VerticalLayoutGroup>();
            layout.padding = new RectOffset(22, 22, 44, 28);
            layout.spacing = 12;
            layout.childControlHeight = true;
            layout.childForceExpandHeight = false;
            ContentSizeFitter fitter = contentObject.GetComponent<ContentSizeFitter>();
            fitter.verticalFit = ContentSizeFitter.FitMode.PreferredSize;
            scrollRect.viewport = viewport;
            scrollRect.content = content;

            foreach (RecipeDefinition recipe in Catalog.Recipes)
            {
                CreateRecipeRow(content, recipe);
            }

            CreateRuneCodexButton();
            CreateBackButton();
        }

        public void ShowEnemyCodex()
        {
            CreateStandardScreen("codex.enemies");

            int seen = SaveData.SeenEnemyIds.Count(
                id => Catalog.GetEnemy(id) != null);
            UiFactory.CreateText(
                _safeRoot,
                "Progress",
                $"{Localization.Get("codex.enemy_progress")}  " +
                $"{seen} / {Catalog.Enemies.Count}",
                27,
                GameTheme.Cyan,
                TextAnchor.MiddleCenter,
                new Vector2(0.06f, 0.835f),
                new Vector2(0.94f, 0.875f),
                Vector2.zero,
                Vector2.zero,
                FontStyle.Bold);
            UiFactory.CreateButton(
                _safeRoot,
                "SwordsTab",
                Localization.Get("codex.swords"),
                GameTheme.PanelLight,
                GameTheme.Text,
                24,
                new Vector2(0.1f, 0.775f),
                new Vector2(0.48f, 0.83f),
                Vector2.zero,
                Vector2.zero,
                ShowCodex);
            UiFactory.CreateButton(
                _safeRoot,
                "RecipesTab",
                Localization.Get("codex.recipes"),
                GameTheme.Violet,
                GameTheme.Text,
                24,
                new Vector2(0.52f, 0.775f),
                new Vector2(0.9f, 0.83f),
                Vector2.zero,
                Vector2.zero,
                ShowRecipeCodex);

            RectTransform viewport = UiFactory.CreatePanel(
                _safeRoot,
                "EnemyViewport",
                new Color(
                    GameTheme.Panel.r,
                    GameTheme.Panel.g,
                    GameTheme.Panel.b,
                    0.86f),
                new Vector2(0.06f, 0.12f),
                new Vector2(0.94f, 0.765f),
                Vector2.zero,
                Vector2.zero);
            Mask mask = viewport.gameObject.AddComponent<Mask>();
            mask.showMaskGraphic = true;
            ScrollRect scrollRect = viewport.gameObject.AddComponent<ScrollRect>();
            scrollRect.horizontal = false;

            GameObject contentObject = new GameObject(
                "Content",
                typeof(RectTransform),
                typeof(VerticalLayoutGroup),
                typeof(ContentSizeFitter));
            contentObject.transform.SetParent(viewport, false);
            RectTransform content = contentObject.GetComponent<RectTransform>();
            content.anchorMin = new Vector2(0, 1);
            content.anchorMax = new Vector2(1, 1);
            // Same width as the viewport (a new RectTransform is 100 wider).
            content.sizeDelta = Vector2.zero;
            content.pivot = new Vector2(0.5f, 1);
            content.anchoredPosition = Vector2.zero;
            VerticalLayoutGroup layout =
                contentObject.GetComponent<VerticalLayoutGroup>();
            layout.padding = new RectOffset(22, 22, 44, 28);
            layout.spacing = 12;
            layout.childControlHeight = true;
            layout.childForceExpandHeight = false;
            contentObject.GetComponent<ContentSizeFitter>().verticalFit =
                ContentSizeFitter.FitMode.PreferredSize;
            scrollRect.viewport = viewport;
            scrollRect.content = content;

            foreach (EnemyDefinition enemy in Catalog.Enemies.Values
                         .OrderBy(item => item.IsBoss)
                         .ThenBy(item => item.IsElite)
                         .ThenBy(item => item.Id))
            {
                CreateEnemyCodexRow(content, enemy);
            }

            CreateRuneCodexButton();
            CreateBackButton();
        }

        public void ShowRuneCodex()
        {
            CreateStandardScreen("codex.runes");

            int seen = SaveData.SeenRuneIds.Count(
                id => RunContentCatalog.GetRune(id) != null);
            UiFactory.CreateText(
                _safeRoot,
                "Progress",
                $"{Localization.Get("codex.rune_progress")}  " +
                $"{seen} / {RunContentCatalog.Runes.Count}",
                27,
                GameTheme.Cyan,
                TextAnchor.MiddleCenter,
                new Vector2(0.06f, 0.835f),
                new Vector2(0.94f, 0.875f),
                Vector2.zero,
                Vector2.zero,
                FontStyle.Bold);
            UiFactory.CreateButton(
                _safeRoot,
                "SwordsTab",
                Localization.Get("codex.swords"),
                GameTheme.PanelLight,
                GameTheme.Text,
                22,
                new Vector2(0.06f, 0.775f),
                new Vector2(0.34f, 0.83f),
                Vector2.zero,
                Vector2.zero,
                ShowCodex);
            UiFactory.CreateButton(
                _safeRoot,
                "RecipesTab",
                Localization.Get("codex.recipes"),
                GameTheme.Violet,
                GameTheme.Text,
                22,
                new Vector2(0.36f, 0.775f),
                new Vector2(0.64f, 0.83f),
                Vector2.zero,
                Vector2.zero,
                ShowRecipeCodex);
            UiFactory.CreateButton(
                _safeRoot,
                "EnemiesTab",
                Localization.Get("codex.enemies"),
                GameTheme.PanelLight,
                GameTheme.Text,
                22,
                new Vector2(0.66f, 0.775f),
                new Vector2(0.94f, 0.83f),
                Vector2.zero,
                Vector2.zero,
                ShowEnemyCodex);

            RectTransform viewport = UiFactory.CreatePanel(
                _safeRoot,
                "RuneViewport",
                new Color(
                    GameTheme.Panel.r,
                    GameTheme.Panel.g,
                    GameTheme.Panel.b,
                    0.86f),
                new Vector2(0.06f, 0.12f),
                new Vector2(0.94f, 0.765f),
                Vector2.zero,
                Vector2.zero);
            Mask mask = viewport.gameObject.AddComponent<Mask>();
            mask.showMaskGraphic = true;
            ScrollRect scrollRect = viewport.gameObject.AddComponent<ScrollRect>();
            scrollRect.horizontal = false;

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
            layout.padding = new RectOffset(22, 22, 44, 28);
            layout.spacing = 12;
            layout.childControlHeight = true;
            layout.childForceExpandHeight = false;
            contentObject.GetComponent<ContentSizeFitter>().verticalFit =
                ContentSizeFitter.FitMode.PreferredSize;
            scrollRect.viewport = viewport;
            scrollRect.content = content;

            foreach (RuneDefinition rune in RunContentCatalog.Runes)
            {
                CreateRuneCodexRow(content, rune);
            }

            CreateBackButton();
        }

        public void ShowStatistics()
        {
            CreateStandardScreen("stats.title");

            RectTransform overview = UiFactory.CreatePanel(
                _safeRoot,
                "Overview",
                GameTheme.Panel,
                new Vector2(0.07f, 0.56f),
                new Vector2(0.93f, 0.82f),
                Vector2.zero,
                Vector2.zero);
            UiFactory.CreateText(
                overview,
                "OverviewText",
                $"{Localization.Get("menu.level")}  LV.{SaveData.PlayerLevel}\n" +
                $"{Localization.Get("stats.runs")}  {SaveData.RunsPlayed:N0}    " +
                $"{Localization.Get("stats.victories")}  {SaveData.Victories:N0}\n" +
                $"{Localization.Get("stats.best_score")}  {SaveData.BestScore:N0}    " +
                $"{Localization.Get("stats.highest_wave")}  {SaveData.HighestWave:N0}",
                29,
                GameTheme.Text,
                TextAnchor.MiddleLeft,
                new Vector2(0.13f, 0.12f),
                new Vector2(0.87f, 0.88f),
                Vector2.zero,
                Vector2.zero,
                FontStyle.Bold);

            RectTransform lifetime = UiFactory.CreatePanel(
                _safeRoot,
                "Lifetime",
                GameTheme.Panel,
                new Vector2(0.07f, 0.20f),
                new Vector2(0.93f, 0.53f),
                Vector2.zero,
                Vector2.zero);
            UiFactory.CreateText(
                lifetime,
                "LifetimeText",
                $"{Localization.Get("stats.damage")}  {SaveData.TotalDamageDealt:N0}\n" +
                $"{(Localization.IsKorean ? "처치" : "Defeated")}  {SaveData.TotalEnemiesDefeated:N0}    " +
                $"{(Localization.IsKorean ? "보스" : "Bosses")}  {SaveData.TotalBossesDefeated:N0}\n" +
                $"{Localization.Get("stats.summons")}  {SaveData.TotalSwordsSummoned:N0}\n" +
                $"{Localization.Get("stats.forges")}  {SaveData.TotalSuccessfulForges:N0}\n" +
                $"{Localization.Get("stats.ultimates")}  {SaveData.TotalUltimatesUsed:N0}\n" +
                $"{Localization.Get("stats.enemy_skills")}  {SaveData.TotalEnemyAbilitiesTriggered:N0}",
                29,
                GameTheme.Cyan,
                TextAnchor.MiddleLeft,
                new Vector2(0.13f, 0.10f),
                new Vector2(0.87f, 0.90f),
                Vector2.zero,
                Vector2.zero);
            CreateBackButton();
        }

        public void ShowUpgrades()
        {
            CreateStandardScreen("upgrade.title");

            UiFactory.CreateText(
                _safeRoot,
                "Currency",
                $"⚒  {SaveData.IronPowder:N0}",
                34,
                GameTheme.Gold,
                TextAnchor.MiddleRight,
                new Vector2(0.58f, 0.84f),
                new Vector2(0.92f, 0.89f),
                Vector2.zero,
                Vector2.zero,
                FontStyle.Bold);

            CreateUpgradeCard(
                "Smithing",
                "upgrade.smithing",
                "upgrade.smithing_desc",
                0.62f,
                UpgradeBranch.Smithing,
                GameTheme.Gold);
            CreateUpgradeCard(
                "SwordControl",
                "upgrade.sword_control",
                "upgrade.sword_control_desc",
                0.39f,
                UpgradeBranch.SwordControl,
                GameTheme.Cyan);
            CreateUpgradeCard(
                "Guardianship",
                "upgrade.guardianship",
                "upgrade.guardianship_desc",
                0.16f,
                UpgradeBranch.Guardianship,
                GameTheme.Mint);

            if (!string.IsNullOrWhiteSpace(_upgradeFeedback))
            {
                UiFactory.CreateText(
                    _safeRoot,
                    "Feedback",
                    _upgradeFeedback,
                    22,
                    GameTheme.Mint,
                    TextAnchor.MiddleCenter,
                    new Vector2(0.38f, 0.04f),
                    new Vector2(0.92f, 0.10f),
                    Vector2.zero,
                    Vector2.zero,
                    FontStyle.Bold);
            }
            CreateBackButton();
        }

        public void ShowStageSelect()
        {
            CreateStandardScreen("menu.stage_select");

            CreateStageList();

            Button dailyButton = UiFactory.CreateButton(
                _safeRoot,
                "Daily",
                Localization.Get("menu.daily"),
                GameTheme.Gold,
                GameTheme.Background,
                28,
                new Vector2(0.07f, 0.135f),
                new Vector2(0.48f, 0.235f),
                Vector2.zero,
                Vector2.zero,
                () => ShowLoadout("stage_starlit_forge", RunMode.Daily, 0));
            Button endlessButton = UiFactory.CreateButton(
                _safeRoot,
                "Endless",
                Localization.Get("menu.endless"),
                GameTheme.PanelLight,
                GameTheme.Text,
                28,
                new Vector2(0.52f, 0.135f),
                new Vector2(0.93f, 0.235f),
                Vector2.zero,
                Vector2.zero,
                () => ShowLoadout("stage_worldroot_void", RunMode.Endless, 0));
            endlessButton.interactable = ProgressionService.IsStageUnlocked(
                SaveData,
                "stage_worldroot_void");
            dailyButton.interactable = LearningPath.IsComplete(SaveData);
            // One-line buttons; the explanation sits underneath, clear of the frame art.
            UiFactory.CreateText(
                _safeRoot,
                "DailyDescription",
                Localization.Get("menu.daily_desc"),
                20,
                GameTheme.MutedText,
                TextAnchor.MiddleCenter,
                new Vector2(0.07f, 0.10f),
                new Vector2(0.48f, 0.135f),
                Vector2.zero,
                Vector2.zero);
            UiFactory.CreateText(
                _safeRoot,
                "EndlessDescription",
                Localization.Get("menu.endless_desc"),
                20,
                GameTheme.MutedText,
                TextAnchor.MiddleCenter,
                new Vector2(0.52f, 0.10f),
                new Vector2(0.93f, 0.135f),
                Vector2.zero,
                Vector2.zero);
            CreateBackButton();
        }

        // A step number sticks through the summon-pool screen and back.
        private int _loadoutStep;

        private void CreateStageList()
        {
            RectTransform viewport = UiFactory.CreatePanel(
                _safeRoot,
                "StageListViewport",
                new Color(GameTheme.Panel.r, GameTheme.Panel.g, GameTheme.Panel.b, 0.86f),
                new Vector2(0.06f, 0.255f),
                new Vector2(0.94f, 0.835f),
                Vector2.zero,
                Vector2.zero);
            viewport.gameObject.AddComponent<Mask>().showMaskGraphic = true;
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
            content.sizeDelta = Vector2.zero;
            content.pivot = new Vector2(0.5f, 1f);
            content.anchoredPosition = Vector2.zero;
            VerticalLayoutGroup layout = contentObject.GetComponent<VerticalLayoutGroup>();
            layout.padding = new RectOffset(22, 22, 62, 28);
            layout.spacing = 10;
            layout.childControlHeight = true;
            layout.childForceExpandHeight = false;
            contentObject.GetComponent<ContentSizeFitter>().verticalFit =
                ContentSizeFitter.FitMode.PreferredSize;
            scroll.viewport = viewport;
            scroll.content = content;

            bool korean = Localization.IsKorean;
            foreach (LearningStep step in LearningPath.Steps)
            {
                int number = step.Number;
                bool cleared = LearningPath.IsCleared(SaveData, number);
                bool unlocked = LearningPath.IsUnlocked(SaveData, number);
                int best = LearningPath.BestWave(SaveData, number);
                string status = cleared
                    ? $"✓ {LearningPath.StarText(LearningPath.BestStars(SaveData, number))}"
                    : !unlocked
                        ? Localization.Get("common.locked")
                        : best > 0
                            ? (korean ? $"▶ 최고 {best}/{step.Waves}" : $"▶ Best {best}/{step.Waves}")
                            : (korean ? "▶ 도전" : "▶ Play");
                CreateStageRow(
                    content,
                    $"Step_{number}",
                    korean ? $"{number}단계" : $"Step {number}",
                    korean ? step.NameKo : step.NameEn,
                    korean ? $"{step.Waves}웨이브" : $"{step.Waves} waves",
                    status,
                    cleared ? GameTheme.Mint : unlocked ? GameTheme.Gold : GameTheme.MutedText,
                    unlocked,
                    () =>
                    {
                        // Steps before the ultimate have nothing to set up.
                        if (LearningPath.Get(number).Has(GameFeature.Ultimate))
                        {
                            ShowLoadout(LearningPath.StageId, RunMode.Standard, number);
                        }
                        else
                        {
                            StartBattle(LearningPath.StageId, RunMode.Standard, number);
                        }
                    });
            }

            foreach (StageDefinition stage in Catalog.Stages.Values)
            {
                if (stage.Id == LearningPath.StageId)
                {
                    continue;
                }
                bool unlocked = ProgressionService.IsStageUnlocked(SaveData, stage.Id);
                bool cleared = SaveData.CompletedStageIds.Contains(stage.Id);
                CreateStageRow(
                    content,
                    stage.Id,
                    korean ? "균열" : "Rift",
                    Localization.StageName(stage),
                    korean ? $"{RunBalanceRules.TotalWaves(RunMode.Standard)}웨이브" : $"{RunBalanceRules.TotalWaves(RunMode.Standard)} waves",
                    cleared
                        ? (korean ? "✓ 클리어" : "✓ Cleared")
                        : unlocked
                            ? (korean ? "▶ 도전" : "▶ Play")
                            : Localization.Get("common.locked"),
                    cleared ? GameTheme.Mint : unlocked ? GameTheme.Gold : GameTheme.MutedText,
                    unlocked,
                    () => ShowLoadout(stage.Id, RunMode.Standard, 0));
            }
        }

        private void CreateStageRow(
            Transform parent,
            string name,
            string label,
            string title,
            string detail,
            string status,
            Color statusColor,
            bool interactable,
            Action onTap)
        {
            GameObject rowObject = new GameObject(
                name,
                typeof(RectTransform),
                typeof(Image),
                typeof(LayoutElement),
                typeof(Button));
            rowObject.transform.SetParent(parent, false);
            UiFactory.ApplyOrnatePanel(rowObject.GetComponent<Image>());
            rowObject.GetComponent<LayoutElement>().preferredHeight = 118;
            Button button = rowObject.GetComponent<Button>();
            button.interactable = interactable;
            button.onClick.AddListener(() =>
            {
                AudioService.PlayUiClick();
                onTap?.Invoke();
            });

            UiFactory.CreateText(
                rowObject.transform,
                "Label",
                label,
                22,
                interactable ? GameTheme.Gold : GameTheme.MutedText,
                TextAnchor.MiddleCenter,
                new Vector2(0.03f, 0.15f),
                new Vector2(0.20f, 0.85f),
                Vector2.zero,
                Vector2.zero,
                FontStyle.Bold).raycastTarget = false;
            UiFactory.CreateText(
                rowObject.transform,
                "Name",
                title,
                27,
                interactable ? GameTheme.Text : GameTheme.MutedText,
                TextAnchor.MiddleCenter,
                new Vector2(0.20f, 0.46f),
                new Vector2(0.70f, 0.90f),
                Vector2.zero,
                Vector2.zero,
                FontStyle.Bold).raycastTarget = false;
            UiFactory.CreateText(
                rowObject.transform,
                "Detail",
                detail,
                20,
                GameTheme.MutedText,
                TextAnchor.MiddleCenter,
                new Vector2(0.20f, 0.10f),
                new Vector2(0.70f, 0.46f),
                Vector2.zero,
                Vector2.zero).raycastTarget = false;
            UiFactory.CreateText(
                rowObject.transform,
                $"{name}_Status",
                status,
                24,
                statusColor,
                TextAnchor.MiddleCenter,
                new Vector2(0.70f, 0.15f),
                new Vector2(0.97f, 0.85f),
                Vector2.zero,
                Vector2.zero,
                FontStyle.Bold).raycastTarget = false;
        }

        public void ShowLoadout(string stageId, RunMode mode, int learningStep = -1)
        {
            if (learningStep >= 0)
            {
                _loadoutStep = learningStep;
            }
            StageDefinition stage = Catalog.GetStage(stageId);
            if (stage == null ||
                (mode != RunMode.Daily &&
                 !ProgressionService.IsStageUnlocked(SaveData, stageId)))
            {
                ShowStageSelect();
                return;
            }

            CreateStandardScreen("loadout.title");
            UiFactory.CreateText(
                _safeRoot,
                "Stage",
                Localization.StageName(stage),
                32,
                GameTheme.Cyan,
                TextAnchor.MiddleCenter,
                new Vector2(0.10f, 0.80f),
                new Vector2(0.90f, 0.86f),
                Vector2.zero,
                Vector2.zero,
                FontStyle.Bold);
            UiFactory.CreateText(
                _safeRoot,
                "UltimateTitle",
                Localization.Get("loadout.ultimate"),
                26,
                GameTheme.Gold,
                TextAnchor.MiddleCenter,
                new Vector2(0.10f, 0.73f),
                new Vector2(0.90f, 0.79f),
                Vector2.zero,
                Vector2.zero,
                FontStyle.Bold);

            for (int index = 0;
                 index < RunContentCatalog.Ultimates.Count;
                 index++)
            {
                UltimateDefinition ultimate =
                    RunContentCatalog.Ultimates[index];
                bool selected =
                    SaveData.SelectedUltimateId == ultimate.Id;
                float top = 0.69f - index * 0.15f;
                string name = Localization.IsKorean
                    ? ultimate.NameKo
                    : ultimate.NameEn;
                string description = Localization.IsKorean
                    ? ultimate.DescriptionKo
                    : ultimate.DescriptionEn;
                Button ultimateButton = UiFactory.CreateButton(
                    _safeRoot,
                    $"Ultimate_{ultimate.Id}",
                    $"{(selected ? "◆ " : string.Empty)}{name}\n{description}",
                    selected ? GameTheme.Gold : GameTheme.PanelLight,
                    selected ? GameTheme.Background : GameTheme.Text,
                    28,
                    new Vector2(0.10f, top - 0.12f),
                    new Vector2(0.90f, top),
                    Vector2.zero,
                    Vector2.zero,
                    () =>
                    {
                        SaveData.SelectedUltimateId = ultimate.Id;
                        Save();
                        ShowLoadout(stageId, mode);
                    });
                // Two lines sit between the corner ornaments, not over them.
                RectTransform ultimateLabel =
                    (RectTransform)ultimateButton.GetComponentInChildren<Text>().transform;
                ultimateLabel.anchorMin = new Vector2(0.17f, 0.18f);
                ultimateLabel.anchorMax = new Vector2(0.83f, 0.82f);
            }

            UiFactory.CreateButton(
                _safeRoot,
                "SummonPool",
                $"{Localization.Get("loadout.pool_edit")}  " +
                $"{SaveData.SelectedBaseSwordIds.Count}/6",
                GameTheme.Violet,
                GameTheme.Text,
                23,
                new Vector2(0.18f, 0.20f),
                new Vector2(0.82f, 0.26f),
                Vector2.zero,
                Vector2.zero,
                () => ShowSummonPool(stageId, mode));
            UiFactory.CreateButton(
                _safeRoot,
                "Start",
                Localization.Get("loadout.start"),
                GameTheme.Mint,
                GameTheme.Background,
                31,
                new Vector2(0.18f, 0.11f),
                new Vector2(0.82f, 0.18f),
                Vector2.zero,
                Vector2.zero,
                () => StartBattle(stageId, mode, _loadoutStep));
            CreateBackButton();
        }

        private void ShowSummonPool(string stageId, RunMode mode)
        {
            CreateStandardScreen("loadout.pool");
            UiFactory.CreateText(
                _safeRoot,
                "Rule",
                Localization.Get("loadout.pool_rule"),
                24,
                GameTheme.MutedText,
                TextAnchor.MiddleCenter,
                new Vector2(0.08f, 0.81f),
                new Vector2(0.92f, 0.87f),
                Vector2.zero,
                Vector2.zero);

            List<SwordDefinition> baseSwords = Catalog.BaseSwordIds
                .Select(Catalog.GetSword)
                .Where(item => item != null)
                .ToList();
            for (int index = 0; index < baseSwords.Count; index++)
            {
                SwordDefinition sword = baseSwords[index];
                int column = index % 2;
                int row = index / 2;
                float minX = column == 0 ? 0.07f : 0.51f;
                float maxX = column == 0 ? 0.49f : 0.93f;
                float maxY = 0.78f - row * 0.105f;
                bool unlocked =
                    SaveData.UnlockedSwordIds.Contains(sword.Id);
                bool selected =
                    SaveData.SelectedBaseSwordIds.Contains(sword.Id);
                Button button = UiFactory.CreateButton(
                    _safeRoot,
                    $"Pool_{sword.Id}",
                    $"{(selected ? "◆ " : string.Empty)}" +
                    $"{(unlocked ? Localization.SwordName(sword) : "???")}",
                    selected
                        ? GameTheme.FromHex(sword.ColorRgb)
                        : GameTheme.PanelLight,
                    selected ? GameTheme.Background : GameTheme.Text,
                    21,
                    new Vector2(minX, maxY - 0.085f),
                    new Vector2(maxX, maxY),
                    Vector2.zero,
                    Vector2.zero,
                    () =>
                    {
                        ToggleSummonPoolSword(sword.Id);
                        ShowSummonPool(stageId, mode);
                    });
                button.interactable = unlocked;
            }

            UiFactory.CreateButton(
                _safeRoot,
                "Done",
                Localization.Get("menu.back"),
                GameTheme.Gold,
                GameTheme.Background,
                28,
                new Vector2(0.18f, 0.08f),
                new Vector2(0.82f, 0.145f),
                Vector2.zero,
                Vector2.zero,
                () => ShowLoadout(stageId, mode));
            _backAction = () => ShowLoadout(stageId, mode);
        }

        private void ToggleSummonPoolSword(string swordId)
        {
            if (SaveData.SelectedBaseSwordIds.Contains(swordId))
            {
                if (SaveData.SelectedBaseSwordIds.Count > 4)
                {
                    SaveData.SelectedBaseSwordIds.Remove(swordId);
                }
            }
            else if (SaveData.SelectedBaseSwordIds.Count < 6 &&
                     SaveData.UnlockedSwordIds.Contains(swordId))
            {
                SaveData.SelectedBaseSwordIds.Add(swordId);
            }
            Save();
        }

        public void ShowShop()
        {
            CreateStandardScreen("shop.title");

            UiFactory.CreateText(
                _safeRoot,
                "Currency",
                $"✦ {SaveData.Starsteel:N0}    ⚒ {SaveData.IronPowder:N0}",
                31,
                GameTheme.Gold,
                TextAnchor.MiddleRight,
                new Vector2(0.38f, 0.84f),
                new Vector2(0.93f, 0.89f),
                Vector2.zero,
                Vector2.zero,
                FontStyle.Bold);

            RectTransform blueprintCard = UiFactory.CreatePanel(
                _safeRoot,
                "BlueprintCard",
                GameTheme.Panel,
                new Vector2(0.07f, 0.47f),
                new Vector2(0.93f, 0.81f),
                Vector2.zero,
                Vector2.zero);
            UiFactory.CreateText(
                blueprintCard,
                "Title",
                Localization.Get("shop.blueprint"),
                34,
                GameTheme.Cyan,
                TextAnchor.MiddleLeft,
                new Vector2(0.11f, 0.71f),
                new Vector2(0.89f, 0.94f),
                Vector2.zero,
                Vector2.zero,
                FontStyle.Bold);
            UiFactory.CreateText(
                blueprintCard,
                "Description",
                Localization.Get("shop.blueprint_desc"),
                23,
                GameTheme.MutedText,
                TextAnchor.MiddleLeft,
                new Vector2(0.11f, 0.51f),
                new Vector2(0.89f, 0.72f),
                Vector2.zero,
                Vector2.zero);
            UiFactory.CreateText(
                blueprintCard,
                "Odds",
                Localization.Get("shop.odds"),
                21,
                GameTheme.Gold,
                TextAnchor.MiddleLeft,
                new Vector2(0.11f, 0.29f),
                new Vector2(0.89f, 0.51f),
                Vector2.zero,
                Vector2.zero,
                FontStyle.Bold);
            UiFactory.CreateButton(
                blueprintCard,
                "Pull",
                Localization.Get("shop.pull"),
                GameTheme.Gold,
                GameTheme.Background,
                26,
                new Vector2(0.12f, 0.07f),
                new Vector2(0.88f, 0.27f),
                Vector2.zero,
                Vector2.zero,
                PullBlueprint);

            RectTransform growthCard = UiFactory.CreatePanel(
                _safeRoot,
                "GrowthCard",
                GameTheme.Panel,
                new Vector2(0.07f, 0.235f),
                new Vector2(0.93f, 0.43f),
                Vector2.zero,
                Vector2.zero);
            UiFactory.CreateText(
                growthCard,
                "Title",
                Localization.Get("shop.materials"),
                29,
                GameTheme.Violet,
                TextAnchor.MiddleCenter,
                new Vector2(0.15f, 0.70f),
                new Vector2(0.85f, 0.88f),
                Vector2.zero,
                Vector2.zero,
                FontStyle.Bold);
            UiFactory.CreateText(
                growthCard,
                "Materials",
                $"◇ {SaveData.UniversalBlueprintFragments:N0}    " +
                $"◆ {SaveData.Catalysts:N0}    " +
                $"◈ {SaveData.BossCores:N0}",
                26,
                GameTheme.Text,
                TextAnchor.MiddleCenter,
                new Vector2(0.15f, 0.50f),
                new Vector2(0.85f, 0.68f),
                Vector2.zero,
                Vector2.zero);
            Button universalButton = UiFactory.CreateButton(
                growthCard,
                "UniversalUnlock",
                Localization.Get("shop.universal_unlock"),
                GameTheme.Violet,
                GameTheme.Text,
                24,
                new Vector2(0.22f, 0.12f),
                new Vector2(0.78f, 0.44f),
                Vector2.zero,
                Vector2.zero,
                UnlockWithUniversalFragments);
            universalButton.interactable =
                SaveData.UniversalBlueprintFragments >=
                GachaService.UnlockRequirement;

            if (!string.IsNullOrWhiteSpace(_shopFeedback))
            {
                UiFactory.CreateText(
                    _safeRoot,
                    "Feedback",
                    _shopFeedback,
                    28,
                    GameTheme.Mint,
                    TextAnchor.MiddleCenter,
                    new Vector2(0.08f, 0.16f),
                    new Vector2(0.92f, 0.225f),
                    Vector2.zero,
                    Vector2.zero,
                    FontStyle.Bold);
            }

            CreateBackButton();
        }

        public void Save()
        {
            SaveData.Locale = Localization.Locale;
            _saveService.Save(SaveData);
        }

        private void PullBlueprint()
        {
            GachaPullResult result = Gacha.TryPull(SaveData);
            if (!result.Success)
            {
                _shopFeedback = Localization.Get(result.MessageKey);
                ShowShop();
                return;
            }

            SwordDefinition sword = Catalog.GetSword(result.SwordId);
            _shopFeedback =
                $"{Localization.Get(result.Unlocked ? "shop.unlocked" : result.MessageKey)}\n" +
                $"{Localization.SwordName(sword)}  +{result.Fragments}";
            Save();
            ShowShop();
        }

        private void UnlockWithUniversalFragments()
        {
            GachaPullResult result =
                Gacha.TryUnlockWithUniversalFragments(SaveData);
            if (!result.Success)
            {
                _shopFeedback = Localization.Get(result.MessageKey);
            }
            else
            {
                SwordDefinition sword = Catalog.GetSword(result.SwordId);
                _shopFeedback =
                    $"{Localization.Get(result.MessageKey)}\n" +
                    $"{Localization.SwordName(sword)}";
                Save();
            }
            ShowShop();
        }

        public RunRewardResult HandleRunCompleted(RunSummary summary)
        {
            int today =
                DateTime.Now.Year * 10000 +
                DateTime.Now.Month * 100 +
                DateTime.Now.Day;
            RunRewardResult reward =
                ProgressionService.ApplyRun(SaveData, summary, today);
            Save();
            return reward;
        }

        public void StartBattle()
        {
            StartBattle("stage_starlit_forge", RunMode.Standard);
        }

        public void StartBattle(string stageId, RunMode mode, int learningStep = 0)
        {
            DisposeBattle();
            DestroyCurrentCanvas();
            GameObject battleObject = new GameObject("BattleController");
            battleObject.transform.SetParent(transform, false);
            _battle = battleObject.AddComponent<BattleController>();
            StageDefinition stage = Catalog.GetStage(stageId) ?? Catalog.Stages.Values.First();
            _battle.Initialize(this, stage, mode, LearningPath.Get(learningStep));
        }

        private void DisposeBattle()
        {
            if (_battle != null)
            {
                Destroy(_battle.gameObject);
                _battle = null;
            }
        }

        private void OnLocaleChanged()
        {
            Save();
            if (_battle != null)
            {
                _battle.RefreshLocalization();
            }
            else
            {
                ShowMainMenu();
            }
        }

        private void CreateScreenCanvas(string name)
        {
            DestroyCurrentCanvas();
            _backAction = null;
            _canvas = UiFactory.CreateCanvas(name);
            _canvas.transform.SetParent(transform, false);

            _safeRoot = UiFactory.CreatePanel(
                _canvas.transform,
                "SafeArea",
                Color.clear,
                Vector2.zero,
                Vector2.one,
                Vector2.zero,
                Vector2.zero);
            _safeRoot.gameObject.AddComponent<SafeAreaFitter>();
        }

        private void DestroyCurrentCanvas()
        {
            if (_canvas != null)
            {
                Destroy(_canvas.gameObject);
                _canvas = null;
                _safeRoot = null;
            }
        }

        private void CreateStandardScreen(string titleKey)
        {
            CreateScreenCanvas($"{titleKey}Canvas");
            RectTransform background = UiFactory.CreatePanel(
                _canvas.transform,
                "Background",
                GameTheme.Background,
                Vector2.zero,
                Vector2.one,
                Vector2.zero,
                Vector2.zero);
            background.SetAsFirstSibling();
            Sprite backgroundSprite = Resources.Load<Sprite>(
                "Art/UI/MainMenuBackground");
            if (backgroundSprite != null)
            {
                Image backgroundImage = background.GetComponent<Image>();
                backgroundImage.sprite = backgroundSprite;
                backgroundImage.color = new Color(
                    0.42f,
                    0.46f,
                    0.62f,
                    1f);
            }

            UiFactory.CreatePanel(
                _safeRoot,
                "BackdropVeil",
                new Color(0.015f, 0.02f, 0.08f, 0.58f),
                Vector2.zero,
                Vector2.one,
                Vector2.zero,
                Vector2.zero);
            UiFactory.CreatePanel(
                _safeRoot,
                "HeaderPlate",
                Color.white,
                new Vector2(0.035f, 0.875f),
                new Vector2(0.965f, 0.98f),
                Vector2.zero,
                Vector2.zero);
            UiFactory.CreateText(
                _safeRoot,
                "Header",
                Localization.Get(titleKey),
                52,
                GameTheme.Text,
                TextAnchor.MiddleCenter,
                new Vector2(0.15f, 0.885f),
                new Vector2(0.85f, 0.97f),
                Vector2.zero,
                Vector2.zero,
                FontStyle.Bold);
        }

        private void CreateMenuButton(
            string name,
            string labelKey,
            float minY,
            float maxY,
            Color background,
            Color foreground,
            Action action)
        {
            UiFactory.CreateButton(
                _safeRoot,
                name,
                Localization.Get(labelKey),
                background,
                foreground,
                36,
                new Vector2(0.13f, minY),
                new Vector2(0.87f, maxY),
                Vector2.zero,
                Vector2.zero,
                action);
        }

        private void CreateBackButton()
        {
            _backAction = ShowMainMenu;
            UiFactory.CreateButton(
                _safeRoot,
                "BackButton",
                Localization.Get("menu.back"),
                GameTheme.PanelLight,
                GameTheme.Text,
                28,
                new Vector2(0.07f, 0.035f),
                new Vector2(0.37f, 0.095f),
                Vector2.zero,
                Vector2.zero,
                ShowMainMenu);
        }

        private void CreateRuneCodexButton()
        {
            UiFactory.CreateButton(
                _safeRoot,
                "RunesTab",
                Localization.Get("codex.runes"),
                GameTheme.Cyan,
                GameTheme.Background,
                22,
                new Vector2(0.68f, 0.035f),
                new Vector2(0.93f, 0.095f),
                Vector2.zero,
                Vector2.zero,
                ShowRuneCodex);
        }

        private static void CreateHeroPortrait(
            Transform parent,
            Vector2 anchorMin,
            Vector2 anchorMax,
            float alpha)
        {
            Sprite sprite = Resources.Load<Sprite>(
                "Art/Characters/HeroSwordsmith");
            if (sprite == null)
            {
                return;
            }

            GameObject portrait = new GameObject(
                "HeroPortrait",
                typeof(RectTransform),
                typeof(Image));
            portrait.transform.SetParent(parent, false);
            RectTransform rect = portrait.GetComponent<RectTransform>();
            rect.anchorMin = anchorMin;
            rect.anchorMax = anchorMax;
            rect.offsetMin = Vector2.zero;
            rect.offsetMax = Vector2.zero;
            Image image = portrait.GetComponent<Image>();
            image.sprite = sprite;
            image.preserveAspect = true;
            image.color = new Color(1f, 1f, 1f, Mathf.Clamp01(alpha));
            image.raycastTarget = false;
        }

        private void CreateCodexRow(Transform parent, SwordDefinition sword)
        {
            bool unlocked = SaveData.UnlockedSwordIds.Contains(sword.Id);
            GameObject rowObject = new GameObject(
                sword.Id,
                typeof(RectTransform),
                typeof(Image),
                typeof(LayoutElement));
            rowObject.transform.SetParent(parent, false);
            UiFactory.ApplyOrnatePanel(
                rowObject.GetComponent<Image>());
            LayoutElement layout = rowObject.GetComponent<LayoutElement>();
            layout.preferredHeight = 116;

            Color swordColor = GameTheme.FromHex(sword.ColorRgb);
            RectTransform artFrame = UiFactory.CreatePanel(
                rowObject.transform,
                "SwordArtFrame",
                new Color(
                    swordColor.r,
                    swordColor.g,
                    swordColor.b,
                    unlocked ? 0.26f : 0.10f),
                new Vector2(0.02f, 0.06f),
                new Vector2(0.18f, 0.94f),
                Vector2.zero,
                Vector2.zero);
            GameObject artObject = new GameObject(
                "SwordArt",
                typeof(RectTransform),
                typeof(Image));
            artObject.transform.SetParent(
                artFrame,
                false);
            RectTransform artRect =
                artObject.GetComponent<RectTransform>();
            UiFactory.SetStretch(artRect);
            Image art = artObject.GetComponent<Image>();
            bool hiddenResult = Catalog.Recipes.Any(
                recipe =>
                    recipe.ResultSwordId == sword.Id &&
                    recipe.Visibility ==
                    RecipeVisibility.Hidden);
            art.sprite = Resources.Load<Sprite>(
                $"Art/Swords/{sword.Id}");
            art.preserveAspect = true;
            art.raycastTarget = false;
            art.color = unlocked
                ? Color.white
                : hiddenResult
                    ? new Color(0f, 0f, 0f, 0f)
                    : new Color(0.03f, 0.04f, 0.10f, 0.82f);
            UiFactory.CreateText(
                rowObject.transform,
                "Name",
                unlocked ? Localization.SwordName(sword) : "???",
                28,
                unlocked ? GameTheme.Text : GameTheme.MutedText,
                TextAnchor.MiddleLeft,
                new Vector2(0.20f, 0.44f),
                new Vector2(0.73f, 0.86f),
                Vector2.zero,
                Vector2.zero,
                FontStyle.Bold);
            SwordFamily family =
                SwordFamilyRules.Resolve(sword);
            int appearanceCount =
                SaveData.OwnedAppearanceIds.Count(id =>
                    id.StartsWith(
                        sword.Id + ":",
                        StringComparison.Ordinal));
            string familyName = Localization.IsKorean
                ? SwordFamilyRules.NameKo(family)
                : SwordFamilyRules.NameEn(family);
            UiFactory.CreateText(
                rowObject.transform,
                "Detail",
                unlocked
                    ? $"{familyName} · {Localization.RarityName(sword.Rarity)}" +
                      (appearanceCount > 0
                          ? $" · ✦ {appearanceCount}"
                          : string.Empty)
                    : Localization.Get("codex.locked"),
                21,
                GameTheme.MutedText,
                TextAnchor.MiddleLeft,
                new Vector2(0.20f, 0.10f),
                new Vector2(0.73f, 0.46f),
                Vector2.zero,
                Vector2.zero);

            if (unlocked)
            {
                int masteryLevel =
                    SaveData.GetSwordMasteryLevel(
                        sword.Id);
                bool maximum =
                    masteryLevel >=
                    RunBalanceRules
                        .MaximumSwordMasteryLevel;
                int cost =
                    RunBalanceRules.SwordMasteryCost(
                        masteryLevel);
                Button masteryButton =
                    UiFactory.CreateButton(
                        rowObject.transform,
                        "Mastery",
                        maximum
                            ? $"LV.{masteryLevel}\nMAX"
                            : $"LV.{masteryLevel}\n⚒ {cost}",
                        maximum
                            ? GameTheme.PanelLight
                            : GameTheme.Violet,
                        GameTheme.Text,
                        20,
                        new Vector2(0.75f, 0.14f),
                        new Vector2(0.97f, 0.86f),
                        Vector2.zero,
                        Vector2.zero,
                        () =>
                        {
                            ProgressionService
                                .TryUpgradeSwordMastery(
                                    SaveData,
                                    sword.Id);
                            Save();
                            ShowCodex();
                        });
                masteryButton.interactable =
                    !maximum &&
                    SaveData.IronPowder >= cost;
                Image masteryImage = masteryButton.GetComponent<Image>();
                masteryImage.sprite = null;
                masteryImage.color = maximum
                    ? GameTheme.PanelLight
                    : GameTheme.Violet;
            }
        }

        private void CreateEnemyCodexRow(
            Transform parent,
            EnemyDefinition enemy)
        {
            bool seen = SaveData.SeenEnemyIds.Contains(enemy.Id);
            GameObject rowObject = new GameObject(
                enemy.Id,
                typeof(RectTransform),
                typeof(Image),
                typeof(LayoutElement));
            rowObject.transform.SetParent(parent, false);
            UiFactory.ApplyOrnatePanel(
                rowObject.GetComponent<Image>());
            EnemyAbilityDefinition ability =
                EnemyAbilityCatalog.Get(enemy.Id);
            rowObject.GetComponent<LayoutElement>().preferredHeight =
                ability == null ? 116 : 154;

            // The enemy's own art; unseen enemies show as a dark silhouette.
            GameObject artObject = new GameObject(
                "EnemyArt",
                typeof(RectTransform),
                typeof(Image));
            artObject.transform.SetParent(rowObject.transform, false);
            RectTransform artRect = artObject.GetComponent<RectTransform>();
            artRect.anchorMin = new Vector2(0.05f, 0.10f);
            artRect.anchorMax = new Vector2(0.15f, 0.90f);
            artRect.offsetMin = Vector2.zero;
            artRect.offsetMax = Vector2.zero;
            Image art = artObject.GetComponent<Image>();
            art.sprite = Resources.Load<Sprite>($"Art/Enemies/{enemy.Id}");
            art.preserveAspect = true;
            art.raycastTarget = false;
            art.color = seen
                ? Color.white
                : new Color(0.03f, 0.04f, 0.10f, 0.82f);
            UiFactory.CreateText(
                rowObject.transform,
                "Name",
                seen ? Localization.EnemyName(enemy) : "???",
                28,
                seen ? GameTheme.Text : GameTheme.MutedText,
                TextAnchor.MiddleLeft,
                new Vector2(0.17f, 0.44f),
                new Vector2(0.82f, 0.86f),
                Vector2.zero,
                Vector2.zero,
                FontStyle.Bold);
            string type = enemy.IsBoss
                ? (Localization.IsKorean ? "보스" : "BOSS")
                : enemy.IsElite
                    ? (Localization.IsKorean ? "정예" : "ELITE")
                    : (Localization.IsKorean ? "일반" : "NORMAL");
            UiFactory.CreateText(
                rowObject.transform,
                "Detail",
                seen
                    ? (Localization.IsKorean
                        ? $"{type}  ·  체력 {enemy.BaseHealth:0}  ·  피해 {enemy.CoreDamage}"
                        : $"{type}  ·  HP {enemy.BaseHealth:0}  ·  ATK {enemy.CoreDamage}")
                    : Localization.Get("codex.locked"),
                20,
                GameTheme.MutedText,
                TextAnchor.MiddleLeft,
                new Vector2(0.17f, ability == null ? 0.10f : 0.30f),
                new Vector2(0.92f, ability == null ? 0.46f : 0.52f),
                Vector2.zero,
                Vector2.zero);
            if (seen && ability != null)
            {
                string abilityName = Localization.IsKorean
                    ? ability.NameKo
                    : ability.NameEn;
                string description = Localization.IsKorean
                    ? ability.DescriptionKo
                    : ability.DescriptionEn;
                UiFactory.CreateText(
                    rowObject.transform,
                    "Ability",
                    $"◆ {abilityName}  ·  {description}",
                    18,
                    GameTheme.Coral,
                    TextAnchor.UpperLeft,
                    new Vector2(0.17f, 0.02f),
                    new Vector2(0.94f, 0.30f),
                    Vector2.zero,
                    Vector2.zero);
            }
        }

        private void CreateRuneCodexRow(
            Transform parent,
            RuneDefinition rune)
        {
            bool seen = SaveData.SeenRuneIds.Contains(rune.Id);
            bool locked = rune.UnlockLevel > SaveData.PlayerLevel;
            GameObject rowObject = new GameObject(
                rune.Id,
                typeof(RectTransform),
                typeof(Image),
                typeof(LayoutElement));
            rowObject.transform.SetParent(parent, false);
            UiFactory.ApplyOrnatePanel(
                rowObject.GetComponent<Image>());
            rowObject.GetComponent<LayoutElement>().preferredHeight = 124;
            // A round gem in the engraving's grade colour.
            Color gradeColor = !seen
                ? new Color(0.30f, 0.33f, 0.42f, 1f)
                : rune.Rarity == RuneRarity.Legendary
                    ? GameTheme.Gold
                    : rune.Rarity == RuneRarity.Rare
                        ? GameTheme.Cyan
                        : GameTheme.MutedText;
            RectTransform mark = UiFactory.CreatePanel(
                rowObject.transform,
                "RuneMark",
                gradeColor,
                new Vector2(0.085f, 0.5f),
                new Vector2(0.085f, 0.5f),
                Vector2.zero,
                Vector2.zero);
            mark.sizeDelta = new Vector2(50f, 50f);
            mark.anchoredPosition = Vector2.zero;
            Image markImage = mark.GetComponent<Image>();
            markImage.sprite = UiFactory.CircleSprite;
            markImage.raycastTarget = false;
            string name = seen
                ? (Localization.IsKorean ? rune.NameKo : rune.NameEn)
                : "???";
            string description = seen
                ? (Localization.IsKorean
                    ? rune.DescriptionKo
                    : rune.DescriptionEn)
                : locked
                    ? (Localization.IsKorean
                        ? $"대장장이 LV.{rune.UnlockLevel}에 해금"
                        : $"Unlocks at smith LV.{rune.UnlockLevel}")
                    : Localization.Get("codex.rune_hidden");
            UiFactory.CreateText(
                rowObject.transform,
                "Name",
                name,
                27,
                seen ? GameTheme.Gold : GameTheme.MutedText,
                TextAnchor.MiddleLeft,
                new Vector2(0.16f, 0.50f),
                new Vector2(0.94f, 0.90f),
                Vector2.zero,
                Vector2.zero,
                FontStyle.Bold);
            UiFactory.CreateText(
                rowObject.transform,
                "Description",
                description,
                20,
                GameTheme.MutedText,
                TextAnchor.MiddleLeft,
                new Vector2(0.16f, 0.10f),
                new Vector2(0.94f, 0.52f),
                Vector2.zero,
                Vector2.zero);
        }

        private void CreateRecipeRow(Transform parent, RecipeDefinition recipe)
        {
            bool discovered = SaveData.DiscoveredRecipeIds.Contains(recipe.Id);
            bool publicRecipe = recipe.Visibility == RecipeVisibility.Public;
            SwordDefinition resultSword = Catalog.GetSword(recipe.ResultSwordId);

            GameObject rowObject = new GameObject(
                recipe.Id,
                typeof(RectTransform),
                typeof(Image),
                typeof(LayoutElement));
            rowObject.transform.SetParent(parent, false);
            UiFactory.ApplyOrnatePanel(
                rowObject.GetComponent<Image>());
            rowObject.GetComponent<LayoutElement>().preferredHeight = 142;

            string title = discovered || publicRecipe
                ? Localization.SwordName(resultSword)
                : recipe.Visibility == RecipeVisibility.Hidden ? "?????" : "???";
            Color titleColor = discovered
                ? GameTheme.Gold
                : publicRecipe ? GameTheme.Cyan : GameTheme.MutedText;
            UiFactory.CreateText(
                rowObject.transform,
                "Title",
                title,
                27,
                titleColor,
                TextAnchor.MiddleLeft,
                new Vector2(0.04f, 0.62f),
                new Vector2(0.96f, 0.94f),
                Vector2.zero,
                Vector2.zero,
                FontStyle.Bold);

            string detail;
            if (discovered || publicRecipe)
            {
                string ingredients = string.Join(
                    " + ",
                    recipe.IngredientSwordIds.Select(
                        id => Localization.SwordName(Catalog.GetSword(id))));
                detail = $"{ingredients}  →  {Localization.SwordName(resultSword)}";
            }
            else if (recipe.Visibility == RecipeVisibility.Hidden)
            {
                detail = Localization.Get("codex.hidden_recipe");
            }
            else
            {
                detail = Localization.RecipeHint(recipe);
            }

            UiFactory.CreateText(
                rowObject.transform,
                "Detail",
                detail,
                20,
                GameTheme.MutedText,
                TextAnchor.UpperLeft,
                new Vector2(0.04f, 0.08f),
                new Vector2(0.96f, 0.62f),
                Vector2.zero,
                Vector2.zero);
        }

        private void CreateUpgradeCard(
            string name,
            string titleKey,
            string descriptionKey,
            float minY,
            UpgradeBranch branch,
            Color accent)
        {
            RectTransform card = UiFactory.CreatePanel(
                _safeRoot,
                name,
                GameTheme.Panel,
                new Vector2(0.07f, minY),
                new Vector2(0.93f, minY + 0.19f),
                Vector2.zero,
                Vector2.zero);
            UiFactory.ApplyOrnatePanel(
                card.GetComponent<Image>());
            UiFactory.CreatePanel(
                card,
                "Accent",
                accent,
                new Vector2(0, 0),
                new Vector2(0.018f, 1),
                Vector2.zero,
                Vector2.zero);
            UiFactory.CreateText(
                card,
                "Title",
                Localization.Get(titleKey),
                32,
                accent,
                TextAnchor.MiddleLeft,
                new Vector2(0.06f, 0.56f),
                new Vector2(0.62f, 0.92f),
                Vector2.zero,
                Vector2.zero,
                FontStyle.Bold);
            UiFactory.CreateText(
                card,
                "Description",
                Localization.Get(descriptionKey),
                23,
                GameTheme.MutedText,
                TextAnchor.UpperLeft,
                new Vector2(0.06f, 0.10f),
                new Vector2(0.66f, 0.58f),
                Vector2.zero,
                Vector2.zero);

            int level = GetUpgradeLevel(branch);
            int cost = RunBalanceRules.UpgradeCost(level);
            int catalystCost =
                RunBalanceRules.UpgradeCatalystCost(level);
            int bossCoreCost =
                RunBalanceRules.UpgradeBossCoreCost(level);
            string materialCost =
                $"{(catalystCost > 0 ? $"  ◆{catalystCost}" : string.Empty)}" +
                $"{(bossCoreCost > 0 ? $"  ◈{bossCoreCost}" : string.Empty)}";
            Button upgradeButton = UiFactory.CreateButton(
                card,
                "Upgrade",
                level >= RunBalanceRules.MaximumUpgradeLevel
                    ? $"{Localization.Get("upgrade.level")} MAX"
                    : $"{Localization.Get("upgrade.level")} {level}\n" +
                      $"{Localization.Get("upgrade.cost")} {cost}" +
                      materialCost,
                accent,
                GameTheme.Background,
                22,
                new Vector2(0.70f, 0.17f),
                new Vector2(0.96f, 0.83f),
                Vector2.zero,
                Vector2.zero,
                () =>
                {
                    UpgradeResult result =
                        ProgressionService.TryUpgrade(SaveData, branch);
                    _upgradeFeedback = Localization.Get(result.MessageKey);
                    Save();
                    ShowUpgrades();
                });
            upgradeButton.interactable =
                level < RunBalanceRules.MaximumUpgradeLevel;
        }

        private int GetUpgradeLevel(UpgradeBranch branch)
        {
            return branch switch
            {
                UpgradeBranch.Smithing => SaveData.SmithingLevel,
                UpgradeBranch.SwordControl => SaveData.SwordControlLevel,
                UpgradeBranch.Guardianship => SaveData.GuardianshipLevel,
                _ => 0
            };
        }

        private void CreateDecorativeBlades()
        {
            Color[] colors =
            {
                new Color(GameTheme.Cyan.r, GameTheme.Cyan.g, GameTheme.Cyan.b, 0.18f),
                new Color(GameTheme.Gold.r, GameTheme.Gold.g, GameTheme.Gold.b, 0.16f),
                new Color(GameTheme.Violet.r, GameTheme.Violet.g, GameTheme.Violet.b, 0.18f)
            };

            for (int index = 0; index < 7; index++)
            {
                float x = 0.08f + (index % 4) * 0.27f;
                float y = 0.12f + (index / 4) * 0.62f;
                RectTransform blade = UiFactory.CreatePanel(
                    _safeRoot,
                    $"DecorativeBlade{index}",
                    colors[index % colors.Length],
                    new Vector2(x, y),
                    new Vector2(x + 0.028f, y + 0.22f),
                    Vector2.zero,
                    Vector2.zero);
                blade.localRotation = Quaternion.Euler(0, 0, -38 + index * 13);
            }
        }

        private static void EnsureCamera()
        {
            Camera camera = Camera.main;
            if (camera == null)
            {
                GameObject cameraObject = new GameObject("Main Camera", typeof(Camera));
                cameraObject.tag = "MainCamera";
                camera = cameraObject.GetComponent<Camera>();
            }

            camera.orthographic = true;
            camera.orthographicSize = 10f;
            camera.transform.position = new Vector3(0, 0, -10);
            camera.backgroundColor = GameTheme.Background;
            camera.clearFlags = CameraClearFlags.SolidColor;
            if (UnityEngine.Object
                    .FindFirstObjectByType<AudioListener>() ==
                null)
            {
                camera.gameObject.AddComponent<AudioListener>();
            }
        }

    }
}
