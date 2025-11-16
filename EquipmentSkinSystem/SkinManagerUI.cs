using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using TMPro;
using Duckov.UI;
using HarmonyLib;

namespace EquipmentSkinSystem
{
    /// <summary>
    /// 裝備外觀管理 UI
    /// </summary>
    public class SkinManagerUI : MonoBehaviour
    {
        private GameObject? _uiPanel;
        private bool _isUIVisible = false;
        private Dictionary<EquipmentSlotType, SlotUIElements> _slotUIElements = new Dictionary<EquipmentSlotType, SlotUIElements>();
        private Sprite? _roundedSprite;
        private Sprite? _roundedButtonSprite;
        private Sprite? _roundedSlotSprite;

        private class SlotUIElements
        {
            public GameObject Container = null!;
            public TextMeshProUGUI SlotNameText = null!;
            public TMP_InputField SkinItemInput = null!;
            public Toggle UseSkinToggle = null!;
            public Button ClearButton = null!;
        }

        public void Initialize()
        {
            CreateUI();
        }

        /// <summary>
        /// 創建 UI 界面
        /// </summary>
        private void CreateUI()
        {
            try
            {
                // 創建圓角 Sprite
                _roundedSprite = CreateRoundedSprite(200, 200, 20);        // 主面板
                _roundedButtonSprite = CreateRoundedSprite(100, 50, 15);   // 按鈕
                _roundedSlotSprite = CreateRoundedSprite(100, 50, 10);     // 槽位
                
                // 創建主面板
                _uiPanel = new GameObject("EquipmentSkinUI");
                _uiPanel.transform.SetParent(null);
                DontDestroyOnLoad(_uiPanel);

                Canvas canvas = _uiPanel.AddComponent<Canvas>();
                canvas.renderMode = RenderMode.ScreenSpaceOverlay;
                canvas.sortingOrder = 100; // 適中的層級

                CanvasScaler scaler = _uiPanel.AddComponent<CanvasScaler>();
                scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
                scaler.referenceResolution = new Vector2(1920, 1080);

                GraphicRaycaster raycaster = _uiPanel.AddComponent<GraphicRaycaster>();
                raycaster.ignoreReversedGraphics = true;
                raycaster.blockingObjects = GraphicRaycaster.BlockingObjects.None;
                
                // 確保有 EventSystem（UI 互動必需）
                EnsureEventSystem();
                
                Debug.Log("[EquipmentSkinSystem] Canvas created with GraphicRaycaster");

                // 創建背景面板
                GameObject backgroundPanel = CreateBackgroundPanel(_uiPanel.transform);

                // 創建標題
                CreateTitle(backgroundPanel.transform);

                // 創建槽位列表
                CreateSlotList(backgroundPanel.transform);

                // 創建底部按鈕
                CreateBottomButtons(backgroundPanel.transform);

                _uiPanel.SetActive(false);

                Debug.Log("[EquipmentSkinSystem] UI created successfully");
            }
            catch (Exception e)
            {
                Debug.LogError($"[EquipmentSkinSystem] Failed to create UI: {e.Message}");
            }
        }

        private GameObject CreateBackgroundPanel(Transform parent)
        {
            GameObject panel = new GameObject("BackgroundPanel");
            panel.transform.SetParent(parent, false);

            RectTransform rect = panel.AddComponent<RectTransform>();
            rect.anchorMin = new Vector2(0.5f, 0.5f);
            rect.anchorMax = new Vector2(0.5f, 0.5f);
            rect.sizeDelta = new Vector2(850, 650);
            rect.anchoredPosition = Vector2.zero;

            // 使用圓角 Sprite
            Image image = panel.AddComponent<Image>();
            if (_roundedSprite != null)
            {
                image.sprite = _roundedSprite;
                image.type = Image.Type.Sliced;
            }
            image.color = new Color(0.12f, 0.18f, 0.25f, 0.92f);

            return panel;
        }

        private void CreateTitle(Transform parent)
        {
            GameObject titleObj = new GameObject("Title");
            titleObj.transform.SetParent(parent, false);

            RectTransform rect = titleObj.AddComponent<RectTransform>();
            rect.anchorMin = new Vector2(0.5f, 1f);
            rect.anchorMax = new Vector2(0.5f, 1f);
            rect.sizeDelta = new Vector2(750, 70);
            rect.anchoredPosition = new Vector2(0, -45);

            TextMeshProUGUI text = titleObj.AddComponent<TextMeshProUGUI>();
            text.text = "裝備外觀管理系統";
            text.fontSize = 32;
            text.alignment = TextAlignmentOptions.Center;
            text.color = new Color(1f, 0.95f, 0.85f, 1f); // 暖白色
            text.fontStyle = FontStyles.Bold;
            
            // 添加文字陰影
            Shadow textShadow = titleObj.AddComponent<Shadow>();
            textShadow.effectColor = new Color(0, 0, 0, 0.8f);
            textShadow.effectDistance = new Vector2(2, -2);
        }

        private void CreateSlotList(Transform parent)
        {
            GameObject scrollViewObj = new GameObject("SlotScrollView");
            scrollViewObj.transform.SetParent(parent, false);

            RectTransform scrollRect = scrollViewObj.AddComponent<RectTransform>();
            scrollRect.anchorMin = new Vector2(0.5f, 0f);
            scrollRect.anchorMax = new Vector2(0.5f, 1f);
            scrollRect.offsetMin = new Vector2(-375, 120); // 左下角：距底部120（按鈕區域）
            scrollRect.offsetMax = new Vector2(375, -80);   // 右上角：距頂部80（標題區域）

            Image scrollBg = scrollViewObj.AddComponent<Image>();
            if (_roundedSlotSprite != null)
            {
                scrollBg.sprite = _roundedSlotSprite;
                scrollBg.type = Image.Type.Sliced;
            }
            // 更深的背景色
            scrollBg.color = new Color(0.08f, 0.12f, 0.18f, 0.7f);
            
            // 添加 Mask 組件裁剪超出內容
            Mask mask = scrollViewObj.AddComponent<Mask>();
            mask.showMaskGraphic = true;

            ScrollRect scrollComponent = scrollViewObj.AddComponent<ScrollRect>();
            scrollComponent.horizontal = false;
            scrollComponent.vertical = true;
            scrollComponent.movementType = ScrollRect.MovementType.Clamped; // 限制滾動範圍

            // 創建內容容器
            GameObject contentObj = new GameObject("Content");
            contentObj.transform.SetParent(scrollViewObj.transform, false);

            RectTransform contentRect = contentObj.AddComponent<RectTransform>();
            contentRect.anchorMin = new Vector2(0, 1);
            contentRect.anchorMax = new Vector2(1, 1);
            contentRect.pivot = new Vector2(0.5f, 1);
            contentRect.sizeDelta = new Vector2(0, 0);

            VerticalLayoutGroup layoutGroup = contentObj.AddComponent<VerticalLayoutGroup>();
            layoutGroup.spacing = 10;
            layoutGroup.padding = new RectOffset(10, 10, 10, 10);
            layoutGroup.childControlHeight = false;
            layoutGroup.childControlWidth = true;
            layoutGroup.childForceExpandHeight = false;
            layoutGroup.childForceExpandWidth = true;

            ContentSizeFitter sizeFitter = contentObj.AddComponent<ContentSizeFitter>();
            sizeFitter.verticalFit = ContentSizeFitter.FitMode.PreferredSize;

            scrollComponent.content = contentRect;

            // 為每個槽位創建 UI 元素
            foreach (EquipmentSlotType slotType in Enum.GetValues(typeof(EquipmentSlotType)))
            {
                CreateSlotUIElement(contentObj.transform, slotType);
            }
        }

        private void CreateSlotUIElement(Transform parent, EquipmentSlotType slotType)
        {
            GameObject slotObj = new GameObject($"Slot_{slotType}");
            slotObj.transform.SetParent(parent, false);

            RectTransform rect = slotObj.AddComponent<RectTransform>();
            rect.sizeDelta = new Vector2(0, 60); // 降低高度

            Image bg = slotObj.AddComponent<Image>();
            if (_roundedSlotSprite != null)
            {
                bg.sprite = _roundedSlotSprite;
                bg.type = Image.Type.Sliced;
            }
            // 深灰色槽位背景（更透明）
            bg.color = new Color(0.2f, 0.25f, 0.3f, 0.6f);

            HorizontalLayoutGroup layout = slotObj.AddComponent<HorizontalLayoutGroup>();
            layout.spacing = 10;
            layout.padding = new RectOffset(10, 10, 10, 10);
            layout.childControlHeight = true;
            layout.childControlWidth = false;
            layout.childForceExpandHeight = true;
            layout.childForceExpandWidth = false;

            SlotUIElements elements = new SlotUIElements();
            elements.Container = slotObj;

            // 啟用外觀開關（最左邊）
            elements.UseSkinToggle = CreateToggle(slotObj.transform, slotType);

            // 槽位名稱
            elements.SlotNameText = CreateText(slotObj.transform, GetSlotDisplayName(slotType), 120);

            // 外觀裝備輸入框
            elements.SkinItemInput = CreateInputField(slotObj.transform, "外觀ID", 150, (value) => OnSkinItemChanged(slotType, value));

            // 清空按鈕（最右邊）
            elements.ClearButton = CreateButton(slotObj.transform, "清空", () => OnClearSlotClicked(slotType));

            _slotUIElements[slotType] = elements;
        }

        private TextMeshProUGUI CreateText(Transform parent, string text, float width)
        {
            GameObject textObj = new GameObject("Text");
            textObj.transform.SetParent(parent, false);

            RectTransform rect = textObj.AddComponent<RectTransform>();
            rect.sizeDelta = new Vector2(width, 0);

            TextMeshProUGUI tmp = textObj.AddComponent<TextMeshProUGUI>();
            tmp.text = text;
            tmp.fontSize = 18;
            tmp.alignment = TextAlignmentOptions.Center;
            tmp.color = Color.white;

            LayoutElement layoutElement = textObj.AddComponent<LayoutElement>();
            layoutElement.preferredWidth = width;

            return tmp;
        }

        private Toggle CreateToggle(Transform parent, EquipmentSlotType slotType)
        {
            GameObject toggleObj = new GameObject("Toggle");
            toggleObj.transform.SetParent(parent, false);

            RectTransform rect = toggleObj.AddComponent<RectTransform>();
            rect.sizeDelta = new Vector2(50, 30);

            Toggle toggle = toggleObj.AddComponent<Toggle>();
            toggle.onValueChanged.AddListener((value) => OnToggleChanged(slotType, value));

            // 創建背景（深色方框）
            GameObject bgObj = new GameObject("Background");
            bgObj.transform.SetParent(toggleObj.transform, false);
            RectTransform bgRect = bgObj.AddComponent<RectTransform>();
            bgRect.anchorMin = Vector2.zero;
            bgRect.anchorMax = Vector2.one;
            bgRect.sizeDelta = Vector2.zero;
            Image bgImage = bgObj.AddComponent<Image>();
            if (_roundedSlotSprite != null)
            {
                bgImage.sprite = _roundedSlotSprite;
                bgImage.type = Image.Type.Sliced;
            }
            bgImage.color = new Color(0.2f, 0.25f, 0.3f, 1f); // 深灰藍色
            
            // 添加邊框
            Outline bgOutline = bgObj.AddComponent<Outline>();
            bgOutline.effectColor = new Color(0.4f, 0.45f, 0.5f, 0.8f);
            bgOutline.effectDistance = new Vector2(1, -1);

            // 創建勾選標記（✓ 符號）
            GameObject checkmarkObj = new GameObject("Checkmark");
            checkmarkObj.transform.SetParent(bgObj.transform, false);
            RectTransform checkRect = checkmarkObj.AddComponent<RectTransform>();
            checkRect.anchorMin = new Vector2(0.15f, 0.15f);
            checkRect.anchorMax = new Vector2(0.85f, 0.85f);
            checkRect.sizeDelta = Vector2.zero;
            
            TextMeshProUGUI checkText = checkmarkObj.AddComponent<TextMeshProUGUI>();
            checkText.text = "✓";
            checkText.fontSize = 26;
            checkText.alignment = TextAlignmentOptions.Center;
            checkText.color = new Color(1f, 0.6f, 0.2f, 1f); // 橙色勾勾（遊戲風格）
            checkText.fontStyle = FontStyles.Bold;
            
            // 添加發光效果
            Shadow checkShadow = checkmarkObj.AddComponent<Shadow>();
            checkShadow.effectColor = new Color(1f, 0.5f, 0f, 0.8f);
            checkShadow.effectDistance = new Vector2(0, 0);

            toggle.targetGraphic = bgImage;
            toggle.graphic = checkText;

            LayoutElement layoutElement = toggleObj.AddComponent<LayoutElement>();
            layoutElement.preferredWidth = 50;

            return toggle;
        }

        private TMP_InputField CreateInputField(Transform parent, string placeholder, float width, Action<string> onValueChanged)
        {
            GameObject inputObj = new GameObject($"Input_{placeholder}");
            inputObj.transform.SetParent(parent, false);

            RectTransform rect = inputObj.AddComponent<RectTransform>();
            rect.sizeDelta = new Vector2(width, 35); // 稍微高一點

            // 背景（類似搜尋欄的深灰黑色背景）
            Image bgImage = inputObj.AddComponent<Image>();
            if (_roundedSlotSprite != null)
            {
                bgImage.sprite = _roundedSlotSprite;
                bgImage.type = Image.Type.Sliced;
            }
            bgImage.color = new Color(0.1f, 0.1f, 0.1f, 0.9f); // 深灰黑色
            
            // 添加邊框（淺灰色邊框）
            Outline outline = inputObj.AddComponent<Outline>();
            outline.effectColor = new Color(0.4f, 0.4f, 0.4f, 0.6f);
            outline.effectDistance = new Vector2(1, -1);

            TMP_InputField inputField = inputObj.AddComponent<TMP_InputField>();
            inputField.contentType = TMP_InputField.ContentType.Standard; // 改用 Standard 以支援負數
            inputField.characterValidation = TMP_InputField.CharacterValidation.None; // 允許輸入負號

            // 創建文字區域
            GameObject textArea = new GameObject("TextArea");
            textArea.transform.SetParent(inputObj.transform, false);
            RectTransform textAreaRect = textArea.AddComponent<RectTransform>();
            textAreaRect.anchorMin = Vector2.zero;
            textAreaRect.anchorMax = Vector2.one;
            textAreaRect.sizeDelta = new Vector2(-10, -10);
            textAreaRect.anchoredPosition = Vector2.zero;

            // 創建文字組件
            GameObject textObj = new GameObject("Text");
            textObj.transform.SetParent(textArea.transform, false);
            RectTransform textRect = textObj.AddComponent<RectTransform>();
            textRect.anchorMin = Vector2.zero;
            textRect.anchorMax = Vector2.one;
            textRect.sizeDelta = Vector2.zero;
            textRect.anchoredPosition = Vector2.zero;

            TextMeshProUGUI textComponent = textObj.AddComponent<TextMeshProUGUI>();
            textComponent.fontSize = 14;
            textComponent.color = Color.white;
            textComponent.alignment = TextAlignmentOptions.Center;

            // 創建 Placeholder
            GameObject placeholderObj = new GameObject("Placeholder");
            placeholderObj.transform.SetParent(textArea.transform, false);
            RectTransform placeholderRect = placeholderObj.AddComponent<RectTransform>();
            placeholderRect.anchorMin = Vector2.zero;
            placeholderRect.anchorMax = Vector2.one;
            placeholderRect.sizeDelta = Vector2.zero;
            placeholderRect.anchoredPosition = Vector2.zero;

            TextMeshProUGUI placeholderText = placeholderObj.AddComponent<TextMeshProUGUI>();
            placeholderText.text = placeholder;
            placeholderText.fontSize = 14;
            placeholderText.color = new Color(0.5f, 0.5f, 0.5f, 1f);
            placeholderText.alignment = TextAlignmentOptions.Center;

            inputField.textViewport = textAreaRect;
            inputField.textComponent = textComponent;
            inputField.placeholder = placeholderText;

            // 使用 onEndEdit 而不是 onValueChanged，這樣輸入完成後才觸發（支援負數）
            inputField.onEndEdit.AddListener((value) => onValueChanged?.Invoke(value));

            LayoutElement layoutElement = inputObj.AddComponent<LayoutElement>();
            layoutElement.preferredWidth = width;

            return inputField;
        }

        private Button CreateButton(Transform parent, string text, Action onClick)
        {
            GameObject buttonObj = new GameObject($"Button_{text}");
            buttonObj.transform.SetParent(parent, false);

            RectTransform rect = buttonObj.AddComponent<RectTransform>();
            rect.sizeDelta = new Vector2(100, 35); // 降低按鈕高度

            Image image = buttonObj.AddComponent<Image>();
            if (_roundedButtonSprite != null)
            {
                image.sprite = _roundedButtonSprite;
                image.type = Image.Type.Sliced;
            }
            image.color = new Color(0.2f, 0.5f, 0.7f, 0.9f); // 淺藍色按鈕（遊戲暫停選單風格）
            image.raycastTarget = true;

            Button button = buttonObj.AddComponent<Button>();
            button.targetGraphic = image;
            button.interactable = true;
            button.onClick.AddListener(() => {
                Debug.Log($"[EquipmentSkinSystem] Button '{text}' clicked!");
                onClick?.Invoke();
            });

            // 遊戲風格的顏色變化（天藍色系）
            ColorBlock colors = button.colors;
            colors.normalColor = new Color(0.2f, 0.6f, 0.85f, 0.9f);     // 天藍色
            colors.highlightedColor = new Color(0.3f, 0.7f, 0.95f, 1f);  // 亮天藍色
            colors.pressedColor = new Color(0.15f, 0.5f, 0.75f, 1f);     // 深天藍色
            colors.selectedColor = new Color(0.2f, 0.6f, 0.85f, 0.9f);
            colors.disabledColor = new Color(0.3f, 0.3f, 0.3f, 0.5f);
            colors.colorMultiplier = 1f;
            colors.fadeDuration = 0.1f;
            button.colors = colors;

            GameObject textObj = new GameObject("Text");
            textObj.transform.SetParent(buttonObj.transform, false);

            RectTransform textRect = textObj.AddComponent<RectTransform>();
            textRect.anchorMin = Vector2.zero;
            textRect.anchorMax = Vector2.one;
            textRect.sizeDelta = Vector2.zero;

            TextMeshProUGUI tmp = textObj.AddComponent<TextMeshProUGUI>();
            tmp.text = text;
            tmp.fontSize = 18; // 降低字體大小
            tmp.alignment = TextAlignmentOptions.Center;
            tmp.color = new Color(1f, 1f, 1f, 1f); // 純白色
            tmp.fontStyle = FontStyles.Bold;
            tmp.raycastTarget = false;

            LayoutElement layoutElement = buttonObj.AddComponent<LayoutElement>();
            layoutElement.preferredWidth = 100;
            layoutElement.preferredHeight = 35;

            return button;
        }

        private void CreateBottomButtons(Transform parent)
        {
            GameObject buttonContainer = new GameObject("BottomButtons");
            buttonContainer.transform.SetParent(parent, false);

            RectTransform rect = buttonContainer.AddComponent<RectTransform>();
            rect.anchorMin = new Vector2(0.5f, 0f);
            rect.anchorMax = new Vector2(0.5f, 0f);
            rect.sizeDelta = new Vector2(400, 60);
            rect.anchoredPosition = new Vector2(0, 60); // 提高位置避免重疊

            HorizontalLayoutGroup layout = buttonContainer.AddComponent<HorizontalLayoutGroup>();
            layout.spacing = 20;
            layout.childControlHeight = true;
            layout.childControlWidth = true;
            layout.childForceExpandHeight = false;
            layout.childForceExpandWidth = true;
            layout.padding = new RectOffset(10, 10, 5, 5);

            CreateButton(buttonContainer.transform, "保存配置", OnSaveClicked);
            CreateButton(buttonContainer.transform, "重置配置", OnResetClicked);
            CreateButton(buttonContainer.transform, "關閉", OnCloseClicked);
        }

        private string GetSlotDisplayName(EquipmentSlotType slotType)
        {
            return slotType switch
            {
                EquipmentSlotType.Armor => "護甲",
                EquipmentSlotType.Helmet => "頭盔",
                EquipmentSlotType.FaceMask => "面罩",
                EquipmentSlotType.Backpack => "背包",
                EquipmentSlotType.Headset => "耳機",
                _ => slotType.ToString()
            };
        }

        void Update()
        {
            // 當 UI 開啟時，監聽 ESC 鍵關閉
            if (_isUIVisible && Input.GetKeyDown(KeyCode.Escape))
            {
                HideUI();
            }
        }

        public void ToggleUI()
        {
            _isUIVisible = !_isUIVisible;
            if (_uiPanel != null)
            {
                _uiPanel.SetActive(_isUIVisible);
                if (_isUIVisible)
                {
                    ShowUI();
                }
                else
                {
                    HideUI();
                }
            }
        }

        public void ShowUI()
        {
            _isUIVisible = true;
            if (_uiPanel != null)
            {
                _uiPanel.SetActive(true);
                
                // 確保載入最新配置
                Debug.Log("[EquipmentSkinSystem] ShowUI - Loading config...");
                DataPersistence.LoadConfig();
                
                // 顯示當前配置狀態
                var profile = EquipmentSkinDataManager.Instance.CurrentProfile;
                Debug.Log($"[EquipmentSkinSystem] ShowUI - Current profile: {profile?.ProfileName}");
                Debug.Log($"[EquipmentSkinSystem] ShowUI - SlotConfigs count: {profile?.SlotConfigs?.Count ?? 0}");
                if (profile?.SlotConfigs != null)
                {
                    foreach (var kvp in profile.SlotConfigs)
                    {
                        Debug.Log($"[EquipmentSkinSystem] ShowUI - {kvp.Key}: SkinID={kvp.Value.SkinItemTypeID}, UseSkin={kvp.Value.UseSkin}");
                    }
                }
                
                RefreshUI();
                
                // 使用遊戲的輸入管理系統（和物品欄一樣的方式）
                InputManager.DisableInput(_uiPanel);
                
                Debug.Log("[EquipmentSkinSystem] UI opened - Input disabled via InputManager");
            }
        }

        public void HideUI()
        {
            _isUIVisible = false;
            if (_uiPanel != null)
            {
                _uiPanel.SetActive(false);
                
                // 恢復輸入
                InputManager.ActiveInput(_uiPanel);
                
                Debug.Log("[EquipmentSkinSystem] UI closed - Input restored");
            }
        }

        private void RefreshUI()
        {
            try
            {
                var profile = EquipmentSkinDataManager.Instance.CurrentProfile;
                if (profile == null || profile.SlotConfigs == null)
                {
                    Debug.LogError("[EquipmentSkinSystem] Profile or SlotConfigs is null!");
                    return;
                }

                foreach (var kvp in _slotUIElements)
                {
                    var slotType = kvp.Key;
                    var elements = kvp.Value;
                    
                    if (!profile.SlotConfigs.ContainsKey(slotType))
                    {
                        Debug.LogWarning($"[EquipmentSkinSystem] SlotType {slotType} not found in profile!");
                        continue;
                    }
                    
                    var config = profile.SlotConfigs[slotType];

                    // 顯示 ID：0 = 空欄，-1 或正數 = 顯示數字
                    if (config.SkinItemTypeID == 0)
                    {
                        elements.SkinItemInput.text = "";
                    }
                    else
                    {
                        elements.SkinItemInput.text = config.SkinItemTypeID.ToString();
                    }
                    elements.UseSkinToggle.isOn = config.UseSkin;
                    
                    Debug.Log($"[EquipmentSkinSystem] RefreshUI - Slot {slotType}: SkinID={config.SkinItemTypeID}, UseSkin={config.UseSkin}");
                }
            }
            catch (Exception e)
            {
                Debug.LogError($"[EquipmentSkinSystem] Error in RefreshUI: {e.Message}");
                Debug.LogError($"[EquipmentSkinSystem] Stack trace: {e.StackTrace}");
            }
        }

        private void OnToggleChanged(EquipmentSlotType slotType, bool value)
        {
            try
            {
                var profile = EquipmentSkinDataManager.Instance.CurrentProfile;
                if (profile != null && profile.SlotConfigs != null && profile.SlotConfigs.ContainsKey(slotType))
                {
                    profile.SlotConfigs[slotType].UseSkin = value;
                    Debug.Log($"[EquipmentSkinSystem] ✅ Slot {slotType} UseSkin changed to: {value}");
                    Debug.Log($"[EquipmentSkinSystem] Current config: SkinID={profile.SlotConfigs[slotType].SkinItemTypeID}");
                    
                    // 立即應用變更（觸發裝備重新渲染）
                    RefreshEquipmentVisual(slotType);
                }
                else
                {
                    Debug.LogError($"[EquipmentSkinSystem] Cannot toggle slot {slotType} - profile or config is null");
                }
            }
            catch (Exception e)
            {
                Debug.LogError($"[EquipmentSkinSystem] Error in OnToggleChanged: {e.Message}");
            }
        }


        private void OnSkinItemChanged(EquipmentSlotType slotType, string value)
        {
            try
            {
                var profile = EquipmentSkinDataManager.Instance.CurrentProfile;
                if (profile == null || profile.SlotConfigs == null || !profile.SlotConfigs.ContainsKey(slotType))
                {
                    Debug.LogError($"[EquipmentSkinSystem] Cannot set skin item for slot {slotType}");
                    return;
                }

                if (string.IsNullOrEmpty(value))
                {
                    // 空欄 = 不套用任何外觀（保持原樣）
                    profile.SlotConfigs[slotType].SkinItemTypeID = 0;
                    Debug.Log($"[EquipmentSkinSystem] Slot {slotType} skin cleared (will use original visual)");
                    
                    // 如果已啟用，立即應用變更
                    if (profile.SlotConfigs[slotType].UseSkin)
                    {
                        RefreshEquipmentVisual(slotType);
                    }
                }
                else if (int.TryParse(value, out int itemID))
                {
                    profile.SlotConfigs[slotType].SkinItemTypeID = itemID;
                    Debug.Log($"[EquipmentSkinSystem] Slot {slotType} skin item set to: {itemID} (including -1 for hide)");
                    
                    // 如果已啟用，立即應用變更
                    if (profile.SlotConfigs[slotType].UseSkin)
                    {
                        RefreshEquipmentVisual(slotType);
                    }
                }
            }
            catch (Exception e)
            {
                Debug.LogError($"[EquipmentSkinSystem] Error in OnSkinItemChanged: {e.Message}");
            }
        }


        /// <summary>
        /// 立即刷新裝備視覺（觸發重新渲染）
        /// </summary>
        private void RefreshEquipmentVisual(EquipmentSlotType slotType)
        {
            try
            {
                // 盡量使用遊戲原本的主角引用
                CharacterMainControl? player = null;
                try
                {
                    player = LevelManager.Instance?.MainCharacter;
                }
                catch
                {
                    // ignore, fallback below
                }

                if (player == null)
                {
                    // 後備方案：在場景中尋找
                    player = GameObject.FindObjectOfType<CharacterMainControl>();
                }

                if (player == null)
                {
                    Debug.LogWarning("[EquipmentSkinSystem] Cannot find player CharacterMainControl");
                    return;
                }

                // 透過 Harmony Traverse 取得 private 字段 characterItem（避免直接引用 System.Reflection）
                var characterItem = Traverse.Create(player)
                                            .Field("characterItem")
                                            .GetValue<object>();
                if (characterItem == null)
                {
                    Debug.LogWarning("[EquipmentSkinSystem] Cannot find characterItem field on player");
                    return;
                }

                // 取得 Slots 屬性
                var itemType = characterItem.GetType();
                var slots = Traverse.Create(characterItem).Property("Slots").GetValue();
                if (slots == null)
                {
                    Debug.LogWarning("[EquipmentSkinSystem] Slots is null");
                    return;
                }

                // 根據槽位類型取得對應的 Slot
                string? slotKey = GetSlotKeyFromType(slotType);
                if (string.IsNullOrEmpty(slotKey))
                {
                    Debug.LogWarning($"[EquipmentSkinSystem] Unknown slot type: {slotType}");
                    return;
                }

                int slotHash = slotKey.GetHashCode();
                var slotTraverse = Traverse.Create(slots).Method("GetSlot", new object[] { slotHash });
                if (slotTraverse == null)
                {
                    Debug.LogWarning("[EquipmentSkinSystem] Cannot find GetSlot(int) method on Slots");
                    return;
                }

                var slot = slotTraverse.GetValue();
                if (slot == null)
                {
                    Debug.LogWarning($"[EquipmentSkinSystem] Cannot find slot: {slotKey}");
                    return;
                }

                // 觸發槽位內容變更事件，讓遊戲重新渲染這個槽位
                var forceInvokeMethod = slot.GetType().GetMethod("ForceInvokeSlotContentChangedEvent");
                if (forceInvokeMethod != null)
                {
                    forceInvokeMethod.Invoke(slot, null);
                    Debug.Log($"[EquipmentSkinSystem] ✅ Refreshed visual for slot: {slotType}");
                }
            }
            catch (Exception e)
            {
                Debug.LogError($"[EquipmentSkinSystem] Error refreshing equipment visual: {e.Message}");
                Debug.LogError($"[EquipmentSkinSystem] Stack trace: {e.StackTrace}");
            }
        }

        /// <summary>
        /// 將 EquipmentSlotType 轉換為遊戲的槽位 Key
        /// </summary>
        private string? GetSlotKeyFromType(EquipmentSlotType slotType)
        {
            switch (slotType)
            {
                case EquipmentSlotType.Armor:
                    return "Armor";
                case EquipmentSlotType.Helmet:
                    return "Helmat"; // 注意：遊戲裡拼錯了
                case EquipmentSlotType.FaceMask:
                    return "FaceMask";
                case EquipmentSlotType.Backpack:
                    return "Backpack";
                case EquipmentSlotType.Headset:
                    return "Headset";
                default:
                    return null;
            }
        }

        private void OnSaveClicked()
        {
            Debug.Log("[EquipmentSkinSystem] Save button clicked!");
            
            // 先保存配置到文件
            try
            {
                DataPersistence.SaveConfig();
                Debug.Log("[EquipmentSkinSystem] ✅ Configuration saved to file!");
            }
            catch (Exception e)
            {
                Debug.LogError($"[EquipmentSkinSystem] Failed to save config: {e.Message}");
            }
            
            // 立即應用所有槽位的設定
            foreach (EquipmentSlotType slotType in Enum.GetValues(typeof(EquipmentSlotType)))
            {
                RefreshEquipmentVisual(slotType);
            }
            
            Debug.Log("[EquipmentSkinSystem] ✅ Configuration applied!");
            
            // 保存後自動關閉介面
            HideUI();
        }

        private void OnClearSlotClicked(EquipmentSlotType slotType)
        {
            Debug.Log($"[EquipmentSkinSystem] Clear slot: {slotType}");
            
            var profile = EquipmentSkinDataManager.Instance.CurrentProfile;
            if (profile != null && profile.SlotConfigs != null && profile.SlotConfigs.ContainsKey(slotType))
            {
                // 清空設定
                profile.SlotConfigs[slotType].SkinItemTypeID = 0;
                profile.SlotConfigs[slotType].UseSkin = false;
                
                // 刷新 UI
                RefreshUI();
                
                // 立即應用變更
                RefreshEquipmentVisual(slotType);
                
                Debug.Log($"[EquipmentSkinSystem] ✅ Slot {slotType} cleared!");
            }
        }

        private void OnResetClicked()
        {
            Debug.Log("[EquipmentSkinSystem] Reset button clicked!");
            EquipmentSkinDataManager.Instance.ResetProfile();
            RefreshUI();
            Debug.Log("[EquipmentSkinSystem] ✅ Configuration reset!");
        }

        private void OnCloseClicked()
        {
            Debug.Log("[EquipmentSkinSystem] Close button clicked!");
            HideUI();
        }

        private void EnsureEventSystem()
        {
            // 檢查是否已經有 EventSystem
            if (UnityEngine.EventSystems.EventSystem.current == null)
            {
                GameObject eventSystemObj = new GameObject("EventSystem");
                DontDestroyOnLoad(eventSystemObj);
                eventSystemObj.AddComponent<UnityEngine.EventSystems.EventSystem>();
                eventSystemObj.AddComponent<UnityEngine.EventSystems.StandaloneInputModule>();
                Debug.Log("[EquipmentSkinSystem] Created EventSystem for UI interaction");
            }
            else
            {
                Debug.Log("[EquipmentSkinSystem] EventSystem already exists");
            }
        }

        private void OnDestroy()
        {
            // 確保恢復輸入
            if (_isUIVisible && _uiPanel != null)
            {
                InputManager.ActiveInput(_uiPanel);
            }
            
            if (_uiPanel != null)
            {
                Destroy(_uiPanel);
            }
            
            if (_roundedSprite != null)
            {
                Destroy(_roundedSprite.texture);
                Destroy(_roundedSprite);
            }
            if (_roundedButtonSprite != null)
            {
                Destroy(_roundedButtonSprite.texture);
                Destroy(_roundedButtonSprite);
            }
            if (_roundedSlotSprite != null)
            {
                Destroy(_roundedSlotSprite.texture);
                Destroy(_roundedSlotSprite);
            }
        }

        /// <summary>
        /// 創建圓角矩形 Sprite
        /// </summary>
        private Sprite CreateRoundedSprite(int width, int height, int cornerRadius)
        {
            Texture2D texture = new Texture2D(width, height, TextureFormat.RGBA32, false);
            Color[] pixels = new Color[width * height];
            
            for (int y = 0; y < height; y++)
            {
                for (int x = 0; x < width; x++)
                {
                    float alpha = 1f;
                    
                    // 左上角
                    if (x < cornerRadius && y > height - cornerRadius)
                    {
                        float dx = cornerRadius - x;
                        float dy = y - (height - cornerRadius);
                        float distance = Mathf.Sqrt(dx * dx + dy * dy);
                        if (distance > cornerRadius)
                        {
                            alpha = 0f;
                        }
                        else if (distance > cornerRadius - 2)
                        {
                            alpha = 1f - (distance - (cornerRadius - 2)) / 2f;
                        }
                    }
                    // 右上角
                    else if (x > width - cornerRadius && y > height - cornerRadius)
                    {
                        float dx = x - (width - cornerRadius);
                        float dy = y - (height - cornerRadius);
                        float distance = Mathf.Sqrt(dx * dx + dy * dy);
                        if (distance > cornerRadius)
                        {
                            alpha = 0f;
                        }
                        else if (distance > cornerRadius - 2)
                        {
                            alpha = 1f - (distance - (cornerRadius - 2)) / 2f;
                        }
                    }
                    // 左下角
                    else if (x < cornerRadius && y < cornerRadius)
                    {
                        float dx = cornerRadius - x;
                        float dy = cornerRadius - y;
                        float distance = Mathf.Sqrt(dx * dx + dy * dy);
                        if (distance > cornerRadius)
                        {
                            alpha = 0f;
                        }
                        else if (distance > cornerRadius - 2)
                        {
                            alpha = 1f - (distance - (cornerRadius - 2)) / 2f;
                        }
                    }
                    // 右下角
                    else if (x > width - cornerRadius && y < cornerRadius)
                    {
                        float dx = x - (width - cornerRadius);
                        float dy = cornerRadius - y;
                        float distance = Mathf.Sqrt(dx * dx + dy * dy);
                        if (distance > cornerRadius)
                        {
                            alpha = 0f;
                        }
                        else if (distance > cornerRadius - 2)
                        {
                            alpha = 1f - (distance - (cornerRadius - 2)) / 2f;
                        }
                    }
                    
                    pixels[y * width + x] = new Color(1f, 1f, 1f, alpha);
                }
            }
            
            texture.SetPixels(pixels);
            texture.Apply();
            
            Sprite sprite = Sprite.Create(
                texture,
                new Rect(0, 0, width, height),
                new Vector2(0.5f, 0.5f),
                100f,
                0,
                SpriteMeshType.FullRect,
                new Vector4(cornerRadius, cornerRadius, cornerRadius, cornerRadius)
            );
            
            return sprite;
        }
    }
}

