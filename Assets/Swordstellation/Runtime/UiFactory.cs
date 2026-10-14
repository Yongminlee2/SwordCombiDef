using System;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

namespace Swordstellation.Runtime
{
    public static class UiFactory
    {
        private static Font _font;
        private static Font _titleFont;
        private static Sprite _buttonFrameBlue;
        private static Sprite _buttonFrameViolet;
        private static Sprite _panelFrameBlueGold;

        public static Font DefaultFont
        {
            get
            {
                if (_font == null)
                {
                    _font = Resources.Load<Font>("Fonts/Pretendard-Bold");
                    if (_font == null)
                    {
                        _font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
                    }
                }

                return _font;
            }
        }

        // Hahmlet: titles, sword names and engraving names only.
        public static Font TitleFont
        {
            get
            {
                if (_titleFont == null)
                {
                    _titleFont = Resources.Load<Font>("Fonts/Hahmlet-Bold") ?? DefaultFont;
                }

                return _titleFont;
            }
        }

        private static Sprite _circle;
        private static Sprite _ring;

        public static Sprite RingSprite =>
            _ring != null ? _ring : (_ring = CreateDisc(0.80f));

        // Soft-edged white disc, generated once (no circle sprite ships with the game).
        public static Sprite CircleSprite
        {
            get
            {
                if (_circle == null)
                {
                    _circle = CreateDisc(0f);
                }

                return _circle;
            }
        }

        // innerRatio 0 = filled disc, otherwise a ring with that hole size.
        private static Sprite CreateDisc(float innerRatio)
        {
            const int size = 128;
            Texture2D texture = new Texture2D(
                size,
                size,
                TextureFormat.RGBA32,
                false)
            {
                wrapMode = TextureWrapMode.Clamp,
                filterMode = FilterMode.Bilinear
            };
            float radius = size / 2f;
            Color32[] pixels = new Color32[size * size];
            for (int y = 0; y < size; y++)
            {
                for (int x = 0; x < size; x++)
                {
                    float distance = Vector2.Distance(
                        new Vector2(x + 0.5f, y + 0.5f),
                        new Vector2(radius, radius));
                    float outer = Mathf.Clamp01(radius - distance);
                    float inner = innerRatio <= 0f
                        ? 1f
                        : Mathf.Clamp01(distance - radius * innerRatio);
                    byte alpha = (byte)(outer * inner * 255f);
                    pixels[y * size + x] = new Color32(255, 255, 255, alpha);
                }
            }
            texture.SetPixels32(pixels);
            texture.Apply();
            return Sprite.Create(
                texture,
                new Rect(0, 0, size, size),
                new Vector2(0.5f, 0.5f),
                100f);
        }

        public static Text UseTitleFont(Text text)
        {
            if (text != null)
            {
                text.font = TitleFont;
            }
            return text;
        }

        public static Canvas CreateCanvas(string name, int sortingOrder = 0)
        {
            GameObject canvasObject = new GameObject(name, typeof(RectTransform), typeof(Canvas), typeof(CanvasScaler), typeof(GraphicRaycaster));
            Canvas canvas = canvasObject.GetComponent<Canvas>();
            canvas.renderMode = RenderMode.ScreenSpaceOverlay;
            canvas.sortingOrder = sortingOrder;

            CanvasScaler scaler = canvasObject.GetComponent<CanvasScaler>();
            scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
            scaler.referenceResolution = new Vector2(1080, 1920);
            scaler.screenMatchMode = CanvasScaler.ScreenMatchMode.Expand;

            if (UnityEngine.Object.FindFirstObjectByType<EventSystem>() == null)
            {
                GameObject eventSystem = new GameObject("EventSystem", typeof(EventSystem), typeof(StandaloneInputModule));
                UnityEngine.Object.DontDestroyOnLoad(eventSystem);
            }

            return canvas;
        }

        public static RectTransform CreatePanel(
            Transform parent,
            string name,
            Color color,
            Vector2 anchorMin,
            Vector2 anchorMax,
            Vector2 offsetMin,
            Vector2 offsetMax)
        {
            GameObject panelObject = new GameObject(name, typeof(RectTransform), typeof(Image));
            panelObject.transform.SetParent(parent, false);
            RectTransform rect = panelObject.GetComponent<RectTransform>();
            rect.anchorMin = anchorMin;
            rect.anchorMax = anchorMax;
            rect.offsetMin = offsetMin;
            rect.offsetMax = offsetMax;
            Image image = panelObject.GetComponent<Image>();
            image.color = color;
            if (ShouldUseOrnatePanel(name))
            {
                ApplyOrnatePanel(image);
            }
            return rect;
        }

        public static Text CreateText(
            Transform parent,
            string name,
            string value,
            int fontSize,
            Color color,
            TextAnchor alignment,
            Vector2 anchorMin,
            Vector2 anchorMax,
            Vector2 offsetMin,
            Vector2 offsetMax,
            FontStyle style = FontStyle.Normal)
        {
            GameObject textObject = new GameObject(name, typeof(RectTransform), typeof(Text));
            textObject.transform.SetParent(parent, false);
            RectTransform rect = textObject.GetComponent<RectTransform>();
            rect.anchorMin = anchorMin;
            rect.anchorMax = anchorMax;
            rect.offsetMin = offsetMin;
            rect.offsetMax = offsetMax;

            // Phones are read at arm's length: every label is drawn ~20% larger
            // (best fit still shrinks it if the box is too small), never below
            // 22, and centred unless it is a screen-edge HUD readout.
            fontSize = Mathf.Max(22, Mathf.RoundToInt(fontSize * 1.2f));
            Text text = textObject.GetComponent<Text>();
            text.font = UsesTitleFont(name) ? TitleFont : DefaultFont;
            text.text = value;
            text.fontSize = fontSize;
            text.fontStyle = FontStyle.Normal;
            text.color = color;
            text.alignment = KeepsEdgeAlignment(name)
                ? alignment
                : Centered(alignment);
            text.horizontalOverflow = HorizontalWrapMode.Wrap;
            text.verticalOverflow = VerticalWrapMode.Truncate;
            // Shrink instead of clipping or spilling when a label is longer
            // than its box (Korean/English lengths differ a lot).
            text.resizeTextForBestFit = true;
            text.resizeTextMaxSize = fontSize;
            text.resizeTextMinSize = Mathf.Max(
                10,
                Mathf.RoundToInt(fontSize * 0.6f));
            return text;
        }

        public static Button CreateButton(
            Transform parent,
            string name,
            string label,
            Color background,
            Color foreground,
            int fontSize,
            Vector2 anchorMin,
            Vector2 anchorMax,
            Vector2 offsetMin,
            Vector2 offsetMax,
            Action onClick)
        {
            GameObject buttonObject = new GameObject(name, typeof(RectTransform), typeof(Image), typeof(Button));
            buttonObject.transform.SetParent(parent, false);
            RectTransform rect = buttonObject.GetComponent<RectTransform>();
            rect.anchorMin = anchorMin;
            rect.anchorMax = anchorMax;
            rect.offsetMin = offsetMin;
            rect.offsetMax = offsetMax;

            Image image = buttonObject.GetComponent<Image>();
            bool primary = IsPrimaryAction(name, background);
            ApplyButtonFrame(image, primary);

            Button button = buttonObject.GetComponent<Button>();
            ColorBlock colors = button.colors;
            colors.normalColor = Color.white;
            colors.highlightedColor = new Color(1.08f, 1.08f, 1.08f, 1f);
            colors.pressedColor = new Color(0.78f, 0.78f, 0.78f, 1f);
            colors.disabledColor = new Color(0.45f, 0.45f, 0.45f, 0.65f);
            button.colors = colors;
            if (onClick != null)
            {
                button.onClick.AddListener(() =>
                {
                    AudioService.PlayUiClick();
                    onClick();
                });
            }

            Text text = CreateText(
                buttonObject.transform,
                "Label",
                label,
                fontSize,
                primary ? GameTheme.Gold : GameTheme.Text,
                TextAnchor.MiddleCenter,
                new Vector2(0.09f, 0.14f),
                new Vector2(0.91f, 0.86f),
                Vector2.zero,
                Vector2.zero,
                FontStyle.Bold);
            text.raycastTarget = false;
            return button;
        }

        public static void ApplyOrnatePanel(Image image)
        {
            if (image == null)
            {
                return;
            }

            Sprite frame = LoadPanelFrame();
            if (frame == null)
            {
                image.color = new Color(
                    GameTheme.Panel.r,
                    GameTheme.Panel.g,
                    GameTheme.Panel.b,
                    0.96f);
                AddOutline(
                    image,
                    new Color(
                        GameTheme.Gold.r,
                        GameTheme.Gold.g,
                        GameTheme.Gold.b,
                        0.55f),
                    new Vector2(2, -2));
                return;
            }

            image.sprite = frame;
            image.type = Image.Type.Sliced;
            image.color = Color.white;
            FitSlicedFrame(image, 160f);
        }

        public static void ApplyButtonFrame(
            Image image,
            bool primary = false)
        {
            if (image == null)
            {
                return;
            }

            Sprite frame = primary
                ? LoadButtonFrameViolet()
                : LoadButtonFrameBlue();
            if (frame == null)
            {
                image.color = primary
                    ? GameTheme.Violet
                    : GameTheme.PanelLight;
                return;
            }

            image.sprite = frame;
            image.type = Image.Type.Sliced;
            image.color = Color.white;
            FitSlicedFrame(image, 0f);
        }

        private static void FitSlicedFrame(Image image, float plainBelow)
        {
            SlicedFrameFitter fitter =
                image.GetComponent<SlicedFrameFitter>();
            if (fitter == null)
            {
                fitter = image.gameObject.AddComponent<SlicedFrameFitter>();
            }
            fitter.PlainBelow = plainBelow;
            fitter.Fit();
        }

        public static void AddOutline(Graphic graphic, Color color, Vector2 distance)
        {
            Outline outline = graphic.gameObject.AddComponent<Outline>();
            outline.effectColor = color;
            outline.effectDistance = distance;
        }

        public static void SetStretch(RectTransform rect)
        {
            rect.anchorMin = Vector2.zero;
            rect.anchorMax = Vector2.one;
            rect.offsetMin = Vector2.zero;
            rect.offsetMax = Vector2.zero;
        }

        private static bool KeepsEdgeAlignment(string name)
        {
            return name == "Wave" ||
                   name == "Mission" ||
                   name == "UltimateCharge" ||
                   name == "Gold" ||
                   name == "Rank";
        }

        private static TextAnchor Centered(TextAnchor alignment)
        {
            return alignment switch
            {
                TextAnchor.UpperLeft or TextAnchor.UpperRight => TextAnchor.UpperCenter,
                TextAnchor.LowerLeft or TextAnchor.LowerRight => TextAnchor.LowerCenter,
                TextAnchor.MiddleLeft or TextAnchor.MiddleRight => TextAnchor.MiddleCenter,
                _ => alignment
            };
        }

        private static bool UsesTitleFont(string name)
        {
            return name == "Title" ||
                   name == "Header" ||
                   name == "Name" ||
                   name == "SwordName" ||
                   (name.EndsWith("Title", StringComparison.Ordinal) &&
                    name != "ActionTitle");
        }

        private static bool ShouldUseOrnatePanel(string name)
        {
            return name == "TitlePlate" ||
                   name == "HeaderPlate" ||
                   name == "Overview" ||
                   name == "Lifetime" ||
                   name.EndsWith("Card", StringComparison.Ordinal) ||
                   name.EndsWith("Viewport", StringComparison.Ordinal);
        }

        private static bool IsPrimaryAction(
            string name,
            Color background)
        {
            return name.IndexOf(
                       "Start",
                       StringComparison.OrdinalIgnoreCase) >= 0 ||
                   name.IndexOf(
                       "Play",
                       StringComparison.OrdinalIgnoreCase) >= 0 ||
                   name.IndexOf(
                       "Daily",
                       StringComparison.OrdinalIgnoreCase) >= 0 ||
                   name.IndexOf(
                       "Shop",
                       StringComparison.OrdinalIgnoreCase) >= 0 ||
                   name.IndexOf(
                       "Upgrade",
                       StringComparison.OrdinalIgnoreCase) >= 0 ||
                   name.IndexOf(
                       "Craft",
                       StringComparison.OrdinalIgnoreCase) >= 0 ||
                   name.IndexOf(
                       "Summon",
                       StringComparison.OrdinalIgnoreCase) >= 0;
        }

        private static Sprite LoadButtonFrameBlue()
        {
            if (_buttonFrameBlue == null)
            {
                _buttonFrameBlue = Resources.Load<Sprite>(
                    "Art/UI/Battle/ButtonFrameBlue");
            }

            return _buttonFrameBlue;
        }

        private static Sprite LoadButtonFrameViolet()
        {
            if (_buttonFrameViolet == null)
            {
                _buttonFrameViolet = Resources.Load<Sprite>(
                    "Art/UI/Battle/ButtonFrameViolet");
            }

            return _buttonFrameViolet;
        }

        private static Sprite LoadPanelFrame()
        {
            if (_panelFrameBlueGold == null)
            {
                _panelFrameBlueGold = Resources.Load<Sprite>(
                    "Art/UI/Battle/PanelFrameBlueGold");
            }

            return _panelFrameBlueGold;
        }
    }
}
