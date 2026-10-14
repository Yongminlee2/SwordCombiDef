using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

namespace Swordstellation.Runtime
{
    public sealed class ConstellationForgeEffect : MonoBehaviour
    {
        public bool IsPlaying { get; private set; }

        public IEnumerator Play(
            Canvas canvas,
            IReadOnlyList<RectTransform> materialSlots,
            IReadOnlyList<Sprite> materialSprites,
            Sprite resultSprite,
            string resultName,
            string discoveryLabel,
            Color resultColor,
            bool firstDiscovery,
            Action onForgeFlash)
        {
            if (canvas == null)
            {
                throw new ArgumentNullException(nameof(canvas));
            }
            if (materialSlots == null ||
                materialSprites == null ||
                materialSlots.Count != materialSprites.Count)
            {
                throw new ArgumentException(
                    "Matching material slots and sprites are required.");
            }

            IsPlaying = true;
            RectTransform canvasRect =
                canvas.transform as RectTransform;
            RectTransform root = UiFactory.CreatePanel(
                canvas.transform,
                "ConstellationForgeEffect",
                new Color(
                    0.015f,
                    0.01f,
                    0.08f,
                    firstDiscovery ? 0.88f : 0.22f),
                Vector2.zero,
                Vector2.one,
                Vector2.zero,
                Vector2.zero);
            root.SetAsLastSibling();
            // The battle keeps running underneath, so taps reach the field.
            root.GetComponent<Image>().raycastTarget = false;

            Vector2 center = new Vector2(
                0f,
                firstDiscovery ? 40f : 120f);
            List<RectTransform> ghosts =
                new List<RectTransform>();
            List<Vector2> starts =
                new List<Vector2>();
            List<Image> lines =
                new List<Image>();

            for (int index = 0;
                 index < materialSlots.Count;
                 index++)
            {
                Vector2 start = SlotCenterInCanvas(
                    canvasRect,
                    materialSlots[index]);
                starts.Add(start);
                lines.Add(CreateLine(
                    root,
                    start,
                    center,
                    new Color(
                        GameTheme.Cyan.r,
                        GameTheme.Cyan.g,
                        GameTheme.Cyan.b,
                        0f)));

                GameObject ghostObject = new GameObject(
                    $"MaterialGhost{index}",
                    typeof(RectTransform),
                    typeof(Image));
                ghostObject.transform.SetParent(root, false);
                RectTransform ghostRect =
                    ghostObject.GetComponent<RectTransform>();
                ghostRect.anchorMin =
                    new Vector2(0.5f, 0.5f);
                ghostRect.anchorMax =
                    new Vector2(0.5f, 0.5f);
                ghostRect.sizeDelta =
                    new Vector2(116f, 174f);
                ghostRect.anchoredPosition = start;
                Image ghostImage =
                    ghostObject.GetComponent<Image>();
                ghostImage.sprite = materialSprites[index];
                ghostImage.preserveAspect = true;
                ghostImage.raycastTarget = false;
                ghosts.Add(ghostRect);
            }

            Text discoveryTitle = UiFactory.CreateText(
                root,
                "DiscoveryTitle",
                discoveryLabel,
                firstDiscovery ? 38 : 28,
                firstDiscovery
                    ? GameTheme.Gold
                    : GameTheme.Cyan,
                TextAnchor.MiddleCenter,
                new Vector2(0.08f, 0.78f),
                new Vector2(0.92f, 0.90f),
                Vector2.zero,
                Vector2.zero,
                FontStyle.Bold);
            discoveryTitle.canvasRenderer.SetAlpha(0f);
            discoveryTitle.CrossFadeAlpha(
                1f,
                0.16f,
                true);

            const float gatherDuration = 0.34f;
            float elapsed = 0f;
            while (elapsed < gatherDuration)
            {
                elapsed += Time.unscaledDeltaTime;
                float progress = Mathf.Clamp01(
                    elapsed / gatherDuration);
                float eased = 1f -
                              Mathf.Pow(1f - progress, 3f);
                for (int index = 0;
                     index < ghosts.Count;
                     index++)
                {
                    ghosts[index].anchoredPosition =
                        Vector2.Lerp(
                            starts[index],
                            center,
                            eased);
                    ghosts[index].localScale =
                        Vector3.one *
                        Mathf.Lerp(1f, 0.62f, eased);
                    Color lineColor = lines[index].color;
                    lineColor.a =
                        Mathf.Sin(progress * Mathf.PI) *
                        0.92f;
                    lines[index].color = lineColor;
                }
                yield return null;
            }

            onForgeFlash?.Invoke();

            Image flash = UiFactory.CreatePanel(
                    root,
                    "ForgeFlash",
                    Color.white,
                    Vector2.zero,
                    Vector2.one,
                    Vector2.zero,
                    Vector2.zero)
                .GetComponent<Image>();
            flash.raycastTarget = false;

            GameObject resultObject = new GameObject(
                "ForgedSword",
                typeof(RectTransform),
                typeof(Image));
            resultObject.transform.SetParent(root, false);
            RectTransform resultRect =
                resultObject.GetComponent<RectTransform>();
            resultRect.anchorMin =
                new Vector2(0.5f, 0.5f);
            resultRect.anchorMax =
                new Vector2(0.5f, 0.5f);
            resultRect.anchoredPosition = center;
            resultRect.sizeDelta =
                firstDiscovery
                    ? new Vector2(290f, 500f)
                    : new Vector2(210f, 350f);
            resultRect.localScale = Vector3.zero;
            Image resultImage =
                resultObject.GetComponent<Image>();
            resultImage.sprite = resultSprite;
            resultImage.preserveAspect = true;
            resultImage.color = Color.white;
            resultImage.raycastTarget = false;
            Outline glow =
                resultObject.AddComponent<Outline>();
            glow.effectColor =
                new Color(
                    resultColor.r,
                    resultColor.g,
                    resultColor.b,
                    0.85f);
            glow.effectDistance =
                new Vector2(8f, -8f);

            RectTransform ribbon = UiFactory.CreatePanel(
                root,
                "SwordNameRibbon",
                new Color(
                    resultColor.r * 0.45f,
                    resultColor.g * 0.45f,
                    resultColor.b * 0.45f,
                    0.96f),
                new Vector2(0.10f, 0.15f),
                new Vector2(0.90f, 0.26f),
                Vector2.zero,
                Vector2.zero);
            // Same ornate frame as the other cards, tinted by grade.
            Image ribbonImage = ribbon.GetComponent<Image>();
            UiFactory.ApplyOrnatePanel(ribbonImage);
            ribbonImage.color = Color.Lerp(
                Color.white,
                resultColor,
                0.35f);
            Text nameText = UiFactory.CreateText(
                ribbon,
                "SwordName",
                resultName,
                firstDiscovery ? 44 : 38,
                Color.white,
                TextAnchor.MiddleCenter,
                Vector2.zero,
                Vector2.one,
                new Vector2(16f, 4f),
                new Vector2(-16f, -4f),
                FontStyle.Bold);
            UiFactory.AddOutline(
                nameText,
                new Color(0f, 0f, 0.08f, 0.9f),
                new Vector2(2f, -2f));

            const float revealDuration = 0.22f;
            elapsed = 0f;
            while (elapsed < revealDuration)
            {
                elapsed += Time.unscaledDeltaTime;
                float progress = Mathf.Clamp01(
                    elapsed / revealDuration);
                resultRect.localScale =
                    Vector3.one *
                    Mathf.LerpUnclamped(
                        0f,
                        1f,
                        1f -
                        Mathf.Pow(1f - progress, 4f));
                flash.color = new Color(
                    1f,
                    1f,
                    1f,
                    1f - progress);
                yield return null;
            }

            float holdDuration =
                firstDiscovery ? 1.05f : 0.3f;
            elapsed = 0f;
            while (elapsed < holdDuration)
            {
                elapsed += Time.unscaledDeltaTime;
                float pulse = 1f +
                              Mathf.Sin(
                                  elapsed * 8f) *
                              0.025f;
                resultRect.localScale =
                    Vector3.one * pulse;
                yield return null;
            }

            Destroy(root.gameObject);
            IsPlaying = false;
        }

        private static Vector2 SlotCenterInCanvas(
            RectTransform canvasRect,
            RectTransform slot)
        {
            Vector3 worldCenter =
                slot.TransformPoint(slot.rect.center);
            Vector2 screenPoint =
                RectTransformUtility.WorldToScreenPoint(
                    null,
                    worldCenter);
            RectTransformUtility
                .ScreenPointToLocalPointInRectangle(
                    canvasRect,
                    screenPoint,
                    null,
                    out Vector2 localPoint);
            return localPoint;
        }

        private static Image CreateLine(
            Transform parent,
            Vector2 start,
            Vector2 end,
            Color color)
        {
            RectTransform line = UiFactory.CreatePanel(
                parent,
                "ConstellationLine",
                color,
                new Vector2(0.5f, 0.5f),
                new Vector2(0.5f, 0.5f),
                Vector2.zero,
                Vector2.zero);
            Vector2 direction = end - start;
            line.sizeDelta =
                new Vector2(direction.magnitude, 6f);
            line.anchoredPosition =
                (start + end) * 0.5f;
            line.localEulerAngles =
                new Vector3(
                    0f,
                    0f,
                    Mathf.Atan2(
                        direction.y,
                        direction.x) *
                    Mathf.Rad2Deg);
            Image image = line.GetComponent<Image>();
            image.raycastTarget = false;
            return image;
        }
    }
}
