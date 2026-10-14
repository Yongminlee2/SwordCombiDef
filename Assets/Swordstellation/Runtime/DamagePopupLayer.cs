using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

namespace Swordstellation.Runtime
{
    // Floating damage numbers over enemies; critical hits pop big and gold.
    public sealed class DamagePopupLayer : MonoBehaviour
    {
        private const int MaxPopups = 28;
        private const int NormalLimit = 18;

        private sealed class Popup
        {
            public Text Text;
            public RectTransform Rect;
            public Vector3 Start;
            public float Age;
            public float Duration;
            public bool Critical;
        }

        private readonly List<Popup> _active = new List<Popup>();
        private readonly Stack<Popup> _free = new Stack<Popup>();
        private RectTransform _root;
        private bool _reducedMotion;

        public int ActiveCount => _active.Count;

        public void Initialize(RectTransform root, bool reducedMotion)
        {
            _root = root;
            _reducedMotion = reducedMotion;
        }

        public void Show(RectTransform target, float damage, bool critical)
        {
            if (_root == null || target == null || damage <= 0f)
            {
                return;
            }
            // Ordinary hits make way for crits when the screen is busy.
            if (!critical && _active.Count >= NormalLimit)
            {
                return;
            }

            Popup popup = Take();
            popup.Critical = critical;
            popup.Age = 0f;
            popup.Duration = critical ? 0.8f : 0.55f;
            popup.Start = _root.InverseTransformPoint(target.position) +
                          new Vector3(0f, target.rect.height * 0.45f, 0f);
            popup.Rect.localPosition = popup.Start;
            popup.Rect.localScale = Vector3.one;
            popup.Text.text = critical ? $"{Format(damage)}!" : Format(damage);
            popup.Text.fontSize = critical ? 44 : 26;
            popup.Text.resizeTextMaxSize = popup.Text.fontSize;
            popup.Text.color = critical ? GameTheme.Gold : GameTheme.Text;
            popup.Rect.SetAsLastSibling();
            popup.Text.gameObject.SetActive(true);
            _active.Add(popup);
        }

        private Popup Take()
        {
            if (_free.Count > 0)
            {
                return _free.Pop();
            }
            if (_active.Count >= MaxPopups)
            {
                Popup oldest = _active[0];
                _active.RemoveAt(0);
                return oldest;
            }

            Text text = UiFactory.CreateText(
                _root,
                "DamagePopup",
                string.Empty,
                26,
                GameTheme.Text,
                TextAnchor.MiddleCenter,
                new Vector2(0.5f, 0.5f),
                new Vector2(0.5f, 0.5f),
                new Vector2(-120f, -36f),
                new Vector2(120f, 36f));
            text.raycastTarget = false;
            UiFactory.AddOutline(text, new Color(0f, 0f, 0.05f, 0.95f), new Vector2(2f, -2f));
            return new Popup { Text = text, Rect = text.rectTransform };
        }

        private void Update()
        {
            float delta = Time.unscaledDeltaTime;
            for (int index = _active.Count - 1; index >= 0; index--)
            {
                Popup popup = _active[index];
                popup.Age += delta;
                float progress = popup.Age / popup.Duration;
                if (progress >= 1f)
                {
                    popup.Text.gameObject.SetActive(false);
                    _active.RemoveAt(index);
                    _free.Push(popup);
                    continue;
                }

                float rise = popup.Critical ? 70f : 42f;
                popup.Rect.localPosition =
                    popup.Start + new Vector3(0f, rise * (1f - (1f - progress) * (1f - progress)), 0f);
                Color color = popup.Text.color;
                color.a = progress < 0.5f ? 1f : 1f - (progress - 0.5f) * 2f;
                popup.Text.color = color;
                if (popup.Critical && !_reducedMotion)
                {
                    float pop = progress < 0.15f ? Mathf.Lerp(1.5f, 1f, progress / 0.15f) : 1f;
                    popup.Rect.localScale = Vector3.one * pop;
                }
            }
        }

        public static string Format(float damage)
        {
            int value = Mathf.RoundToInt(damage);
            return value >= 10000 ? $"{value / 1000f:0.#}K" : value.ToString();
        }
    }
}
