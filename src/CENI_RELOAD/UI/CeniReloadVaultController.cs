using System;
using System.Collections;
using System.Collections.Generic;
using System.Text.RegularExpressions;
using Jaosnake.CeniReload.Core;
using Jaosnake.CeniReload.Localization;
using Jaosnake.CeniReload.Model;
using Jaosnake.CeniReload.Storage;
using RoR2;
using RoR2.UI;
using RoR2.UI.MainMenu;
using TMPro;
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.EventSystems;
using UnityEngine.UI;

namespace Jaosnake.CeniReload.UI
{
    public class CeniReloadVaultController : MonoBehaviour
    {
        private CharacterSelectController _lobby;
        private MainMenuController _mainMenu;
        private bool _isMainMenuMode;

        private MPButton _triggerButton;
        private TextMeshProUGUI _triggerLabel;

        private GameObject _modalRoot;
        private RoR2.UILayer _modalLayer;
        private UILayerKey _modalLayerKey;

        private TextMeshProUGUI _titleText;
        private TextMeshProUGUI _subtitleText;
        private MPButton _filterButton;
        private TextMeshProUGUI _filterButtonLabel;
        private MPButton _closeButton;
        private TextMeshProUGUI _closeButtonLabel;
        private TextMeshProUGUI _footerHintText;

        private ScrollRect _scrollRect;
        private Transform _cardsContainer;
        private Image _inspectorPortraitFrame;
        private TextMeshProUGUI _inspectorTitle;
        private TextMeshProUGUI _inspectorBody;
        private TextMeshProUGUI _inspectorModStatus;

        private MPButton _loadButton;
        private TextMeshProUGUI _loadButtonLabel;
        private MPButton _deleteButton;
        private TextMeshProUGUI _deleteButtonLabel;

        private GameObject _deleteConfirmBar;
        private TextMeshProUGUI _confirmPromptText;
        private MPButton _confirmYesButton;
        private TextMeshProUGUI _confirmYesLabel;
        private MPButton _confirmNoButton;
        private TextMeshProUGUI _confirmNoLabel;

        private bool _showAllSurvivors;
        private CheckpointSnapshot _selectedSnapshot;
        private readonly List<GameObject> _spawnedCards = new List<GameObject>();

        private bool _hasCachedDock;
        private Vector2 _cachedDockSize;
        private Vector3 _cachedDockLocalPos;
        private MPEventSystem.InputSource _lastInputSource;
        private bool _hasLastInputSource;
        private string _lastLanguageName;
        private int _openedFrame = -1;
        private int _closedFrame = -1;
        private Coroutine _alignCoroutine;
        private Coroutine _focusCoroutine;

        public static CeniReloadVaultController AttachTo(CharacterSelectController controller)
        {
            return AttachToLobby(controller);
        }

        public static CeniReloadVaultController AttachToLobby(CharacterSelectController controller)
        {
            if (controller == null) return null;
            var existing = controller.GetComponent<CeniReloadVaultController>();
            if (existing != null) return existing;

            var vault = controller.gameObject.AddComponent<CeniReloadVaultController>();
            vault.InitializeLobby(controller);
            return vault;
        }

        public static CeniReloadVaultController AttachToMainMenu(MainMenuController controller)
        {
            if (controller == null) return null;
            var existing = controller.GetComponent<CeniReloadVaultController>();
            if (existing != null) return existing;

            var vault = controller.gameObject.AddComponent<CeniReloadVaultController>();
            vault.InitializeMainMenu(controller);
            return vault;
        }

        private void InitializeLobby(CharacterSelectController controller)
        {
            _lobby = controller;
            _isMainMenuMode = false;
            _showAllSurvivors = false;

            BuildLobbyTriggerButton();
            BuildVaultModal();
            SetModalOpen(false);
        }

        private void InitializeMainMenu(MainMenuController controller)
        {
            _mainMenu = controller;
            _isMainMenuMode = true;
            _showAllSurvivors = true;

            BuildMainMenuTriggerButton();
            BuildVaultModal();
            SetModalOpen(false);
        }

        private void OnEnable()
        {
            if (!_isMainMenuMode && _triggerButton != null)
            {
                ScheduleLobbyAlignment();
            }
        }

        private void OnDestroy()
        {
            if (_modalLayerKey != null)
            {
                _modalLayerKey.enabled = false;
            }
            if (_modalRoot != null)
            {
                Destroy(_modalRoot);
                _modalRoot = null;
            }
            if (_triggerButton != null)
            {
                Destroy(_triggerButton.gameObject);
                _triggerButton = null;
            }
            if (_modalLayer != null)
            {
                Destroy(_modalLayer);
                _modalLayer = null;
            }
        }

        private void Update()
        {
            var primaryEs = MPEventSystemManager.primaryEventSystem;

            string currentLang = RoR2.Language.currentLanguageName;
            if (!string.Equals(currentLang, _lastLanguageName, StringComparison.Ordinal))
            {
                _lastLanguageName = currentLang;
                if (_modalRoot != null && _modalRoot.activeSelf)
                {
                    RefreshLocalization();
                    RebuildSlotCards();
                }
                else
                {
                    RefreshTriggerLabel();
                }
            }

            if (!_isMainMenuMode && _triggerButton != null)
            {
                if (primaryEs != null)
                {
                    var currentSource = primaryEs.currentInputSource;
                    if (!_hasLastInputSource || currentSource != _lastInputSource)
                    {
                        _hasLastInputSource = true;
                        _lastInputSource = currentSource;
                        UpdateLobbyTriggerText(primaryEs);
                        ScheduleLobbyAlignment();
                    }
                }

                // Gamepad shortcut in Lobby: Y (Xbox) / Triangle (PlayStation) opens the Vault
                if (_modalRoot != null && !_modalRoot.activeSelf &&
                    Time.frameCount > _openedFrame && Time.frameCount > _closedFrame &&
                    _triggerButton.gameObject.activeInHierarchy && IsGamepadVaultShortcutPressed(primaryEs))
                {
                    var lobbyLayer = _triggerButton.requiredTopLayer;
                    if (lobbyLayer == null || lobbyLayer.representsTopLayer)
                    {
                        SetModalOpen(true);
                        return;
                    }
                }
            }

            if (_modalRoot != null && _modalRoot.activeSelf && Time.frameCount > _openedFrame)
            {
                bool cancelPressed = Input.GetKeyDown(KeyCode.Escape) || Input.GetKeyDown(KeyCode.JoystickButton1);
                if (!cancelPressed && primaryEs != null && primaryEs.player != null && primaryEs.player.GetButtonDown(15))
                {
                    cancelPressed = true;
                }

                if (cancelPressed)
                {
                    if (_deleteConfirmBar != null && _deleteConfirmBar.activeSelf)
                    {
                        SetDeleteConfirmVisible(false);
                    }
                    else
                    {
                        SetModalOpen(false);
                    }
                }
            }
        }

        public void RefreshTriggerLabel()
        {
            if (_triggerLabel != null)
            {
                if (_isMainMenuMode)
                {
                    _triggerLabel.text = CeniReloadStrings.Get("CENIRELOAD_BTN_OPEN_VAULT");
                }
                else
                {
                    UpdateLobbyTriggerText(MPEventSystemManager.primaryEventSystem);
                }
            }

            if (!_isMainMenuMode && isActiveAndEnabled && _triggerButton != null)
            {
                ScheduleLobbyAlignment();
            }
        }

        private void UpdateLobbyTriggerText(MPEventSystem es)
        {
            if (_triggerLabel == null) return;
            string baseLabel = CeniReloadStrings.Get("CENIRELOAD_BTN_OPEN_VAULT");
            string glyph = GetGamepadVaultGlyph(es);
            _triggerLabel.text = string.IsNullOrEmpty(glyph) ? baseLabel : $"{glyph}{baseLabel}";
        }

        private static string GetGamepadVaultGlyph(MPEventSystem es)
        {
            if (es == null || es.currentInputSource != MPEventSystem.InputSource.Gamepad)
                return string.Empty;

            string hw = string.Empty;
            var controllers = es.player?.controllers;
            var lastCtrl = controllers?.GetLastActiveController();
            if (lastCtrl != null && lastCtrl.type == Rewired.ControllerType.Joystick)
            {
                hw = $"{lastCtrl.name} {lastCtrl.hardwareName}".ToLowerInvariant();
            }
            else if (controllers != null && controllers.Joysticks != null && controllers.Joysticks.Count > 0)
            {
                var firstJoy = controllers.Joysticks[0];
                if (firstJoy != null)
                {
                    hw = $"{firstJoy.name} {firstJoy.hardwareName}".ToLowerInvariant();
                }
            }

            if (hw.Contains("dualsense") || hw.Contains("ps5"))
                return "<sprite=\"tmpsprPS5GlyphsUnified\" name=\"texPS5GlyphsUnified_Triangle\"> ";
            if (hw.Contains("dualshock") || hw.Contains("ps4") || hw.Contains("playstation") || hw.Contains("sony"))
                return "<sprite=\"tmpsprPS4GlyphsUnified\" name=\"texPS4GlyphsUnified_1\"> ";
            if (hw.Contains("switch") || hw.Contains("pro controller") || hw.Contains("joy-con"))
                return "<sprite=\"tmpsprSwitchGlyphsUnified\" name=\"texSwitchGlyphsUnified_2\"> ";

            return "<sprite=\"tmpsprXboxOneGlyphs\" name=\"texXBoxOneGlyphs_3\"> ";
        }

        private static bool IsGamepadVaultShortcutPressed(MPEventSystem es)
        {
            if (es == null || es.currentInputSource != MPEventSystem.InputSource.Gamepad)
                return false;

            if (es.player != null && es.player.controllers != null)
            {
                var joysticks = es.player.controllers.Joysticks;
                for (int i = 0; i < joysticks.Count; i++)
                {
                    var joy = joysticks[i];
                    if (joy == null) continue;

                    string hw = $"{joy.name} {joy.hardwareName}".ToLowerInvariant();
                    bool isPlayStation = hw.Contains("dualshock") || hw.Contains("dualsense") ||
                                         hw.Contains("playstation") || hw.Contains("ps4") ||
                                         hw.Contains("ps5") || hw.Contains("sony");

                    var buttons = joy.Buttons;
                    for (int b = 0; b < buttons.Count; b++)
                    {
                        var btn = buttons[b];
                        if (btn == null || !btn.justPressed) continue;

                        string elName = btn.elementIdentifier != null ? btn.elementIdentifier.name : string.Empty;
                        int elId = btn.elementIdentifier != null ? btn.elementIdentifier.id : -1;

                        if (elName.Equals("Y", StringComparison.OrdinalIgnoreCase) ||
                            elName.Equals("Triangle", StringComparison.OrdinalIgnoreCase) ||
                            elName.Equals("Action Top Row 2", StringComparison.OrdinalIgnoreCase) ||
                            (isPlayStation && elId == 9) ||
                            (!isPlayStation && elId == 13))
                        {
                            return true;
                        }
                    }
                }
            }

            return Input.GetKeyDown(KeyCode.JoystickButton3);
        }

        private void ScheduleLobbyAlignment()
        {
            if (!isActiveAndEnabled || _isMainMenuMode || _triggerButton == null) return;
            if (_alignCoroutine != null)
            {
                StopCoroutine(_alignCoroutine);
            }
            _alignCoroutine = StartCoroutine(AlignTriggerAboveBackButton());
        }

        private Canvas ResolveHostCanvas()
        {
            Canvas canvas = null;
            if (_isMainMenuMode && _mainMenu != null)
            {
                canvas = _mainMenu.GetComponentInParent<Canvas>();
                if (canvas == null && _mainMenu.mainMenuButtonPanel != null)
                {
                    canvas = _mainMenu.mainMenuButtonPanel.GetComponentInParent<Canvas>();
                }
                if (canvas == null && _mainMenu.singlePlayerButton != null)
                {
                    canvas = _mainMenu.singlePlayerButton.GetComponentInParent<Canvas>();
                }
            }
            else if (_lobby != null)
            {
                canvas = _lobby.GetComponentInParent<Canvas>();
            }

            if (canvas != null && canvas.rootCanvas != null)
            {
                return canvas.rootCanvas;
            }
            return canvas;
        }

        private UILayerKey FindLobbyLayerKey()
        {
            if (_lobby == null) return null;
            if (_lobby.readyButton != null && _lobby.readyButton.requiredTopLayer != null)
            {
                return _lobby.readyButton.requiredTopLayer;
            }
            return _lobby.GetComponentInParent<UILayerKey>()
                ?? _lobby.GetComponentInChildren<UILayerKey>(true);
        }

        private UILayerKey FindMainMenuLayerKey()
        {
            if (_mainMenu == null) return null;
            if (_mainMenu.singlePlayerButton != null && _mainMenu.singlePlayerButton.requiredTopLayer != null)
            {
                return _mainMenu.singlePlayerButton.requiredTopLayer;
            }
            return _mainMenu.GetComponentInParent<UILayerKey>()
                ?? _mainMenu.GetComponentInChildren<UILayerKey>(true);
        }

        private string GetSelectedSurvivorBodyName()
        {
            var localUser = LocalUserManager.GetFirstLocalUser();
            var survivorDef = localUser?.userProfile?.GetSurvivorPreference();
            if (survivorDef != null && survivorDef.bodyPrefab != null)
            {
                return survivorDef.bodyPrefab.name;
            }
            return string.Empty;
        }

        private string GetSelectedSurvivorDisplayName()
        {
            var localUser = LocalUserManager.GetFirstLocalUser();
            var survivorDef = localUser?.userProfile?.GetSurvivorPreference();
            if (survivorDef != null)
            {
                return CeniReloadStrings.LocalizeGameToken(survivorDef.displayNameToken, survivorDef.cachedName);
            }
            return "-";
        }

        private void BuildLobbyTriggerButton()
        {
            var rootCanvas = ResolveHostCanvas();
            Transform hostParent = rootCanvas != null ? rootCanvas.transform : _lobby.transform;
            UILayerKey lobbyLayer = FindLobbyLayerKey();

            _triggerButton = UiFactory.CreateButton(
                "CENIRELOAD_OpenVaultButton",
                hostParent,
                CeniReloadStrings.Get("CENIRELOAD_BTN_OPEN_VAULT"),
                new Color(0.08f, 0.24f, 0.36f, 0.96f),
                UiFactory.AccentCyan,
                14f,
                () => SetModalOpen(true),
                out _triggerLabel,
                lobbyLayer
            );

            if (_triggerLabel != null)
            {
                _triggerLabel.enableAutoSizing = true;
                _triggerLabel.fontSizeMin = 10f;
                _triggerLabel.fontSizeMax = 15f;
                _triggerLabel.enableWordWrapping = false;
            }

            ApplyFallbackBottomRightDock(_triggerButton.GetComponent<RectTransform>(), hostParent as RectTransform);

            var outline = _triggerButton.gameObject.GetComponent<Outline>();
            if (outline != null) outline.effectColor = UiFactory.AccentCyan;

            ScheduleLobbyAlignment();
        }

        private void BuildMainMenuTriggerButton()
        {
            if (_mainMenu == null) return;

            Transform panelParent = null;
            if (_mainMenu.mainMenuButtonPanel != null)
            {
                panelParent = _mainMenu.mainMenuButtonPanel.transform;
            }
            else if (_mainMenu.singlePlayerButton != null)
            {
                panelParent = _mainMenu.singlePlayerButton.transform.parent;
            }

            if (panelParent == null)
            {
                panelParent = _mainMenu.transform;
            }

            Transform existingBtn = panelParent.Find("CENIRELOAD_MainMenuVaultButton");
            if (existingBtn != null)
            {
                existingBtn.gameObject.SetActive(false);
                existingBtn.SetParent(null, false);
                Destroy(existingBtn.gameObject);
            }

            UILayerKey mainMenuLayer = FindMainMenuLayerKey();

            _triggerButton = UiFactory.CreateButton(
                "CENIRELOAD_MainMenuVaultButton",
                panelParent,
                CeniReloadStrings.Get("CENIRELOAD_BTN_OPEN_VAULT"),
                new Color(0.08f, 0.22f, 0.34f, 0.96f),
                UiFactory.AccentCyan,
                18f,
                () => SetModalOpen(true),
                out _triggerLabel,
                mainMenuLayer
            );

            var outline = _triggerButton.gameObject.GetComponent<Outline>();
            if (outline != null) outline.effectColor = UiFactory.AccentCyan;

            var spButton = _mainMenu.singlePlayerButton;
            var trigRect = _triggerButton.GetComponent<RectTransform>();
            var layoutElem = _triggerButton.gameObject.AddComponent<LayoutElement>();

            if (spButton != null)
            {
                if (spButton.transform.parent == panelParent)
                {
                    _triggerButton.transform.SetSiblingIndex(spButton.transform.GetSiblingIndex() + 1);
                }

                var spLayout = spButton.GetComponent<LayoutElement>();
                var spRect = spButton.GetComponent<RectTransform>();
                if (spLayout != null)
                {
                    layoutElem.minHeight = spLayout.minHeight;
                    layoutElem.preferredHeight = spLayout.preferredHeight;
                    layoutElem.flexibleHeight = spLayout.flexibleHeight;
                    layoutElem.minWidth = spLayout.minWidth;
                    layoutElem.preferredWidth = spLayout.preferredWidth;
                    layoutElem.flexibleWidth = spLayout.flexibleWidth;
                    layoutElem.ignoreLayout = spLayout.ignoreLayout;
                }
                else
                {
                    float refHeight = spRect != null && spRect.rect.height >= 24f ? spRect.rect.height : 46f;
                    layoutElem.minHeight = refHeight;
                    layoutElem.preferredHeight = refHeight;
                    layoutElem.flexibleWidth = 1f;
                }

                if (spRect != null && trigRect != null)
                {
                    trigRect.sizeDelta = spRect.sizeDelta.y >= 24f ? spRect.sizeDelta : new Vector2(300f, 46f);
                }

                var spLabel = spButton.GetComponentInChildren<TextMeshProUGUI>(true);
                if (spLabel != null && _triggerLabel != null)
                {
                    if (spLabel.font != null) _triggerLabel.font = spLabel.font;
                    _triggerLabel.alignment = spLabel.alignment;
                    _triggerLabel.fontSize = Mathf.Clamp(spLabel.fontSize, 14f, 22f);
                    UiFactory.StretchFull(_triggerLabel.rectTransform, 16f, 4f);
                }

                // Wire explicit gamepad navigation if Main Menu uses explicit mode
                var spNav = spButton.navigation;
                if (spNav.mode == Navigation.Mode.Explicit)
                {
                    Selectable nextDown = spNav.selectOnDown;
                    spNav.selectOnDown = _triggerButton;
                    spButton.navigation = spNav;

                    var trigNav = _triggerButton.navigation;
                    trigNav.mode = Navigation.Mode.Explicit;
                    trigNav.selectOnUp = spButton;
                    trigNav.selectOnDown = nextDown;
                    _triggerButton.navigation = trigNav;

                    if (nextDown != null)
                    {
                        var nextNav = nextDown.navigation;
                        if (nextNav.mode == Navigation.Mode.Explicit)
                        {
                            nextNav.selectOnUp = _triggerButton;
                            nextDown.navigation = nextNav;
                        }
                    }
                }
            }
            else
            {
                layoutElem.minHeight = 46f;
                layoutElem.preferredHeight = 46f;
                layoutElem.flexibleWidth = 1f;
                if (trigRect != null)
                {
                    trigRect.sizeDelta = new Vector2(300f, 46f);
                }
            }

            if (_triggerLabel != null)
            {
                _triggerLabel.enableAutoSizing = true;
                _triggerLabel.fontSizeMin = 12f;
                _triggerLabel.fontSizeMax = 22f;
                _triggerLabel.enableWordWrapping = false;
            }
        }

        private void ApplyFallbackBottomRightDock(RectTransform trigRect, RectTransform canvasRect)
        {
            if (trigRect == null) return;
            if (canvasRect != null && trigRect.parent != canvasRect)
            {
                trigRect.SetParent(canvasRect, false);
            }

            trigRect.anchorMin = new Vector2(1f, 0f);
            trigRect.anchorMax = new Vector2(1f, 0f);
            trigRect.pivot = new Vector2(1f, 0f);
            trigRect.sizeDelta = new Vector2(180f, 38f);
            trigRect.anchoredPosition = new Vector2(-58f, 72f);
            trigRect.localScale = Vector3.one;
            trigRect.localRotation = Quaternion.identity;
        }

        private void ApplyCachedDock(RectTransform trigRect, RectTransform canvasRect)
        {
            if (trigRect == null || canvasRect == null) return;
            if (trigRect.parent != canvasRect)
            {
                trigRect.SetParent(canvasRect, false);
            }

            trigRect.anchorMin = new Vector2(0.5f, 0.5f);
            trigRect.anchorMax = new Vector2(0.5f, 0.5f);
            trigRect.pivot = new Vector2(0.5f, 0f);
            trigRect.sizeDelta = _cachedDockSize;
            trigRect.localPosition = _cachedDockLocalPos;
            trigRect.localScale = Vector3.one;
            trigRect.localRotation = Quaternion.identity;
        }

        private IEnumerator AlignTriggerAboveBackButton()
        {
            yield return null;
            yield return null;

            _alignCoroutine = null;
            if (_triggerButton == null || _lobby == null || _isMainMenuMode) yield break;

            if (_triggerButton.requiredTopLayer == null)
            {
                var lobbyLayer = FindLobbyLayerKey();
                if (lobbyLayer != null)
                {
                    _triggerButton.requiredTopLayer = lobbyLayer;
                }
            }

            var rootCanvas = ResolveHostCanvas();
            if (rootCanvas == null) yield break;

            var trigRect = _triggerButton.GetComponent<RectTransform>();
            var canvasRect = rootCanvas.transform as RectTransform;
            if (trigRect == null || canvasRect == null) yield break;

            RectTransform backRect = FindBackButtonRect(rootCanvas.transform, canvasRect);
            if (backRect != null)
            {
                Vector3[] corners = new Vector3[4];
                backRect.GetWorldCorners(corners);
                // corners: 0=bottom-left, 1=top-left, 2=top-right, 3=bottom-right
                Vector3 bl = canvasRect.InverseTransformPoint(corners[0]);
                Vector3 tl = canvasRect.InverseTransformPoint(corners[1]);
                Vector3 tr = canvasRect.InverseTransformPoint(corners[2]);

                float width = Mathf.Abs(tr.x - tl.x);
                float height = Mathf.Abs(tl.y - bl.y);
                bool hasNonZeroExtent = width >= 40f && height >= 16f &&
                                        (Mathf.Abs(tl.x) > 0.01f || Mathf.Abs(tl.y) > 0.01f || Mathf.Abs(tr.x) > 0.01f);

                if (hasNonZeroExtent)
                {
                    const float verticalGap = 4f;
                    Vector2 candidateSize = new Vector2(width, height);
                    Vector3 candidatePos = new Vector3((tl.x + tr.x) * 0.5f, tl.y + verticalGap, 0f);
                    Rect candidateLocalRect = new Rect(
                        candidatePos.x - candidateSize.x * 0.5f,
                        candidatePos.y,
                        candidateSize.x,
                        candidateSize.y
                    );

                    if (!OverlapsReadyButton(canvasRect, candidateLocalRect))
                    {
                        _hasCachedDock = true;
                        _cachedDockSize = candidateSize;
                        _cachedDockLocalPos = candidatePos;
                        ApplyCachedDock(trigRect, canvasRect);
                        yield break;
                    }
                }
            }

            if (_hasCachedDock)
            {
                ApplyCachedDock(trigRect, canvasRect);
            }
            else
            {
                ApplyFallbackBottomRightDock(trigRect, canvasRect);
            }
        }

        private bool OverlapsReadyButton(RectTransform canvasRect, Rect candidateLocalRect)
        {
            if (_lobby == null || _lobby.readyButton == null || !_lobby.readyButton.gameObject.activeInHierarchy)
                return false;

            var readyRect = _lobby.readyButton.GetComponent<RectTransform>();
            if (readyRect == null) return false;

            Vector3[] corners = new Vector3[4];
            readyRect.GetWorldCorners(corners);
            Vector3 bl = canvasRect.InverseTransformPoint(corners[0]);
            Vector3 tr = canvasRect.InverseTransformPoint(corners[2]);

            float minX = Mathf.Min(bl.x, tr.x);
            float maxX = Mathf.Max(bl.x, tr.x);
            float minY = Mathf.Min(bl.y, tr.y);
            float maxY = Mathf.Max(bl.y, tr.y);
            if ((maxX - minX) < 10f || (maxY - minY) < 10f) return false;

            Rect readyLocalRect = Rect.MinMaxRect(minX, minY, maxX, maxY);
            return candidateLocalRect.Overlaps(readyLocalRect);
        }

        private bool IsDisallowedDockButton(Button btn)
        {
            if (btn == null || btn == _triggerButton) return true;
            if (_lobby != null)
            {
                if (_lobby.readyButton != null && btn == _lobby.readyButton) return true;
                if (_lobby.unreadyButton != null && btn == _lobby.unreadyButton) return true;
            }

            string name = btn.name ?? string.Empty;
            if (name.StartsWith("CENIRELOAD_", StringComparison.Ordinal)) return true;
            if (name.IndexOf("Ready", StringComparison.OrdinalIgnoreCase) >= 0) return true;
            if (name.IndexOf("Tutorial", StringComparison.OrdinalIgnoreCase) >= 0) return true;
            return false;
        }

        private static string StripRichTextTags(string input)
        {
            if (string.IsNullOrEmpty(input)) return string.Empty;
            return Regex.Replace(input, "<[^>]+>", string.Empty).Trim();
        }

        private bool IsBackButtonCandidate(Button btn, string localizedBack)
        {
            var tmp = btn.GetComponentInChildren<TextMeshProUGUI>(true);
            if (tmp != null && !string.IsNullOrEmpty(tmp.text))
            {
                string txt = StripRichTextTags(tmp.text);
                if (txt.Equals(localizedBack, StringComparison.OrdinalIgnoreCase) ||
                    txt.Equals("Voltar", StringComparison.OrdinalIgnoreCase) ||
                    txt.Equals("Back", StringComparison.OrdinalIgnoreCase) ||
                    txt.Equals("Cancelar", StringComparison.OrdinalIgnoreCase) ||
                    txt.Equals("Cancel", StringComparison.OrdinalIgnoreCase))
                {
                    return true;
                }
            }

            string btnName = btn.name ?? string.Empty;
            return btnName.IndexOf("Back", StringComparison.OrdinalIgnoreCase) >= 0;
        }

        private RectTransform FindBackButtonRect(Transform root, RectTransform canvasRect)
        {
            string localizedBack = StripRichTextTags(RoR2.Language.GetString("BACK") ?? "Back");
            var buttons = root.GetComponentsInChildren<Button>(true);

            RectTransform activeBackMatch = null;
            RectTransform inactiveBackMatch = null;
            RectTransform footerFallback = null;

            for (int i = 0; i < buttons.Length; i++)
            {
                var btn = buttons[i];
                if (IsDisallowedDockButton(btn)) continue;

                var rt = btn.GetComponent<RectTransform>();
                if (rt == null) continue;

                if (IsBackButtonCandidate(btn, localizedBack))
                {
                    if (btn.gameObject.activeInHierarchy && rt.rect.width >= 40f && rt.rect.height >= 16f)
                    {
                        activeBackMatch = rt;
                        break;
                    }

                    if (inactiveBackMatch == null)
                    {
                        inactiveBackMatch = rt;
                    }
                    continue;
                }

                // Secondary search inside FooterPanel / BackPanel only for non-Ready/non-Tutorial buttons in bottom-right
                if (footerFallback == null && IsInsideFooterDock(btn.transform) &&
                    rt.rect.width >= 40f && rt.rect.height >= 16f &&
                    IsInBottomRightQuadrant(rt, canvasRect))
                {
                    footerFallback = rt;
                }
            }

            if (activeBackMatch != null) return activeBackMatch;
            if (inactiveBackMatch != null) return inactiveBackMatch;
            return footerFallback;
        }

        private static bool IsInBottomRightQuadrant(RectTransform rt, RectTransform canvasRect)
        {
            if (rt == null || canvasRect == null) return false;
            Vector3[] corners = new Vector3[4];
            rt.GetWorldCorners(corners);
            Vector3 center = canvasRect.InverseTransformPoint((corners[0] + corners[2]) * 0.5f);
            return center.x >= 0f && center.y <= 0f;
        }

        private static bool IsInsideFooterDock(Transform t)
        {
            Transform cur = t != null ? t.parent : null;
            int depth = 0;
            while (cur != null && depth < 5)
            {
                string n = cur.name ?? string.Empty;
                if (n.IndexOf("FooterPanel", StringComparison.OrdinalIgnoreCase) >= 0 ||
                    n.IndexOf("BackPanel", StringComparison.OrdinalIgnoreCase) >= 0)
                {
                    return true;
                }
                cur = cur.parent;
                depth++;
            }
            return false;
        }

        private void BuildVaultModal()
        {
            var rootCanvas = ResolveHostCanvas();
            Transform hostParent = rootCanvas != null
                ? rootCanvas.transform
                : (_isMainMenuMode ? _mainMenu.transform : _lobby.transform);

            var backdrop = UiFactory.CreatePanel("CENIRELOAD_VaultModalBackdrop", hostParent, UiFactory.BackdropColor);
            UiFactory.StretchFull(backdrop.rectTransform);
            _modalRoot = backdrop.gameObject;

            // Keep inactive while attaching MPEventSystemProvider, UILayerKey, and child MPButtons
            _modalRoot.SetActive(false);

            var esProvider = _modalRoot.AddComponent<MPEventSystemProvider>();
            esProvider.fallBackToMainEventSystem = true;

            _modalLayer = ScriptableObject.CreateInstance<RoR2.UILayer>();
            _modalLayer.priority = 100;

            _modalLayerKey = _modalRoot.AddComponent<UILayerKey>();
            _modalLayerKey.layer = _modalLayer;
            if (_modalLayerKey.onBeginRepresentTopLayer == null)
                _modalLayerKey.onBeginRepresentTopLayer = new UnityEvent();
            if (_modalLayerKey.onEndRepresentTopLayer == null)
                _modalLayerKey.onEndRepresentTopLayer = new UnityEvent();
            _modalLayerKey.enabled = false;

            var window = UiFactory.CreatePanel("CENIRELOAD_VaultWindow", backdrop.transform, UiFactory.PanelBgColor, UiFactory.AccentCyan);
            var winRect = window.rectTransform;
            winRect.anchorMin = new Vector2(0.5f, 0.5f);
            winRect.anchorMax = new Vector2(0.5f, 0.5f);
            winRect.pivot = new Vector2(0.5f, 0.5f);
            winRect.sizeDelta = new Vector2(960f, 620f);

            var header = UiFactory.CreatePanel("HeaderBar", window.transform, new Color(0.04f, 0.07f, 0.12f, 1f));
            var headerRect = header.rectTransform;
            headerRect.anchorMin = new Vector2(0f, 1f);
            headerRect.anchorMax = new Vector2(1f, 1f);
            headerRect.pivot = new Vector2(0.5f, 1f);
            headerRect.sizeDelta = new Vector2(0f, 82f);

            _titleText = UiFactory.CreateText("Title", header.transform, CeniReloadStrings.Get("CENIRELOAD_UI_TITLE"), 22f, UiFactory.AccentCyan, TextAlignmentOptions.Left, FontStyles.Bold);
            _titleText.rectTransform.anchorMin = new Vector2(0f, 0.5f);
            _titleText.rectTransform.anchorMax = new Vector2(0.58f, 1f);
            _titleText.rectTransform.offsetMin = new Vector2(24f, 2f);
            _titleText.rectTransform.offsetMax = new Vector2(0f, -8f);

            _subtitleText = UiFactory.CreateText("Subtitle", header.transform, CeniReloadStrings.Get("CENIRELOAD_UI_SUBTITLE"), 14f, UiFactory.TextMuted, TextAlignmentOptions.Left);
            _subtitleText.rectTransform.anchorMin = new Vector2(0f, 0f);
            _subtitleText.rectTransform.anchorMax = new Vector2(0.58f, 0.5f);
            _subtitleText.rectTransform.offsetMin = new Vector2(24f, 8f);
            _subtitleText.rectTransform.offsetMax = new Vector2(0f, 0f);

            _filterButton = UiFactory.CreateButton(
                "FilterButton",
                header.transform,
                "",
                new Color(0.12f, 0.20f, 0.32f, 1f),
                UiFactory.TextPrimary,
                14f,
                ToggleFilterMode,
                out _filterButtonLabel,
                _modalLayerKey
            );
            var filterRect = _filterButton.GetComponent<RectTransform>();
            filterRect.anchorMin = new Vector2(1f, 0.5f);
            filterRect.anchorMax = new Vector2(1f, 0.5f);
            filterRect.pivot = new Vector2(1f, 0.5f);
            filterRect.sizeDelta = new Vector2(240f, 38f);
            filterRect.anchoredPosition = new Vector2(-126f, 0f);

            _closeButton = UiFactory.CreateButton(
                "CloseButton",
                header.transform,
                CeniReloadStrings.Get("CENIRELOAD_UI_CLOSE"),
                new Color(0.32f, 0.14f, 0.18f, 1f),
                UiFactory.TextPrimary,
                15f,
                () => SetModalOpen(false),
                out _closeButtonLabel,
                _modalLayerKey
            );
            var closeRect = _closeButton.GetComponent<RectTransform>();
            closeRect.anchorMin = new Vector2(1f, 0.5f);
            closeRect.anchorMax = new Vector2(1f, 0.5f);
            closeRect.pivot = new Vector2(1f, 0.5f);
            closeRect.sizeDelta = new Vector2(96f, 38f);
            closeRect.anchoredPosition = new Vector2(-20f, 0f);

            var leftPanel = UiFactory.CreatePanel("LeftSlotList", window.transform, UiFactory.SubPanelBgColor);
            var leftRect = leftPanel.rectTransform;
            leftRect.anchorMin = new Vector2(0f, 0f);
            leftRect.anchorMax = new Vector2(0.54f, 1f);
            leftRect.offsetMin = new Vector2(20f, 40f);
            leftRect.offsetMax = new Vector2(-10f, -98f);

            _scrollRect = leftPanel.gameObject.AddComponent<ScrollRect>();
            _scrollRect.horizontal = false;
            _scrollRect.vertical = true;
            _scrollRect.scrollSensitivity = 28f;

            var viewport = UiFactory.CreateRect("Viewport", leftPanel.transform);
            UiFactory.StretchFull(viewport, 8f, 8f);
            var maskImg = viewport.gameObject.AddComponent<Image>();
            maskImg.color = new Color(1f, 1f, 1f, 0.01f);
            var mask = viewport.gameObject.AddComponent<Mask>();
            mask.showMaskGraphic = false;

            var content = UiFactory.CreateRect("Content", viewport);
            content.anchorMin = new Vector2(0f, 1f);
            content.anchorMax = new Vector2(1f, 1f);
            content.pivot = new Vector2(0.5f, 1f);
            content.sizeDelta = new Vector2(0f, 0f);

            var vlg = content.gameObject.AddComponent<VerticalLayoutGroup>();
            vlg.spacing = 10f;
            vlg.padding = new RectOffset(4, 4, 4, 4);
            vlg.childControlWidth = true;
            vlg.childControlHeight = false;
            vlg.childForceExpandWidth = true;
            vlg.childForceExpandHeight = false;

            var csf = content.gameObject.AddComponent<ContentSizeFitter>();
            csf.verticalFit = ContentSizeFitter.FitMode.PreferredSize;

            _scrollRect.viewport = viewport;
            _scrollRect.content = content;
            _cardsContainer = content;

            var rightPanel = UiFactory.CreatePanel("RightInspector", window.transform, UiFactory.SubPanelBgColor);
            var rightRect = rightPanel.rectTransform;
            rightRect.anchorMin = new Vector2(0.54f, 0f);
            rightRect.anchorMax = new Vector2(1f, 1f);
            rightRect.offsetMin = new Vector2(10f, 40f);
            rightRect.offsetMax = new Vector2(-20f, -98f);

            _inspectorPortraitFrame = UiFactory.CreatePortraitFrame("InspectorPortrait", rightPanel.transform, null, UiFactory.AccentCyan);
            var inspPortRect = _inspectorPortraitFrame.rectTransform;
            inspPortRect.anchorMin = new Vector2(0f, 1f);
            inspPortRect.anchorMax = new Vector2(0f, 1f);
            inspPortRect.pivot = new Vector2(0f, 1f);
            inspPortRect.sizeDelta = new Vector2(48f, 48f);
            inspPortRect.anchoredPosition = new Vector2(18f, -14f);

            _inspectorTitle = UiFactory.CreateText("InspectorTitle", rightPanel.transform, "", 19f, UiFactory.AccentCyan, TextAlignmentOptions.MidlineLeft, FontStyles.Bold);
            _inspectorTitle.enableAutoSizing = true;
            _inspectorTitle.fontSizeMin = 13f;
            _inspectorTitle.fontSizeMax = 19f;
            _inspectorTitle.rectTransform.anchorMin = new Vector2(0f, 1f);
            _inspectorTitle.rectTransform.anchorMax = new Vector2(1f, 1f);
            _inspectorTitle.rectTransform.pivot = new Vector2(0f, 1f);
            _inspectorTitle.rectTransform.offsetMin = new Vector2(76f, -62f);
            _inspectorTitle.rectTransform.offsetMax = new Vector2(-18f, -14f);

            _inspectorBody = UiFactory.CreateText("InspectorBody", rightPanel.transform, "", 14.5f, UiFactory.TextPrimary, TextAlignmentOptions.TopLeft);
            _inspectorBody.rectTransform.anchorMin = new Vector2(0f, 0f);
            _inspectorBody.rectTransform.anchorMax = new Vector2(1f, 1f);
            _inspectorBody.rectTransform.offsetMin = new Vector2(18f, 166f);
            _inspectorBody.rectTransform.offsetMax = new Vector2(-18f, -72f);

            _inspectorModStatus = UiFactory.CreateText("ModStatus", rightPanel.transform, "", 13f, UiFactory.AccentGreen, TextAlignmentOptions.Left, FontStyles.Italic);
            _inspectorModStatus.enableAutoSizing = true;
            _inspectorModStatus.fontSizeMin = 10.5f;
            _inspectorModStatus.fontSizeMax = 13.5f;
            _inspectorModStatus.rectTransform.anchorMin = new Vector2(0f, 0f);
            _inspectorModStatus.rectTransform.anchorMax = new Vector2(1f, 0f);
            _inspectorModStatus.rectTransform.pivot = new Vector2(0.5f, 0f);
            _inspectorModStatus.rectTransform.sizeDelta = new Vector2(-36f, 50f);
            _inspectorModStatus.rectTransform.anchoredPosition = new Vector2(0f, 112f);

            _loadButton = UiFactory.CreateButton(
                "LoadCheckpointButton",
                rightPanel.transform,
                CeniReloadStrings.Get("CENIRELOAD_UI_BTN_LOAD"),
                new Color(0.10f, 0.45f, 0.62f, 1f),
                Color.white,
                16f,
                OnClickLoadSelected,
                out _loadButtonLabel,
                _modalLayerKey
            );
            var loadRect = _loadButton.GetComponent<RectTransform>();
            loadRect.anchorMin = new Vector2(0f, 0f);
            loadRect.anchorMax = new Vector2(1f, 0f);
            loadRect.pivot = new Vector2(0.5f, 0f);
            loadRect.sizeDelta = new Vector2(-36f, 44f);
            loadRect.anchoredPosition = new Vector2(0f, 62f);

            _deleteButton = UiFactory.CreateButton(
                "DeleteSlotButton",
                rightPanel.transform,
                CeniReloadStrings.Get("CENIRELOAD_UI_BTN_DELETE"),
                new Color(0.45f, 0.14f, 0.18f, 1f),
                Color.white,
                14f,
                () => SetDeleteConfirmVisible(true),
                out _deleteButtonLabel,
                _modalLayerKey
            );
            var delRect = _deleteButton.GetComponent<RectTransform>();
            delRect.anchorMin = new Vector2(0f, 0f);
            delRect.anchorMax = new Vector2(1f, 0f);
            delRect.pivot = new Vector2(0.5f, 0f);
            delRect.sizeDelta = new Vector2(-36f, 36f);
            delRect.anchoredPosition = new Vector2(0f, 16f);

            var confirmPanel = UiFactory.CreatePanel("DeleteConfirmBar", rightPanel.transform, new Color(0.28f, 0.08f, 0.10f, 1f), UiFactory.AccentRed);
            var confRect = confirmPanel.rectTransform;
            confRect.anchorMin = new Vector2(0f, 0f);
            confRect.anchorMax = new Vector2(1f, 0f);
            confRect.pivot = new Vector2(0.5f, 0f);
            confRect.sizeDelta = new Vector2(-36f, 40f);
            confRect.anchoredPosition = new Vector2(0f, 14f);
            _deleteConfirmBar = confirmPanel.gameObject;

            _confirmPromptText = UiFactory.CreateText("ConfirmText", confirmPanel.transform, CeniReloadStrings.Get("CENIRELOAD_UI_CONFIRM_DELETE"), 13f, Color.white, TextAlignmentOptions.Left, FontStyles.Bold);
            _confirmPromptText.rectTransform.anchorMin = new Vector2(0f, 0f);
            _confirmPromptText.rectTransform.anchorMax = new Vector2(0.46f, 1f);
            _confirmPromptText.rectTransform.offsetMin = new Vector2(10f, 2f);
            _confirmPromptText.rectTransform.offsetMax = new Vector2(0f, -2f);

            _confirmYesButton = UiFactory.CreateButton(
                "ConfirmYes",
                confirmPanel.transform,
                CeniReloadStrings.Get("CENIRELOAD_UI_BTN_CONFIRM_YES"),
                UiFactory.AccentRed,
                Color.white,
                12f,
                OnConfirmDeleteSelected,
                out _confirmYesLabel,
                _modalLayerKey
            );
            var yesRect = _confirmYesButton.GetComponent<RectTransform>();
            yesRect.anchorMin = new Vector2(0.47f, 0.12f);
            yesRect.anchorMax = new Vector2(0.73f, 0.88f);
            yesRect.offsetMin = Vector2.zero;
            yesRect.offsetMax = Vector2.zero;

            _confirmNoButton = UiFactory.CreateButton(
                "ConfirmNo",
                confirmPanel.transform,
                CeniReloadStrings.Get("CENIRELOAD_UI_BTN_CONFIRM_NO"),
                new Color(0.22f, 0.26f, 0.34f, 1f),
                Color.white,
                12f,
                () => SetDeleteConfirmVisible(false),
                out _confirmNoLabel,
                _modalLayerKey
            );
            var noRect = _confirmNoButton.GetComponent<RectTransform>();
            noRect.anchorMin = new Vector2(0.75f, 0.12f);
            noRect.anchorMax = new Vector2(0.98f, 0.88f);
            noRect.offsetMin = Vector2.zero;
            noRect.offsetMax = Vector2.zero;

            _deleteConfirmBar.SetActive(false);

            var footerBar = UiFactory.CreatePanel("FooterHintBar", window.transform, new Color(0.04f, 0.06f, 0.11f, 0.96f));
            var footerRect = footerBar.rectTransform;
            footerRect.anchorMin = new Vector2(0f, 0f);
            footerRect.anchorMax = new Vector2(1f, 0f);
            footerRect.pivot = new Vector2(0.5f, 0f);
            footerRect.sizeDelta = new Vector2(0f, 28f);

            _footerHintText = UiFactory.CreateText(
                "FooterHintText",
                footerBar.transform,
                CeniReloadStrings.Get("CENIRELOAD_UI_HINT_CLOSE"),
                12f,
                UiFactory.TextMuted,
                TextAlignmentOptions.Center
            );
            UiFactory.StretchFull(_footerHintText.rectTransform, 16f, 2f);
        }

        private void SetModalOpen(bool open)
        {
            if (_modalRoot == null) return;

            if (open)
            {
                _openedFrame = Time.frameCount;
                _modalRoot.SetActive(true);
                if (_modalLayerKey != null) _modalLayerKey.enabled = true;
                _modalRoot.transform.SetAsLastSibling();
                RefreshLocalization();
                RebuildSlotCards();
            }
            else
            {
                bool wasOpen = _modalRoot.activeSelf;
                if (_modalLayerKey != null) _modalLayerKey.enabled = false;
                _modalRoot.SetActive(false);

                if (wasOpen)
                {
                    _closedFrame = Time.frameCount;
                    if (_triggerButton != null && _triggerButton.gameObject.activeInHierarchy && _triggerButton.interactable)
                    {
                        SetSelectedGameObject(_triggerButton.gameObject);
                    }
                }
            }
        }

        private void ToggleFilterMode()
        {
            if (_isMainMenuMode && string.IsNullOrEmpty(GetSelectedSurvivorBodyName()))
            {
                _showAllSurvivors = true;
            }
            else
            {
                _showAllSurvivors = !_showAllSurvivors;
            }

            _selectedSnapshot = null;
            RefreshLocalization();
            RebuildSlotCards();
        }

        private void RefreshLocalization()
        {
            RefreshTriggerLabel();
            _titleText.text = CeniReloadStrings.Get("CENIRELOAD_UI_TITLE");
            _subtitleText.text = CeniReloadStrings.Get("CENIRELOAD_UI_SUBTITLE");
            _closeButtonLabel.text = CeniReloadStrings.Get("CENIRELOAD_UI_CLOSE");
            _loadButtonLabel.text = CeniReloadStrings.Get("CENIRELOAD_UI_BTN_LOAD");
            _deleteButtonLabel.text = CeniReloadStrings.Get("CENIRELOAD_UI_BTN_DELETE");
            _confirmPromptText.text = CeniReloadStrings.Get("CENIRELOAD_UI_CONFIRM_DELETE");
            _confirmYesLabel.text = CeniReloadStrings.Get("CENIRELOAD_UI_BTN_CONFIRM_YES");
            _confirmNoLabel.text = CeniReloadStrings.Get("CENIRELOAD_UI_BTN_CONFIRM_NO");
            if (_footerHintText != null)
            {
                _footerHintText.text = CeniReloadStrings.Get("CENIRELOAD_UI_HINT_CLOSE");
            }

            _filterButtonLabel.text = _showAllSurvivors
                ? CeniReloadStrings.Get("CENIRELOAD_UI_FILTER_ALL")
                : CeniReloadStrings.Format("CENIRELOAD_UI_FILTER_SURVIVOR", GetSelectedSurvivorDisplayName());
        }

        private void RebuildSlotCards()
        {
            foreach (var card in _spawnedCards)
            {
                if (card != null)
                {
                    card.SetActive(false);
                    card.transform.SetParent(null, false);
                    Destroy(card);
                }
            }
            _spawnedCards.Clear();

            string profileKey = VaultRepository.GetCurrentProfileKey();
            if (_selectedSnapshot != null && !IsSnapshotStillValid(_selectedSnapshot, profileKey))
            {
                _selectedSnapshot = null;
            }

            MPButton firstSelectable = null;
            MPButton selectedCardButton = null;
            CheckpointSnapshot firstPopulated = null;

            if (_showAllSurvivors)
            {
                var all = VaultRepository.GetAllProfileSnapshots(profileKey);
                if (all.Count == 0)
                {
                    CreateEmptyNoticeCard();
                }
                else
                {
                    if (_selectedSnapshot == null)
                    {
                        _selectedSnapshot = all[0];
                    }

                    for (int i = 0; i < all.Count; i++)
                    {
                        var snap = all[i];
                        var btn = CreateSlotCard(snap.slotIndex, snap);
                        if (firstSelectable == null) firstSelectable = btn;
                        if (firstPopulated == null) firstPopulated = snap;
                        if (IsSameSnapshot(snap, _selectedSnapshot))
                        {
                            selectedCardButton = btn;
                        }
                    }
                }
            }
            else
            {
                string bodyName = GetSelectedSurvivorBodyName();
                var slots = VaultRepository.GetSurvivorSlots(profileKey, bodyName);

                if (_selectedSnapshot == null)
                {
                    for (int i = 0; i < VaultRepository.MaxSlotsPerSurvivor; i++)
                    {
                        if (slots[i] != null)
                        {
                            _selectedSnapshot = slots[i];
                            break;
                        }
                    }
                }

                for (int i = 0; i < VaultRepository.MaxSlotsPerSurvivor; i++)
                {
                    var snap = slots[i];
                    var btn = CreateSlotCard(i + 1, snap);
                    if (snap != null && firstPopulated == null)
                    {
                        firstPopulated = snap;
                        firstSelectable = btn;
                    }
                    else if (firstSelectable == null)
                    {
                        firstSelectable = btn;
                    }

                    if (snap != null && IsSameSnapshot(snap, _selectedSnapshot))
                    {
                        selectedCardButton = btn;
                    }
                }
            }

            if (_selectedSnapshot == null)
            {
                _selectedSnapshot = firstPopulated;
            }

            UpdateInspectorPanel();

            var focusTarget = selectedCardButton ?? firstSelectable ?? _closeButton;
            if (focusTarget != null)
            {
                SetSelectedGameObject(focusTarget.gameObject);
                if (isActiveAndEnabled && _modalRoot != null && _modalRoot.activeInHierarchy)
                {
                    if (_focusCoroutine != null) StopCoroutine(_focusCoroutine);
                    _focusCoroutine = StartCoroutine(EnsureFocusNextFrame(focusTarget.gameObject));
                }
            }
        }

        private IEnumerator EnsureFocusNextFrame(GameObject target)
        {
            yield return null;
            _focusCoroutine = null;
            if (target != null && target.activeInHierarchy && _modalRoot != null && _modalRoot.activeInHierarchy)
            {
                SetSelectedGameObject(target);
            }
        }

        private static void SetSelectedGameObject(GameObject target)
        {
            if (target == null) return;
            EventSystem es = MPEventSystemManager.primaryEventSystem;
            if (es == null) es = EventSystem.current;
            if (es != null)
            {
                es.SetSelectedGameObject(target);
            }
        }

        private static bool IsSameSnapshot(CheckpointSnapshot a, CheckpointSnapshot b)
        {
            if (a == null || b == null) return false;
            return a.slotIndex == b.slotIndex &&
                   string.Equals(a.survivorBodyName, b.survivorBodyName, StringComparison.Ordinal);
        }

        private bool IsSnapshotStillValid(CheckpointSnapshot snap, string profileKey)
        {
            if (snap == null) return false;
            return VaultRepository.LoadSlot(profileKey, snap.survivorBodyName, snap.slotIndex) != null;
        }

        private void CreateEmptyNoticeCard()
        {
            var panel = UiFactory.CreatePanel("EmptyNotice", _cardsContainer, UiFactory.CardEmptyColor);
            var le = panel.gameObject.AddComponent<LayoutElement>();
            le.preferredHeight = 76f;

            var txt = UiFactory.CreateText("NoticeText", panel.transform, CeniReloadStrings.Get("CENIRELOAD_UI_NO_SAVES"), 15f, UiFactory.TextMuted, TextAlignmentOptions.Center);
            UiFactory.StretchFull(txt.rectTransform, 14f, 10f);
            _spawnedCards.Add(panel.gameObject);
        }

        private MPButton CreateSlotCard(int slotNumber, CheckpointSnapshot snapshot)
        {
            bool hasData = snapshot != null;
            bool isSelected = hasData && IsSameSnapshot(snapshot, _selectedSnapshot);
            bool missingSurvivor = hasData && snapshot.HasMissingSurvivors();
            bool modMismatch = hasData && snapshot.HasModMismatch();

            Color bg = !hasData ? UiFactory.CardEmptyColor : (isSelected ? UiFactory.CardSelectedColor : UiFactory.CardBgColor);
            Color accent = !hasData
                ? new Color(0.28f, 0.34f, 0.44f, 0.7f)
                : (missingSurvivor ? UiFactory.AccentRed : (modMismatch ? UiFactory.AccentAmber : UiFactory.AccentCyan));

            Color borderColor = isSelected
                ? (missingSurvivor ? UiFactory.AccentRed : UiFactory.AccentCyan)
                : (missingSurvivor ? new Color(0.88f, 0.28f, 0.32f, 0.50f) : new Color(1f, 1f, 1f, 0.08f));

            var cardImg = UiFactory.CreatePanel($"CENIRELOAD_SlotCard_{slotNumber}", _cardsContainer, bg, borderColor);
            var le = cardImg.gameObject.AddComponent<LayoutElement>();
            le.preferredHeight = 84f;

            var strip = UiFactory.CreatePanel("AccentStrip", cardImg.transform, accent);
            strip.raycastTarget = false;
            strip.rectTransform.anchorMin = new Vector2(0f, 0f);
            strip.rectTransform.anchorMax = new Vector2(0f, 1f);
            strip.rectTransform.pivot = new Vector2(0f, 0.5f);
            strip.rectTransform.sizeDelta = new Vector2(6f, 0f);

            var btn = cardImg.gameObject.AddComponent<MPButton>();
            btn.targetGraphic = cardImg;
            btn.allowAllEventSystems = true;
            btn.requiredTopLayer = _modalLayerKey;
            btn.defaultFallbackButton = false;

            var colors = btn.colors;
            colors.normalColor = Color.white;
            colors.highlightedColor = new Color(1.16f, 1.18f, 1.22f, 1f);
            colors.selectedColor = new Color(1.22f, 1.26f, 1.32f, 1f);
            colors.pressedColor = new Color(0.88f, 0.88f, 0.88f, 1f);
            colors.colorMultiplier = 1f;
            btn.colors = colors;

            var nav = btn.navigation;
            nav.mode = Navigation.Mode.Automatic;
            btn.navigation = nav;

            btn.onClick.AddListener(() =>
            {
                _selectedSnapshot = snapshot;
                RebuildSlotCards();
            });

            var autoScroll = cardImg.gameObject.AddComponent<VaultAutoScroll>();
            autoScroll.Initialize(_scrollRect, cardImg.rectTransform);

            string slotHeader = CeniReloadStrings.Format("CENIRELOAD_UI_SLOT_LABEL", slotNumber);
            if (!hasData)
            {
                var top = UiFactory.CreateText("SlotTitle", cardImg.transform, $"{slotHeader} — {CeniReloadStrings.Get("CENIRELOAD_UI_SLOT_EMPTY")}", 15f, UiFactory.TextMuted, TextAlignmentOptions.Left, FontStyles.Bold);
                top.rectTransform.anchorMin = new Vector2(0f, 0.5f);
                top.rectTransform.anchorMax = new Vector2(1f, 1f);
                top.rectTransform.offsetMin = new Vector2(18f, 2f);
                top.rectTransform.offsetMax = new Vector2(-14f, -8f);

                var hint = UiFactory.CreateText("SlotHint", cardImg.transform, CeniReloadStrings.Get("CENIRELOAD_UI_SLOT_EMPTY_HINT"), 12f, new Color(0.45f, 0.52f, 0.62f, 1f), TextAlignmentOptions.Left);
                hint.rectTransform.anchorMin = new Vector2(0f, 0f);
                hint.rectTransform.anchorMax = new Vector2(1f, 0.5f);
                hint.rectTransform.offsetMin = new Vector2(18f, 8f);
                hint.rectTransform.offsetMax = new Vector2(-14f, 0f);
            }
            else
            {
                Texture portrait = PortraitCache.GetPortrait(snapshot.survivorBodyName);
                Color portraitBorder = missingSurvivor
                    ? UiFactory.AccentRed
                    : (isSelected ? UiFactory.AccentCyan : new Color(1f, 1f, 1f, 0.22f));

                var portraitFrame = UiFactory.CreatePortraitFrame("Portrait", cardImg.transform, portrait, portraitBorder);
                var portRect = portraitFrame.rectTransform;
                portRect.anchorMin = new Vector2(0f, 0.5f);
                portRect.anchorMax = new Vector2(0f, 0.5f);
                portRect.pivot = new Vector2(0f, 0.5f);
                portRect.sizeDelta = new Vector2(56f, 56f);
                portRect.anchoredPosition = new Vector2(14f, 0f);

                string survName = CeniReloadStrings.LocalizeGameToken(snapshot.survivorDisplayToken, snapshot.survivorBodyName);
                string stageName = CeniReloadStrings.LocalizeGameToken(snapshot.stageDisplayToken, snapshot.stageSceneName);
                string diffName = CeniReloadStrings.LocalizeGameToken(snapshot.difficultyDisplayToken, snapshot.difficultyName);

                var topLeft = UiFactory.CreateText("TopLeft", cardImg.transform, $"{slotHeader}  •  {survName}", 14.5f, UiFactory.TextPrimary, TextAlignmentOptions.Left, FontStyles.Bold);
                topLeft.enableWordWrapping = false;
                topLeft.overflowMode = TextOverflowModes.Ellipsis;
                topLeft.rectTransform.anchorMin = new Vector2(0f, 0.62f);
                topLeft.rectTransform.anchorMax = new Vector2(0.72f, 1f);
                topLeft.rectTransform.offsetMin = new Vector2(80f, 0f);
                topLeft.rectTransform.offsetMax = new Vector2(-4f, -6f);

                var topRight = UiFactory.CreateText("TopRight", cardImg.transform, snapshot.FormatSavedDate(), 11.5f, UiFactory.TextMuted, TextAlignmentOptions.Right);
                topRight.enableWordWrapping = false;
                topRight.rectTransform.anchorMin = new Vector2(0.70f, 0.62f);
                topRight.rectTransform.anchorMax = new Vector2(1f, 1f);
                topRight.rectTransform.offsetMin = new Vector2(0f, 0f);
                topRight.rectTransform.offsetMax = new Vector2(-12f, -6f);

                string stageWord = CeniReloadStrings.Get("CENIRELOAD_UI_LABEL_STAGE");
                string midLine = missingSurvivor
                    ? $"<color=#ff5c5c><b>[{CeniReloadStrings.Get("CENIRELOAD_UI_MISSING_SURVIVOR_BADGE")}]</b></color>  {stageName} ({stageWord} {snapshot.stageClearCount + 1})"
                    : $"{stageName}  ({stageWord} {snapshot.stageClearCount + 1})";

                var mid = UiFactory.CreateText("Mid", cardImg.transform, midLine, 13.5f, UiFactory.AccentCyan, TextAlignmentOptions.Left);
                mid.enableWordWrapping = false;
                mid.overflowMode = TextOverflowModes.Ellipsis;
                mid.rectTransform.anchorMin = new Vector2(0f, 0.30f);
                mid.rectTransform.anchorMax = new Vector2(1f, 0.64f);
                mid.rectTransform.offsetMin = new Vector2(80f, 0f);
                mid.rectTransform.offsetMax = new Vector2(-12f, 0f);

                var bot = UiFactory.CreateText("Bot", cardImg.transform, $"{snapshot.FormatRunTime()}  |  {diffName}  |  {snapshot.GetTotalItemCount()} {CeniReloadStrings.Get("CENIRELOAD_UI_ITEMS_SHORT")}", 12f, UiFactory.TextMuted, TextAlignmentOptions.Left);
                bot.enableWordWrapping = false;
                bot.overflowMode = TextOverflowModes.Ellipsis;
                bot.rectTransform.anchorMin = new Vector2(0f, 0f);
                bot.rectTransform.anchorMax = new Vector2(1f, 0.32f);
                bot.rectTransform.offsetMin = new Vector2(80f, 6f);
                bot.rectTransform.offsetMax = new Vector2(-12f, 0f);
            }

            _spawnedCards.Add(cardImg.gameObject);
            return btn;
        }

        private void UpdateInspectorPanel()
        {
            SetDeleteConfirmVisible(false);

            if (_selectedSnapshot == null)
            {
                if (_inspectorPortraitFrame != null)
                {
                    _inspectorPortraitFrame.gameObject.SetActive(false);
                }

                _inspectorTitle.rectTransform.offsetMin = new Vector2(18f, -62f);
                _inspectorTitle.text = CeniReloadStrings.Get("CENIRELOAD_UI_SLOT_EMPTY");
                _inspectorBody.text = CeniReloadStrings.Get("CENIRELOAD_UI_INSPECT_EMPTY");
                _inspectorModStatus.text = string.Empty;
                _loadButton.interactable = false;
                _deleteButton.interactable = false;
                return;
            }

            var s = _selectedSnapshot;
            bool missingSurvivor = s.HasMissingSurvivors();

            if (_inspectorPortraitFrame != null)
            {
                _inspectorPortraitFrame.gameObject.SetActive(true);
                Texture portrait = PortraitCache.GetPortrait(s.survivorBodyName);
                UiFactory.UpdatePortraitFrame(
                    _inspectorPortraitFrame,
                    portrait,
                    missingSurvivor ? UiFactory.AccentRed : UiFactory.AccentCyan
                );
                _inspectorTitle.rectTransform.offsetMin = new Vector2(76f, -62f);
            }

            string survName = CeniReloadStrings.LocalizeGameToken(s.survivorDisplayToken, s.survivorBodyName);
            string stageName = CeniReloadStrings.LocalizeGameToken(s.stageDisplayToken, s.stageSceneName);
            string diffName = CeniReloadStrings.LocalizeGameToken(s.difficultyDisplayToken, s.difficultyName);
            var firstPlayer = (s.players != null && s.players.Count > 0) ? s.players[0] : null;
            uint gold = firstPlayer != null ? firstPlayer.money : 0;
            uint voidCoins = firstPlayer != null ? firstPlayer.voidCoins : 0;
            int tempItems = s.GetTotalTempItemCount();
            string tempSuffix = tempItems > 0 ? $" (+{tempItems} temp)" : string.Empty;

            _inspectorTitle.text = $"{CeniReloadStrings.Format("CENIRELOAD_UI_SLOT_LABEL", s.slotIndex)} — {survName}";
            _inspectorBody.text =
                $"<b>{CeniReloadStrings.Get("CENIRELOAD_UI_LABEL_STAGE")}:</b> <color=#38bdf8>{stageName}</color>\n" +
                $"<b>{CeniReloadStrings.Get("CENIRELOAD_UI_LABEL_STAGE_NUM")}:</b> {s.stageClearCount + 1}\n" +
                $"<b>{CeniReloadStrings.Get("CENIRELOAD_UI_LABEL_LOOP")}:</b> {s.loopClearCount}\n" +
                $"<b>{CeniReloadStrings.Get("CENIRELOAD_UI_LABEL_TIME")}:</b> {s.FormatRunTime()}\n" +
                $"<b>{CeniReloadStrings.Get("CENIRELOAD_UI_LABEL_DIFFICULTY")}:</b> {diffName}\n" +
                $"<b>{CeniReloadStrings.Get("CENIRELOAD_UI_LABEL_GOLD")}:</b> ${gold} / {voidCoins}\n" +
                $"<b>{CeniReloadStrings.Get("CENIRELOAD_UI_LABEL_ITEMS")}:</b> {s.GetTotalItemCount()}{tempSuffix} / {s.GetTotalMinionCount()}\n" +
                $"<b>{CeniReloadStrings.Get("CENIRELOAD_UI_LABEL_SAVED_AT")}:</b> {s.FormatSavedDate()}";

            if (missingSurvivor)
            {
                _inspectorModStatus.fontStyle = FontStyles.Bold;
                _inspectorModStatus.color = UiFactory.AccentRed;
                _inspectorModStatus.text = CeniReloadStrings.Format(
                    "CENIRELOAD_UI_MISSING_SURVIVOR_WARN",
                    s.GetFirstMissingSurvivorName()
                );
                _loadButton.interactable = false;
                _deleteButton.interactable = true;
            }
            else
            {
                bool mismatch = s.HasModMismatch();
                _inspectorModStatus.fontStyle = FontStyles.Italic;
                _inspectorModStatus.text = mismatch
                    ? CeniReloadStrings.Get("CENIRELOAD_UI_MOD_MISMATCH")
                    : CeniReloadStrings.Get("CENIRELOAD_UI_MOD_OK");
                _inspectorModStatus.color = mismatch ? UiFactory.AccentAmber : UiFactory.AccentGreen;

                _loadButton.interactable = true;
                _deleteButton.interactable = true;
            }
        }

        private void SetDeleteConfirmVisible(bool visible)
        {
            bool wasConfirmVisible = _deleteConfirmBar != null && _deleteConfirmBar.activeSelf;
            if (_deleteConfirmBar != null) _deleteConfirmBar.SetActive(visible);
            if (_deleteButton != null) _deleteButton.gameObject.SetActive(!visible);

            if (_modalRoot != null && _modalRoot.activeInHierarchy)
            {
                if (visible && _confirmNoButton != null)
                {
                    SetSelectedGameObject(_confirmNoButton.gameObject);
                }
                else if (!visible && wasConfirmVisible && _deleteButton != null && _deleteButton.gameObject.activeInHierarchy && _deleteButton.interactable)
                {
                    SetSelectedGameObject(_deleteButton.gameObject);
                }
            }
        }

        private void OnClickLoadSelected()
        {
            if (_selectedSnapshot == null || _selectedSnapshot.HasMissingSurvivors()) return;

            bool started = _isMainMenuMode
                ? SnapshotRestore.BeginLoadFromMainMenu(_selectedSnapshot)
                : SnapshotRestore.BeginLoadFromLobby(_selectedSnapshot);

            if (started)
            {
                SetModalOpen(false);
            }
        }

        private void OnConfirmDeleteSelected()
        {
            if (_selectedSnapshot == null) return;
            string profileKey = VaultRepository.GetCurrentProfileKey();
            VaultRepository.DeleteSlot(profileKey, _selectedSnapshot.survivorBodyName, _selectedSnapshot.slotIndex);
            _selectedSnapshot = null;
            SetDeleteConfirmVisible(false);
            RebuildSlotCards();
        }
    }
}
