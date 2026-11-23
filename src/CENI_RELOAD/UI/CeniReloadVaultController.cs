using System;
using System.Collections.Generic;
using Jaosnake.CeniReload.Core;
using Jaosnake.CeniReload.Localization;
using Jaosnake.CeniReload.Model;
using Jaosnake.CeniReload.Storage;
using RoR2;
using RoR2.UI;
using TMPro;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

namespace Jaosnake.CeniReload.UI
{
    public class CeniReloadVaultController : MonoBehaviour
    {
        private CharacterSelectController _lobby;
        private Button _triggerButton;
        private TextMeshProUGUI _triggerLabel;

        private GameObject _modalRoot;
        private TextMeshProUGUI _titleText;
        private TextMeshProUGUI _subtitleText;
        private TextMeshProUGUI _filterButtonLabel;
        private TextMeshProUGUI _closeButtonLabel;

        private Transform _cardsContainer;
        private TextMeshProUGUI _inspectorTitle;
        private TextMeshProUGUI _inspectorBody;
        private TextMeshProUGUI _inspectorModStatus;

        private Button _loadButton;
        private TextMeshProUGUI _loadButtonLabel;
        private Button _deleteButton;
        private TextMeshProUGUI _deleteButtonLabel;

        private GameObject _deleteConfirmBar;
        private TextMeshProUGUI _confirmPromptText;
        private TextMeshProUGUI _confirmYesLabel;
        private TextMeshProUGUI _confirmNoLabel;

        private bool _showAllSurvivors;
        private CheckpointSnapshot _selectedSnapshot;
        private readonly List<GameObject> _spawnedCards = new List<GameObject>();

        public static CeniReloadVaultController AttachTo(CharacterSelectController controller)
        {
            if (controller == null) return null;
            var existing = controller.GetComponent<CeniReloadVaultController>();
            if (existing != null) return existing;

            var vault = controller.gameObject.AddComponent<CeniReloadVaultController>();
            vault.Initialize(controller);
            return vault;
        }

        private void Initialize(CharacterSelectController controller)
        {
            _lobby = controller;
            BuildTriggerButton();
            BuildVaultModal();
            SetModalOpen(false);
        }

        private void Update()
        {
            if (_modalRoot != null && _modalRoot.activeSelf)
            {
                if (Input.GetKeyDown(KeyCode.Escape) || Input.GetKeyDown(KeyCode.JoystickButton1))
                {
                    SetModalOpen(false);
                }
            }
        }

        public void RefreshTriggerLabel()
        {
            if (_triggerLabel != null)
            {
                _triggerLabel.text = CeniReloadStrings.Get("CENIRELOAD_BTN_OPEN_VAULT");
            }
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

        private void BuildTriggerButton()
        {
            Transform hostParent = _lobby.transform;
            var canvas = _lobby.GetComponentInParent<Canvas>();
            if (canvas != null) hostParent = canvas.transform;

            _triggerButton = UiFactory.CreateButton(
                "CENIRELOAD_OpenVaultButton",
                hostParent,
                CeniReloadStrings.Get("CENIRELOAD_BTN_OPEN_VAULT"),
                new Color(0.08f, 0.24f, 0.36f, 0.96f),
                UiFactory.AccentCyan,
                17f,
                () => SetModalOpen(true),
                out _triggerLabel
            );

            var rect = _triggerButton.GetComponent<RectTransform>();
            rect.anchorMin = new Vector2(1f, 0f);
            rect.anchorMax = new Vector2(1f, 0f);
            rect.pivot = new Vector2(1f, 0f);
            rect.sizeDelta = new Vector2(236f, 44f);
            rect.anchoredPosition = new Vector2(-28f, 118f);

            var outline = _triggerButton.gameObject.GetComponent<Outline>();
            if (outline != null) outline.effectColor = UiFactory.AccentCyan;
        }

        private void BuildVaultModal()
        {
            Transform hostParent = _lobby.transform;
            var canvas = _lobby.GetComponentInParent<Canvas>();
            if (canvas != null) hostParent = canvas.transform;

            var backdrop = UiFactory.CreatePanel("CENIRELOAD_VaultModalBackdrop", hostParent, UiFactory.BackdropColor);
            UiFactory.StretchFull(backdrop.rectTransform);
            _modalRoot = backdrop.gameObject;

            var window = UiFactory.CreatePanel("CENIRELOAD_VaultWindow", backdrop.transform, UiFactory.PanelBgColor, UiFactory.AccentCyan);
            var winRect = window.rectTransform;
            winRect.anchorMin = new Vector2(0.5f, 0.5f);
            winRect.anchorMax = new Vector2(0.5f, 0.5f);
            winRect.pivot = new Vector2(0.5f, 0.5f);
            winRect.sizeDelta = new Vector2(940f, 600f);

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

            var filterBtn = UiFactory.CreateButton(
                "FilterButton",
                header.transform,
                "",
                new Color(0.12f, 0.20f, 0.32f, 1f),
                UiFactory.TextPrimary,
                14f,
                ToggleFilterMode,
                out _filterButtonLabel
            );
            var filterRect = filterBtn.GetComponent<RectTransform>();
            filterRect.anchorMin = new Vector2(1f, 0.5f);
            filterRect.anchorMax = new Vector2(1f, 0.5f);
            filterRect.pivot = new Vector2(1f, 0.5f);
            filterRect.sizeDelta = new Vector2(230f, 38f);
            filterRect.anchoredPosition = new Vector2(-126f, 0f);

            var closeBtn = UiFactory.CreateButton(
                "CloseButton",
                header.transform,
                CeniReloadStrings.Get("CENIRELOAD_UI_CLOSE"),
                new Color(0.32f, 0.14f, 0.18f, 1f),
                UiFactory.TextPrimary,
                15f,
                () => SetModalOpen(false),
                out _closeButtonLabel
            );
            var closeRect = closeBtn.GetComponent<RectTransform>();
            closeRect.anchorMin = new Vector2(1f, 0.5f);
            closeRect.anchorMax = new Vector2(1f, 0.5f);
            closeRect.pivot = new Vector2(1f, 0.5f);
            closeRect.sizeDelta = new Vector2(96f, 38f);
            closeRect.anchoredPosition = new Vector2(-20f, 0f);

            var leftPanel = UiFactory.CreatePanel("LeftSlotList", window.transform, UiFactory.SubPanelBgColor);
            var leftRect = leftPanel.rectTransform;
            leftRect.anchorMin = new Vector2(0f, 0f);
            leftRect.anchorMax = new Vector2(0.52f, 1f);
            leftRect.offsetMin = new Vector2(20f, 20f);
            leftRect.offsetMax = new Vector2(-10f, -98f);

            var scrollRect = leftPanel.gameObject.AddComponent<ScrollRect>();
            scrollRect.horizontal = false;
            scrollRect.vertical = true;
            scrollRect.scrollSensitivity = 28f;

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

            scrollRect.viewport = viewport;
            scrollRect.content = content;
            _cardsContainer = content;

            var rightPanel = UiFactory.CreatePanel("RightInspector", window.transform, UiFactory.SubPanelBgColor);
            var rightRect = rightPanel.rectTransform;
            rightRect.anchorMin = new Vector2(0.52f, 0f);
            rightRect.anchorMax = new Vector2(1f, 1f);
            rightRect.offsetMin = new Vector2(10f, 20f);
            rightRect.offsetMax = new Vector2(-20f, -98f);

            _inspectorTitle = UiFactory.CreateText("InspectorTitle", rightPanel.transform, "", 20f, UiFactory.AccentCyan, TextAlignmentOptions.Left, FontStyles.Bold);
            _inspectorTitle.rectTransform.anchorMin = new Vector2(0f, 1f);
            _inspectorTitle.rectTransform.anchorMax = new Vector2(1f, 1f);
            _inspectorTitle.rectTransform.pivot = new Vector2(0.5f, 1f);
            _inspectorTitle.rectTransform.sizeDelta = new Vector2(-32f, 36f);
            _inspectorTitle.rectTransform.anchoredPosition = new Vector2(0f, -16f);

            _inspectorBody = UiFactory.CreateText("InspectorBody", rightPanel.transform, "", 15f, UiFactory.TextPrimary, TextAlignmentOptions.TopLeft);
            _inspectorBody.rectTransform.anchorMin = new Vector2(0f, 0f);
            _inspectorBody.rectTransform.anchorMax = new Vector2(1f, 1f);
            _inspectorBody.rectTransform.offsetMin = new Vector2(18f, 155f);
            _inspectorBody.rectTransform.offsetMax = new Vector2(-18f, -58f);

            _inspectorModStatus = UiFactory.CreateText("ModStatus", rightPanel.transform, "", 13f, UiFactory.AccentGreen, TextAlignmentOptions.Left, FontStyles.Italic);
            _inspectorModStatus.rectTransform.anchorMin = new Vector2(0f, 0f);
            _inspectorModStatus.rectTransform.anchorMax = new Vector2(1f, 0f);
            _inspectorModStatus.rectTransform.pivot = new Vector2(0.5f, 0f);
            _inspectorModStatus.rectTransform.sizeDelta = new Vector2(-36f, 40f);
            _inspectorModStatus.rectTransform.anchoredPosition = new Vector2(0f, 112f);

            _loadButton = UiFactory.CreateButton(
                "LoadCheckpointButton",
                rightPanel.transform,
                CeniReloadStrings.Get("CENIRELOAD_UI_BTN_LOAD"),
                new Color(0.10f, 0.45f, 0.62f, 1f),
                Color.white,
                16f,
                OnClickLoadSelected,
                out _loadButtonLabel
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
                out _deleteButtonLabel
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

            var yesBtn = UiFactory.CreateButton("ConfirmYes", confirmPanel.transform, CeniReloadStrings.Get("CENIRELOAD_UI_BTN_CONFIRM_YES"), UiFactory.AccentRed, Color.white, 12f, OnConfirmDeleteSelected, out _confirmYesLabel);
            var yesRect = yesBtn.GetComponent<RectTransform>();
            yesRect.anchorMin = new Vector2(0.47f, 0.12f);
            yesRect.anchorMax = new Vector2(0.73f, 0.88f);
            yesRect.offsetMin = Vector2.zero;
            yesRect.offsetMax = Vector2.zero;

            var noBtn = UiFactory.CreateButton("ConfirmNo", confirmPanel.transform, CeniReloadStrings.Get("CENIRELOAD_UI_BTN_CONFIRM_NO"), new Color(0.22f, 0.26f, 0.34f, 1f), Color.white, 12f, () => SetDeleteConfirmVisible(false), out _confirmNoLabel);
            var noRect = noBtn.GetComponent<RectTransform>();
            noRect.anchorMin = new Vector2(0.75f, 0.12f);
            noRect.anchorMax = new Vector2(0.98f, 0.88f);
            noRect.offsetMin = Vector2.zero;
            noRect.offsetMax = Vector2.zero;

            _deleteConfirmBar.SetActive(false);
        }

        private void SetModalOpen(bool open)
        {
            if (_modalRoot == null) return;
            _modalRoot.SetActive(open);

            if (open)
            {
                _modalRoot.transform.SetAsLastSibling();
                RefreshLocalization();
                RebuildSlotCards();
            }
        }

        private void ToggleFilterMode()
        {
            _showAllSurvivors = !_showAllSurvivors;
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

            _filterButtonLabel.text = _showAllSurvivors
                ? CeniReloadStrings.Get("CENIRELOAD_UI_FILTER_ALL")
                : CeniReloadStrings.Format("CENIRELOAD_UI_FILTER_SURVIVOR", GetSelectedSurvivorDisplayName());
        }

        private void RebuildSlotCards()
        {
            foreach (var card in _spawnedCards)
            {
                if (card != null) Destroy(card);
            }
            _spawnedCards.Clear();

            string profileKey = VaultRepository.GetCurrentProfileKey();
            Button firstSelectable = null;
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
                    for (int i = 0; i < all.Count; i++)
                    {
                        var snap = all[i];
                        var btn = CreateSlotCard(snap.slotIndex, snap);
                        if (firstSelectable == null) firstSelectable = btn;
                        if (firstPopulated == null) firstPopulated = snap;
                    }
                }
            }
            else
            {
                string bodyName = GetSelectedSurvivorBodyName();
                var slots = VaultRepository.GetSurvivorSlots(profileKey, bodyName);
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
                }
            }

            if (_selectedSnapshot == null || !IsSnapshotStillValid(_selectedSnapshot, profileKey))
            {
                _selectedSnapshot = firstPopulated;
            }

            UpdateInspectorPanel();

            if (firstSelectable != null && EventSystem.current != null)
            {
                EventSystem.current.SetSelectedGameObject(firstSelectable.gameObject);
            }
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

        private Button CreateSlotCard(int slotNumber, CheckpointSnapshot snapshot)
        {
            bool hasData = snapshot != null;
            bool isSelected = hasData && _selectedSnapshot != null &&
                              _selectedSnapshot.slotIndex == snapshot.slotIndex &&
                              string.Equals(_selectedSnapshot.survivorBodyName, snapshot.survivorBodyName, StringComparison.Ordinal);

            Color bg = !hasData ? UiFactory.CardEmptyColor : (isSelected ? UiFactory.CardSelectedColor : UiFactory.CardBgColor);
            Color accent = !hasData ? new Color(0.28f, 0.34f, 0.44f, 0.7f) : (snapshot.HasModMismatch() ? UiFactory.AccentAmber : UiFactory.AccentCyan);

            var cardImg = UiFactory.CreatePanel($"CENIRELOAD_SlotCard_{slotNumber}", _cardsContainer, bg, isSelected ? UiFactory.AccentCyan : new Color(1f, 1f, 1f, 0.08f));
            var le = cardImg.gameObject.AddComponent<LayoutElement>();
            le.preferredHeight = 84f;

            var strip = UiFactory.CreatePanel("AccentStrip", cardImg.transform, accent);
            strip.rectTransform.anchorMin = new Vector2(0f, 0f);
            strip.rectTransform.anchorMax = new Vector2(0f, 1f);
            strip.rectTransform.pivot = new Vector2(0f, 0.5f);
            strip.rectTransform.sizeDelta = new Vector2(6f, 0f);

            var btn = cardImg.gameObject.AddComponent<Button>();
            btn.targetGraphic = cardImg;
            btn.onClick.AddListener(() =>
            {
                _selectedSnapshot = snapshot;
                RebuildSlotCards();
            });

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
                string survName = CeniReloadStrings.LocalizeGameToken(snapshot.survivorDisplayToken, snapshot.survivorBodyName);
                string stageName = CeniReloadStrings.LocalizeGameToken(snapshot.stageDisplayToken, snapshot.stageSceneName);
                string diffName = CeniReloadStrings.LocalizeGameToken(snapshot.difficultyDisplayToken, snapshot.difficultyName);

                var topLeft = UiFactory.CreateText("TopLeft", cardImg.transform, $"{slotHeader}  •  {survName}", 15f, UiFactory.TextPrimary, TextAlignmentOptions.Left, FontStyles.Bold);
                topLeft.rectTransform.anchorMin = new Vector2(0f, 0.62f);
                topLeft.rectTransform.anchorMax = new Vector2(0.70f, 1f);
                topLeft.rectTransform.offsetMin = new Vector2(18f, 0f);
                topLeft.rectTransform.offsetMax = new Vector2(0f, -6f);

                var topRight = UiFactory.CreateText("TopRight", cardImg.transform, snapshot.FormatSavedDate(), 12f, UiFactory.TextMuted, TextAlignmentOptions.Right);
                topRight.rectTransform.anchorMin = new Vector2(0.68f, 0.62f);
                topRight.rectTransform.anchorMax = new Vector2(1f, 1f);
                topRight.rectTransform.offsetMin = new Vector2(0f, 0f);
                topRight.rectTransform.offsetMax = new Vector2(-14f, -6f);

                var mid = UiFactory.CreateText("Mid", cardImg.transform, $"{stageName}  (Stage {snapshot.stageClearCount + 1})", 14f, UiFactory.AccentCyan, TextAlignmentOptions.Left);
                mid.rectTransform.anchorMin = new Vector2(0f, 0.30f);
                mid.rectTransform.anchorMax = new Vector2(1f, 0.64f);
                mid.rectTransform.offsetMin = new Vector2(18f, 0f);
                mid.rectTransform.offsetMax = new Vector2(-14f, 0f);

                var bot = UiFactory.CreateText("Bot", cardImg.transform, $"{snapshot.FormatRunTime()}  |  {diffName}  |  {snapshot.GetTotalItemCount()} items", 12f, UiFactory.TextMuted, TextAlignmentOptions.Left);
                bot.rectTransform.anchorMin = new Vector2(0f, 0f);
                bot.rectTransform.anchorMax = new Vector2(1f, 0.32f);
                bot.rectTransform.offsetMin = new Vector2(18f, 6f);
                bot.rectTransform.offsetMax = new Vector2(-14f, 0f);
            }

            _spawnedCards.Add(cardImg.gameObject);
            return btn;
        }

        private void UpdateInspectorPanel()
        {
            SetDeleteConfirmVisible(false);

            if (_selectedSnapshot == null)
            {
                _inspectorTitle.text = CeniReloadStrings.Get("CENIRELOAD_UI_SLOT_EMPTY");
                _inspectorBody.text = CeniReloadStrings.Get("CENIRELOAD_UI_INSPECT_EMPTY");
                _inspectorModStatus.text = string.Empty;
                _loadButton.interactable = false;
                _deleteButton.interactable = false;
                return;
            }

            var s = _selectedSnapshot;
            string survName = CeniReloadStrings.LocalizeGameToken(s.survivorDisplayToken, s.survivorBodyName);
            string stageName = CeniReloadStrings.LocalizeGameToken(s.stageDisplayToken, s.stageSceneName);
            string diffName = CeniReloadStrings.LocalizeGameToken(s.difficultyDisplayToken, s.difficultyName);
            uint gold = s.players.Count > 0 ? s.players[0].money : 0;
            uint voidCoins = s.players.Count > 0 ? s.players[0].voidCoins : 0;

            _inspectorTitle.text = $"{CeniReloadStrings.Format("CENIRELOAD_UI_SLOT_LABEL", s.slotIndex)} — {survName}";
            _inspectorBody.text =
                $"<b>{CeniReloadStrings.Get("CENIRELOAD_UI_LABEL_STAGE")}:</b> <color=#38bdf8>{stageName}</color>\n" +
                $"<b>{CeniReloadStrings.Get("CENIRELOAD_UI_LABEL_STAGE_NUM")}:</b> {s.stageClearCount + 1}\n" +
                $"<b>{CeniReloadStrings.Get("CENIRELOAD_UI_LABEL_TIME")}:</b> {s.FormatRunTime()}\n" +
                $"<b>{CeniReloadStrings.Get("CENIRELOAD_UI_LABEL_DIFFICULTY")}:</b> {diffName}\n" +
                $"<b>{CeniReloadStrings.Get("CENIRELOAD_UI_LABEL_GOLD")}:</b> ${gold} / {voidCoins}\n" +
                $"<b>{CeniReloadStrings.Get("CENIRELOAD_UI_LABEL_ITEMS")}:</b> {s.GetTotalItemCount()} / {s.GetTotalMinionCount()}\n" +
                $"<b>{CeniReloadStrings.Get("CENIRELOAD_UI_LABEL_SAVED_AT")}:</b> {s.FormatSavedDate()}";

            bool mismatch = s.HasModMismatch();
            _inspectorModStatus.text = mismatch
                ? CeniReloadStrings.Get("CENIRELOAD_UI_MOD_MISMATCH")
                : CeniReloadStrings.Get("CENIRELOAD_UI_MOD_OK");
            _inspectorModStatus.color = mismatch ? UiFactory.AccentAmber : UiFactory.AccentGreen;

            _loadButton.interactable = true;
            _deleteButton.interactable = true;
        }

        private void SetDeleteConfirmVisible(bool visible)
        {
            if (_deleteConfirmBar != null) _deleteConfirmBar.SetActive(visible);
            if (_deleteButton != null) _deleteButton.gameObject.SetActive(!visible);
        }

        private void OnClickLoadSelected()
        {
            if (_selectedSnapshot == null) return;
            if (SnapshotRestore.BeginLoadFromLobby(_selectedSnapshot))
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
