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
using UnityEngine.Networking;
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
        private MPEventSystemProvider _modalEventSystemProvider;
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
        private Image _leftPanelImage;
        private RectTransform _leftPanelRect;
        private RectTransform _rightPanelRect;
        private Transform _cardsContainer;
        private Image _inspectorPortraitFrame;
        private RawImage _inspectorLockBadge;
        private TextMeshProUGUI _inspectorTitle;
        private TextMeshProUGUI _inspectorBody;
        private TextMeshProUGUI _inspectorModStatus;
        private GameObject _allLockedWarningBanner;
        private TextMeshProUGUI _allLockedWarningText;

        private MPButton _loadButton;
        private TextMeshProUGUI _loadButtonLabel;
        private MPButton _deleteButton;
        private TextMeshProUGUI _deleteButtonLabel;
        private MPButton _backupButton;
        private MPButton _reportButton;
        private MPButton _cancelPreparedButton;
        private TextMeshProUGUI _backupLabel;
        private TextMeshProUGUI _reportLabel;
        private TextMeshProUGUI _cancelPreparedLabel;
        private VaultNoticePanel _notice;
        private ScrollRect _inspectorScroll;
        private float _nextRosterRefresh;

        public static void OpenPreparedLobby()
        {
            var lobby = UnityEngine.Object.FindObjectOfType<CharacterSelectController>();
            if (lobby == null) return;
            var vault = AttachToLobby(lobby);
            vault._selectedSnapshot = SnapshotRestore.PendingSnapshot;
            vault._filterMode = VaultFilterMode.Multiplayer;
            vault.SetModalOpen(true);
        }

        public void OpenPreparedCheckpoint()
        {
            if (!SnapshotRestore.WaitingForLobby) return;
            _selectedSnapshot = SnapshotRestore.PendingSnapshot;
            _filterMode = VaultFilterMode.Multiplayer;
            SetModalOpen(true);
        }

        private GameObject _deleteConfirmBar;
        private TextMeshProUGUI _confirmPromptText;
        private MPButton _confirmYesButton;
        private TextMeshProUGUI _confirmYesLabel;
        private MPButton _confirmNoButton;
        private TextMeshProUGUI _confirmNoLabel;

        private sealed class SlotCardView
        {
            public int SlotNumber;
            public CheckpointSnapshot Snapshot;
            public MPButton Button;
            public Image AccentStrip;
            public Image PortraitFrame;
            public RawImage LockBadge;
            public VaultFocusHighlight Highlight;
        }

        private enum VaultFilterMode
        {
            CurrentSurvivor,
            Multiplayer,
            All
        }

        private VaultFilterMode _filterMode = VaultFilterMode.CurrentSurvivor;
        private CheckpointSnapshot _selectedSnapshot;
        private readonly List<GameObject> _spawnedCards = new List<GameObject>();
        private readonly List<SlotCardView> _cardViews = new List<SlotCardView>();

        private bool _hasCachedDock;
        private Vector2 _cachedDockSize;
        private Vector3 _cachedDockLocalPos;
        private MPEventSystem.InputSource _lastInputSource;
        private bool _hasLastInputSource;
        private string _lastLanguageName;
        private int _openedFrame = -1;
        private int _closedFrame = -1;
        private int _lastActionFrame = -1;
        private float _lastLockToggleTime = -10f;
        private bool _loggedLobbyGamepadInfo;
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
            _filterMode = IsInMultiplayerLobby() ? VaultFilterMode.Multiplayer : VaultFilterMode.CurrentSurvivor;

            BuildLobbyTriggerButton();
            BuildVaultModal();
            SetModalOpen(false);
        }

        private void InitializeMainMenu(MainMenuController controller)
        {
            _mainMenu = controller;
            _isMainMenuMode = true;
            _filterMode = VaultFilterMode.All;

            BuildMainMenuTriggerButton();
            BuildVaultModal();
            SetModalOpen(false);
        }

        private static bool IsInMultiplayerLobby()
        {
            return PreGameController.instance != null &&
                   NetworkUser.readOnlyInstancesList != null &&
                   NetworkUser.readOnlyInstancesList.Count > 1;
        }

        private bool IsLobbyHostAllowed()
        {
            if (_isMainMenuMode) return true;
            if (PreGameController.instance != null && !NetworkServer.active) return false;
            return true;
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
            if (CeniReloadPlugin.IsApplicationQuitting)
            {
                return;
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
            var primaryEs = ResolveActiveEventSystem();
            if (_modalRoot != null && _modalRoot.activeSelf && !_isMainMenuMode && Time.unscaledTime >= _nextRosterRefresh &&
                (_notice == null || !_notice.IsOpen) && (_deleteConfirmBar == null || !_deleteConfirmBar.activeSelf))
            {
                _nextRosterRefresh = Time.unscaledTime + 0.5f;
                UpdateInspectorPanel();
            }
            if (_notice != null && _notice.IsOpen)
            {
                if (primaryEs != null && !_notice.Contains(primaryEs.currentSelectedGameObject)) FocusDefaultModalControl();
                if (Input.GetKey(KeyCode.JoystickButton4) || Input.GetKey(KeyCode.PageUp)) _notice.Scroll(Time.unscaledDeltaTime);
                if (Input.GetKey(KeyCode.JoystickButton5) || Input.GetKey(KeyCode.PageDown)) _notice.Scroll(-Time.unscaledDeltaTime);
            }
            else if (_inspectorScroll != null && _modalRoot != null && _modalRoot.activeSelf)
            {
                if (Input.GetKey(KeyCode.JoystickButton4) || Input.GetKey(KeyCode.PageUp)) _inspectorScroll.verticalNormalizedPosition = Mathf.Clamp01(_inspectorScroll.verticalNormalizedPosition + Time.unscaledDeltaTime);
                if (Input.GetKey(KeyCode.JoystickButton5) || Input.GetKey(KeyCode.PageDown)) _inspectorScroll.verticalNormalizedPosition = Mathf.Clamp01(_inspectorScroll.verticalNormalizedPosition - Time.unscaledDeltaTime);
            }

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

            if (primaryEs != null)
            {
                var currentSource = primaryEs.currentInputSource;
                if (!_hasLastInputSource || currentSource != _lastInputSource)
                {
                    _hasLastInputSource = true;
                    _lastInputSource = currentSource;
                    if (!_isMainMenuMode && _triggerButton != null)
                    {
                        UpdateLobbyTriggerText(primaryEs);
                        ScheduleLobbyAlignment();
                    }
                    UpdateFooterHint(primaryEs);
                }
            }

            if (!_isMainMenuMode && _triggerButton != null)
            {
                bool hostAllowed = IsLobbyHostAllowed();
                if (_triggerButton.gameObject.activeSelf != hostAllowed)
                {
                    _triggerButton.gameObject.SetActive(hostAllowed);
                    if (!hostAllowed && _modalRoot != null && _modalRoot.activeSelf)
                    {
                        SetModalOpen(false);
                    }
                }

                if (hostAllowed)
                {
                    if (!_loggedLobbyGamepadInfo && primaryEs != null && primaryEs.currentInputSource == MPEventSystem.InputSource.Gamepad)
                    {
                        _loggedLobbyGamepadInfo = true;
                        LogLobbyGamepadDiagnostics(primaryEs);
                    }

                    // Gamepad shortcut in Lobby: Y (Xbox) / Triangle (PlayStation) / Top Face Button opens the Vault
                    if (_modalRoot != null && !_modalRoot.activeSelf &&
                        Time.frameCount > _openedFrame && Time.frameCount > _closedFrame &&
                        _triggerButton.gameObject.activeInHierarchy && IsGamepadVaultShortcutPressed(primaryEs))
                    {
                        if (!IsLobbyBlockedByOverlay())
                        {
                            CeniReloadPlugin.Log?.LogInfo("Opening Checkpoint Vault via Lobby gamepad shortcut.");
                            SetModalOpen(true);
                            return;
                        }
                    }
                }
            }

            if (_modalRoot != null && _modalRoot.activeSelf && Time.frameCount > _openedFrame)
            {
                // Keep gamepad focus trapped inside the Vault modal so D-pad/stick can never vanish off-screen
                if (primaryEs != null && primaryEs.currentInputSource == MPEventSystem.InputSource.Gamepad &&
                    (_modalLayerKey == null || _modalLayerKey.representsTopLayer))
                {
                    GameObject selected = primaryEs.currentSelectedGameObject;
                    if (selected == null || !selected.activeInHierarchy || !selected.transform.IsChildOf(_modalRoot.transform))
                    {
                        FocusDefaultModalControl();
                    }
                }

                // Gamepad Lock / Unlock toggle: X (Xbox) / Square (PlayStation) / Y-left (Switch)
                if ((_notice == null || !_notice.IsOpen) && _selectedSnapshot != null && _lastActionFrame != Time.frameCount &&
                    (_deleteConfirmBar == null || !_deleteConfirmBar.activeSelf) &&
                    IsGamepadLockButtonPressed(primaryEs))
                {
                    _lastActionFrame = Time.frameCount;
                    ToggleLockOnSnapshot(_selectedSnapshot);
                    return;
                }

                bool cancelPressed = Input.GetKeyDown(KeyCode.Escape) || Input.GetKeyDown(KeyCode.JoystickButton1);
                if (!cancelPressed && primaryEs != null && primaryEs.player != null && primaryEs.player.GetButtonDown(15))
                {
                    cancelPressed = true;
                }

                if (cancelPressed && _lastActionFrame != Time.frameCount)
                {
                    _lastActionFrame = Time.frameCount;
                    if (_notice != null && _notice.IsOpen) { _notice.Close(); FocusDefaultModalControl(); }
                    else if (_deleteConfirmBar != null && _deleteConfirmBar.activeSelf)
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
                    _triggerLabel.text = CeniReloadStrings.Get("CENIRELOAD_MAINMENU_LOAD");
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

            // Verified from RoR2.Glyphs.RegisterXboxController: element ID 9 (Y) => texXBoxOneGlyphs_11
            return "<sprite=\"tmpsprXboxOneGlyphs\" name=\"texXBoxOneGlyphs_11\"> ";
        }

        private MPEventSystem ResolveActiveEventSystem()
        {
            if (_lobby != null)
            {
                var loc = _lobby.GetComponent<MPEventSystemLocator>();
                if (loc != null && loc.eventSystem != null)
                {
                    return loc.eventSystem;
                }
            }
            return MPEventSystemManager.primaryEventSystem ?? MPEventSystemManager.combinedEventSystem;
        }

        private bool IsGamepadActive(MPEventSystem es = null)
        {
            var activeEs = es ?? ResolveActiveEventSystem();
            return activeEs != null && activeEs.currentInputSource == MPEventSystem.InputSource.Gamepad;
        }

        private static string GetGamepadLockGlyph(MPEventSystem es)
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
                return "<sprite=\"tmpsprPS5GlyphsUnified\" name=\"texPS5GlyphsUnified_Square\"> ";
            if (hw.Contains("dualshock") || hw.Contains("ps4") || hw.Contains("playstation") || hw.Contains("sony"))
                return "<sprite=\"tmpsprPS4GlyphsUnified\" name=\"texPS4GlyphsUnified_3\"> ";
            if (hw.Contains("switch") || hw.Contains("pro controller") || hw.Contains("joy-con"))
                return "<sprite=\"tmpsprSwitchGlyphsUnified\" name=\"texSwitchGlyphsUnified_3\"> ";

            // Verified from RoR2.Glyphs.RegisterXboxController: element ID 8 (X) => texXBoxOneGlyphs_7
            return "<sprite=\"tmpsprXboxOneGlyphs\" name=\"texXBoxOneGlyphs_7\"> ";
        }

        private void UpdateFooterHint(MPEventSystem es = null)
        {
            if (_footerHintText == null) return;
            var activeEs = es ?? ResolveActiveEventSystem();
            if (IsGamepadActive(activeEs))
            {
                string lockGlyph = GetGamepadLockGlyph(activeEs);
                _footerHintText.text = CeniReloadStrings.Format("CENIRELOAD_UI_HINT_GAMEPAD", lockGlyph);
            }
            else
            {
                _footerHintText.text = CeniReloadStrings.Get("CENIRELOAD_UI_HINT_KBM");
            }
        }

        private bool IsGamepadLockButtonPressed(MPEventSystem es)
        {
            bool hasRewiredJoystick = false;
            MPEventSystem activeEs = es ?? ResolveActiveEventSystem();
            if (activeEs != null && activeEs.player != null)
            {
                // RewiredConsts.Action.UISubmitAlt = 31 (X on Xbox / Square on PS)
                if (activeEs.currentInputSource == MPEventSystem.InputSource.Gamepad &&
                    activeEs.player.GetButtonDown(31))
                {
                    return true;
                }

                if (activeEs.player.controllers != null)
                {
                    var joysticks = activeEs.player.controllers.Joysticks;
                    if (joysticks.Count > 0) hasRewiredJoystick = true;
                    for (int i = 0; i < joysticks.Count; i++)
                    {
                        if (IsJoystickLeftFaceButtonJustPressed(joysticks[i])) return true;
                    }
                }
            }

            if (Rewired.ReInput.isReady && Rewired.ReInput.controllers != null)
            {
                var allJoys = Rewired.ReInput.controllers.Joysticks;
                if (allJoys.Count > 0) hasRewiredJoystick = true;
                for (int i = 0; i < allJoys.Count; i++)
                {
                    if (IsJoystickLeftFaceButtonJustPressed(allJoys[i])) return true;
                }
            }

            // Only fall back to Unity legacy Input when Rewired has no joysticks (prevents 1-frame skew double-toggle!)
            return !hasRewiredJoystick && Input.GetKeyDown(KeyCode.JoystickButton2);
        }

        private static bool IsJoystickLeftFaceButtonJustPressed(Rewired.Joystick joy)
        {
            if (joy == null) return false;

            var templates = joy.Templates;
            if (templates != null)
            {
                for (int t = 0; t < templates.Count; t++)
                {
                    var tpl = templates[t];
                    if (tpl == null || tpl.elements == null) continue;
                    var elements = tpl.elements;
                    for (int e = 0; e < elements.Count; e++)
                    {
                        if (elements[e] is Rewired.IControllerTemplateButton tplBtn && tplBtn.exists && tplBtn.justPressed)
                        {
                            string desc = elements[e].descriptiveName ?? string.Empty;
                            if (desc.IndexOf("Action Top Row 1", StringComparison.OrdinalIgnoreCase) >= 0 ||
                                desc.IndexOf("Action Left", StringComparison.OrdinalIgnoreCase) >= 0 ||
                                desc.IndexOf("Left Face", StringComparison.OrdinalIgnoreCase) >= 0)
                            {
                                return true;
                            }
                        }
                    }
                }
            }

            string hw = $"{joy.name} {joy.hardwareName}".ToLowerInvariant();
            bool isSwitch = hw.Contains("switch") || hw.Contains("pro controller") || hw.Contains("joy-con");
            if (isSwitch)
            {
                if (joy.GetButtonDownById(6)) return true;
            }
            else
            {
                // Verified from RoR2.Glyphs: Xbox X / PS4 Square / PS5 Square = element ID 8
                if (joy.GetButtonDownById(8)) return true;
            }

            var buttons = joy.Buttons;
            for (int b = 0; b < buttons.Count; b++)
            {
                var btn = buttons[b];
                if (btn == null || !btn.justPressed) continue;

                string elName = btn.elementIdentifier != null ? (btn.elementIdentifier.name ?? string.Empty) : string.Empty;
                int elId = btn.elementIdentifier != null ? btn.elementIdentifier.id : -1;

                if ((!isSwitch && elId == 8) ||
                    (isSwitch && elId == 6) ||
                    (!isSwitch && elName.Equals("X", StringComparison.OrdinalIgnoreCase)) ||
                    (isSwitch && elName.Equals("Y", StringComparison.OrdinalIgnoreCase)) ||
                    elName.Equals("Square", StringComparison.OrdinalIgnoreCase) ||
                    elName.Equals("Action Top Row 1", StringComparison.OrdinalIgnoreCase) ||
                    elName.Equals("Button X", StringComparison.OrdinalIgnoreCase))
                {
                    return true;
                }

                if (b == 2 && IsGenericOrUnlabeledButton(elName))
                {
                    return true;
                }
            }

            return false;
        }

        private bool IsLobbyBlockedByOverlay()
        {
            if (PauseScreenController.instancesList != null && PauseScreenController.instancesList.Count > 0)
                return true;
            if (SimpleDialogBox.instancesList != null && SimpleDialogBox.instancesList.Count > 0)
                return true;

            UILayerKey lobbyLayer = _triggerButton != null ? _triggerButton.requiredTopLayer : null;
            if (lobbyLayer != null && lobbyLayer.isActiveAndEnabled && !lobbyLayer.representsTopLayer)
            {
                return true;
            }
            return false;
        }

        private void LogLobbyGamepadDiagnostics(MPEventSystem es)
        {
            try
            {
                string joyInfo = "none";
                if (es != null && es.player != null && es.player.controllers != null && es.player.controllers.Joysticks.Count > 0)
                {
                    var j = es.player.controllers.Joysticks[0];
                    if (j != null)
                    {
                        joyInfo = $"'{j.name}' (hw='{j.hardwareName}', buttons={j.buttonCount}, templates={j.templateCount})";
                    }
                }
                else if (Rewired.ReInput.isReady && Rewired.ReInput.controllers != null && Rewired.ReInput.controllers.Joysticks.Count > 0)
                {
                    var j = Rewired.ReInput.controllers.Joysticks[0];
                    if (j != null)
                    {
                        joyInfo = $"global '{j.name}' (hw='{j.hardwareName}', buttons={j.buttonCount}, templates={j.templateCount})";
                    }
                }

                UILayerKey lobbyLayer = _triggerButton != null ? _triggerButton.requiredTopLayer : null;
                string layerInfo = lobbyLayer != null
                    ? $"{lobbyLayer.name} (active={lobbyLayer.isActiveAndEnabled}, top={lobbyLayer.representsTopLayer})"
                    : "null";
                CeniReloadPlugin.Log?.LogInfo($"Lobby gamepad ready: joystick={joyInfo}, lobbyLayer={layerInfo}");
            }
            catch
            {
                // Ignore non-critical diagnostics failures
            }
        }

        private bool IsGamepadVaultShortcutPressed(MPEventSystem es)
        {
            MPEventSystem activeEs = es ?? ResolveActiveEventSystem();

            if (activeEs != null && activeEs.player != null && activeEs.player.controllers != null)
            {
                var joysticks = activeEs.player.controllers.Joysticks;
                for (int i = 0; i < joysticks.Count; i++)
                {
                    if (IsJoystickTopFaceButtonJustPressed(joysticks[i])) return true;
                }
            }

            var allEventSystems = MPEventSystem.readOnlyInstancesList;
            if (allEventSystems != null)
            {
                for (int e = 0; e < allEventSystems.Count; e++)
                {
                    var mpEs = allEventSystems[e];
                    if (mpEs == null || mpEs == activeEs || mpEs.player == null || mpEs.player.controllers == null) continue;
                    var joysticks = mpEs.player.controllers.Joysticks;
                    for (int i = 0; i < joysticks.Count; i++)
                    {
                        if (IsJoystickTopFaceButtonJustPressed(joysticks[i])) return true;
                    }
                }
            }

            if (Rewired.ReInput.isReady && Rewired.ReInput.controllers != null)
            {
                var allJoys = Rewired.ReInput.controllers.Joysticks;
                for (int i = 0; i < allJoys.Count; i++)
                {
                    if (IsJoystickTopFaceButtonJustPressed(allJoys[i])) return true;
                }
            }

            return Input.GetKeyDown(KeyCode.JoystickButton3);
        }

        private static bool IsJoystickTopFaceButtonJustPressed(Rewired.Joystick joy)
        {
            if (joy == null) return false;

            // 1. Universal Rewired Controller Templates (GamepadTemplate -> Action Top Row 2 = Y / Triangle / Switch X)
            var templates = joy.Templates;
            if (templates != null)
            {
                for (int t = 0; t < templates.Count; t++)
                {
                    var tpl = templates[t];
                    if (tpl == null || tpl.elements == null) continue;
                    var elements = tpl.elements;
                    for (int e = 0; e < elements.Count; e++)
                    {
                        if (elements[e] is Rewired.IControllerTemplateButton tplBtn && tplBtn.exists && tplBtn.justPressed)
                        {
                            string desc = elements[e].descriptiveName ?? string.Empty;
                            if (desc.IndexOf("Action Top Row 2", StringComparison.OrdinalIgnoreCase) >= 0 ||
                                desc.IndexOf("Top Face", StringComparison.OrdinalIgnoreCase) >= 0)
                            {
                                return true;
                            }
                        }
                    }
                }
            }

            // 2. Verified RoR2 hardware element IDs: Switch/Pro uses ID 7; Xbox/PS4/PS5/Stadia use ID 9
            string hw = $"{joy.name} {joy.hardwareName}".ToLowerInvariant();
            bool isSwitch = hw.Contains("switch") || hw.Contains("pro controller") || hw.Contains("joy-con");
            if (isSwitch)
            {
                if (joy.GetButtonDownById(7)) return true;
            }
            else
            {
                if (joy.GetButtonDownById(9)) return true;
            }

            // 3. Element name scan + safe generic button index 3 fallback (for DirectInput / 8BitDo / Steam Input virtual gamepads)
            var buttons = joy.Buttons;
            for (int b = 0; b < buttons.Count; b++)
            {
                var btn = buttons[b];
                if (btn == null || !btn.justPressed) continue;

                string elName = btn.elementIdentifier != null ? (btn.elementIdentifier.name ?? string.Empty) : string.Empty;
                int elId = btn.elementIdentifier != null ? btn.elementIdentifier.id : -1;

                if ((!isSwitch && elId == 9) ||
                    (isSwitch && elId == 7) ||
                    elName.Equals("Y", StringComparison.OrdinalIgnoreCase) ||
                    elName.Equals("Triangle", StringComparison.OrdinalIgnoreCase) ||
                    elName.Equals("Action Top Row 2", StringComparison.OrdinalIgnoreCase) ||
                    elName.Equals("Button Y", StringComparison.OrdinalIgnoreCase) ||
                    elName.Equals("Face Button Top", StringComparison.OrdinalIgnoreCase))
                {
                    return true;
                }

                if (b == 3 && IsGenericOrUnlabeledButton(elName))
                {
                    return true;
                }
            }

            return false;
        }

        private static bool IsGenericOrUnlabeledButton(string elName)
        {
            if (string.IsNullOrWhiteSpace(elName)) return true;
            string n = elName.Trim();
            if (n.StartsWith("Button ", StringComparison.OrdinalIgnoreCase))
                return true;

            // Reject any explicitly named non-top-face button so we never misfire on A/B/X/bumpers/triggers/start
            if (n.Equals("A", StringComparison.OrdinalIgnoreCase) ||
                n.Equals("B", StringComparison.OrdinalIgnoreCase) ||
                n.Equals("X", StringComparison.OrdinalIgnoreCase) ||
                n.Equals("Cross", StringComparison.OrdinalIgnoreCase) ||
                n.Equals("Circle", StringComparison.OrdinalIgnoreCase) ||
                n.Equals("Square", StringComparison.OrdinalIgnoreCase) ||
                n.IndexOf("Left", StringComparison.OrdinalIgnoreCase) >= 0 ||
                n.IndexOf("Right", StringComparison.OrdinalIgnoreCase) >= 0 ||
                n.IndexOf("Shoulder", StringComparison.OrdinalIgnoreCase) >= 0 ||
                n.IndexOf("Trigger", StringComparison.OrdinalIgnoreCase) >= 0 ||
                n.IndexOf("Bumper", StringComparison.OrdinalIgnoreCase) >= 0 ||
                n.IndexOf("Start", StringComparison.OrdinalIgnoreCase) >= 0 ||
                n.IndexOf("Back", StringComparison.OrdinalIgnoreCase) >= 0 ||
                n.IndexOf("Select", StringComparison.OrdinalIgnoreCase) >= 0 ||
                n.IndexOf("Menu", StringComparison.OrdinalIgnoreCase) >= 0 ||
                n.IndexOf("View", StringComparison.OrdinalIgnoreCase) >= 0 ||
                n.IndexOf("Options", StringComparison.OrdinalIgnoreCase) >= 0 ||
                n.IndexOf("Share", StringComparison.OrdinalIgnoreCase) >= 0 ||
                n.IndexOf("Stick", StringComparison.OrdinalIgnoreCase) >= 0 ||
                n.IndexOf("D-Pad", StringComparison.OrdinalIgnoreCase) >= 0 ||
                n.IndexOf("DPad", StringComparison.OrdinalIgnoreCase) >= 0)
            {
                return false;
            }

            return false;
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
            if (_lobby.readyButton != null &&
                _lobby.readyButton.requiredTopLayer != null &&
                _lobby.readyButton.requiredTopLayer.isActiveAndEnabled)
            {
                return _lobby.readyButton.requiredTopLayer;
            }

            var parentLayer = _lobby.GetComponentInParent<UILayerKey>();
            if (parentLayer != null && parentLayer.isActiveAndEnabled)
            {
                return parentLayer;
            }

            var activeChildLayer = _lobby.GetComponentInChildren<UILayerKey>(false);
            if (activeChildLayer != null && activeChildLayer.isActiveAndEnabled)
            {
                return activeChildLayer;
            }

            return null;
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

            var triggerNav = _triggerButton.navigation;
            triggerNav.mode = Navigation.Mode.None;
            _triggerButton.navigation = triggerNav;

            var highlight = _triggerButton.gameObject.GetComponent<VaultFocusHighlight>();
            if (highlight != null)
            {
                highlight.SetBaseColors(new Color(0.08f, 0.24f, 0.36f, 0.96f), UiFactory.AccentCyan);
            }
            else
            {
                var outline = _triggerButton.gameObject.GetComponent<Outline>();
                if (outline != null) outline.effectColor = UiFactory.AccentCyan;
            }

            ScheduleLobbyAlignment();
        }

        private void BuildMainMenuTriggerButton()
        {
            if (_mainMenu == null) return;

            HGButton templateBtn = _mainMenu.singlePlayerButton;
            Transform listParent = templateBtn != null ? templateBtn.transform.parent : null;

            if (listParent == null && _mainMenu.mainMenuButtonPanel != null)
            {
                var found = _mainMenu.mainMenuButtonPanel.GetComponentInChildren<HGButton>(true);
                if (found != null)
                {
                    templateBtn = found;
                    listParent = found.transform.parent;
                }
            }

            if (listParent == null)
            {
                listParent = _mainMenu.transform;
            }

            // Clean up any pre-existing button before cloning
            foreach (string oldName in new[] { "CENIRELOAD_MainMenuLoadButton", "CENIRELOAD_MainMenuVaultButton" })
            {
                Transform existingBtn = listParent.Find(oldName);
                if (existingBtn != null)
                {
                    existingBtn.gameObject.SetActive(false);
                    existingBtn.SetParent(null, false);
                    Destroy(existingBtn.gameObject);
                }
            }

            // Locate the Multiplayer button inside the same menu list so we insert right between Multiplayer and Alternate Game Modes
            HGButton multiplayerBtn = null;
            if (_mainMenu.onlineMultiplayerButtonTransition != null &&
                _mainMenu.onlineMultiplayerButtonTransition.transform.parent == listParent)
            {
                multiplayerBtn = _mainMenu.onlineMultiplayerButtonTransition;
            }
            else
            {
                string localizedMulti = RoR2.Language.GetString("TITLE_MULTIPLAYER") ?? "Multiplayer";
                for (int i = 0; i < listParent.childCount; i++)
                {
                    var child = listParent.GetChild(i);
                    var hg = child.GetComponent<HGButton>();
                    if (hg == null || hg == templateBtn) continue;

                    var tmp = hg.GetComponentInChildren<TextMeshProUGUI>(true);
                    string txt = tmp != null ? StripRichTextTags(tmp.text).Trim() : string.Empty;
                    if (child.name.IndexOf("Multi", StringComparison.OrdinalIgnoreCase) >= 0 ||
                        txt.Equals(localizedMulti, StringComparison.OrdinalIgnoreCase) ||
                        txt.Equals("Multiplayer", StringComparison.OrdinalIgnoreCase) ||
                        txt.Equals("Multijogador", StringComparison.OrdinalIgnoreCase))
                    {
                        multiplayerBtn = hg;
                        break;
                    }
                }
            }

            if (templateBtn != null)
            {
                // Clone native Main Menu button so font, sprite, hover highlight, sound, and layout are 100% identical
                GameObject clone = Instantiate(templateBtn.gameObject, listParent, false);
                clone.name = "CENIRELOAD_MainMenuLoadButton";
                clone.SetActive(true);

                // Remove any LanguageTextMeshController and ViewableTag so RoR2 doesn't overwrite our label or throw on shutdown
                var langControllers = clone.GetComponentsInChildren<LanguageTextMeshController>(true);
                for (int i = 0; i < langControllers.Length; i++)
                {
                    DestroyImmediate(langControllers[i]);
                }
                var viewableTags = clone.GetComponentsInChildren<ViewableTag>(true);
                for (int i = 0; i < viewableTags.Length; i++)
                {
                    DestroyImmediate(viewableTags[i]);
                }

                // Hide any "NEW" notification badge if present
                for (int i = clone.transform.childCount - 1; i >= 0; i--)
                {
                    var c = clone.transform.GetChild(i);
                    if (c.name.IndexOf("New", StringComparison.OrdinalIgnoreCase) >= 0 ||
                        c.name.IndexOf("Notification", StringComparison.OrdinalIgnoreCase) >= 0)
                    {
                        c.gameObject.SetActive(false);
                    }
                }

                var hgBtn = clone.GetComponent<HGButton>();
                _triggerButton = hgBtn;
                _triggerLabel = clone.GetComponentInChildren<TextMeshProUGUI>(true);

                if (_triggerLabel != null)
                {
                    _triggerLabel.text = CeniReloadStrings.Get("CENIRELOAD_MAINMENU_LOAD");
                }

                if (hgBtn != null)
                {
                    hgBtn.onClick = new Button.ButtonClickedEvent();
                    hgBtn.onClick.AddListener(() => SetModalOpen(true));
                    hgBtn.updateTextOnHover = false;
                    hgBtn.hoverToken = string.Empty;

                    var hoverTarget = hgBtn.hoverLanguageTextMeshController;
                    if (hoverTarget != null)
                    {
                        var hoverTmp = hoverTarget.GetComponent<TextMeshProUGUI>();
                        if (hoverTmp != null)
                        {
                            hgBtn.onSelect.AddListener(() =>
                            {
                                hoverTmp.text = CeniReloadStrings.Get("CENIRELOAD_MAINMENU_LOAD_DESC");
                            });
                            var trigger = clone.GetComponent<EventTrigger>() ?? clone.AddComponent<EventTrigger>();
                            var entry = new EventTrigger.Entry { eventID = EventTriggerType.PointerEnter };
                            entry.callback.AddListener(_ =>
                            {
                                hoverTmp.text = CeniReloadStrings.Get("CENIRELOAD_MAINMENU_LOAD_DESC");
                            });
                            trigger.triggers.Add(entry);
                        }
                    }
                }

                // Insert strictly between Multiplayer and Alternate Game Modes
                int insertIndex = multiplayerBtn != null
                    ? multiplayerBtn.transform.GetSiblingIndex() + 1
                    : Math.Min(templateBtn.transform.GetSiblingIndex() + 2, listParent.childCount - 1);
                clone.transform.SetSiblingIndex(insertIndex);

                // Rewire explicit navigation if Main Menu uses Navigation.Mode.Explicit
                Selectable anchorAbove = multiplayerBtn != null ? (Selectable)multiplayerBtn : templateBtn;
                if (anchorAbove != null && _triggerButton != null && anchorAbove.navigation.mode == Navigation.Mode.Explicit)
                {
                    var aboveNav = anchorAbove.navigation;
                    Selectable nextDown = aboveNav.selectOnDown;
                    aboveNav.selectOnDown = _triggerButton;
                    anchorAbove.navigation = aboveNav;

                    var trigNav = _triggerButton.navigation;
                    trigNav.mode = Navigation.Mode.Explicit;
                    trigNav.selectOnUp = anchorAbove;
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
                UILayerKey mainMenuLayer = FindMainMenuLayerKey();
                _triggerButton = UiFactory.CreateButton(
                    "CENIRELOAD_MainMenuLoadButton",
                    listParent,
                    CeniReloadStrings.Get("CENIRELOAD_MAINMENU_LOAD"),
                    new Color(0.16f, 0.20f, 0.26f, 0.94f),
                    UiFactory.TextPrimary,
                    18f,
                    () => SetModalOpen(true),
                    out _triggerLabel,
                    mainMenuLayer
                );
                var layoutElem = _triggerButton.gameObject.AddComponent<LayoutElement>();
                layoutElem.minHeight = 46f;
                layoutElem.preferredHeight = 46f;
                layoutElem.flexibleWidth = 1f;
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
            trigRect.sizeDelta = new Vector2(192f, 40f);
            trigRect.anchoredPosition = new Vector2(-92f, 88f);
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

            // Sync TMP font/spriteAsset from lobby Ready button so Xbox/PS glyphs render crisp and full-color
            if (_triggerLabel != null && _lobby.readyButton != null)
            {
                var readyTmp = _lobby.readyButton.GetComponentInChildren<TextMeshProUGUI>(true);
                if (readyTmp != null)
                {
                    if (readyTmp.font != null) _triggerLabel.font = readyTmp.font;
                    if (readyTmp.spriteAsset != null) _triggerLabel.spriteAsset = readyTmp.spriteAsset;
                }
            }

            // In Gamepad mode, RoR2 replaces Back/Voltar with the taller footer prompt bar ((B) Back / (LT)(RT) Change),
            // so always dock cleanly above the footer divider instead of reusing the lower KBM Back-button Y coordinate.
            var activeEs = ResolveActiveEventSystem();
            if (activeEs != null && activeEs.currentInputSource == MPEventSystem.InputSource.Gamepad)
            {
                ApplyFallbackBottomRightDock(trigRect, canvasRect);
                yield break;
            }

            RectTransform backRect = FindBackButtonRect(rootCanvas.transform, canvasRect);
            if (backRect != null && backRect.gameObject.activeInHierarchy)
            {
                Vector3[] corners = new Vector3[4];
                backRect.GetWorldCorners(corners);
                // corners: 0=bottom-left, 1=top-left, 2=top-right, 3=bottom-right
                Vector3 bl = canvasRect.InverseTransformPoint(corners[0]);
                Vector3 tl = canvasRect.InverseTransformPoint(corners[1]);
                Vector3 tr = canvasRect.InverseTransformPoint(corners[2]);

                float width = Mathf.Abs(tr.x - tl.x);
                float height = Mathf.Abs(tl.y - bl.y);
                float centerX = (tl.x + tr.x) * 0.5f;
                float topY = tl.y;

                // Strictly verify the button is laid out in the bottom-right corner (never an un-laid-out inactive prefab center)
                bool isValidBottomRight = width >= 110f && height >= 26f &&
                                          centerX >= canvasRect.rect.width * 0.25f &&
                                          topY <= -canvasRect.rect.height * 0.25f;

                if (isValidBottomRight)
                {
                    const float verticalGap = 4f;
                    Vector2 candidateSize = new Vector2(width, height);
                    Vector3 candidatePos = new Vector3(centerX, topY + verticalGap, 0f);
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

            _modalEventSystemProvider = _modalRoot.AddComponent<MPEventSystemProvider>();
            _modalEventSystemProvider.fallBackToMainEventSystem = true;
            var initialEs = ResolveActiveEventSystem();
            if (initialEs != null)
            {
                _modalEventSystemProvider.eventSystem = initialEs;
            }

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
                OnClickCloseModal,
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
            _leftPanelImage = leftPanel;
            _leftPanelRect = leftPanel.rectTransform;
            _leftPanelRect.anchorMin = new Vector2(0f, 0f);
            _leftPanelRect.anchorMax = new Vector2(0.54f, 1f);
            _leftPanelRect.offsetMin = new Vector2(20f, 40f);
            _leftPanelRect.offsetMax = new Vector2(-10f, -98f);

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
            _rightPanelRect = rightPanel.rectTransform;
            _rightPanelRect.anchorMin = new Vector2(0.54f, 0f);
            _rightPanelRect.anchorMax = new Vector2(1f, 1f);
            _rightPanelRect.offsetMin = new Vector2(10f, 40f);
            _rightPanelRect.offsetMax = new Vector2(-20f, -98f);

            _inspectorPortraitFrame = UiFactory.CreatePortraitFrame("InspectorPortrait", rightPanel.transform, null, UiFactory.AccentCyan);
            var inspPortRect = _inspectorPortraitFrame.rectTransform;
            inspPortRect.anchorMin = new Vector2(0f, 1f);
            inspPortRect.anchorMax = new Vector2(0f, 1f);
            inspPortRect.pivot = new Vector2(0f, 1f);
            inspPortRect.sizeDelta = new Vector2(48f, 48f);
            inspPortRect.anchoredPosition = new Vector2(18f, -14f);

            _inspectorLockBadge = UiFactory.CreatePadlockBadge("InspectorLockBadge", rightPanel.transform, 34f);
            var inspLockRect = _inspectorLockBadge.rectTransform;
            inspLockRect.anchorMin = new Vector2(1f, 1f);
            inspLockRect.anchorMax = new Vector2(1f, 1f);
            inspLockRect.pivot = new Vector2(1f, 1f);
            inspLockRect.anchoredPosition = new Vector2(-16f, -20f);
            _inspectorLockBadge.gameObject.SetActive(false);

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
            _inspectorBody.rectTransform.offsetMin = new Vector2(18f, 256f);
            _inspectorBody.rectTransform.offsetMax = new Vector2(-18f, -72f);

            var inspectorViewport = UiFactory.CreateRect("InspectorViewport", rightPanel.transform);
            inspectorViewport.anchorMin = Vector2.zero; inspectorViewport.anchorMax = Vector2.one;
            inspectorViewport.offsetMin = new Vector2(18f, 256f); inspectorViewport.offsetMax = new Vector2(-18f, -72f);
            var inspectorBackground = inspectorViewport.gameObject.AddComponent<Image>(); inspectorBackground.color = new Color(0f, 0f, 0f, 0.01f);
            inspectorViewport.gameObject.AddComponent<RectMask2D>();
            _inspectorBody.transform.SetParent(inspectorViewport, false);
            _inspectorBody.rectTransform.anchorMin = new Vector2(0f, 1f); _inspectorBody.rectTransform.anchorMax = Vector2.one;
            _inspectorBody.rectTransform.pivot = new Vector2(0.5f, 1f); _inspectorBody.rectTransform.offsetMin = Vector2.zero; _inspectorBody.rectTransform.offsetMax = Vector2.zero;
            _inspectorScroll = inspectorViewport.gameObject.AddComponent<ScrollRect>();
            _inspectorScroll.content = _inspectorBody.rectTransform; _inspectorScroll.viewport = inspectorViewport;
            _inspectorScroll.horizontal = false; _inspectorScroll.movementType = ScrollRect.MovementType.Clamped; _inspectorScroll.scrollSensitivity = 25f;

            _inspectorModStatus = UiFactory.CreateText("ModStatus", rightPanel.transform, "", 13f, UiFactory.AccentGreen, TextAlignmentOptions.Left, FontStyles.Italic);
            _inspectorModStatus.enableAutoSizing = true;
            _inspectorModStatus.fontSizeMin = 10.5f;
            _inspectorModStatus.fontSizeMax = 13.5f;
            _inspectorModStatus.rectTransform.anchorMin = new Vector2(0f, 0f);
            _inspectorModStatus.rectTransform.anchorMax = new Vector2(1f, 0f);
            _inspectorModStatus.rectTransform.pivot = new Vector2(0.5f, 0f);
            _inspectorModStatus.rectTransform.sizeDelta = new Vector2(-36f, 50f);
            _inspectorModStatus.rectTransform.anchoredPosition = new Vector2(0f, 202f);

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
                OnClickRequestDelete,
                out _deleteButtonLabel,
                _modalLayerKey
            );
            var delRect = _deleteButton.GetComponent<RectTransform>();
            delRect.anchorMin = new Vector2(0f, 0f);
            delRect.anchorMax = new Vector2(1f, 0f);
            delRect.pivot = new Vector2(0.5f, 0f);
            delRect.sizeDelta = new Vector2(-36f, 36f);
            delRect.anchoredPosition = new Vector2(0f, 16f);

            _backupButton = UiFactory.CreateButton("PreviousBackup", rightPanel.transform, "", new Color(0.20f, 0.30f, 0.40f, 1f), Color.white, 12f,
                OnClickBackup, out _backupLabel, _modalLayerKey);
            _reportButton = UiFactory.CreateButton("LastRestoreReport", rightPanel.transform, "", new Color(0.20f, 0.30f, 0.40f, 1f), Color.white, 12f,
                OnClickLastReport, out _reportLabel, _modalLayerKey);
            _cancelPreparedButton = UiFactory.CreateButton("CancelPreparedCheckpoint", rightPanel.transform, "", new Color(0.40f, 0.22f, 0.14f, 1f), Color.white, 12f,
                () => { if (!CanHandleModalButtonClick()) return; SnapshotRestore.CancelPrepared(); UpdateInspectorPanel(); }, out _cancelPreparedLabel, _modalLayerKey);
            PositionUtilityButton(_backupButton, 166f); PositionUtilityButton(_reportButton, 133f); PositionUtilityButton(_cancelPreparedButton, 100f);

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
                OnClickCancelDelete,
                out _confirmNoLabel,
                _modalLayerKey
            );
            var noRect = _confirmNoButton.GetComponent<RectTransform>();
            noRect.anchorMin = new Vector2(0.75f, 0.12f);
            noRect.anchorMax = new Vector2(0.98f, 0.88f);
            noRect.offsetMin = Vector2.zero;
            noRect.offsetMax = Vector2.zero;

            _deleteConfirmBar.SetActive(false);

            var allLockedPanel = UiFactory.CreatePanel("AllLockedWarningBanner", window.transform, new Color(0.32f, 0.08f, 0.10f, 0.98f), UiFactory.AccentAmber);
            var warnRect = allLockedPanel.rectTransform;
            warnRect.anchorMin = new Vector2(0f, 0f);
            warnRect.anchorMax = new Vector2(1f, 0f);
            warnRect.pivot = new Vector2(0.5f, 0f);
            warnRect.sizeDelta = new Vector2(-40f, 32f);
            warnRect.anchoredPosition = new Vector2(0f, 34f);
            _allLockedWarningBanner = allLockedPanel.gameObject;

            _allLockedWarningText = UiFactory.CreateText(
                "AllLockedWarningText",
                allLockedPanel.transform,
                "",
                12.5f,
                new Color(1.0f, 0.88f, 0.32f, 1f),
                TextAlignmentOptions.Center,
                FontStyles.Bold
            );
            _allLockedWarningText.enableAutoSizing = true;
            _allLockedWarningText.fontSizeMin = 10f;
            _allLockedWarningText.fontSizeMax = 13f;
            UiFactory.StretchFull(_allLockedWarningText.rectTransform, 12f, 3f);
            _allLockedWarningBanner.SetActive(false);

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
            SyncFooterSpriteAsset();
            _notice = new VaultNoticePanel(_modalRoot.transform, _modalLayerKey);
        }

        private void SyncFooterSpriteAsset()
        {
            if (_footerHintText == null) return;
            TextMeshProUGUI sourceTmp = null;
            if (_lobby != null && _lobby.readyButton != null)
            {
                sourceTmp = _lobby.readyButton.GetComponentInChildren<TextMeshProUGUI>(true);
            }
            else if (_mainMenu != null && _mainMenu.singlePlayerButton != null)
            {
                sourceTmp = _mainMenu.singlePlayerButton.GetComponentInChildren<TextMeshProUGUI>(true);
            }
            if (sourceTmp != null)
            {
                if (sourceTmp.font != null) _footerHintText.font = sourceTmp.font;
                if (sourceTmp.spriteAsset != null) _footerHintText.spriteAsset = sourceTmp.spriteAsset;
            }
        }

        private void SetModalOpen(bool open)
        {
            if (_modalRoot == null) return;

            if (open)
            {
                var activeEs = ResolveActiveEventSystem();
                if (_modalEventSystemProvider != null && activeEs != null)
                {
                    _modalEventSystemProvider.eventSystem = activeEs;
                }

                _openedFrame = Time.frameCount;
                _lastActionFrame = Time.frameCount;
                if (!_isMainMenuMode && IsInMultiplayerLobby())
                {
                    _filterMode = VaultFilterMode.Multiplayer;
                }
                _modalRoot.SetActive(true);
                if (_modalLayerKey != null) _modalLayerKey.enabled = true;
                _modalRoot.transform.SetAsLastSibling();
                RefreshLocalization();
                RebuildSlotCards();
            }
            else
            {
                bool wasOpen = _modalRoot.activeSelf;
                if (_focusCoroutine != null)
                {
                    StopCoroutine(_focusCoroutine);
                    _focusCoroutine = null;
                }
                if (_modalLayerKey != null) _modalLayerKey.enabled = false;
                _notice?.Close();
                _modalRoot.SetActive(false);

                if (wasOpen)
                {
                    _closedFrame = Time.frameCount;
                    if (_isMainMenuMode)
                    {
                        if (_triggerButton != null && _triggerButton.gameObject.activeInHierarchy && _triggerButton.interactable)
                        {
                            SetSelectedGameObject(_triggerButton.gameObject);
                        }
                    }
                    else if (_lobby != null && _lobby.readyButton != null && _lobby.readyButton.gameObject.activeInHierarchy)
                    {
                        SetSelectedGameObject(_lobby.readyButton.gameObject);
                    }
                }
            }
        }

        private bool CanHandleModalButtonClick()
        {
            if (_modalRoot == null || !_modalRoot.activeSelf || (_notice != null && _notice.IsOpen)) return false;
            if (Time.frameCount <= _openedFrame || _lastActionFrame == Time.frameCount) return false;
            _lastActionFrame = Time.frameCount;
            return true;
        }

        private void OnClickCloseModal()
        {
            if (!CanHandleModalButtonClick()) return;
            SetModalOpen(false);
        }

        private void OnClickRequestDelete()
        {
            if (!CanHandleModalButtonClick()) return;
            if (_selectedSnapshot == null || _selectedSnapshot.isLocked) return;
            SetDeleteConfirmVisible(true);
        }

        private void OnClickCancelDelete()
        {
            if (!CanHandleModalButtonClick()) return;
            SetDeleteConfirmVisible(false);
        }

        private void ToggleFilterMode()
        {
            if (!CanHandleModalButtonClick()) return;

            string profileKey = VaultRepository.GetCurrentProfileKey();
            bool hasMp = IsInMultiplayerLobby() || VaultRepository.HasAnyMultiplayerSnapshots(profileKey);
            bool hasSurvivor = !string.IsNullOrEmpty(GetSelectedSurvivorBodyName());

            if (_filterMode == VaultFilterMode.CurrentSurvivor)
            {
                _filterMode = hasMp ? VaultFilterMode.Multiplayer : VaultFilterMode.All;
            }
            else if (_filterMode == VaultFilterMode.Multiplayer)
            {
                _filterMode = VaultFilterMode.All;
            }
            else
            {
                if (!_isMainMenuMode && IsInMultiplayerLobby())
                {
                    _filterMode = VaultFilterMode.Multiplayer;
                }
                else if (hasSurvivor)
                {
                    _filterMode = VaultFilterMode.CurrentSurvivor;
                }
                else if (hasMp)
                {
                    _filterMode = VaultFilterMode.Multiplayer;
                }
                else
                {
                    _filterMode = VaultFilterMode.All;
                }
            }

            _selectedSnapshot = null;
            RefreshLocalization();
            RebuildSlotCards();
        }

        private void RefreshLocalization()
        {
            RefreshTriggerLabel();
            _backupLabel.text = CeniReloadStrings.Get("CENIRELOAD_UI_BACKUP");
            _reportLabel.text = CeniReloadStrings.Get("CENIRELOAD_UI_LAST_REPORT");
            _cancelPreparedLabel.text = CeniReloadStrings.Get("CENIRELOAD_UI_CANCEL_PREPARED");
            _titleText.text = CeniReloadStrings.Get("CENIRELOAD_UI_TITLE");
            string subtitleBase = CeniReloadStrings.Get("CENIRELOAD_UI_SUBTITLE");
            _subtitleText.text = VaultRepository.IsUsingCloudStorage()
                ? $"{subtitleBase}  •  <color=#38bdf8>[{CeniReloadStrings.Get("CENIRELOAD_UI_STORAGE_CLOUD")}]</color>"
                : subtitleBase;
            _closeButtonLabel.text = CeniReloadStrings.Get("CENIRELOAD_UI_CLOSE");
            _loadButtonLabel.text = CeniReloadStrings.Get("CENIRELOAD_UI_BTN_LOAD");
            _deleteButtonLabel.text = CeniReloadStrings.Get("CENIRELOAD_UI_BTN_DELETE");
            _confirmPromptText.text = CeniReloadStrings.Get("CENIRELOAD_UI_CONFIRM_DELETE");
            _confirmYesLabel.text = CeniReloadStrings.Get("CENIRELOAD_UI_BTN_CONFIRM_YES");
            _confirmNoLabel.text = CeniReloadStrings.Get("CENIRELOAD_UI_BTN_CONFIRM_NO");
            UpdateFooterHint();

            if (_filterMode == VaultFilterMode.All)
            {
                _filterButtonLabel.text = CeniReloadStrings.Get("CENIRELOAD_UI_FILTER_ALL");
            }
            else if (_filterMode == VaultFilterMode.Multiplayer)
            {
                _filterButtonLabel.text = CeniReloadStrings.Get("CENIRELOAD_UI_FILTER_MULTIPLAYER");
            }
            else
            {
                _filterButtonLabel.text = CeniReloadStrings.Format("CENIRELOAD_UI_FILTER_SURVIVOR", GetSelectedSurvivorDisplayName());
            }
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
            _cardViews.Clear();

            string profileKey = VaultRepository.GetCurrentProfileKey();
            if (_selectedSnapshot != null && !IsSnapshotStillValid(_selectedSnapshot, profileKey))
            {
                _selectedSnapshot = null;
            }

            MPButton firstSelectable = null;
            MPButton selectedCardButton = null;
            CheckpointSnapshot firstPopulated = null;

            if (_filterMode == VaultFilterMode.All)
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
                string bucketKey = _filterMode == VaultFilterMode.Multiplayer
                    ? VaultRepository.MultiplayerBucket
                    : GetSelectedSurvivorBodyName();
                var slots = VaultRepository.GetSurvivorSlots(profileKey, bucketKey);

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

            UpdateCardVisualsInPlace();
            UpdateInspectorPanel();
            WireModalNavigation();

            var focusTarget = selectedCardButton ?? firstSelectable ?? _closeButton;
            if (focusTarget != null)
            {
                ScheduleFocusNextFrame(focusTarget.gameObject);
            }
        }

        private void SelectSnapshotInPlace(CheckpointSnapshot snapshot)
        {
            if (IsSameSnapshot(_selectedSnapshot, snapshot) && (_selectedSnapshot == null) == (snapshot == null))
            {
                return;
            }

            _selectedSnapshot = snapshot;
            UpdateCardVisualsInPlace();
            UpdateInspectorPanel();
            WireModalNavigation();
        }

        private void ToggleLockOnSnapshot(CheckpointSnapshot snapshot)
        {
            if (snapshot == null) return;
            if ((Time.unscaledTime - _lastLockToggleTime) < 0.18f) return;
            _lastLockToggleTime = Time.unscaledTime;

            string profileKey = VaultRepository.GetCurrentProfileKey();
            string bucketKey = snapshot.GetStorageBucket();
            if (!VaultRepository.ToggleSlotLock(profileKey, bucketKey, snapshot.slotIndex, out bool newLocked))
            {
                return;
            }

            snapshot.isLocked = newLocked;
            if (IsSameSnapshot(_selectedSnapshot, snapshot))
            {
                _selectedSnapshot.isLocked = newLocked;
            }

            // Keep any matching SlotCardView snapshot synchronized
            for (int i = 0; i < _cardViews.Count; i++)
            {
                var v = _cardViews[i];
                if (v?.Snapshot != null && IsSameSnapshot(v.Snapshot, snapshot))
                {
                    v.Snapshot.isLocked = newLocked;
                }
            }

            UpdateCardVisualsInPlace();
            UpdateInspectorPanel();
            WireModalNavigation();

            // If user locked a slot while focus was sitting on Delete, shift focus cleanly to Load / Card
            var activeEs = ResolveActiveEventSystem();
            if (newLocked && activeEs != null && _deleteButton != null &&
                activeEs.currentSelectedGameObject == _deleteButton.gameObject)
            {
                MPButton fallback = GetPrimaryActionButton() ?? GetSelectedOrFirstCardButton() ?? _closeButton;
                if (fallback != null)
                {
                    ScheduleFocusNextFrame(fallback.gameObject);
                }
            }
        }

        private static Color GetCardBackgroundColor(CheckpointSnapshot snapshot, bool isSelected)
        {
            if (snapshot == null) return UiFactory.CardEmptyColor;
            if (snapshot.IsMultiplayer())
                return isSelected ? UiFactory.CardMultiplayerSelectedColor : UiFactory.CardMultiplayerBgColor;
            if (snapshot.IsSpecialModeOrArtifact())
                return isSelected ? UiFactory.CardSpecialModeSelectedColor : UiFactory.CardSpecialModeBgColor;
            return isSelected ? UiFactory.CardSelectedColor : UiFactory.CardBgColor;
        }

        private static Color GetCardAccentColor(CheckpointSnapshot snapshot, bool missingSurvivor, bool modMismatch)
        {
            if (snapshot == null) return new Color(0.28f, 0.34f, 0.44f, 0.7f);
            if (missingSurvivor) return UiFactory.AccentRed;
            if (snapshot.isLocked) return UiFactory.AccentAmber;
            if (snapshot.IsMultiplayer()) return UiFactory.AccentRed;
            if (snapshot.IsSpecialModeOrArtifact()) return UiFactory.AccentGreen;
            if (modMismatch) return UiFactory.AccentAmber;
            return UiFactory.AccentCyan;
        }

        private void UpdateLeftPanelThemeColor()
        {
            if (_leftPanelImage == null) return;

            bool isMpContext = false;
            bool isSpecialContext = false;

            if (_filterMode == VaultFilterMode.Multiplayer)
            {
                isMpContext = true;
            }
            else if (_selectedSnapshot != null)
            {
                isMpContext = _selectedSnapshot.IsMultiplayer();
                isSpecialContext = !isMpContext && _selectedSnapshot.IsSpecialModeOrArtifact();
            }
            else if (PreGameController.instance != null)
            {
                isMpContext = NetworkUser.readOnlyInstancesList != null && NetworkUser.readOnlyInstancesList.Count > 1;
                string gm = GameModeCatalog.GetGameModeName(PreGameController.instance.gameModeIndex) ?? string.Empty;
                if (!isMpContext && gm.IndexOf("InfiniteTower", StringComparison.OrdinalIgnoreCase) >= 0)
                {
                    isSpecialContext = true;
                }
            }

            if (isMpContext)
                _leftPanelImage.color = UiFactory.SubPanelMultiplayerBgColor;
            else if (isSpecialContext)
                _leftPanelImage.color = UiFactory.SubPanelSpecialModeBgColor;
            else
                _leftPanelImage.color = UiFactory.SubPanelBgColor;
        }

        private void UpdateAllLockedWarningBanner()
        {
            if (_allLockedWarningBanner == null || _allLockedWarningText == null) return;

            string profileKey = VaultRepository.GetCurrentProfileKey();
            string checkBucket = _selectedSnapshot != null
                ? _selectedSnapshot.GetStorageBucket()
                : (_filterMode == VaultFilterMode.Multiplayer
                    ? VaultRepository.MultiplayerBucket
                    : (_filterMode == VaultFilterMode.CurrentSurvivor ? GetSelectedSurvivorBodyName() : string.Empty));

            bool allLocked = !string.IsNullOrEmpty(checkBucket) && VaultRepository.AreAllSlotsLocked(profileKey, checkBucket);
            _allLockedWarningBanner.SetActive(allLocked);

            if (allLocked)
            {
                string displayLabel = string.Equals(checkBucket, VaultRepository.MultiplayerBucket, StringComparison.OrdinalIgnoreCase)
                    ? "Multiplayer"
                    : (_selectedSnapshot != null
                        ? CeniReloadStrings.LocalizeGameToken(_selectedSnapshot.survivorDisplayToken, _selectedSnapshot.survivorBodyName)
                        : GetSelectedSurvivorDisplayName());
                _allLockedWarningText.text = CeniReloadStrings.Format("CENIRELOAD_UI_ALL_LOCKED_WARN", displayLabel);
            }

            float bottomOffset = allLocked ? 72f : 40f;
            if (_leftPanelRect != null)
            {
                _leftPanelRect.offsetMin = new Vector2(_leftPanelRect.offsetMin.x, bottomOffset);
            }
            if (_rightPanelRect != null)
            {
                _rightPanelRect.offsetMin = new Vector2(_rightPanelRect.offsetMin.x, bottomOffset);
            }
        }

        private void UpdateCardVisualsInPlace()
        {
            for (int i = 0; i < _cardViews.Count; i++)
            {
                var view = _cardViews[i];
                if (view == null) continue;

                bool hasData = view.Snapshot != null;
                bool isSelected = hasData && IsSameSnapshot(view.Snapshot, _selectedSnapshot);
                bool missingSurvivor = hasData && view.Snapshot.HasMissingSurvivors();
                bool modMismatch = hasData && view.Snapshot.HasModMismatch();
                bool isLocked = hasData && view.Snapshot.isLocked;

                Color bg = GetCardBackgroundColor(view.Snapshot, isSelected);
                Color accent = GetCardAccentColor(view.Snapshot, missingSurvivor, modMismatch);
                Color borderColor = isSelected
                    ? (missingSurvivor ? UiFactory.AccentRed : (isLocked ? UiFactory.AccentAmber : UiFactory.AccentCyan))
                    : (missingSurvivor
                        ? new Color(0.88f, 0.28f, 0.32f, 0.50f)
                        : (isLocked ? new Color(0.98f, 0.78f, 0.22f, 0.65f) : new Color(1f, 1f, 1f, 0.12f)));

                if (view.Highlight != null)
                {
                    view.Highlight.SetBaseColors(bg, borderColor);
                }

                if (view.AccentStrip != null)
                {
                    view.AccentStrip.color = accent;
                }

                if (view.LockBadge != null)
                {
                    view.LockBadge.gameObject.SetActive(isLocked);
                }

                if (view.PortraitFrame != null && hasData)
                {
                    Color portraitBorder = missingSurvivor
                        ? UiFactory.AccentRed
                        : (isLocked ? UiFactory.AccentAmber : (isSelected ? UiFactory.AccentCyan : new Color(1f, 1f, 1f, 0.22f)));
                    var pOutline = view.PortraitFrame.GetComponent<Outline>();
                    if (pOutline != null)
                    {
                        pOutline.effectColor = portraitBorder;
                    }
                }
            }

            UpdateLeftPanelThemeColor();
        }

        private MPButton GetPrimaryActionButton()
        {
            if (_loadButton != null && _loadButton.gameObject.activeInHierarchy && _loadButton.interactable)
                return _loadButton;
            if (_deleteConfirmBar != null && _deleteConfirmBar.activeSelf && _confirmNoButton != null && _confirmNoButton.interactable)
                return _confirmNoButton;
            if (_deleteButton != null && _deleteButton.gameObject.activeInHierarchy && _deleteButton.interactable)
                return _deleteButton;
            return _closeButton;
        }

        private MPButton GetSelectedOrFirstCardButton()
        {
            MPButton first = null;
            for (int i = 0; i < _cardViews.Count; i++)
            {
                var v = _cardViews[i];
                if (v == null || v.Button == null || !v.Button.gameObject.activeInHierarchy) continue;
                if (first == null) first = v.Button;
                if (v.Snapshot != null && IsSameSnapshot(v.Snapshot, _selectedSnapshot))
                {
                    return v.Button;
                }
            }
            return first;
        }

        private void FocusDefaultModalControl()
        {
            if (_notice != null && _notice.IsOpen) { SetSelectedGameObject(_notice.Confirm.gameObject); return; }
            MPButton target = GetSelectedOrFirstCardButton() ?? GetPrimaryActionButton() ?? _closeButton;
            if (target != null && target.gameObject.activeInHierarchy)
            {
                SetSelectedGameObject(target.gameObject);
            }
        }

        private void WireModalNavigation()
        {
            if (_notice != null && _notice.IsOpen) return;
            MPButton selectedCard = GetSelectedOrFirstCardButton();
            MPButton rightAction = GetPrimaryActionButton();

            bool loadActive = _loadButton != null && _loadButton.gameObject.activeInHierarchy && _loadButton.interactable;
            bool deleteActive = _deleteButton != null && _deleteButton.gameObject.activeInHierarchy && _deleteButton.interactable;
            bool confirmActive = _deleteConfirmBar != null && _deleteConfirmBar.activeSelf;

            // Left column: slot cards
            for (int i = 0; i < _cardViews.Count; i++)
            {
                var btn = _cardViews[i]?.Button;
                if (btn == null) continue;

                MPButton prev = i > 0 ? _cardViews[i - 1]?.Button : _filterButton;
                MPButton next = (i + 1) < _cardViews.Count ? _cardViews[i + 1]?.Button : null;

                SetExplicitNavigation(btn, prev, next, null, rightAction);
            }

            // Header bar: Filter <-> Close
            SetExplicitNavigation(_filterButton, null, selectedCard ?? rightAction, null, _closeButton);
            SetExplicitNavigation(_closeButton, null, rightAction ?? selectedCard, _filterButton, null);

            // Right inspector: Load, Delete / Confirm Yes & No
            MPButton belowLoad = confirmActive
                ? _confirmNoButton
                : (deleteActive ? _deleteButton : null);

            if (_loadButton != null)
            {
                SetExplicitNavigation(_loadButton, _closeButton, belowLoad, selectedCard ?? _filterButton, null);
            }

            if (_deleteButton != null)
            {
                SetExplicitNavigation(_deleteButton, loadActive ? _loadButton : _closeButton, null, selectedCard ?? _filterButton, null);
            }

            if (_confirmYesButton != null)
            {
                SetExplicitNavigation(_confirmYesButton, loadActive ? _loadButton : _closeButton, null, selectedCard ?? _filterButton, _confirmNoButton);
            }

            if (_confirmNoButton != null)
            {
                SetExplicitNavigation(_confirmNoButton, loadActive ? _loadButton : _closeButton, null, _confirmYesButton, null);
            }
            var utilities = new List<MPButton>();
            foreach (var button in new[] { _backupButton, _reportButton, _cancelPreparedButton, _loadButton, _deleteButton })
                if (button != null && button.gameObject.activeInHierarchy && button.interactable) utilities.Add(button);
            for (int i = 0; i < utilities.Count; i++)
                SetExplicitNavigation(utilities[i], i == 0 ? _closeButton : utilities[i - 1],
                    i + 1 < utilities.Count ? utilities[i + 1] : _closeButton, selectedCard ?? _filterButton, null);
            if (confirmActive)
            {
                SetExplicitNavigation(_loadButton, _reportButton, _confirmNoButton, selectedCard, null);
                SetExplicitNavigation(_confirmNoButton, _loadButton, _closeButton, _confirmYesButton, null);
            }
        }

        private static void SetExplicitNavigation(Selectable target, Selectable up, Selectable down, Selectable left, Selectable right)
        {
            if (target == null) return;
            var nav = target.navigation;
            nav.mode = Navigation.Mode.Explicit;
            nav.selectOnUp = up;
            nav.selectOnDown = down;
            nav.selectOnLeft = left;
            nav.selectOnRight = right;
            target.navigation = nav;
        }

        private void ScheduleFocusNextFrame(GameObject target)
        {
            if (target == null) return;
            if (!isActiveAndEnabled || _modalRoot == null || !_modalRoot.activeInHierarchy)
            {
                SetSelectedGameObject(target);
                return;
            }

            if (_focusCoroutine != null)
            {
                StopCoroutine(_focusCoroutine);
            }
            _focusCoroutine = StartCoroutine(EnsureFocusNextFrame(target));
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

        private void SetSelectedGameObject(GameObject target)
        {
            if (target == null) return;
            EventSystem es = ResolveActiveEventSystem();
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
                   string.Equals(a.GetStorageBucket(), b.GetStorageBucket(), StringComparison.OrdinalIgnoreCase);
        }

        private bool IsSnapshotStillValid(CheckpointSnapshot snap, string profileKey)
        {
            if (snap == null) return false;
            return VaultRepository.LoadSlot(profileKey, snap.GetStorageBucket(), snap.slotIndex) != null;
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
            bool isLocked = hasData && snapshot.isLocked;

            Color bg = GetCardBackgroundColor(snapshot, isSelected);
            Color accent = GetCardAccentColor(snapshot, missingSurvivor, modMismatch);
            Color borderColor = isSelected
                ? (missingSurvivor ? UiFactory.AccentRed : (isLocked ? UiFactory.AccentAmber : UiFactory.AccentCyan))
                : (missingSurvivor
                    ? new Color(0.88f, 0.28f, 0.32f, 0.50f)
                    : (isLocked ? new Color(0.98f, 0.78f, 0.22f, 0.65f) : new Color(1f, 1f, 1f, 0.12f)));

            var cardImg = UiFactory.CreatePanel($"CENIRELOAD_SlotCard_{slotNumber}", _cardsContainer, bg, borderColor);
            var cardOutline = cardImg.GetComponent<Outline>();
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
            colors.highlightedColor = Color.white;
            colors.selectedColor = Color.white;
            colors.pressedColor = new Color(0.84f, 0.86f, 0.90f, 1f);
            colors.colorMultiplier = 1f;
            btn.colors = colors;

            var nav = btn.navigation;
            nav.mode = Navigation.Mode.Explicit;
            btn.navigation = nav;

            var highlight = cardImg.gameObject.AddComponent<VaultFocusHighlight>();
            highlight.Configure(cardImg, cardOutline, bg, borderColor, () =>
            {
                // Live in-place preview while navigating with D-pad/stick (never destroys the card!)
                SelectSnapshotInPlace(snapshot);
            });

            if (hasData)
            {
                var rightClick = cardImg.gameObject.AddComponent<VaultRightClickHandler>();
                rightClick.Initialize(() =>
                {
                    if (!CanHandleModalButtonClick()) return;
                    SelectSnapshotInPlace(snapshot);
                    ToggleLockOnSnapshot(snapshot);
                });
            }

            btn.onClick.AddListener(() =>
            {
                if (!CanHandleModalButtonClick()) return;
                SelectSnapshotInPlace(snapshot);

                // In Gamepad mode, pressing A on a slot card moves focus straight to LOAD CHECKPOINT (or DELETE SLOT)
                if (snapshot != null && IsGamepadActive())
                {
                    MPButton actionBtn = GetPrimaryActionButton();
                    if (actionBtn != null && actionBtn != btn)
                    {
                        ScheduleFocusNextFrame(actionBtn.gameObject);
                    }
                }
            });

            var autoScroll = cardImg.gameObject.AddComponent<VaultAutoScroll>();
            autoScroll.Initialize(_scrollRect, cardImg.rectTransform);

            Image portraitFrame = null;
            RawImage lockBadge = null;
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
                    : (isLocked ? UiFactory.AccentAmber : (isSelected ? UiFactory.AccentCyan : new Color(1f, 1f, 1f, 0.22f)));

                portraitFrame = UiFactory.CreatePortraitFrame("Portrait", cardImg.transform, portrait, portraitBorder);
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
                mid.rectTransform.offsetMax = new Vector2(-46f, 0f);

                var bot = UiFactory.CreateText("Bot", cardImg.transform, $"{snapshot.FormatRunTime()}  |  {diffName}  |  {snapshot.GetTotalItemCount()} {CeniReloadStrings.Get("CENIRELOAD_UI_ITEMS_SHORT")}", 12f, UiFactory.TextMuted, TextAlignmentOptions.Left);
                bot.enableWordWrapping = false;
                bot.overflowMode = TextOverflowModes.Ellipsis;
                bot.rectTransform.anchorMin = new Vector2(0f, 0f);
                bot.rectTransform.anchorMax = new Vector2(1f, 0.32f);
                bot.rectTransform.offsetMin = new Vector2(80f, 6f);
                bot.rectTransform.offsetMax = new Vector2(-46f, 0f);

                // Glowing RoR2 Character-Select-style 2D Padlock badge in bottom-right of card
                lockBadge = UiFactory.CreatePadlockBadge("LockBadge", cardImg.transform, 30f);
                var lockRect = lockBadge.rectTransform;
                lockRect.anchorMin = new Vector2(1f, 0f);
                lockRect.anchorMax = new Vector2(1f, 0f);
                lockRect.pivot = new Vector2(1f, 0f);
                lockRect.anchoredPosition = new Vector2(-10f, 8f);
                lockBadge.gameObject.SetActive(isLocked);
            }

            _cardViews.Add(new SlotCardView
            {
                SlotNumber = slotNumber,
                Snapshot = snapshot,
                Button = btn,
                AccentStrip = strip,
                PortraitFrame = portraitFrame,
                LockBadge = lockBadge,
                Highlight = highlight
            });

            _spawnedCards.Add(cardImg.gameObject);
            return btn;
        }

        private void UpdateInspectorPanel()
        {
            SetDeleteConfirmVisible(false, skipFocusShift: true);
            _backupButton.interactable = _selectedSnapshot != null;
            _cancelPreparedButton.interactable = SnapshotRestore.WaitingForLobby;
            _loadButtonLabel.text = CeniReloadStrings.Get(_selectedSnapshot != null && _selectedSnapshot.isBackupPreview ? "CENIRELOAD_UI_RECOVER" :
                SnapshotRestore.IsPrepared(_selectedSnapshot) ? "CENIRELOAD_UI_LAUNCH_PREPARED" :
                _selectedSnapshot != null && _selectedSnapshot.IsMultiplayer() ? "CENIRELOAD_UI_PREPARE" : "CENIRELOAD_UI_BTN_LOAD");

            if (_selectedSnapshot == null)
            {
                if (_inspectorPortraitFrame != null)
                {
                    _inspectorPortraitFrame.gameObject.SetActive(false);
                }
                if (_inspectorLockBadge != null)
                {
                    _inspectorLockBadge.gameObject.SetActive(false);
                }

                _inspectorTitle.rectTransform.offsetMin = new Vector2(18f, -62f);
                _inspectorTitle.rectTransform.offsetMax = new Vector2(-18f, -14f);
                _inspectorTitle.text = CeniReloadStrings.Get("CENIRELOAD_UI_SLOT_EMPTY");
                _inspectorBody.text = CeniReloadStrings.Get("CENIRELOAD_UI_INSPECT_EMPTY");
                _inspectorModStatus.text = string.Empty;
                _loadButton.interactable = false;
                _deleteButton.interactable = false;
                _deleteButtonLabel.text = CeniReloadStrings.Get("CENIRELOAD_UI_BTN_DELETE");
                UpdateAllLockedWarningBanner();
                UpdateLeftPanelThemeColor();
                WireModalNavigation();
                return;
            }

            var s = _selectedSnapshot;
            bool missingSurvivor = s.HasMissingSurvivors();
            bool isLocked = s.isLocked;

            if (_inspectorPortraitFrame != null)
            {
                _inspectorPortraitFrame.gameObject.SetActive(true);
                Texture portrait = PortraitCache.GetPortrait(s.survivorBodyName);
                UiFactory.UpdatePortraitFrame(
                    _inspectorPortraitFrame,
                    portrait,
                    missingSurvivor ? UiFactory.AccentRed : (isLocked ? UiFactory.AccentAmber : UiFactory.AccentCyan)
                );
                _inspectorTitle.rectTransform.offsetMin = new Vector2(76f, -62f);
            }

            if (_inspectorLockBadge != null)
            {
                _inspectorLockBadge.gameObject.SetActive(isLocked);
            }
            _inspectorTitle.rectTransform.offsetMax = new Vector2(isLocked ? -58f : -18f, -14f);

            string survName = CeniReloadStrings.LocalizeGameToken(s.survivorDisplayToken, s.survivorBodyName);
            string stageName = CeniReloadStrings.LocalizeGameToken(s.stageDisplayToken, s.stageSceneName);
            string diffName = CeniReloadStrings.LocalizeGameToken(s.difficultyDisplayToken, s.difficultyName);
            var firstPlayer = (s.players != null && s.players.Count > 0) ? s.players[0] : null;
            uint gold = firstPlayer != null ? firstPlayer.money : 0;
            uint voidCoins = firstPlayer != null ? firstPlayer.voidCoins : 0;
            int tempItems = s.GetTotalTempItemCount();
            string tempSuffix = tempItems > 0 ? $" (+{tempItems} temp)" : string.Empty;
            string waveLine = s.simulacrumWaveIndex > 0
                ? $"<b>{CeniReloadStrings.Get("CENIRELOAD_UI_LABEL_WAVE")}:</b> {s.simulacrumWaveIndex}\n"
                : string.Empty;
            string playersRosterLine = BuildMultiplayerRosterText(s);

            _inspectorTitle.text = s.IsMultiplayer()
                ? $"{CeniReloadStrings.Format("CENIRELOAD_UI_SLOT_LABEL", s.slotIndex)} — MP ({s.players.Count}P)"
                : $"{CeniReloadStrings.Format("CENIRELOAD_UI_SLOT_LABEL", s.slotIndex)} — {survName}";
            _inspectorBody.text =
                $"<b>{CeniReloadStrings.Get("CENIRELOAD_UI_LABEL_STAGE")}:</b> <color=#38bdf8>{stageName}</color>\n" +
                $"<b>{CeniReloadStrings.Get("CENIRELOAD_UI_LABEL_STAGE_NUM")}:</b> {s.stageClearCount + 1}\n" +
                waveLine +
                $"<b>{CeniReloadStrings.Get("CENIRELOAD_UI_LABEL_LOOP")}:</b> {s.loopClearCount}\n" +
                $"<b>{CeniReloadStrings.Get("CENIRELOAD_UI_LABEL_TIME")}:</b> {s.FormatRunTime()}\n" +
                $"<b>{CeniReloadStrings.Get("CENIRELOAD_UI_LABEL_DIFFICULTY")}:</b> {diffName}\n" +
                $"<b>{CeniReloadStrings.Get("CENIRELOAD_UI_LABEL_GOLD")}:</b> ${gold} / {voidCoins}\n" +
                $"<b>{CeniReloadStrings.Get("CENIRELOAD_UI_LABEL_ITEMS")}:</b> {s.GetTotalItemCount()}{tempSuffix} / {s.GetTotalMinionCount()}\n" +
                $"<b>{CeniReloadStrings.Get("CENIRELOAD_UI_LABEL_SAVED_AT")}:</b> {s.FormatSavedDate()}" +
                playersRosterLine;

            _inspectorBody.rectTransform.sizeDelta = new Vector2(0f, Mathf.Max(_inspectorScroll.viewport.rect.height,
                _inspectorBody.GetPreferredValues(_inspectorBody.text, _inspectorScroll.viewport.rect.width, 0f).y + 8f));
            if (missingSurvivor)
            {
                _inspectorModStatus.fontStyle = FontStyles.Bold;
                _inspectorModStatus.color = UiFactory.AccentRed;
                _inspectorModStatus.text = CeniReloadStrings.Format(
                    "CENIRELOAD_UI_MISSING_SURVIVOR_WARN",
                    s.GetFirstMissingSurvivorName()
                );
                _loadButton.interactable = true;
                _deleteButton.interactable = !isLocked;
            }
            else if (isLocked)
            {
                bool mismatch = s.HasModMismatch();
                _inspectorModStatus.fontStyle = FontStyles.Bold;
                _inspectorModStatus.color = UiFactory.AccentAmber;
                _inspectorModStatus.text = mismatch
                    ? $"{CeniReloadStrings.Get("CENIRELOAD_UI_LOCK_PROTECTED")}\n{CeniReloadStrings.Get("CENIRELOAD_UI_MOD_MISMATCH")}"
                    : CeniReloadStrings.Get("CENIRELOAD_UI_LOCK_PROTECTED");

                _loadButton.interactable = true;
                _deleteButton.interactable = false;
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

            var check = SnapshotRestore.ValidateCheckpoint(s);
            if (!check.CanLoad)
            {
                _inspectorModStatus.text = CeniReloadStrings.Get("CENIRELOAD_UI_VALIDATION_TITLE") + ": " + check.Issues.Find(i => i.Blocking);
                _inspectorModStatus.color = UiFactory.AccentRed;
                // Keep the action available to open the full scrollable error report.
            }
            if (s.isBackupPreview)
            {
                _inspectorModStatus.text = CeniReloadStrings.Get("CENIRELOAD_UI_BACKUP_NOTICE");
                _inspectorModStatus.color = UiFactory.AccentAmber;
                _loadButton.interactable = true;
                _deleteButton.interactable = false;
            }
            _subtitleText.text = CeniReloadStrings.Get(SnapshotRestore.IsPrepared(s) ? "CENIRELOAD_LOBBY_READY" : "CENIRELOAD_UI_SUBTITLE");
            _subtitleText.enableAutoSizing = true; _subtitleText.fontSizeMin = 9f; _subtitleText.fontSizeMax = 14f;
            _backupButton.interactable = VaultRepository.LoadBackup(VaultRepository.GetCurrentProfileKey(), s.GetStorageBucket(), s.slotIndex) != null;

            _deleteButtonLabel.text = isLocked
                ? CeniReloadStrings.Get("CENIRELOAD_UI_BTN_LOCKED")
                : CeniReloadStrings.Get("CENIRELOAD_UI_BTN_DELETE");

            UpdateAllLockedWarningBanner();
            UpdateLeftPanelThemeColor();
            WireModalNavigation();
        }

        private void SetDeleteConfirmVisible(bool visible, bool skipFocusShift = false)
        {
            bool wasConfirmVisible = _deleteConfirmBar != null && _deleteConfirmBar.activeSelf;
            if (_deleteConfirmBar != null) _deleteConfirmBar.SetActive(visible);
            if (_deleteButton != null) _deleteButton.gameObject.SetActive(!visible);

            WireModalNavigation();

            if (!skipFocusShift && _modalRoot != null && _modalRoot.activeInHierarchy)
            {
                if (visible && _confirmNoButton != null)
                {
                    ScheduleFocusNextFrame(_confirmNoButton.gameObject);
                }
                else if (!visible && wasConfirmVisible && _deleteButton != null && _deleteButton.gameObject.activeInHierarchy && _deleteButton.interactable)
                {
                    ScheduleFocusNextFrame(_deleteButton.gameObject);
                }
            }
        }

        private static void PositionUtilityButton(MPButton button, float bottom)
        {
            var rect = button.GetComponent<RectTransform>();
            rect.anchorMin = new Vector2(0f, 0f); rect.anchorMax = new Vector2(1f, 0f);
            rect.pivot = new Vector2(0.5f, 0f); rect.sizeDelta = new Vector2(-36f, 28f); rect.anchoredPosition = new Vector2(0f, bottom);
        }

        private void ShowNotice(string text, string action = null, Action callback = null)
        {
            _notice.Open(text, action, callback);
            ScheduleFocusNextFrame(_notice.Confirm.gameObject);
        }

        private void OnClickLastReport()
        {
            if (!CanHandleModalButtonClick()) return;
            var report = SnapshotRestore.LastOutcome;
            ShowNotice(report == null ? CeniReloadStrings.Get("CENIRELOAD_UI_NO_REPORT") :
                CeniReloadStrings.Format(report.StatusKey, report.PlayersRestored, report.AlliesRestored) + "\n\n" + report.Details);
        }

        private void OnClickBackup()
        {
            if (!CanHandleModalButtonClick() || _selectedSnapshot == null) return;
            var backup = VaultRepository.LoadBackup(VaultRepository.GetCurrentProfileKey(), _selectedSnapshot.GetStorageBucket(), _selectedSnapshot.slotIndex);
            if (backup == null) return;
            _selectedSnapshot = backup;
            UpdateInspectorPanel();
            ShowNotice(CeniReloadStrings.Get("CENIRELOAD_UI_BACKUP_NOTICE"), CeniReloadStrings.Get("CENIRELOAD_UI_RECOVER"), RecoverSelectedBackup);
        }

        private void RecoverSelectedBackup()
        {
            if (_selectedSnapshot == null) return;
            var selected = _selectedSnapshot;
            SnapshotRestore.CancelPrepared();
            bool recovered = VaultRepository.RecoverBackup(VaultRepository.GetCurrentProfileKey(), selected.GetStorageBucket(), selected.slotIndex);
            _selectedSnapshot = null;
            RebuildSlotCards();
            ShowNotice(CeniReloadStrings.Get(recovered ? "CENIRELOAD_UI_BACKUP_DONE" : "CENIRELOAD_UI_BACKUP_FAILED"));
        }

        private void OnClickLoadSelected()
        {
            if (!CanHandleModalButtonClick() || _selectedSnapshot == null) return;
            if (_selectedSnapshot.isBackupPreview)
            {
                ShowNotice(CeniReloadStrings.Get("CENIRELOAD_UI_BACKUP_NOTICE"), CeniReloadStrings.Get("CENIRELOAD_UI_RECOVER"), RecoverSelectedBackup);
                return;
            }
            var check = SnapshotRestore.ValidateCheckpoint(_selectedSnapshot);
            if (!check.CanLoad) { ShowNotice(CeniReloadStrings.Get("CENIRELOAD_UI_VALIDATION_TITLE") + "\n\n" + check); return; }
            var selected = _selectedSnapshot;
            if (check.Issues.Count > 0)
                ShowNotice(CeniReloadStrings.Get("CENIRELOAD_UI_VALIDATION_TITLE") + "\n\n" + check,
                    CeniReloadStrings.Get("CENIRELOAD_UI_LOAD_WARNINGS"), () => StartSelected(selected));
            else StartSelected(selected);
        }

        private void StartSelected(CheckpointSnapshot selected)
        {
            bool started = _isMainMenuMode ? SnapshotRestore.BeginLoadFromMainMenu(selected) : SnapshotRestore.BeginLoadFromLobby(selected);
            if (!started) { ShowNotice(SnapshotRestore.ValidateCheckpoint(selected).ToString()); return; }
            if (SnapshotRestore.WaitingForLobby && !_isMainMenuMode) { UpdateInspectorPanel(); return; }
            SetModalOpen(false);
        }

        private string BuildMultiplayerRosterText(CheckpointSnapshot snap)
        {
            if (snap == null || !snap.IsMultiplayer() || snap.players == null || snap.players.Count == 0)
                return string.Empty;

            var lobbyIds = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
            bool hasLobby = !_isMainMenuMode && NetworkUser.readOnlyInstancesList != null && NetworkUser.readOnlyInstancesList.Count > 0;
            if (hasLobby)
            {
                for (int i = 0; i < NetworkUser.readOnlyInstancesList.Count; i++)
                {
                    string id = SnapshotCapture.BuildPlayerIdentity(NetworkUser.readOnlyInstancesList[i]);
                    if (!string.IsNullOrEmpty(id)) lobbyIds.Add(id);
                }
            }

            var sb = new System.Text.StringBuilder();
            int presentCount = snap.players.FindAll(p => p != null && !string.IsNullOrEmpty(p.playerId) && lobbyIds.Contains(p.playerId)).Count;
            sb.Append($"\n<b>{CeniReloadStrings.Get("CENIRELOAD_UI_LABEL_PLAYERS")}:</b> {presentCount}/{snap.players.Count}\n");
            for (int i = 0; i < snap.players.Count; i++)
            {
                var p = snap.players[i];
                if (p == null) continue;
                if (i > 0) sb.Append(", ");

                string pName = CheckpointIssue.Escape(!string.IsNullOrEmpty(p.playerName) ? p.playerName : $"P{i + 1}");
                if (hasLobby && !string.IsNullOrEmpty(p.playerId))
                {
                    bool present = lobbyIds.Contains(p.playerId);
                    string badge = present
                        ? $"<color=#4ade80>{pName}</color>"
                        : $"<color=#f87171>{pName} ({CeniReloadStrings.Get("CENIRELOAD_UI_PLAYER_ABSENT")})</color>";
                    sb.Append(badge);
                }
                else
                {
                    sb.Append(pName);
                }
            }
            return sb.ToString();
        }

        private void OnConfirmDeleteSelected()
        {
            if (!CanHandleModalButtonClick()) return;
            if (_selectedSnapshot == null || _selectedSnapshot.isLocked) return;
            string profileKey = VaultRepository.GetCurrentProfileKey();
            VaultRepository.DeleteSlot(profileKey, _selectedSnapshot.GetStorageBucket(), _selectedSnapshot.slotIndex);
            _selectedSnapshot = null;
            SetDeleteConfirmVisible(false, skipFocusShift: true);
            RebuildSlotCards();
        }
    }
}
