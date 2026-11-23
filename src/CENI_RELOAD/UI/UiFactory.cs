using System;
using RoR2.UI;
using TMPro;
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.EventSystems;
using UnityEngine.UI;

namespace Jaosnake.CeniReload.UI
{
    internal class VaultFocusHighlight : MonoBehaviour, ISelectHandler, IDeselectHandler, IPointerEnterHandler, IPointerExitHandler
    {
        public static readonly Color FocusYellow = new Color(0.98f, 0.80f, 0.08f, 1f);

        private Image _background;
        private Outline _outline;
        private Color _baseBgColor;
        private Color _baseBorderColor;
        private Vector2 _baseBorderDistance = new Vector2(1.5f, -1.5f);
        private Vector2 _focusBorderDistance = new Vector2(3f, -3f);
        private Action _onSelected;

        private bool _isSelected;
        private bool _isHovered;

        public void Configure(Image bg, Outline outline, Color baseBg, Color baseBorder, Action onSelected = null)
        {
            _background = bg;
            _outline = outline;
            _baseBgColor = baseBg;
            _baseBorderColor = baseBorder;
            _onSelected = onSelected;
            ApplyVisualState();
        }

        public void SetBaseColors(Color baseBg, Color baseBorder)
        {
            _baseBgColor = baseBg;
            _baseBorderColor = baseBorder;
            ApplyVisualState();
        }

        public void OnSelect(BaseEventData eventData)
        {
            _isSelected = true;
            ApplyVisualState();
            _onSelected?.Invoke();
        }

        public void OnDeselect(BaseEventData eventData)
        {
            _isSelected = false;
            ApplyVisualState();
        }

        public void OnPointerEnter(PointerEventData eventData)
        {
            _isHovered = true;
            ApplyVisualState();
        }

        public void OnPointerExit(PointerEventData eventData)
        {
            _isHovered = false;
            ApplyVisualState();
        }

        private void OnDisable()
        {
            _isSelected = false;
            _isHovered = false;
            ApplyVisualState();
        }

        private void ApplyVisualState()
        {
            if (_background != null)
            {
                if (_isSelected)
                {
                    _background.color = Color.Lerp(_baseBgColor, Color.white, 0.14f);
                }
                else if (_isHovered)
                {
                    _background.color = Color.Lerp(_baseBgColor, Color.white, 0.08f);
                }
                else
                {
                    _background.color = _baseBgColor;
                }
            }

            if (_outline != null)
            {
                if (_isSelected)
                {
                    _outline.effectColor = FocusYellow;
                    _outline.effectDistance = _focusBorderDistance;
                }
                else if (_isHovered)
                {
                    _outline.effectColor = Color.Lerp(_baseBorderColor, FocusYellow, 0.55f);
                    _outline.effectDistance = new Vector2(2f, -2f);
                }
                else
                {
                    _outline.effectColor = _baseBorderColor;
                    _outline.effectDistance = _baseBorderDistance;
                }
            }
        }
    }

    internal class VaultRightClickHandler : MonoBehaviour, IPointerClickHandler
    {
        private Action _onRightClick;

        public void Initialize(Action onRightClick)
        {
            _onRightClick = onRightClick;
        }

        public void OnPointerClick(PointerEventData eventData)
        {
            if (eventData != null && eventData.button == PointerEventData.InputButton.Right)
            {
                _onRightClick?.Invoke();
            }
        }
    }

    internal static class UiFactory
    {
        private static Texture2D _lockedPadlockTexture;
        public static readonly Color BackdropColor = new Color(0.02f, 0.04f, 0.08f, 0.82f);
        public static readonly Color PanelBgColor = new Color(0.06f, 0.09f, 0.15f, 0.98f);
        public static readonly Color SubPanelBgColor = new Color(0.09f, 0.13f, 0.21f, 0.96f);
        public static readonly Color SubPanelMultiplayerBgColor = new Color(0.22f, 0.08f, 0.10f, 0.96f);
        public static readonly Color SubPanelSpecialModeBgColor = new Color(0.07f, 0.20f, 0.13f, 0.96f);
        public static readonly Color CardBgColor = new Color(0.11f, 0.16f, 0.25f, 0.95f);
        public static readonly Color CardSelectedColor = new Color(0.14f, 0.27f, 0.38f, 1.0f);
        public static readonly Color CardMultiplayerBgColor = new Color(0.28f, 0.10f, 0.13f, 0.95f);
        public static readonly Color CardMultiplayerSelectedColor = new Color(0.42f, 0.14f, 0.18f, 1.0f);
        public static readonly Color CardSpecialModeBgColor = new Color(0.09f, 0.24f, 0.15f, 0.95f);
        public static readonly Color CardSpecialModeSelectedColor = new Color(0.13f, 0.36f, 0.22f, 1.0f);
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
            Color restBorder = new Color(1f, 1f, 1f, 0.16f);
            var img = CreatePanel(name, parent, normalColor, restBorder);
            var outline = img.GetComponent<Outline>();
            var btn = img.gameObject.AddComponent<MPButton>();
            btn.targetGraphic = img;
            btn.allowAllEventSystems = true;
            btn.requiredTopLayer = requiredTopLayer;
            btn.defaultFallbackButton = false;

            var colors = btn.colors;
            colors.normalColor = Color.white;
            colors.highlightedColor = Color.white;
            colors.selectedColor = Color.white;
            colors.pressedColor = new Color(0.82f, 0.84f, 0.88f, 1f);
            colors.disabledColor = new Color(0.50f, 0.50f, 0.50f, 0.55f);
            colors.colorMultiplier = 1f;
            btn.colors = colors;

            var nav = btn.navigation;
            nav.mode = Navigation.Mode.Explicit;
            btn.navigation = nav;

            var highlight = img.gameObject.AddComponent<VaultFocusHighlight>();
            highlight.Configure(img, outline, normalColor, restBorder);

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

        public static Texture2D GetLockedPadlockTexture()
        {
            if (_lockedPadlockTexture != null) return _lockedPadlockTexture;

            const int size = 64;
            var tex = new Texture2D(size, size, TextureFormat.RGBA32, false);
            tex.filterMode = FilterMode.Bilinear;
            tex.wrapMode = TextureWrapMode.Clamp;

            Color goldBright = new Color(1.00f, 0.87f, 0.28f, 1f);
            Color goldMid = new Color(0.96f, 0.72f, 0.14f, 1f);
            Color goldDeep = new Color(0.70f, 0.45f, 0.06f, 1f);
            Color bgCenter = new Color(0.11f, 0.14f, 0.22f, 0.98f);
            Color bgEdge = new Color(0.05f, 0.08f, 0.13f, 0.98f);
            Color keyholeColor = new Color(0.08f, 0.09f, 0.14f, 1f);

            float half = (size - 1) * 0.5f;

            for (int y = 0; y < size; y++)
            {
                for (int x = 0; x < size; x++)
                {
                    int edgeDist = Mathf.Min(Mathf.Min(x, size - 1 - x), Mathf.Min(y, size - 1 - y));
                    float dx = x - half;
                    float dy = y - half;
                    float radial = Mathf.Clamp01(Mathf.Sqrt(dx * dx + dy * dy) / (half * 1.15f));

                    // Character-Select portrait-style dark metallic base with subtle radial golden glow
                    Color px = Color.Lerp(bgCenter, bgEdge, radial);
                    float centerGlow = Mathf.Clamp01(1f - radial * 1.15f);
                    px = Color.Lerp(px, new Color(0.36f, 0.26f, 0.07f, 1f), centerGlow * 0.42f);

                    // Luminous golden portrait frame (matches RoR2 survivor selection glowing border)
                    if (edgeDist <= 1)
                    {
                        px = new Color(1f, 0.88f, 0.32f, 0.96f);
                    }
                    else if (edgeDist == 2)
                    {
                        px = goldBright;
                    }
                    else if (edgeDist == 3)
                    {
                        px = Color.Lerp(goldMid, px, 0.35f);
                    }
                    else if (edgeDist <= 8)
                    {
                        float glowT = (8f - edgeDist) / 5f;
                        px = Color.Lerp(px, goldMid, glowT * glowT * 0.36f);
                    }

                    // Padlock shackle (top U-arch)
                    float sx = x - half;
                    float sy = y - 36.5f;
                    bool inShackle = false;
                    if (y >= 36)
                    {
                        float r = Mathf.Sqrt(sx * sx + sy * sy);
                        if (r >= 7.2f && r <= 12.2f) inShackle = true;
                    }
                    else if (y >= 28 && y < 36)
                    {
                        float absX = Mathf.Abs(sx);
                        if (absX >= 7.2f && absX <= 12.2f) inShackle = true;
                    }

                    if (inShackle)
                    {
                        float shackleShade = Mathf.Clamp01((y - 28f) / 22f);
                        px = Color.Lerp(new Color(0.84f, 0.86f, 0.92f, 1f), Color.white, shackleShade * 0.55f);
                    }

                    // Padlock body (beveled golden metallic rectangle, y = 12..32, x = 16..47)
                    if (x >= 16 && x <= 47 && y >= 12 && y <= 32)
                    {
                        // Soft 2px rounded corners
                        bool cutCorner =
                            ((x == 16 || x == 47) && (y == 12 || y == 32));
                        if (!cutCorner)
                        {
                            float bodyT = (y - 12f) / 20f;
                            Color bodyCol = Color.Lerp(goldDeep, goldBright, bodyT);
                            if (y == 32 || x == 16) bodyCol = Color.Lerp(bodyCol, Color.white, 0.35f);
                            else if (y == 12 || x == 47) bodyCol = Color.Lerp(bodyCol, Color.black, 0.25f);
                            px = bodyCol;

                            // Keyhole circle + slot
                            float kx = x - half;
                            float ky = y - 24.0f;
                            float kr = Mathf.Sqrt(kx * kx + ky * ky);
                            bool inKeyhole = (kr <= 3.1f) || (Mathf.Abs(kx) <= 1.7f && y >= 16 && y <= 23);
                            if (inKeyhole)
                            {
                                px = keyholeColor;
                            }
                        }
                    }

                    tex.SetPixel(x, y, px);
                }
            }

            tex.Apply(false, true);
            _lockedPadlockTexture = tex;
            return _lockedPadlockTexture;
        }

        public static RawImage CreatePadlockBadge(string name, Transform parent, float size)
        {
            var rect = CreateRect(name, parent);
            rect.sizeDelta = new Vector2(size, size);
            var raw = rect.gameObject.AddComponent<RawImage>();
            raw.texture = GetLockedPadlockTexture();
            raw.color = Color.white;
            raw.raycastTarget = false;
            return raw;
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
