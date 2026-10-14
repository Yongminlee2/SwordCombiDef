using System;
using System.IO;
using System.Linq;
using System.Reflection;
using Swordstellation.Core;
using Swordstellation.Runtime;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;

namespace Swordstellation.Editor
{
    public static class VisualAuditCapture
    {
        private const string MainScenePath = "Assets/Scenes/Main.unity";
        private static readonly string OutputDirectory = Path.Combine(
            Directory.GetCurrentDirectory(),
            "Documentation",
            "Design",
            "Audit-2026-10-11");

        private static double _startedAt;
        private static double _phaseStartedAt;
        private static int _phase;
        private static string _pendingCapture;
        private static bool _previousEnterPlayModeOptionsEnabled;
        private static EnterPlayModeOptions _previousEnterPlayModeOptions;

        public static void CaptureCurrentBattleFlow()
        {
            Directory.CreateDirectory(OutputDirectory);
            ConfigurePortraitGameView();
            EditorSceneManager.OpenScene(MainScenePath);
            _startedAt = EditorApplication.timeSinceStartup;
            _phaseStartedAt = _startedAt;
            _phase = 0;
            _pendingCapture = null;
            _previousEnterPlayModeOptionsEnabled =
                EditorSettings.enterPlayModeOptionsEnabled;
            _previousEnterPlayModeOptions =
                EditorSettings.enterPlayModeOptions;
            EditorSettings.enterPlayModeOptionsEnabled = true;
            EditorSettings.enterPlayModeOptions =
                EnterPlayModeOptions.DisableDomainReload;
            EditorApplication.playModeStateChanged += OnPlayModeStateChanged;
            EditorApplication.update += Tick;
            EditorApplication.EnterPlaymode();
        }

        public static void ValidateRuntimeBindings()
        {
            string[] paths =
            {
                "Assets/Swordstellation/Runtime/GameBootstrap.cs",
                "Assets/Swordstellation/Runtime/BattleController.cs",
                "Assets/Swordstellation/Runtime/RecipeBookOverlay.cs"
            };
            bool valid = true;
            foreach (string path in paths)
            {
                MonoScript script =
                    AssetDatabase.LoadAssetAtPath<MonoScript>(path);
                Type scriptClass = script == null
                    ? null
                    : script.GetClass();
                string guid = AssetDatabase.AssetPathToGUID(path);
                Debug.Log(
                    $"Runtime binding validation: path={path}, guid={guid}, " +
                    $"script={(script == null ? "null" : script.name)}, " +
                    $"class={(scriptClass == null ? "null" : scriptClass.AssemblyQualifiedName)}");
                valid &= scriptClass != null;
            }
            EditorApplication.Exit(valid ? 0 : 3);
        }

        private static void OnPlayModeStateChanged(PlayModeStateChange state)
        {
            if (state == PlayModeStateChange.EnteredPlayMode)
            {
                Screen.SetResolution(1080, 1920, false);
                _phaseStartedAt = EditorApplication.timeSinceStartup;
                return;
            }

            if (state == PlayModeStateChange.EnteredEditMode && _phase >= 90)
            {
                Cleanup();
                EditorApplication.Exit(0);
            }
        }

        private static void Tick()
        {
            double now = EditorApplication.timeSinceStartup;
            if (now - _startedAt > 95d)
            {
                Debug.LogError("Visual audit capture timed out.");
                Cleanup();
                EditorApplication.Exit(2);
                return;
            }

            if (!EditorApplication.isPlaying)
            {
                return;
            }

            if (!string.IsNullOrEmpty(_pendingCapture))
            {
                if (!File.Exists(_pendingCapture) || new FileInfo(_pendingCapture).Length == 0)
                {
                    return;
                }

                Debug.Log($"Visual audit screenshot ready: {_pendingCapture}");
                _pendingCapture = null;
                _phase++;
                _phaseStartedAt = now;
                return;
            }

            if (now - _phaseStartedAt < 1.2d)
            {
                return;
            }

            GameBootstrap bootstrap = UnityEngine.Object.FindFirstObjectByType<GameBootstrap>();
            if (bootstrap == null)
            {
                return;
            }

            if (_phase == 0)
            {
                bootstrap.StartBattle();
                _phase = 1;
                _phaseStartedAt = now;
                return;
            }

            BattleController battle = UnityEngine.Object.FindFirstObjectByType<BattleController>();
            if (battle == null)
            {
                return;
            }

            switch (_phase)
            {
                case 1:
                    Capture("01-battle-entry.png");
                    break;
                case 2:
                    AddAuditBoard(battle);
                    _phase = 3;
                    _phaseStartedAt = now;
                    break;
                case 3:
                    Capture("02-combination-ready.png");
                    break;
                case 4:
                    battle.OpenRecipeBook();
                    _phase = 5;
                    _phaseStartedAt = now;
                    break;
                case 5:
                    Capture("03-recipe-book.png");
                    break;
                case 6:
                    battle.GetComponent<RecipeBookOverlay>()?.Close();
                    FillBoard(battle);
                    _phase = 7;
                    _phaseStartedAt = now;
                    break;
                case 7:
                    Capture("04-board-full-sell-hint.png");
                    break;
                case 8:
                    typeof(BattleController)
                        .GetField(
                            "_draggingSlot",
                            BindingFlags.NonPublic | BindingFlags.Instance)
                        ?.SetValue(battle, 0);
                    _phase = 9;
                    _phaseStartedAt = now;
                    break;
                case 9:
                    Capture("05-drag-to-sell.png");
                    break;
                case 10:
                    _phase = 90;
                    EditorApplication.ExitPlaymode();
                    break;
            }
        }

        private static void Capture(string fileName)
        {
            _pendingCapture = Path.Combine(OutputDirectory, fileName);
            if (File.Exists(_pendingCapture))
            {
                File.Delete(_pendingCapture);
            }
            ScreenCapture.CaptureScreenshot(_pendingCapture, 1);
        }

        private static void AddAuditBoard(BattleController battle)
        {
            (string id, int stars)[] swords =
            {
                ("ember_blade", 1),
                ("ember_blade", 1),
                ("frost_blade", 1),
                ("gale_blade", 1),
                ("thunder_blade", 1),
                ("stone_blade", 2),
                ("sprout_blade", 1)
            };

            for (int index = 0; index < swords.Length; index++)
            {
                (string id, int stars) = swords[index];
                battle.Inventory.TryAdd(
                    new SwordInstance(
                        id,
                        stars,
                        (ulong)(1000 + index),
                        $"visual-audit-{index}"),
                    out _);
            }
        }

        private static void FillBoard(BattleController battle)
        {
            string[] ids = new ContentCatalog().BaseSwordIds.ToArray();
            for (int index = 0;
                 battle.Inventory.EmptyCount > 0;
                 index++)
            {
                battle.Inventory.TryAdd(
                    new SwordInstance(
                        ids[index % ids.Length],
                        1 + index % 2,
                        (ulong)(2000 + index),
                        $"visual-audit-fill-{index}"),
                    out _);
            }
        }

        private static void Cleanup()
        {
            EditorApplication.update -= Tick;
            EditorApplication.playModeStateChanged -= OnPlayModeStateChanged;
            EditorSettings.enterPlayModeOptionsEnabled =
                _previousEnterPlayModeOptionsEnabled;
            EditorSettings.enterPlayModeOptions =
                _previousEnterPlayModeOptions;
        }

        private static void ConfigurePortraitGameView()
        {
            try
            {
                Assembly editorAssembly = typeof(UnityEditor.Editor).Assembly;
                Type sizesType = editorAssembly.GetType("UnityEditor.GameViewSizes");
                Type sizeType = editorAssembly.GetType("UnityEditor.GameViewSize");
                Type sizeKindType = editorAssembly.GetType("UnityEditor.GameViewSizeType");
                Type gameViewType = editorAssembly.GetType("UnityEditor.GameView");
                if (sizesType == null || sizeType == null ||
                    sizeKindType == null || gameViewType == null)
                {
                    throw new InvalidOperationException(
                        "Unity GameView reflection types were not found.");
                }

                Type singletonType = typeof(ScriptableSingleton<>).MakeGenericType(sizesType);
                object sizes = singletonType
                    .GetProperty("instance", BindingFlags.Public | BindingFlags.Static)
                    ?.GetValue(null);
                MethodInfo getGroup = sizesType.GetMethod(
                    "GetGroup",
                    BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance);
                Type groupKindType = getGroup?.GetParameters()[0].ParameterType;
                object groupKind = Enum.Parse(groupKindType, "Android");
                object group = getGroup.Invoke(sizes, new[] { groupKind });
                Type groupType = group.GetType();

                int builtinCount = (int)groupType.GetMethod(
                    "GetBuiltinCount",
                    BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance)
                    .Invoke(group, null);
                int customCount = (int)groupType.GetMethod(
                    "GetCustomCount",
                    BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance)
                    .Invoke(group, null);
                MethodInfo getSize = groupType.GetMethod(
                    "GetGameViewSize",
                    BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance);
                int selectedIndex = -1;
                for (int index = 0; index < builtinCount + customCount; index++)
                {
                    object size = getSize.Invoke(group, new object[] { index });
                    int width = ReadIntMember(size, "width");
                    int height = ReadIntMember(size, "height");
                    if (width == 1080 && height == 1920)
                    {
                        selectedIndex = index;
                        break;
                    }
                }

                if (selectedIndex < 0)
                {
                    ConstructorInfo constructor = sizeType
                        .GetConstructors(BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance)
                        .First(item => item.GetParameters().Length == 4);
                    object fixedResolution = Enum.Parse(sizeKindType, "FixedResolution");
                    object newSize = constructor.Invoke(new object[]
                    {
                        fixedResolution,
                        1080,
                        1920,
                        "Swordstellation Audit Portrait"
                    });
                    groupType.GetMethod(
                        "AddCustomSize",
                        BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance)
                        .Invoke(group, new[] { newSize });
                    selectedIndex = builtinCount + customCount;
                }

                EditorWindow gameView = EditorWindow.GetWindow(gameViewType);
                PropertyInfo selectedSize = gameViewType.GetProperty(
                    "selectedSizeIndex",
                    BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance);
                selectedSize.SetValue(gameView, selectedIndex);
                gameView.Repaint();
                Debug.Log($"Visual audit GameView set to 1080x1920 at index {selectedIndex}.");
            }
            catch (Exception exception)
            {
                Debug.LogWarning($"Could not configure portrait GameView: {exception}");
            }
        }

        private static int ReadIntMember(object target, string name)
        {
            Type type = target.GetType();
            PropertyInfo property = type.GetProperty(
                name,
                BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance);
            if (property != null)
            {
                return Convert.ToInt32(property.GetValue(target));
            }

            FieldInfo field = type.GetField(
                name,
                BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance);
            return field == null
                ? -1
                : Convert.ToInt32(field.GetValue(target));
        }
    }
}
