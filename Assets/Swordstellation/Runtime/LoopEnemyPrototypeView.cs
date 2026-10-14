using System;
using System.Collections;
using Swordstellation.Core;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

namespace Swordstellation.Runtime
{
    [RequireComponent(typeof(RectTransform))]
    public sealed class LoopEnemyPrototypeView :
        MonoBehaviour,
        IPointerClickHandler
    {
        public Action<BattleEnemyState> Tapped;
        private Image _focusMark;

        private RectTransform _rectTransform;
        private RectTransform _arena;
        private float _progress;
        private float _speed;
        private float _size;
        private float _bobPhase;
        private Vector2 _pathPosition;
        private BattleEnemyState _state;
        private Image _healthFill;
        private Image _enemyImage;
        private CanvasGroup _canvasGroup;
        private Coroutine _hitRoutine;
        private Coroutine _abilityRoutine;

        public float Progress => _progress;
        public BattleEnemyState State => _state;

        public void Initialize(
            RectTransform arena,
            float initialProgress,
            float speed,
            float size = 90f)
        {
            _arena = arena;
            _rectTransform = (RectTransform)transform;
            _rectTransform.anchorMin = new Vector2(0.5f, 0.5f);
            _rectTransform.anchorMax = new Vector2(0.5f, 0.5f);
            _rectTransform.pivot = new Vector2(0.5f, 0.5f);
            _size = Mathf.Max(48f, size);
            _rectTransform.sizeDelta =
                new Vector2(_size, _size);
            _progress = Mathf.Repeat(initialProgress, 1f);
            _speed = Mathf.Max(0f, speed);
            _bobPhase = initialProgress *
                        Mathf.PI * 8f;
            EnsureVisuals();
            ApplyPosition();
        }

        public void Initialize(
            RectTransform arena,
            BattleEnemyState state,
            float size)
        {
            if (state == null)
            {
                throw new System.ArgumentNullException(
                    nameof(state));
            }

            _state = state;
            Initialize(
                arena,
                state.Progress,
                0f,
                size);
            ApplyHealth();

            if (state.Definition.IsBoss ||
                state.Definition.IsElite)
            {
                Outline outline =
                    gameObject.AddComponent<Outline>();
                outline.effectColor =
                    state.Definition.IsBoss
                        ? new Color(1f, 0.55f, 0.12f, 0.95f)
                        : new Color(0.70f, 0.38f, 1f, 0.90f);
                outline.effectDistance =
                    state.Definition.IsBoss
                        ? new Vector2(7f, -7f)
                        : new Vector2(4f, -4f);
            }

            // Enemies are small and moving; a finger needs a bigger target.
            GameObject tapArea = new GameObject(
                "TapArea",
                typeof(RectTransform),
                typeof(Image));
            tapArea.transform.SetParent(transform, false);
            RectTransform tapRect =
                tapArea.GetComponent<RectTransform>();
            tapRect.anchorMin = new Vector2(-0.3f, -0.3f);
            tapRect.anchorMax = new Vector2(1.3f, 1.3f);
            tapRect.offsetMin = Vector2.zero;
            tapRect.offsetMax = Vector2.zero;
            tapArea.GetComponent<Image>().color = Color.clear;
        }

        public void OnPointerClick(PointerEventData eventData)
        {
            if (_state != null && _state.IsAlive)
            {
                Tapped?.Invoke(_state);
            }
        }

        // A cyan reticle ring; an Outline tinted by the sprite read as murky green.
        public void SetFocused(bool focused)
        {
            if (_focusMark == null)
            {
                if (!focused)
                {
                    return;
                }
                GameObject mark = new GameObject(
                    "FocusMark",
                    typeof(RectTransform),
                    typeof(Image));
                mark.transform.SetParent(transform, false);
                RectTransform rect = mark.GetComponent<RectTransform>();
                rect.anchorMin = new Vector2(-0.18f, -0.18f);
                rect.anchorMax = new Vector2(1.18f, 1.18f);
                rect.offsetMin = Vector2.zero;
                rect.offsetMax = Vector2.zero;
                _focusMark = mark.GetComponent<Image>();
                _focusMark.sprite = UiFactory.RingSprite;
                _focusMark.color = GameTheme.Cyan;
                _focusMark.raycastTarget = false;
            }
            _focusMark.enabled = focused;
        }

        private void Update()
        {
            _progress = _state == null
                ? Mathf.Repeat(
                    _progress + _speed * Time.deltaTime,
                    1f)
                : _state.Progress;
            ApplyPosition();
            ApplyHealth();
            float bob =
                Mathf.Sin(
                    Time.time * 3.2f +
                    _bobPhase) *
                _size * 0.045f;
            _rectTransform.anchoredPosition =
                _pathPosition +
                Vector2.up * bob;
        }

        public void ShowDamage()
        {
            if (_hitRoutine != null)
            {
                StopCoroutine(_hitRoutine);
            }
            _hitRoutine = StartCoroutine(HitRoutine());
        }

        public void PlayDefeat()
        {
            StartCoroutine(DefeatRoutine());
        }

        public void ShowAbility()
        {
            if (_abilityRoutine != null)
            {
                StopCoroutine(_abilityRoutine);
            }
            _abilityRoutine =
                StartCoroutine(AbilityRoutine());
        }

        private void ApplyPosition()
        {
            if (_arena == null || _rectTransform == null)
            {
                return;
            }

            Rect rect = _arena.rect;
            float left = rect.xMin + rect.width * 0.075f;
            float right = rect.xMax - rect.width * 0.075f;
            float bottom = rect.yMin + rect.height * 0.067f;
            float top = rect.yMax - rect.height * 0.067f;
            float segment = _progress * 4f;
            Vector2 position;

            if (segment < 1f)
            {
                position = new Vector2(
                    Mathf.Lerp(left, right, segment),
                    top);
            }
            else if (segment < 2f)
            {
                position = new Vector2(
                    right,
                    Mathf.Lerp(top, bottom, segment - 1f));
            }
            else if (segment < 3f)
            {
                position = new Vector2(
                    Mathf.Lerp(right, left, segment - 2f),
                    bottom);
            }
            else
            {
                position = new Vector2(
                    left,
                    Mathf.Lerp(bottom, top, segment - 3f));
            }

            _pathPosition = position;
            _rectTransform.anchoredPosition =
                _pathPosition;
        }

        private void EnsureVisuals()
        {
            _enemyImage = GetComponent<Image>();
            _canvasGroup = GetComponent<CanvasGroup>();
            if (_canvasGroup == null)
            {
                _canvasGroup =
                    gameObject.AddComponent<CanvasGroup>();
            }

            GameObject healthBackground =
                new GameObject(
                    "HealthBar",
                    typeof(RectTransform),
                    typeof(Image));
            healthBackground.transform.SetParent(
                transform,
                false);
            RectTransform backgroundRect =
                healthBackground.GetComponent<RectTransform>();
            backgroundRect.anchorMin =
                new Vector2(0.08f, -0.02f);
            backgroundRect.anchorMax =
                new Vector2(0.92f, 0.08f);
            backgroundRect.offsetMin = Vector2.zero;
            backgroundRect.offsetMax = Vector2.zero;
            Image background =
                healthBackground.GetComponent<Image>();
            background.color =
                new Color(0.015f, 0.02f, 0.06f, 0.92f);
            background.raycastTarget = false;

            GameObject fillObject =
                new GameObject(
                    "HealthFill",
                    typeof(RectTransform),
                    typeof(Image));
            fillObject.transform.SetParent(
                healthBackground.transform,
                false);
            RectTransform fillRect =
                fillObject.GetComponent<RectTransform>();
            fillRect.anchorMin = Vector2.zero;
            fillRect.anchorMax = Vector2.one;
            fillRect.offsetMin = new Vector2(2f, 2f);
            fillRect.offsetMax = new Vector2(-2f, -2f);
            _healthFill =
                fillObject.GetComponent<Image>();
            _healthFill.color =
                new Color(0.36f, 0.91f, 0.70f, 1f);
            _healthFill.raycastTarget = false;
        }

        private void ApplyHealth()
        {
            if (_state == null || _healthFill == null)
            {
                return;
            }

            bool shielded = _state.Shield > 0.01f;
            float ratio = Mathf.Clamp01(
                (_state.Health + _state.Shield) /
                _state.MaxHealth);
            _healthFill.rectTransform.anchorMax =
                new Vector2(ratio, 1f);
            _healthFill.color = shielded
                ? new Color(0.36f, 0.80f, 1f, 1f)
                : Color.Lerp(
                    new Color(0.96f, 0.25f, 0.30f, 1f),
                    new Color(0.36f, 0.91f, 0.70f, 1f),
                    ratio);
        }

        private IEnumerator HitRoutine()
        {
            if (_enemyImage == null)
            {
                yield break;
            }

            Color original = _enemyImage.color;
            _enemyImage.color = Color.white * 1.25f;
            transform.localScale =
                Vector3.one * 1.12f;
            yield return new WaitForSeconds(0.06f);
            _enemyImage.color = original;
            transform.localScale = Vector3.one;
            _hitRoutine = null;
        }

        private IEnumerator AbilityRoutine()
        {
            if (_enemyImage == null)
            {
                yield break;
            }

            Color original = _enemyImage.color;
            float elapsed = 0f;
            const float duration = 0.32f;
            while (elapsed < duration)
            {
                elapsed += Time.deltaTime;
                float progress =
                    Mathf.Clamp01(elapsed / duration);
                float pulse =
                    Mathf.Sin(progress * Mathf.PI);
                _enemyImage.color = Color.Lerp(
                    original,
                    new Color(0.72f, 0.46f, 1f, 1f),
                    pulse);
                transform.localScale =
                    Vector3.one *
                    Mathf.Lerp(1f, 1.24f, pulse);
                yield return null;
            }

            _enemyImage.color = original;
            transform.localScale = Vector3.one;
            _abilityRoutine = null;
        }

        private IEnumerator DefeatRoutine()
        {
            float elapsed = 0f;
            const float duration = 0.22f;
            while (elapsed < duration)
            {
                elapsed += Time.deltaTime;
                float progress =
                    Mathf.Clamp01(elapsed / duration);
                transform.localScale =
                    Vector3.one *
                    Mathf.Lerp(1.15f, 0.15f, progress);
                if (_canvasGroup != null)
                {
                    _canvasGroup.alpha = 1f - progress;
                }
                yield return null;
            }

            Destroy(gameObject);
        }
    }
}
