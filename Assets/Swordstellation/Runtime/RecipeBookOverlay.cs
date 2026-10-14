using System;
using System.Linq;
using Swordstellation.Core;
using UnityEngine;
using UnityEngine.UI;

namespace Swordstellation.Runtime
{
    public sealed class RecipeBookOverlay : MonoBehaviour
    {
        private BattleController _battle;
        private GameBootstrap _bootstrap;
        private RectTransform _root;
        private RectTransform _panel;
        private RectTransform _content;
        private Text _guideText;

        public bool IsOpen =>
            _root != null &&
            _root.gameObject.activeSelf;

        public void Initialize(
            BattleController battle,
            GameBootstrap bootstrap,
            RectTransform safeRoot)
        {
            _battle = battle ??
                throw new ArgumentNullException(nameof(battle));
            _bootstrap = bootstrap ??
                throw new ArgumentNullException(nameof(bootstrap));

            _root = UiFactory.CreatePanel(
                safeRoot,
                "RecipeBookOverlay",
                new Color(0.005f, 0.008f, 0.035f, 0.86f),
                Vector2.zero,
                Vector2.one,
                Vector2.zero,
                Vector2.zero);
            _root.SetAsLastSibling();

            _panel = UiFactory.CreatePanel(
                _root,
                "ConstellationRecipeBookPanel",
                new Color(0.035f, 0.045f, 0.12f, 0.98f),
                new Vector2(0.035f, 0.035f),
                new Vector2(0.965f, 0.965f),
                Vector2.zero,
                Vector2.zero);
            UiFactory.ApplyOrnatePanel(
                _panel.GetComponent<Image>());

            UiFactory.CreateText(
                _panel,
                "RecipeBookTitle",
                Localized("별자리 조합서", "CONSTELLATION RECIPES"),
                35,
                GameTheme.Gold,
                TextAnchor.MiddleCenter,
                new Vector2(0.15f, 0.855f),
                new Vector2(0.85f, 0.905f),
                Vector2.zero,
                Vector2.zero,
                FontStyle.Bold);

            UiFactory.CreateButton(
                _panel,
                "RecipeBookCloseButton",
                Localized("닫기", "Close"),
                GameTheme.PanelLight,
                GameTheme.Text,
                28,
                new Vector2(0.32f, 0.045f),
                new Vector2(0.68f, 0.115f),
                Vector2.zero,
                Vector2.zero,
                Close);
            _guideText = UiFactory.CreateText(
                _panel,
                "RecipeBookGuide",
                Localized(
                    "빛나는 조합은 지금 바로 제작할 수 있습니다.",
                    "Glowing recipes can be forged now."),
                26,
                GameTheme.Text,
                TextAnchor.MiddleCenter,
                new Vector2(0.10f, 0.80f),
                new Vector2(0.90f, 0.85f),
                Vector2.zero,
                Vector2.zero);

            CreateScrollView();
            _root.gameObject.SetActive(false);
        }

        public void Open()
        {
            if (_root == null)
            {
                return;
            }

            _root.gameObject.SetActive(true);
            _root.SetAsLastSibling();
            Rebuild();
        }

        public void Close()
        {
            if (_root != null)
            {
                _root.gameObject.SetActive(false);
            }
        }

        public void RefreshIfOpen()
        {
            if (IsOpen)
            {
                Rebuild();
            }
        }

        private void CreateScrollView()
        {
            RectTransform viewport = UiFactory.CreatePanel(
                _panel,
                "RecipeViewport",
                new Color(0.008f, 0.012f, 0.055f, 0.94f),
                new Vector2(0.06f, 0.13f),
                new Vector2(0.94f, 0.79f),
                Vector2.zero,
                Vector2.zero);
            Image viewportImage =
                viewport.GetComponent<Image>();
            viewportImage.raycastTarget = true;
            viewport.gameObject.AddComponent<Mask>()
                .showMaskGraphic = false;

            GameObject contentObject = new GameObject(
                "RecipeContent",
                typeof(RectTransform),
                typeof(VerticalLayoutGroup),
                typeof(ContentSizeFitter));
            contentObject.transform.SetParent(viewport, false);
            _content =
                contentObject.GetComponent<RectTransform>();
            _content.anchorMin = new Vector2(0f, 1f);
            _content.anchorMax = new Vector2(1f, 1f);
            // Same width as the viewport (a new RectTransform is 100 wider).
            _content.sizeDelta = Vector2.zero;
            _content.pivot = new Vector2(0.5f, 1f);
            _content.offsetMin = Vector2.zero;
            _content.offsetMax = Vector2.zero;

            VerticalLayoutGroup layout =
                contentObject.GetComponent<VerticalLayoutGroup>();
            layout.padding = new RectOffset(12, 12, 24, 24);
            layout.spacing = 10f;
            layout.childControlHeight = true;
            layout.childControlWidth = true;
            layout.childForceExpandHeight = false;
            layout.childForceExpandWidth = true;
            ContentSizeFitter fitter =
                contentObject.GetComponent<ContentSizeFitter>();
            fitter.verticalFit =
                ContentSizeFitter.FitMode.PreferredSize;

            ScrollRect scroll =
                viewport.gameObject.AddComponent<ScrollRect>();
            scroll.viewport = viewport;
            scroll.content = _content;
            scroll.horizontal = false;
            scroll.vertical = true;
            scroll.movementType =
                ScrollRect.MovementType.Clamped;
            scroll.scrollSensitivity = 48f;
        }

        private void Rebuild()
        {
            for (int index =
                     _content.childCount - 1;
                 index >= 0;
                 index--)
            {
                Destroy(
                    _content.GetChild(index).gameObject);
            }

            int readyCount = _bootstrap.Catalog.Recipes.Count(
                recipe => _battle.CanCraftRecipe(recipe.Id));
            if (_guideText != null)
            {
                _guideText.text = readyCount > 0
                    ? Localized(
                        $"지금 제작 가능한 조합 {readyCount}개 · 빛나는 항목을 누르세요.",
                        $"{readyCount} recipes ready · tap a glowing forge.")
                    : Localized(
                        "보드의 재료가 채워질수록 조합이 위로 올라옵니다.",
                        "Recipes rise as their board materials are collected.");
                _guideText.color = readyCount > 0
                    ? GameTheme.Gold
                    : GameTheme.MutedText;
            }

            foreach (RecipeDefinition recipe in
                     _bootstrap.Catalog.Recipes
                         .OrderByDescending(item =>
                             _battle.CanCraftRecipe(item.Id))
                         .ThenBy(item =>
                             item.Visibility ==
                             RecipeVisibility.Hidden
                                 ? 1
                                 : 0)
                         .ThenBy(item =>
                             MissingIngredientCount(item))
                         .ThenByDescending(item =>
                             _bootstrap.Catalog
                                 .GetSword(
                                     item.ResultSwordId)
                                 .Rarity)
                         .ThenBy(item => item.Id))
            {
                CreateRecipeRow(recipe);
            }
        }

        private int MissingIngredientCount(
            RecipeDefinition recipe)
        {
            var available = _battle.Inventory.Slots
                .Where(sword => sword != null)
                .GroupBy(sword => sword.DefinitionId)
                .ToDictionary(group => group.Key, group => group.Count());
            int missing = 0;
            foreach (string ingredientId in recipe.IngredientSwordIds)
            {
                if (available.TryGetValue(
                        ingredientId,
                        out int count) &&
                    count > 0)
                {
                    available[ingredientId] = count - 1;
                }
                else
                {
                    missing++;
                }
            }
            return missing;
        }

        private void CreateRecipeRow(
            RecipeDefinition recipe)
        {
            bool discovered =
                _bootstrap.SaveData
                    .DiscoveredRecipeIds
                    .Contains(recipe.Id);
            bool hidden =
                recipe.Visibility ==
                RecipeVisibility.Hidden;
            bool ready =
                _battle.CanCraftRecipe(recipe.Id);
            SwordDefinition result =
                _bootstrap.Catalog.GetSword(
                    recipe.ResultSwordId);

            GameObject rowObject = new GameObject(
                recipe.Id,
                typeof(RectTransform),
                typeof(Image),
                typeof(LayoutElement));
            rowObject.transform.SetParent(
                _content,
                false);
            Image rowImage =
                rowObject.GetComponent<Image>();
            rowImage.sprite = null;
            rowImage.color = ready
                ? new Color(0.12f, 0.065f, 0.27f, 0.96f)
                : new Color(0.025f, 0.045f, 0.12f, 0.94f);
            UiFactory.AddOutline(
                rowImage,
                ready
                    ? new Color(0.72f, 0.42f, 1f, 0.82f)
                    : new Color(0.25f, 0.56f, 0.95f, 0.42f),
                new Vector2(2f, -2f));
            LayoutElement layout =
                rowObject.GetComponent<LayoutElement>();
            layout.preferredHeight =
                hidden && !discovered ? 150f : 172f;

            RectTransform readyAccent = UiFactory.CreatePanel(
                rowObject.transform,
                "ReadyAccent",
                ready ? GameTheme.Gold : GameTheme.Cyan,
                new Vector2(0.008f, 0.18f),
                new Vector2(0.014f, 0.82f),
                Vector2.zero,
                Vector2.zero);
            readyAccent.GetComponent<Image>().raycastTarget = false;

            GameObject iconObject = new GameObject(
                "ResultSilhouette",
                typeof(RectTransform),
                typeof(Image));
            iconObject.transform.SetParent(
                rowObject.transform,
                false);
            RectTransform iconRect =
                iconObject.GetComponent<RectTransform>();
            iconRect.anchorMin =
                new Vector2(0.025f, 0.16f);
            iconRect.anchorMax =
                new Vector2(0.155f, 0.84f);
            iconRect.offsetMin = Vector2.zero;
            iconRect.offsetMax = Vector2.zero;
            Image icon =
                iconObject.GetComponent<Image>();
            icon.sprite = Resources.Load<Sprite>(
                $"Art/Swords/{result.Id}");
            icon.preserveAspect = true;
            icon.raycastTarget = false;
            icon.color = discovered
                ? Color.white
                : new Color(0.20f, 0.18f, 0.36f, 1f);

            string title = discovered
                ? _bootstrap.Localization
                    .SwordName(result)
                : hidden
                    ? Localized(
                        "숨겨진 전설",
                        "HIDDEN LEGEND")
                    : "???";
            UiFactory.CreateText(
                rowObject.transform,
                "ResultName",
                ready && !hidden
                    ? $"✦ {title}"
                    : title,
                29,
                ready
                    ? GameTheme.Gold
                    : discovered
                        ? RarityColor(result.Rarity)
                        : GameTheme.MutedText,
                TextAnchor.MiddleLeft,
                new Vector2(0.17f, 0.54f),
                new Vector2(0.72f, 0.80f),
                Vector2.zero,
                Vector2.zero,
                FontStyle.Bold);

            if (hidden && !discovered)
            {
                UiFactory.CreateText(
                    rowObject.transform,
                    "RecipeRiddle",
                    _bootstrap.Localization.RecipeHint(recipe),
                    22,
                    GameTheme.MutedText,
                    TextAnchor.MiddleLeft,
                    new Vector2(0.19f, 0.12f),
                    new Vector2(0.94f, 0.57f),
                    Vector2.zero,
                    Vector2.zero);
            }
            else
            {
                CreateMaterialStrip(
                    rowObject.transform,
                    recipe);
            }

            if (!hidden || discovered)
            {
                Button craft = UiFactory.CreateButton(
                    rowObject.transform,
                    "CraftButton",
                    ready
                        ? Localized("제작", "CRAFT")
                        : Localized(
                            $"{MissingIngredientCount(recipe)} 부족",
                            $"NEED {MissingIngredientCount(recipe)}"),
                    Color.white,
                    Color.white,
                    25,
                    new Vector2(0.75f, 0.22f),
                    new Vector2(0.955f, 0.78f),
                    Vector2.zero,
                    Vector2.zero,
                    () =>
                    {
                        if (_battle.TryCraftRecipe(
                                recipe.Id))
                        {
                            Close();
                        }
                    });
                craft.interactable = ready;
                Image craftImage =
                    craft.GetComponent<Image>();
                craftImage.sprite = null;
                craftImage.color = ready
                    ? new Color(0.35f, 0.16f, 0.62f, 0.96f)
                    : new Color(0.055f, 0.09f, 0.20f, 0.96f);
                UiFactory.AddOutline(
                    craftImage,
                    ready
                        ? new Color(0.78f, 0.48f, 1f, 0.92f)
                        : new Color(0.30f, 0.56f, 0.94f, 0.52f),
                    new Vector2(2f, -2f));
                ColorBlock colors = craft.colors;
                colors.disabledColor =
                    new Color(0.55f, 0.60f, 0.76f, 0.72f);
                craft.colors = colors;
            }
        }

        private void CreateMaterialStrip(
            Transform parent,
            RecipeDefinition recipe)
        {
            var available = _battle.Inventory.Slots
                .Where(sword => sword != null)
                .GroupBy(sword => sword.DefinitionId)
                .ToDictionary(group => group.Key, group => group.Count());
            const float startX = 0.19f;
            const float itemWidth = 0.16f;

            for (int index = 0;
                 index < recipe.IngredientSwordIds.Length;
                 index++)
            {
                string ingredientId =
                    recipe.IngredientSwordIds[index];
                bool owned = available.TryGetValue(
                    ingredientId,
                    out int count) && count > 0;
                if (owned)
                {
                    available[ingredientId] = count - 1;
                }

                float minX = startX + index * itemWidth;
                GameObject materialObject = new GameObject(
                    $"Material{index}",
                    typeof(RectTransform),
                    typeof(Image));
                materialObject.transform.SetParent(parent, false);
                RectTransform materialRect =
                    materialObject.GetComponent<RectTransform>();
                materialRect.anchorMin =
                    new Vector2(minX, 0.06f);
                materialRect.anchorMax =
                    new Vector2(minX + 0.095f, 0.58f);
                materialRect.offsetMin = Vector2.zero;
                materialRect.offsetMax = Vector2.zero;
                Image material =
                    materialObject.GetComponent<Image>();
                material.sprite = Resources.Load<Sprite>(
                    $"Art/Swords/{ingredientId}");
                material.preserveAspect = true;
                material.raycastTarget = false;
                material.color = owned
                    ? Color.white
                    : new Color(0.40f, 0.45f, 0.72f, 0.85f);

                Text state = UiFactory.CreateText(
                    parent,
                    $"MaterialState{index}",
                    owned ? "✓" : "·",
                    26,
                    owned ? GameTheme.Mint : GameTheme.MutedText,
                    TextAnchor.MiddleCenter,
                    new Vector2(minX + 0.093f, 0.12f),
                    new Vector2(minX + 0.135f, 0.52f),
                    Vector2.zero,
                    Vector2.zero,
                    FontStyle.Bold);
                state.raycastTarget = false;

                if (index + 1 < recipe.IngredientSwordIds.Length)
                {
                    Text plus = UiFactory.CreateText(
                        parent,
                        $"Plus{index}",
                        "+",
                        24,
                        GameTheme.MutedText,
                        TextAnchor.MiddleCenter,
                        new Vector2(minX + 0.134f, 0.12f),
                        new Vector2(minX + 0.16f, 0.52f),
                        Vector2.zero,
                        Vector2.zero,
                        FontStyle.Bold);
                    plus.raycastTarget = false;
                }
            }
        }

        private string Localized(
            string korean,
            string english)
        {
            return _bootstrap.Localization.IsKorean
                ? korean
                : english;
        }

        private static Color RarityColor(
            SwordRarity rarity)
        {
            return rarity switch
            {
                SwordRarity.Rare => GameTheme.Cyan,
                SwordRarity.Heroic => GameTheme.Violet,
                SwordRarity.Legendary => GameTheme.Gold,
                SwordRarity.Mythic =>
                    new Color(0.65f, 0.92f, 1f, 1f),
                _ => GameTheme.MutedText
            };
        }
    }
}
