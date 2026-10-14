using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

namespace Swordstellation.Runtime
{
    // Ornate frame sprites are drawn at 1000+ px, so their slice borders were
    // rendered at full size and crushed small buttons (ornaments crossing the
    // label). Scale the borders with the element's short side instead, keeping
    // the frame's drawn proportions at any size.
    [RequireComponent(typeof(Image))]
    public sealed class SlicedFrameFitter : UIBehaviour
    {
        private static readonly Color PlainColor = new Color(
            GameTheme.Panel.r,
            GameTheme.Panel.g,
            GameTheme.Panel.b,
            0.94f);

        // Panels shorter than this (list rows) read as a ladder of gold
        // ornaments, so they show a plain bordered panel instead.
        public float PlainBelow;

        private Sprite _frame;
        private Color _frameColor = Color.white;
        private Outline _plainOutline;
        private bool _isPlain;
        private bool _released;

        protected override void OnEnable()
        {
            base.OnEnable();
            Fit();
        }

        protected override void OnRectTransformDimensionsChange()
        {
            Fit();
        }

        public void Fit()
        {
            Image image = GetComponent<Image>();
            if (image == null || _released)
            {
                return;
            }
            // Callers that restyle the image afterwards (flat buttons) own it.
            if (_frame != null &&
                image.sprite != (_isPlain ? null : _frame))
            {
                _released = true;
                return;
            }
            if (_frame == null && image.sprite != null)
            {
                _frame = image.sprite;
            }

            Rect rect = ((RectTransform)transform).rect;
            float shortSide = Mathf.Min(rect.width, rect.height);
            if (_frame == null || shortSide < 1f)
            {
                return;
            }

            bool plain = shortSide < PlainBelow;
            if (plain != _isPlain)
            {
                // Only touch the colour when switching, so callers may tint
                // the frame after creation.
                if (plain)
                {
                    _frameColor = image.color;
                }
                image.color = plain ? PlainColor : _frameColor;
            }
            _isPlain = plain;
            image.sprite = plain ? null : _frame;
            image.type = plain ? Image.Type.Simple : Image.Type.Sliced;
            if (plain && _plainOutline == null)
            {
                _plainOutline = gameObject.AddComponent<Outline>();
                _plainOutline.effectColor =
                    new Color(0.25f, 0.56f, 0.95f, 0.45f);
                _plainOutline.effectDistance = new Vector2(2f, -2f);
            }
            if (_plainOutline != null)
            {
                _plainOutline.enabled = plain;
            }
            if (!plain)
            {
                Vector2 spriteSize = _frame.rect.size;
                image.pixelsPerUnitMultiplier =
                    Mathf.Min(spriteSize.x, spriteSize.y) / shortSide;
            }
        }
    }
}
