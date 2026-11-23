using RoR2.UI;
using TMPro;
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.UI;

namespace Jaosnake.CeniReload.UI
{
    internal static class UiFactory
    {
        public static readonly Color BackdropColor = new Color(0.02f, 0.04f, 0.08f, 0.82f);
        public static readonly Color PanelBgColor = new Color(0.06f, 0.09f, 0.15f, 0.98f);
        public static readonly Color SubPanelBgColor = new Color(0.09f, 0.13f, 0.21f, 0.96f);
        public static readonly Color CardBgColor = new Color(0.11f, 0.16f, 0.25f, 0.95f);
        public static readonly Color CardSelectedColor = new Color(0.14f, 0.27f, 0.38f, 1.0f);
        public static readonly Color CardEmptyColor = new Color(0.08f, 0.11f, 0.17f, 0.72f);

        public static readonly Color AccentCyan = new Color(0.22f, 0.78f, 0.98f, 1f);
        public static readonly Color AccentAmber = new Color(0.98f, 0.72f, 0.20f, 1f);
        public static readonly Color AccentGreen = new Color(0.28f, 0.86f, 0.56f, 1f);
        public static readonly Color AccentRed = new Color(0.88f, 0.28f, 0.32f, 1f);
        public static readonly Color TextPrimary = new Color(0.94f, 0.97f, 1f, 1f);
        public static readonly Color TextMuted = new Color(0.62f, 0.70f, 0.82f, 1f);

        public static RectTransform CreateRect(string name, Transform parent)
        {
            var go = new GameObject(name, typeof(RectTransform));
            go.transform.SetParent(parent, false);
            return go.GetComponent<RectTransform>();
        }

        public static Image CreatePanel(string name, Transform parent, Color color, Color? borderColor = null)
        {
            var rect = CreateRect(name, parent);
            var img = rect.gameObject.AddComponent<Image>();
            img.color = color;

            if (borderColor.HasValue)
            {
                var outline = rect.gameObject.AddComponent<Outline>();
                outline.effectColor = borderColor.Value;
                outline.effectDistance = new Vector2(1.5f, -1.5f);
            }

            return img;
        }

        public static TextMeshProUGUI CreateText(
            string name,
            Transform parent,
            string text,
            float fontSize,
            Color color,
            TextAlignmentOptions alignment = TextAlignmentOptions.Left,
            FontStyles style = FontStyles.Normal)
        {
            var rect = CreateRect(name, parent);
            var tmp = rect.gameObject.AddComponent<TextMeshProUGUI>();
            tmp.text = text;
            tmp.fontSize = fontSize;
            tmp.color = color;
            tmp.alignment = alignment;
            tmp.fontStyle = style;
            tmp.enableWordWrapping = true;
            tmp.raycastTarget = false;
            return tmp;
        }

        public static MPButton CreateButton(
            string name,
            Transform parent,
            string label,
            Color normalColor,
            Color textColor,
            float fontSize,
            UnityAction onClick,
            out TextMeshProUGUI labelComponent,
            UILayerKey requiredTopLayer = null)
        {
            var img = CreatePanel(name, parent, normalColor, new Color(1f, 1f, 1f, 0.14f));
            var btn = img.gameObject.AddComponent<MPButton>();
            btn.targetGraphic = img;
            btn.allowAllEventSystems = true;
            btn.requiredTopLayer = requiredTopLayer;
            btn.defaultFallbackButton = false;

            var colors = btn.colors;
            colors.normalColor = Color.white;
            colors.highlightedColor = new Color(1.18f, 1.18f, 1.18f, 1f);
            colors.selectedColor = new Color(1.22f, 1.26f, 1.32f, 1f);
            colors.pressedColor = new Color(0.85f, 0.85f, 0.85f, 1f);
            colors.disabledColor = new Color(0.55f, 0.55f, 0.55f, 0.6f);
            colors.colorMultiplier = 1f;
            btn.colors = colors;

            var nav = btn.navigation;
            nav.mode = Navigation.Mode.Automatic;
            btn.navigation = nav;

            if (onClick != null)
            {
                btn.onClick.AddListener(onClick);
            }

            labelComponent = CreateText("Label", img.transform, label, fontSize, textColor, TextAlignmentOptions.Center, FontStyles.Bold);
            StretchFull(labelComponent.rectTransform, 10f, 4f);
            return btn;
        }

        public static Image CreatePortraitFrame(string name, Transform parent, Texture portrait, Color borderColor)
        {
            var frame = CreatePanel(name, parent, new Color(0.05f, 0.08f, 0.13f, 1f), borderColor);
            frame.raycastTarget = false;

            var rawRect = CreateRect("PortraitImage", frame.transform);
            StretchFull(rawRect, 2f, 2f);
            var rawImg = rawRect.gameObject.AddComponent<RawImage>();
            rawImg.raycastTarget = false;

            var placeholder = CreateText("PortraitPlaceholder", frame.transform, "?", 22f, TextMuted, TextAlignmentOptions.Center, FontStyles.Bold);
            placeholder.enableAutoSizing = true;
            placeholder.fontSizeMin = 12f;
            placeholder.fontSizeMax = 28f;
            StretchFull(placeholder.rectTransform, 2f, 2f);

            UpdatePortraitFrame(frame, portrait, borderColor);
            return frame;
        }

        public static void UpdatePortraitFrame(Image frame, Texture portrait, Color borderColor)
        {
            if (frame == null) return;

            var outline = frame.GetComponent<Outline>();
            if (outline != null)
            {
                outline.effectColor = borderColor;
            }

            var rawImg = frame.GetComponentInChildren<RawImage>(true);
            var placeholder = frame.GetComponentInChildren<TextMeshProUGUI>(true);

            bool hasPortrait = portrait != null;
            if (rawImg != null)
            {
                rawImg.texture = portrait;
                rawImg.color = Color.white;
                rawImg.enabled = hasPortrait;
            }

            if (placeholder != null)
            {
                placeholder.gameObject.SetActive(!hasPortrait);
                placeholder.color = borderColor == AccentRed ? AccentRed : TextMuted;
            }
        }

        public static void StretchFull(RectTransform rect, float padX = 0f, float padY = 0f)
        {
            rect.anchorMin = Vector2.zero;
            rect.anchorMax = Vector2.one;
            rect.offsetMin = new Vector2(padX, padY);
            rect.offsetMax = new Vector2(-padX, -padY);
        }
    }
}
