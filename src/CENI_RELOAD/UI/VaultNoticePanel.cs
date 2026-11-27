using System;
using RoR2.UI;
using TMPro;
using UnityEngine;
using UnityEngine.UI;
using Jaosnake.CeniReload.Localization;

namespace Jaosnake.CeniReload.UI
{
    // A scrollable confirmation/details surface inside the Vault's existing input layer.
    internal sealed class VaultNoticePanel
    {
        private readonly GameObject _root;
        private readonly TextMeshProUGUI _body;
        private readonly TextMeshProUGUI _confirmLabel;
        private readonly ScrollRect _scroll;
        private Action _action;
        private int _openedFrame;
        public MPButton Confirm { get; }
        public MPButton Cancel { get; }
        public bool IsOpen => _root.activeInHierarchy;
        public bool Contains(GameObject selected) => selected != null && selected.transform.IsChildOf(_root.transform);
        public VaultNoticePanel(Transform parent, UILayerKey layer)
        {
            var backdrop = UiFactory.CreatePanel("CheckpointNoticeBackdrop", parent, new Color(0f, 0f, 0f, 0.94f));
            UiFactory.StretchFull(backdrop.rectTransform); _root = backdrop.gameObject;
            var panel = UiFactory.CreatePanel("CheckpointNotice", backdrop.transform, UiFactory.PanelBgColor, UiFactory.AccentCyan);
            panel.rectTransform.anchorMin = panel.rectTransform.anchorMax = new Vector2(0.5f, 0.5f);
            panel.rectTransform.sizeDelta = new Vector2(720f, 470f);
            var viewport = UiFactory.CreatePanel("NoticeViewport", panel.transform, new Color(0f, 0f, 0f, 0.1f));
            viewport.rectTransform.anchorMin = Vector2.zero; viewport.rectTransform.anchorMax = Vector2.one;
            viewport.rectTransform.offsetMin = new Vector2(20f, 75f); viewport.rectTransform.offsetMax = new Vector2(-20f, -20f);
            viewport.gameObject.AddComponent<RectMask2D>();
            var content = UiFactory.CreatePanel("NoticeContent", viewport.transform, Color.clear);
            content.rectTransform.anchorMin = new Vector2(0f, 1f); content.rectTransform.anchorMax = Vector2.one;
            content.rectTransform.pivot = new Vector2(0.5f, 1f);
            content.rectTransform.sizeDelta = Vector2.zero;
            _body = UiFactory.CreateText("NoticeText", content.transform, "", 16f, Color.white, TextAlignmentOptions.TopLeft);
            UiFactory.StretchFull(_body.rectTransform);
            _scroll = viewport.gameObject.AddComponent<ScrollRect>();
            _scroll.viewport = viewport.rectTransform; _scroll.content = content.rectTransform;
            _scroll.horizontal = false; _scroll.vertical = true; _scroll.movementType = ScrollRect.MovementType.Clamped;
            Confirm = UiFactory.CreateButton("NoticeConfirm", panel.transform, "", UiFactory.AccentCyan, Color.white, 14f,
                () => { if (Time.frameCount <= _openedFrame) return; var action = _action; Close(); action?.Invoke(); }, out _confirmLabel, layer);
            Cancel = UiFactory.CreateButton("NoticeClose", panel.transform, CeniReloadStrings.Get("CENIRELOAD_UI_CLOSE"),
                new Color(0.22f, 0.26f, 0.34f, 1f), Color.white, 14f, Close, out _, layer);
            SetButtonRect(Confirm, 0.04f, 0.63f); SetButtonRect(Cancel, 0.66f, 0.96f);
            var navigation = new Navigation { mode = Navigation.Mode.Explicit, selectOnRight = Cancel, selectOnDown = Cancel, selectOnLeft = Cancel, selectOnUp = Cancel };
            Confirm.navigation = navigation;
            Cancel.navigation = new Navigation { mode = Navigation.Mode.Explicit, selectOnLeft = Confirm, selectOnUp = Confirm, selectOnRight = Confirm, selectOnDown = Confirm };
            Close();
        }
        private static void SetButtonRect(MPButton button, float min, float max)
        {
            var rect = button.GetComponent<RectTransform>();
            rect.anchorMin = new Vector2(min, 0f); rect.anchorMax = new Vector2(max, 0f);
            rect.offsetMin = new Vector2(0f, 18f); rect.offsetMax = new Vector2(0f, 60f);
        }
        public void Open(string text, string actionLabel = null, Action action = null)
        {
            _action = action; _body.text = text; _confirmLabel.text = actionLabel ?? CeniReloadStrings.Get("CENIRELOAD_UI_CLOSE");
            _openedFrame = Time.frameCount;
            Confirm.gameObject.SetActive(true); Cancel.gameObject.SetActive(action != null);
            _root.SetActive(true); _root.transform.SetAsLastSibling();
            Canvas.ForceUpdateCanvases();
            _scroll.content.sizeDelta = new Vector2(0f, Mathf.Max(_scroll.viewport.rect.height, _body.GetPreferredValues(text, _scroll.viewport.rect.width, 0f).y + 12f));
            _scroll.verticalNormalizedPosition = 1f;
        }
        public void Close() { _root.SetActive(false); _action = null; }
        public void Scroll(float amount) { if (IsOpen) _scroll.verticalNormalizedPosition = Mathf.Clamp01(_scroll.verticalNormalizedPosition + amount); }
    }
}
