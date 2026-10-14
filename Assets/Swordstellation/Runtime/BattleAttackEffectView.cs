using System.Collections;
using System.Collections.Generic;
using Swordstellation.Core;
using UnityEngine;
using UnityEngine.UI;

namespace Swordstellation.Runtime
{
    public sealed class BattleAttackEffectView : MonoBehaviour
    {
        private RectTransform _arena;
        private bool _reducedMotion;

        public void Initialize(
            RectTransform arena,
            bool reducedMotion)
        {
            _arena = arena;
            _reducedMotion = reducedMotion;
        }

        public void Play(
            RectTransform source,
            IReadOnlyList<LoopEnemyPrototypeView> targets,
            BattleAttackEvent attack)
        {
            if (_arena == null ||
                source == null ||
                targets == null ||
                targets.Count == 0 ||
                attack == null)
            {
                return;
            }

            Vector2 sourcePoint =
                _arena.InverseTransformPoint(
                    source.position);
            Color color =
                GameTheme.FromHex(
                    attack.Definition.ColorRgb);

            SwordFamily family =
                SwordFamilyRules.Resolve(
                    attack.Definition);
            switch (family)
            {
                case SwordFamily.StraightSword:
                    StartCoroutine(
                        SlashRoutine(
                            sourcePoint,
                            TargetPoint(targets[0]),
                            color));
                    break;
                case SwordFamily.CurvedBlade:
                    StartCoroutine(
                        BoomerangRoutine(
                            sourcePoint,
                            TargetPoint(targets[0]),
                            attack.Definition.Id,
                            color));
                    break;
                case SwordFamily.Greatsword:
                    StartCoroutine(
                        AreaRoutine(
                            TargetPoint(targets[0]),
                            color,
                            attack.Instance.Stars + 2));
                    break;
                case SwordFamily.Rapier:
                    StartCoroutine(
                        ThinPierceRoutine(
                            sourcePoint,
                            TargetPoint(targets[0]),
                            color));
                    break;
                case SwordFamily.TwinBlades:
                    StartCoroutine(
                        SlashRoutine(
                            sourcePoint +
                            new Vector2(-10f, 7f),
                            TargetPoint(targets[0]) +
                            new Vector2(10f, -7f),
                            color));
                    StartCoroutine(
                        SlashRoutine(
                            sourcePoint +
                            new Vector2(10f, -7f),
                            TargetPoint(targets[0]) +
                            new Vector2(-10f, 7f),
                            Color.Lerp(
                                color,
                                Color.white,
                                0.3f)));
                    break;
                case SwordFamily.MagicBlade:
                    foreach (LoopEnemyPrototypeView target in targets)
                    {
                        StartCoroutine(
                            ProjectileRoutine(
                                sourcePoint,
                                TargetPoint(target),
                                attack.Definition.Id,
                                color));
                    }
                    break;
                case SwordFamily.HolyBlade:
                    StartCoroutine(
                        ChainRoutine(
                            sourcePoint,
                            targets,
                            color));
                    break;
                case SwordFamily.DragonBlade:
                    StartCoroutine(
                        AreaRoutine(
                            TargetPoint(targets[0]),
                            color,
                            attack.Instance.Stars + 3));
                    StartCoroutine(
                        ProjectileRoutine(
                            sourcePoint,
                            TargetPoint(targets[0]),
                            attack.Definition.Id,
                            color));
                    break;
                case SwordFamily.ReaperBlade:
                    StartCoroutine(
                        SlashRoutine(
                            sourcePoint,
                            TargetPoint(targets[0]),
                            color));
                    StartCoroutine(
                        AreaRoutine(
                            TargetPoint(targets[0]),
                            color,
                            attack.Instance.Stars));
                    break;
                case SwordFamily.AxeBlade:
                    StartCoroutine(
                        AreaRoutine(
                            TargetPoint(targets[0]),
                            Color.Lerp(
                                color,
                                GameTheme.Gold,
                                0.35f),
                            attack.Instance.Stars + 1));
                    break;
                case SwordFamily.SpearBlade:
                    StartCoroutine(
                        ThinPierceRoutine(
                            sourcePoint,
                            TargetPoint(targets[0]),
                            Color.Lerp(
                                color,
                                Color.white,
                                0.45f)));
                    break;
                case SwordFamily.SpiritBlade:
                    StartCoroutine(
                        ChainRoutine(
                            sourcePoint,
                            targets,
                            color));
                    foreach (LoopEnemyPrototypeView target in targets)
                    {
                        StartCoroutine(
                            ProjectileRoutine(
                                sourcePoint,
                                TargetPoint(target),
                                attack.Definition.Id,
                                Color.Lerp(
                                    color,
                                    Color.white,
                                    0.35f)));
                    }
                    break;
                default:
                    foreach (LoopEnemyPrototypeView target in targets)
                    {
                        StartCoroutine(
                            ProjectileRoutine(
                                sourcePoint,
                                TargetPoint(target),
                                attack.Definition.Id,
                                color));
                    }
                    break;
            }
        }

        private IEnumerator ProjectileRoutine(
            Vector2 source,
            Vector2 destination,
            string swordId,
            Color color)
        {
            GameObject effect = new GameObject(
                "SwordProjectile",
                typeof(RectTransform),
                typeof(CanvasGroup),
                typeof(Image));
            effect.transform.SetParent(_arena, false);
            RectTransform rect =
                effect.GetComponent<RectTransform>();
            rect.anchorMin = rect.anchorMax =
                new Vector2(0.5f, 0.5f);
            rect.sizeDelta = new Vector2(34f, 72f);
            Image image = effect.GetComponent<Image>();
            image.sprite = Resources.Load<Sprite>(
                $"Art/Swords/{swordId}");
            image.preserveAspect = true;
            image.color = Color.Lerp(
                Color.white,
                color,
                0.38f);
            image.raycastTarget = false;

            Vector2 direction = destination - source;
            rect.localRotation = Quaternion.Euler(
                0f,
                0f,
                Mathf.Atan2(direction.y, direction.x) *
                Mathf.Rad2Deg -
                90f);
            float duration =
                _reducedMotion ? 0.06f : 0.18f;
            float elapsed = 0f;
            while (elapsed < duration)
            {
                elapsed += Time.deltaTime;
                float progress =
                    Mathf.Clamp01(elapsed / duration);
                rect.anchoredPosition =
                    Vector2.Lerp(
                        source,
                        destination,
                        1f -
                        Mathf.Pow(1f - progress, 2f));
                yield return null;
            }

            Destroy(effect);
        }

        private IEnumerator BoomerangRoutine(
            Vector2 source,
            Vector2 destination,
            string swordId,
            Color color)
        {
            GameObject effect = new GameObject(
                "OrbitBoomerang",
                typeof(RectTransform),
                typeof(CanvasGroup),
                typeof(Image));
            effect.transform.SetParent(_arena, false);
            RectTransform rect =
                effect.GetComponent<RectTransform>();
            rect.anchorMin = rect.anchorMax =
                new Vector2(0.5f, 0.5f);
            rect.sizeDelta = new Vector2(54f, 82f);
            Image image = effect.GetComponent<Image>();
            image.sprite = Resources.Load<Sprite>(
                $"Art/Swords/{swordId}");
            image.preserveAspect = true;
            image.color =
                Color.Lerp(Color.white, color, 0.30f);
            image.raycastTarget = false;

            Vector2 direction = destination - source;
            Vector2 normal =
                new Vector2(-direction.y, direction.x)
                    .normalized;
            float duration =
                _reducedMotion ? 0.10f : 0.30f;
            float elapsed = 0f;
            while (elapsed < duration)
            {
                elapsed += Time.deltaTime;
                float progress =
                    Mathf.Clamp01(elapsed / duration);
                Vector2 basePoint =
                    Vector2.Lerp(
                        source,
                        destination,
                        progress);
                rect.anchoredPosition =
                    basePoint +
                    normal *
                    Mathf.Sin(progress * Mathf.PI) *
                    62f;
                rect.localRotation =
                    Quaternion.Euler(
                        0f,
                        0f,
                        progress * 720f);
                yield return null;
            }

            Destroy(effect);
        }

        private IEnumerator ThinPierceRoutine(
            Vector2 source,
            Vector2 destination,
            Color color)
        {
            Vector2 direction =
                (destination - source).normalized;
            Vector2 extended =
                destination + direction * 86f;
            Image core = CreateLine(
                "RapierPierceCore",
                source,
                extended,
                Color.white,
                4f);
            Image glow = CreateLine(
                "RapierPierceGlow",
                source,
                extended,
                new Color(
                    color.r,
                    color.g,
                    color.b,
                    0.55f),
                12f);
            glow.transform.SetAsFirstSibling();

            float duration =
                _reducedMotion ? 0.07f : 0.18f;
            float elapsed = 0f;
            while (elapsed < duration)
            {
                elapsed += Time.deltaTime;
                float progress =
                    Mathf.Clamp01(elapsed / duration);
                Color coreColor = core.color;
                coreColor.a = 1f - progress;
                core.color = coreColor;
                Color glowColor = glow.color;
                glowColor.a =
                    (1f - progress) * 0.55f;
                glow.color = glowColor;
                yield return null;
            }

            Destroy(core.gameObject);
            Destroy(glow.gameObject);
        }

        private IEnumerator SlashRoutine(
            Vector2 source,
            Vector2 destination,
            Color color)
        {
            Image line = CreateLine(
                "PiercingSlash",
                source,
                destination,
                Color.Lerp(color, Color.white, 0.34f),
                12f);
            RectTransform rect = line.rectTransform;
            CanvasGroup group =
                line.gameObject.AddComponent<CanvasGroup>();
            float duration =
                _reducedMotion ? 0.08f : 0.22f;
            float elapsed = 0f;
            while (elapsed < duration)
            {
                elapsed += Time.deltaTime;
                float progress =
                    Mathf.Clamp01(elapsed / duration);
                group.alpha = 1f - progress;
                rect.localScale = new Vector3(
                    1f,
                    1f + progress * 2.4f,
                    1f);
                yield return null;
            }

            Destroy(line.gameObject);
        }

        private IEnumerator ChainRoutine(
            Vector2 source,
            IReadOnlyList<LoopEnemyPrototypeView> targets,
            Color color)
        {
            List<Image> lines = new List<Image>();
            Vector2 previous = source;
            foreach (LoopEnemyPrototypeView target in targets)
            {
                Vector2 point = TargetPoint(target);
                lines.Add(
                    CreateLine(
                        "ConstellationChain",
                        previous,
                        point,
                        Color.Lerp(
                            color,
                            Color.white,
                            0.52f),
                        7f));
                previous = point;
            }

            float duration =
                _reducedMotion ? 0.09f : 0.26f;
            float elapsed = 0f;
            while (elapsed < duration)
            {
                elapsed += Time.deltaTime;
                float progress =
                    Mathf.Clamp01(elapsed / duration);
                float alpha =
                    (1f - progress) *
                    (0.70f +
                     Mathf.Sin(progress * Mathf.PI * 6f) *
                     0.25f);
                foreach (Image line in lines)
                {
                    Color current = line.color;
                    current.a = alpha;
                    line.color = current;
                }
                yield return null;
            }

            foreach (Image line in lines)
            {
                Destroy(line.gameObject);
            }
        }

        private IEnumerator AreaRoutine(
            Vector2 center,
            Color color,
            int stars)
        {
            Text star = UiFactory.CreateText(
                _arena,
                "StarfallImpact",
                "✦",
                82 + stars * 8,
                Color.Lerp(color, Color.white, 0.25f),
                TextAnchor.MiddleCenter,
                new Vector2(0.5f, 0.5f),
                new Vector2(0.5f, 0.5f),
                center - new Vector2(100f, 100f),
                center + new Vector2(100f, 100f),
                FontStyle.Bold);
            star.raycastTarget = false;
            CanvasGroup group =
                star.gameObject.AddComponent<CanvasGroup>();
            float duration =
                _reducedMotion ? 0.10f : 0.34f;
            float elapsed = 0f;
            while (elapsed < duration)
            {
                elapsed += Time.deltaTime;
                float progress =
                    Mathf.Clamp01(elapsed / duration);
                star.rectTransform.localScale =
                    Vector3.one *
                    Mathf.Lerp(0.35f, 1.65f, progress);
                group.alpha =
                    Mathf.Sin(progress * Mathf.PI);
                yield return null;
            }

            Destroy(star.gameObject);
        }

        private Image CreateLine(
            string name,
            Vector2 start,
            Vector2 end,
            Color color,
            float thickness)
        {
            GameObject lineObject = new GameObject(
                name,
                typeof(RectTransform),
                typeof(Image));
            lineObject.transform.SetParent(_arena, false);
            RectTransform rect =
                lineObject.GetComponent<RectTransform>();
            rect.anchorMin = rect.anchorMax =
                new Vector2(0.5f, 0.5f);
            Vector2 difference = end - start;
            rect.sizeDelta = new Vector2(
                difference.magnitude,
                thickness);
            rect.anchoredPosition =
                (start + end) * 0.5f;
            rect.localRotation = Quaternion.Euler(
                0f,
                0f,
                Mathf.Atan2(
                    difference.y,
                    difference.x) *
                Mathf.Rad2Deg);
            Image image =
                lineObject.GetComponent<Image>();
            image.color = color;
            image.raycastTarget = false;
            return image;
        }

        private Vector2 TargetPoint(
            LoopEnemyPrototypeView target)
        {
            return target == null
                ? Vector2.zero
                : (Vector2)_arena.InverseTransformPoint(
                    target.transform.position);
        }
    }
}
