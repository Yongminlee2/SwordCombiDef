using System.Collections;
using System;
using Swordstellation.Core;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

namespace Swordstellation.Runtime
{
    [RequireComponent(typeof(RectTransform))]
    [RequireComponent(typeof(Image))]
    [RequireComponent(typeof(CanvasGroup))]
    public sealed class PrototypeSwordSlotView :
        MonoBehaviour,
        IBeginDragHandler,
        IDragHandler,
        IEndDragHandler,
        IDropHandler
    {
        private int _slotIndex;
        private Canvas _canvas;
        private Func<int, int, bool> _mergeAction;
        private Action<int, PointerEventData, bool> _dragAction;
        private CanvasGroup _canvasGroup;
        private Image _frame;
        private Image _swordImage;
        private Outline _swordGlow;
        private Text _rankText;
        private Text _nameText;
        private Text _emptyText;
        private SwordInstance _instance;
        private GameObject _dragGhost;
        private Coroutine _attackRoutine;
        private Color _baseGlowColor;
        private bool _mergeReady;
        private float _pulseOffset;

        public int SlotIndex => _slotIndex;
        public SwordInstance Instance => _instance;
        public bool IsMergeReady => _mergeReady;

        public void Initialize(
            int slotIndex,
            Canvas canvas,
            Func<int, int, bool> mergeAction,
            Action<int, PointerEventData, bool> dragAction = null)
        {
            _slotIndex = slotIndex;
            _pulseOffset = slotIndex * 0.63f;
            _canvas = canvas;
            _mergeAction = mergeAction;
            _dragAction = dragAction;
            _canvasGroup = GetComponent<CanvasGroup>();
            _frame = GetComponent<Image>();
            _frame.color = Color.clear;

            _swordImage = CreateImage(
                "SwordArt",
                new Vector2(0.03f, 0.02f),
                new Vector2(0.97f, 0.98f));
            _swordImage.preserveAspect = true;
            _swordImage.raycastTarget = false;
            _swordGlow =
                _swordImage.gameObject
                    .AddComponent<Outline>();
            _swordGlow.effectDistance =
                new Vector2(5f, -5f);
            _swordGlow.enabled = false;

            _rankText = UiFactory.CreateText(
                transform,
                "Rank",
                string.Empty,
                25,
                GameTheme.Gold,
                TextAnchor.UpperRight,
                new Vector2(0.60f, 0.76f),
                new Vector2(0.98f, 0.98f),
                Vector2.zero,
                Vector2.zero,
                FontStyle.Bold);
            _rankText.raycastTarget = false;

            _nameText = UiFactory.CreateText(
                transform,
                "SwordName",
                string.Empty,
                16,
                GameTheme.Text,
                TextAnchor.MiddleCenter,
                new Vector2(0.04f, 0.025f),
                new Vector2(0.96f, 0.19f),
                Vector2.zero,
                Vector2.zero,
                FontStyle.Bold);
            _nameText.raycastTarget = false;
            _nameText.gameObject.SetActive(false);

            _emptyText = UiFactory.CreateText(
                transform,
                "Empty",
                "✦",
                25,
                new Color(0.55f, 0.66f, 1f, 0.28f),
                TextAnchor.MiddleCenter,
                Vector2.zero,
                Vector2.one,
                Vector2.zero,
                Vector2.zero,
                FontStyle.Bold);
            _emptyText.raycastTarget = false;
        }

        private void Update()
        {
            if (!_mergeReady ||
                _instance == null ||
                _swordGlow == null)
            {
                return;
            }

            float pulse =
                0.5f +
                0.5f * Mathf.Sin(
                    Time.unscaledTime * 5.2f +
                    _pulseOffset);
            _swordGlow.effectColor = Color.Lerp(
                GameTheme.Gold,
                Color.white,
                pulse * 0.42f);
            _swordGlow.effectDistance = new Vector2(
                Mathf.Lerp(7f, 12f, pulse),
                Mathf.Lerp(-7f, -12f, pulse));
            if (_attackRoutine == null)
            {
                float scale = Mathf.Lerp(1.02f, 1.075f, pulse);
                _swordImage.rectTransform.localScale =
                    Vector3.one * scale;
            }
        }

        public void Bind(
            SwordInstance instance,
            SwordDefinition definition,
            string displayName)
        {
            _instance = instance;
            bool occupied = instance != null &&
                            definition != null;
            _swordImage.enabled = occupied;
            _rankText.text = occupied
                ? $"★{instance.Stars}"
                : string.Empty;
            _nameText.text = occupied
                ? displayName
                : string.Empty;
            _emptyText.gameObject.SetActive(!occupied);

            if (!occupied)
            {
                _swordImage.sprite = null;
                _frame.color = Color.clear;
                _swordGlow.enabled = false;
                _mergeReady = false;
                return;
            }

            _swordImage.sprite = Resources.Load<Sprite>(
                $"Art/Swords/{definition.Id}");
            AppearanceResult appearance =
                AppearanceRules.Evaluate(
                    instance.AppearanceSeed,
                    definition.Rarity);
            _swordImage.color =
                appearance.IsMutation
                    ? Color.Lerp(
                        Color.white,
                        GameTheme.FromHex(
                            appearance.AccentRgb),
                        0.18f)
                    : Color.white;
            _frame.color = Color.clear;
            _baseGlowColor =
                appearance.IsMutation
                    ? GameTheme.FromHex(
                        appearance.AccentRgb)
                    : RarityGlow(definition.Rarity);
            _swordGlow.effectColor = _mergeReady
                ? GameTheme.Gold
                : _baseGlowColor;
            _swordGlow.effectDistance =
                appearance.IsMutation
                    ? new Vector2(9f, -9f)
                    : new Vector2(5f, -5f);
            _swordGlow.enabled = true;
        }

        public void SetMergeReady(bool ready)
        {
            _mergeReady = ready && _instance != null;
            if (_swordGlow == null)
            {
                return;
            }

            if (!_mergeReady)
            {
                _swordGlow.effectColor = _baseGlowColor;
                _swordGlow.effectDistance =
                    new Vector2(5f, -5f);
                if (_attackRoutine == null && _swordImage != null)
                {
                    _swordImage.rectTransform.localScale =
                        Vector3.one;
                }
            }
        }

        public void OnBeginDrag(
            PointerEventData eventData)
        {
            if (_instance == null ||
                _swordImage.sprite == null ||
                _canvas == null)
            {
                return;
            }

            _canvasGroup.blocksRaycasts = false;
            _dragGhost = new GameObject(
                "SwordDragGhost",
                typeof(RectTransform),
                typeof(CanvasGroup),
                typeof(Image));
            _dragGhost.transform.SetParent(
                _canvas.transform,
                false);
            RectTransform ghostRect =
                _dragGhost.GetComponent<RectTransform>();
            ghostRect.sizeDelta = new Vector2(120f, 190f);
            Image ghostImage =
                _dragGhost.GetComponent<Image>();
            ghostImage.sprite = _swordImage.sprite;
            ghostImage.preserveAspect = true;
            ghostImage.raycastTarget = false;
            CanvasGroup ghostGroup =
                _dragGhost.GetComponent<CanvasGroup>();
            ghostGroup.blocksRaycasts = false;
            ghostGroup.alpha = 0.82f;
            MoveGhost(eventData);
            _dragAction?.Invoke(_slotIndex, eventData, false);
        }

        public void OnDrag(PointerEventData eventData)
        {
            MoveGhost(eventData);
        }

        public void OnEndDrag(
            PointerEventData eventData)
        {
            _canvasGroup.blocksRaycasts = true;
            if (_dragGhost != null)
            {
                Destroy(_dragGhost);
                _dragGhost = null;
                _dragAction?.Invoke(_slotIndex, eventData, true);
            }
        }

        public void OnDrop(PointerEventData eventData)
        {
            PrototypeSwordSlotView source =
                eventData.pointerDrag == null
                    ? null
                    : eventData.pointerDrag
                        .GetComponent<PrototypeSwordSlotView>();
            if (source == null ||
                source == this ||
                source.Instance == null)
            {
                return;
            }

            _mergeAction?.Invoke(
                source.SlotIndex,
                _slotIndex);
        }

        public void PlayAttackPulse(AttackStyle style)
        {
            if (_swordImage == null ||
                !_swordImage.enabled)
            {
                return;
            }

            if (_attackRoutine != null)
            {
                StopCoroutine(_attackRoutine);
            }
            _attackRoutine =
                StartCoroutine(
                    AttackPulseRoutine(style));
        }

        private Image CreateImage(
            string name,
            Vector2 anchorMin,
            Vector2 anchorMax)
        {
            GameObject imageObject = new GameObject(
                name,
                typeof(RectTransform),
                typeof(Image));
            imageObject.transform.SetParent(
                transform,
                false);
            RectTransform rect =
                imageObject.GetComponent<RectTransform>();
            rect.anchorMin = anchorMin;
            rect.anchorMax = anchorMax;
            rect.offsetMin = Vector2.zero;
            rect.offsetMax = Vector2.zero;
            return imageObject.GetComponent<Image>();
        }

        private void MoveGhost(
            PointerEventData eventData)
        {
            if (_dragGhost == null)
            {
                return;
            }

            RectTransform canvasRect =
                _canvas.transform as RectTransform;
            if (canvasRect != null &&
                RectTransformUtility.ScreenPointToLocalPointInRectangle(
                    canvasRect,
                    eventData.position,
                    eventData.pressEventCamera,
                    out Vector2 localPoint))
            {
                _dragGhost
                    .GetComponent<RectTransform>()
                    .anchoredPosition = localPoint;
            }
        }

        private static Color RarityGlow(
            SwordRarity rarity)
        {
            return rarity switch
            {
                SwordRarity.Rare =>
                    new Color(
                        0.20f,
                        0.58f,
                        1f,
                        0.82f),
                SwordRarity.Heroic =>
                    new Color(
                        0.66f,
                        0.30f,
                        1f,
                        0.88f),
                SwordRarity.Legendary =>
                    new Color(
                        1f,
                        0.70f,
                        0.18f,
                        0.92f),
                SwordRarity.Mythic =>
                    new Color(
                        0.45f,
                        0.95f,
                        1f,
                        0.95f),
                _ => new Color(
                    0.52f,
                    0.62f,
                    0.80f,
                    0.72f)
            };
        }

        private IEnumerator AttackPulseRoutine(
            AttackStyle style)
        {
            RectTransform swordRect =
                _swordImage.rectTransform;
            Vector3 originalScale = Vector3.one;
            Quaternion originalRotation =
                Quaternion.identity;
            float duration = style switch
            {
                AttackStyle.Dash => 0.16f,
                AttackStyle.Chain => 0.20f,
                AttackStyle.Area => 0.24f,
                _ => 0.18f
            };
            float rotation = style switch
            {
                AttackStyle.Dash => -14f,
                AttackStyle.Chain => 10f,
                AttackStyle.Area => 22f,
                _ => -7f
            };

            float elapsed = 0f;
            while (elapsed < duration)
            {
                elapsed += Time.deltaTime;
                float progress =
                    Mathf.Clamp01(elapsed / duration);
                float pulse =
                    Mathf.Sin(progress * Mathf.PI);
                swordRect.localScale =
                    originalScale *
                    (1f + pulse * 0.16f);
                swordRect.localRotation =
                    Quaternion.Euler(
                        0f,
                        0f,
                        rotation * pulse);
                yield return null;
            }

            swordRect.localScale = originalScale;
            swordRect.localRotation =
                originalRotation;
            _attackRoutine = null;
        }
    }
}
