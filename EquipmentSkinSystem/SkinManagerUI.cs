using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using TMPro;
using Duckov.UI;
using HarmonyLib;
using ItemStatsSystem;
using ItemStatsSystem.Items;

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
        private Button? _playerButton;
        private Button? _petButton;
        private Button? _settingsButton;
        private GameObject? _settingsPanel;
        private bool _isSettingsPanelVisible = false;
        private GameObject? _backgroundPanel;
        private Button? _logTabButton;
        private Button? _languageTabButton;
        private GameObject? _logTabContent;
        private GameObject? _languageTabContent;
        private string _currentTab = "Log";

        private class SlotUIElements
        {
            public GameObject Container = null!;
            public TextMeshProUGUI SlotNameText = null!;
            public TextMeshProUGUI CurrentEquipmentText = null!;  // 顯示當前裝備 ID
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
                // 創建圓角 Sprite（使用優化的抗鋸齒算法）
                _roundedSprite = CreateRoundedSprite(256, 256, 8);         // 主面板
                _roundedButtonSprite = CreateRoundedSprite(128, 64, 6);    // 按鈕
                _roundedSlotSprite = CreateRoundedSprite(128, 64, 4);      // 槽位
                
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
                
                Logger.Debug("Canvas created with GraphicRaycaster");

                // 創建背景面板
                _backgroundPanel = CreateBackgroundPanel(_uiPanel.transform);

                // 創建標題
                CreateTitle(_backgroundPanel.transform);

                // 創建功能按鈕（右上角）
                CreateSettingsButton(_backgroundPanel.transform);

                // 創建角色切換按鈕
                CreateCharacterToggle(_backgroundPanel.transform);

                // 創建槽位列表
                CreateSlotList(_backgroundPanel.transform);

                // 創建底部按鈕
                CreateBottomButtons(_backgroundPanel.transform);

                // 創建設定面板（與背景面板同級，覆蓋整個背景面板）
                CreateSettingsPanel(_uiPanel.transform);

                _uiPanel.SetActive(false);

                Logger.Debug("UI created successfully");
            }
            catch (Exception e)
            {
                Logger.Error("Failed to create UI", e);
            }
        }

        private GameObject CreateBackgroundPanel(Transform parent)
        {
            GameObject panel = new GameObject("BackgroundPanel");
            panel.transform.SetParent(parent, false);

            RectTransform rect = panel.AddComponent<RectTransform>();
            rect.anchorMin = new Vector2(0.5f, 0.5f);
            rect.anchorMax = new Vector2(0.5f, 0.5f);
            rect.sizeDelta = new Vector2(620, 660); // 調整高度（720 - 60）
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
            rect.sizeDelta = new Vector2(600, 70);
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

        private void CreateSettingsButton(Transform parent)
        {
            GameObject buttonObj = new GameObject("SettingsButton");
            buttonObj.transform.SetParent(parent, false);

            RectTransform rect = buttonObj.AddComponent<RectTransform>();
            rect.anchorMin = new Vector2(1f, 1f);
            rect.anchorMax = new Vector2(1f, 1f);
            rect.sizeDelta = new Vector2(50, 50);
            rect.anchoredPosition = new Vector2(-15, -15); // 右上角，留一點邊距

            Image image = buttonObj.AddComponent<Image>();
            if (_roundedButtonSprite != null)
            {
                image.sprite = _roundedButtonSprite;
                image.type = Image.Type.Sliced;
            }
            image.color = new Color(0.4f, 0.4f, 0.4f, 1f);
            image.raycastTarget = true;

            Button button = buttonObj.AddComponent<Button>();
            ColorBlock colors = button.colors;
            colors.normalColor = new Color(0.4f, 0.4f, 0.4f, 1f);
            colors.highlightedColor = new Color(0.6f, 0.6f, 0.6f, 1f);
            colors.pressedColor = new Color(0.3f, 0.3f, 0.3f, 1f);
            button.colors = colors;

            button.onClick.AddListener(() => ToggleSettingsPanel());

            // 文字（齒輪圖標用文字代替）
            GameObject textObj = new GameObject("Text");
            textObj.transform.SetParent(buttonObj.transform, false);

            TextMeshProUGUI text = textObj.AddComponent<TextMeshProUGUI>();
            text.text = "⚙";
            text.fontSize = 24;
            text.alignment = TextAlignmentOptions.Center;
            text.color = Color.white;
            text.raycastTarget = false;

            RectTransform textRect = textObj.GetComponent<RectTransform>();
            textRect.anchorMin = Vector2.zero;
            textRect.anchorMax = Vector2.one;
            textRect.sizeDelta = Vector2.zero;

            _settingsButton = button;
        }

        private void CreateCharacterToggle(Transform parent)
        {
            GameObject toggleContainer = new GameObject("CharacterToggle");
            toggleContainer.transform.SetParent(parent, false);

            RectTransform rect = toggleContainer.AddComponent<RectTransform>();
            rect.anchorMin = new Vector2(0.5f, 1f);
            rect.anchorMax = new Vector2(0.5f, 1f);
            rect.sizeDelta = new Vector2(300, 50);
            rect.anchoredPosition = new Vector2(0, -100);
            rect.pivot = new Vector2(0.5f, 0.5f); // 容器居中

            // 玩家按鈕（左側，相對於容器中心）
            _playerButton = CreateCharacterButton(toggleContainer.transform, "玩家", CharacterType.Player);
            RectTransform playerRect = _playerButton.GetComponent<RectTransform>();
            playerRect.anchorMin = new Vector2(0.5f, 0.5f);
            playerRect.anchorMax = new Vector2(0.5f, 0.5f);
            playerRect.pivot = new Vector2(0.5f, 0.5f);
            playerRect.anchoredPosition = new Vector2(-75, 0); // 左側：-75 = -(140/2 + 10/2)
            
            // 狗按鈕（右側，相對於容器中心）
            _petButton = CreateCharacterButton(toggleContainer.transform, "狗", CharacterType.Pet);
            RectTransform petRect = _petButton.GetComponent<RectTransform>();
            petRect.anchorMin = new Vector2(0.5f, 0.5f);
            petRect.anchorMax = new Vector2(0.5f, 0.5f);
            petRect.pivot = new Vector2(0.5f, 0.5f);
            petRect.anchoredPosition = new Vector2(75, 0); // 右側：75 = 140/2 + 10/2
        }

        private Button CreateCharacterButton(Transform parent, string label, CharacterType characterType)
        {
            GameObject buttonObj = new GameObject($"{label}Button");
            buttonObj.transform.SetParent(parent, false);

            RectTransform rect = buttonObj.AddComponent<RectTransform>();
            // anchor 和 pivot 會在 CreateCharacterToggle 中設置
            rect.sizeDelta = new Vector2(140, 50); // 增加高度從 45 到 50

            Image image = buttonObj.AddComponent<Image>();
            if (_roundedButtonSprite != null)
            {
                image.sprite = _roundedButtonSprite;
                image.type = Image.Type.Sliced;
            }
            image.color = Color.white;
            image.raycastTarget = true; // 確保可以接收點擊

            Button button = buttonObj.AddComponent<Button>();
            
            // 根據當前選中狀態設置顏色
            bool isSelected = EquipmentSkinDataManager.Instance.CurrentCharacterType == characterType;
            ColorBlock colors = button.colors;
            colors.normalColor = isSelected 
                ? new Color(112f/255f, 204f/255f, 224f/255f, 1f)  // 選中：亮藍色
                : new Color(0.3f, 0.3f, 0.3f, 1f);                 // 未選中：灰色
            colors.highlightedColor = new Color(157f/255f, 220f/255f, 235f/255f, 1f);
            colors.pressedColor = new Color(3f/255f, 159f/255f, 196f/255f, 1f);
            colors.selectedColor = colors.normalColor;
            colors.disabledColor = new Color(0.5f, 0.5f, 0.5f, 0.5f);
            button.colors = colors;

            button.onClick.AddListener(() => OnCharacterTypeChanged(characterType));

            // 文字
            GameObject textObj = new GameObject("Text");
            textObj.transform.SetParent(buttonObj.transform, false);

            TextMeshProUGUI text = textObj.AddComponent<TextMeshProUGUI>();
            text.text = label;
            text.fontSize = 20;
            text.alignment = TextAlignmentOptions.Center;
            text.color = Color.white;
            text.fontStyle = FontStyles.Bold;
            text.raycastTarget = false; // 文字不阻擋點擊

            RectTransform textRect = textObj.GetComponent<RectTransform>();
            textRect.anchorMin = Vector2.zero;
            textRect.anchorMax = Vector2.one;
            textRect.sizeDelta = Vector2.zero;

            return button;
        }

        private void OnCharacterTypeChanged(CharacterType newType)
        {
            try
            {
                Logger.Debug($"Character type changed to: {newType}");
                
                // 切換角色類型
                EquipmentSkinDataManager.Instance.SetCurrentCharacterType(newType);
                
                // 更新按鈕顏色狀態
                UpdateCharacterButtonColors();
                
                // 重新載入 UI（顯示新角色的配置）
                RefreshUIForCurrentCharacter();
                
                // 立即更新當前裝備 ID 顯示
                UpdateCurrentEquipmentDisplay();
                
                // 刷新裝備渲染
                RefreshAllEquipment();
            }
            catch (Exception e)
            {
                Logger.Error("Error changing character type", e);
            }
        }

        private void UpdateCharacterButtonColors()
        {
            var currentType = EquipmentSkinDataManager.Instance.CurrentCharacterType;
            
            // 更新玩家按鈕
            if (_playerButton != null)
            {
                var colors = _playerButton.colors;
                colors.normalColor = currentType == CharacterType.Player
                    ? new Color(112f/255f, 204f/255f, 224f/255f, 1f)  // 選中：亮藍色
                    : new Color(0.3f, 0.3f, 0.3f, 1f);                 // 未選中：灰色
                colors.selectedColor = colors.normalColor;
                _playerButton.colors = colors;
            }
            
            // 更新狗按鈕
            if (_petButton != null)
            {
                var colors = _petButton.colors;
                colors.normalColor = currentType == CharacterType.Pet
                    ? new Color(112f/255f, 204f/255f, 224f/255f, 1f)  // 選中：亮藍色
                    : new Color(0.3f, 0.3f, 0.3f, 1f);                 // 未選中：灰色
                colors.selectedColor = colors.normalColor;
                _petButton.colors = colors;
            }
        }

        private void RefreshUIForCurrentCharacter()
        {
            // 重新載入當前角色的配置到 UI
            var profile = EquipmentSkinDataManager.Instance.CurrentProfile;
            
            foreach (var kvp in _slotUIElements)
            {
                var slotType = kvp.Key;
                var elements = kvp.Value;
                
                if (profile.SlotConfigs.TryGetValue(slotType, out var config))
                {
                    // 顯示 ID：0 = 空欄（顯示 placeholder "外觀ID"），-1 或正數 = 顯示數字
                    if (config.SkinItemTypeID == 0)
                    {
                        elements.SkinItemInput.text = ""; // 空字符串會讓 placeholder "外觀ID" 顯示
                    }
                    else
                    {
                        elements.SkinItemInput.text = config.SkinItemTypeID.ToString();
                    }
                    elements.UseSkinToggle.isOn = config.UseSkin;
                }
            }
            
            Logger.Debug($"UI refreshed for {EquipmentSkinDataManager.Instance.CurrentCharacterType}");
        }

        private void CreateSlotList(Transform parent)
        {
            GameObject scrollViewObj = new GameObject("SlotScrollView");
            scrollViewObj.transform.SetParent(parent, false);

            RectTransform scrollRect = scrollViewObj.AddComponent<RectTransform>();
            scrollRect.anchorMin = new Vector2(0.5f, 0f);
            scrollRect.anchorMax = new Vector2(0.5f, 1f);
            scrollRect.offsetMin = new Vector2(-290, 110); // 左下角：縮小寬度
            scrollRect.offsetMax = new Vector2(290, -160);  // 右上角：為角色切換按鈕留出更多空間（按鈕在 -100，高度 50，所以需要 -160）

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

            // 為每個槽位創建 UI 元素（按指定順序）
            EquipmentSlotType[] slotOrder = new EquipmentSlotType[]
            {
                EquipmentSlotType.Helmet,   // 頭盔
                EquipmentSlotType.Armor,    // 護甲
                EquipmentSlotType.FaceMask, // 面部
                EquipmentSlotType.Headset,  // 耳機
                EquipmentSlotType.Backpack  // 背包
            };

            foreach (EquipmentSlotType slotType in slotOrder)
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
            elements.SlotNameText = CreateText(slotObj.transform, GetSlotDisplayName(slotType), 80);

            // 當前裝備 ID 顯示
            elements.CurrentEquipmentText = CreateText(slotObj.transform, "當前: --", 100);
            elements.CurrentEquipmentText.color = new Color(0.7f, 0.9f, 1f, 1f); // 淺藍色
            elements.CurrentEquipmentText.fontSize = 14;

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
            image.color = Color.white; // 使用白色基礎，讓 ColorBlock 顏色不被影響
            image.raycastTarget = true;

            Button button = buttonObj.AddComponent<Button>();
            button.targetGraphic = image;
            button.interactable = true;
            button.onClick.AddListener(() => {
                Logger.Debug($"Button '{text}' clicked!");
                onClick?.Invoke();
            });

            // 遊戲風格的顏色變化（使用你指定的 RGB 顏色）
            ColorBlock colors = button.colors;
            colors.normalColor = new Color(112f/255f, 204f/255f, 224f/255f, 1f);     // 正常: RGB(112,204,224)
            colors.highlightedColor = new Color(157f/255f, 220f/255f, 235f/255f, 1f); // 變亮: RGB(157,220,235)
            colors.pressedColor = new Color(3f/255f, 159f/255f, 196f/255f, 1f);       // 變暗: RGB(3,159,196)
            colors.selectedColor = new Color(112f/255f, 204f/255f, 224f/255f, 1f);   // 選中後回到正常色
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

        private void CreateSettingsPanel(Transform parent)
        {
            // 創建設定面板背景（覆蓋整個背景面板）
            GameObject panelObj = new GameObject("SettingsPanel");
            panelObj.transform.SetParent(parent, false);

            RectTransform panelRect = panelObj.AddComponent<RectTransform>();
            // 設定面板需要與背景面板同級，才能獨立顯示
            // 使用與背景面板相同的尺寸和位置
            panelRect.anchorMin = new Vector2(0.5f, 0.5f);
            panelRect.anchorMax = new Vector2(0.5f, 0.5f);
            panelRect.sizeDelta = new Vector2(620, 660); // 與背景面板相同尺寸
            panelRect.anchoredPosition = Vector2.zero;

            Image panelImage = panelObj.AddComponent<Image>();
            if (_roundedSprite != null)
            {
                panelImage.sprite = _roundedSprite;
                panelImage.type = Image.Type.Sliced;
            }
            panelImage.color = new Color(0.12f, 0.18f, 0.25f, 0.92f); // 維持透明設計

            // 標題（與主 UI 相同風格）
            GameObject titleObj = new GameObject("Title");
            titleObj.transform.SetParent(panelObj.transform, false);

            RectTransform titleRect = titleObj.AddComponent<RectTransform>();
            titleRect.anchorMin = new Vector2(0.5f, 1f);
            titleRect.anchorMax = new Vector2(0.5f, 1f);
            titleRect.sizeDelta = new Vector2(600, 70);
            titleRect.anchoredPosition = new Vector2(0, -45);

            TextMeshProUGUI titleText = titleObj.AddComponent<TextMeshProUGUI>();
            titleText.text = "功能設定";
            titleText.fontSize = 32;
            titleText.alignment = TextAlignmentOptions.Center;
            titleText.color = new Color(1f, 0.95f, 0.85f, 1f); // 暖白色
            titleText.fontStyle = FontStyles.Bold;
            
            // 添加文字陰影
            Shadow textShadow = titleObj.AddComponent<Shadow>();
            textShadow.effectColor = new Color(0, 0, 0, 0.8f);
            textShadow.effectDistance = new Vector2(2, -2);

            // 頁籤按鈕容器
            GameObject tabContainer = new GameObject("TabContainer");
            tabContainer.transform.SetParent(panelObj.transform, false);

            RectTransform tabContainerRect = tabContainer.AddComponent<RectTransform>();
            tabContainerRect.anchorMin = new Vector2(0.5f, 1f);
            tabContainerRect.anchorMax = new Vector2(0.5f, 1f);
            tabContainerRect.sizeDelta = new Vector2(300, 50);
            tabContainerRect.anchoredPosition = new Vector2(0, -100);
            tabContainerRect.pivot = new Vector2(0.5f, 0.5f);

            // 日誌設定頁籤
            _logTabButton = CreateTabButton(tabContainer.transform, "日誌設定", "Log");
            RectTransform logTabRect = _logTabButton.GetComponent<RectTransform>();
            logTabRect.anchorMin = new Vector2(0.5f, 0.5f);
            logTabRect.anchorMax = new Vector2(0.5f, 0.5f);
            logTabRect.pivot = new Vector2(0.5f, 0.5f);
            logTabRect.anchoredPosition = new Vector2(-75, 0);

            // 語言設定頁籤
            _languageTabButton = CreateTabButton(tabContainer.transform, "語言", "Language");
            RectTransform languageTabRect = _languageTabButton.GetComponent<RectTransform>();
            languageTabRect.anchorMin = new Vector2(0.5f, 0.5f);
            languageTabRect.anchorMax = new Vector2(0.5f, 0.5f);
            languageTabRect.pivot = new Vector2(0.5f, 0.5f);
            languageTabRect.anchoredPosition = new Vector2(75, 0);

            // 內容區域（與主 UI 的槽位列表區域相同位置）
            GameObject contentContainer = new GameObject("ContentContainer");
            contentContainer.transform.SetParent(panelObj.transform, false);

            RectTransform contentContainerRect = contentContainer.AddComponent<RectTransform>();
            contentContainerRect.anchorMin = new Vector2(0.5f, 0f);
            contentContainerRect.anchorMax = new Vector2(0.5f, 1f);
            contentContainerRect.sizeDelta = new Vector2(600, 0);
            contentContainerRect.anchoredPosition = new Vector2(0, 0);
            contentContainerRect.offsetMin = new Vector2(-300, 160); // 底部留空間給按鈕
            contentContainerRect.offsetMax = new Vector2(300, -160); // 頂部留空間給標題和頁籤

            // 日誌設定內容
            _logTabContent = CreateLogTabContent(contentContainer.transform);
            _logTabContent.SetActive(true);

            // 語言設定內容
            _languageTabContent = CreateLanguageTabContent(contentContainer.transform);
            _languageTabContent.SetActive(false);

            // 底部按鈕（與主 UI 相同風格）
            GameObject bottomButtonContainer = new GameObject("BottomButtons");
            bottomButtonContainer.transform.SetParent(panelObj.transform, false);

            RectTransform bottomButtonRect = bottomButtonContainer.AddComponent<RectTransform>();
            bottomButtonRect.anchorMin = new Vector2(0.5f, 0f);
            bottomButtonRect.anchorMax = new Vector2(0.5f, 0f);
            bottomButtonRect.sizeDelta = new Vector2(400, 60);
            bottomButtonRect.anchoredPosition = new Vector2(0, 60); // 恢復到原本位置

            HorizontalLayoutGroup bottomLayout = bottomButtonContainer.AddComponent<HorizontalLayoutGroup>();
            bottomLayout.spacing = 20;
            bottomLayout.childControlHeight = true;
            bottomLayout.childControlWidth = true;
            bottomLayout.childForceExpandHeight = false;
            bottomLayout.childForceExpandWidth = true;
            bottomLayout.padding = new RectOffset(10, 10, 5, 5);

            CreateButton(bottomButtonContainer.transform, "關閉", () => ToggleSettingsPanel());

            _settingsPanel = panelObj;
            _settingsPanel.SetActive(false);
        }

        private Button CreateTabButton(Transform parent, string label, string tabName)
        {
            GameObject buttonObj = new GameObject($"{tabName}TabButton");
            buttonObj.transform.SetParent(parent, false);

            RectTransform rect = buttonObj.AddComponent<RectTransform>();
            rect.sizeDelta = new Vector2(140, 50);

            Image image = buttonObj.AddComponent<Image>();
            if (_roundedButtonSprite != null)
            {
                image.sprite = _roundedButtonSprite;
                image.type = Image.Type.Sliced;
            }
            image.color = Color.white;
            image.raycastTarget = true;

            Button button = buttonObj.AddComponent<Button>();
            
            // 根據當前選中狀態設置顏色
            bool isSelected = _currentTab == tabName;
            ColorBlock colors = button.colors;
            colors.normalColor = isSelected 
                ? new Color(112f/255f, 204f/255f, 224f/255f, 1f)  // 選中：亮藍色
                : new Color(0.3f, 0.3f, 0.3f, 1f);                 // 未選中：灰色
            colors.highlightedColor = new Color(157f/255f, 220f/255f, 235f/255f, 1f);
            colors.pressedColor = new Color(3f/255f, 159f/255f, 196f/255f, 1f);
            colors.selectedColor = colors.normalColor;
            colors.disabledColor = new Color(0.5f, 0.5f, 0.5f, 0.5f);
            button.colors = colors;

            button.onClick.AddListener(() => SwitchTab(tabName));

            // 文字
            GameObject textObj = new GameObject("Text");
            textObj.transform.SetParent(buttonObj.transform, false);

            TextMeshProUGUI text = textObj.AddComponent<TextMeshProUGUI>();
            text.text = label;
            text.fontSize = 20;
            text.alignment = TextAlignmentOptions.Center;
            text.color = Color.white;
            text.fontStyle = FontStyles.Bold;
            text.raycastTarget = false;

            RectTransform textRect = textObj.GetComponent<RectTransform>();
            textRect.anchorMin = Vector2.zero;
            textRect.anchorMax = Vector2.one;
            textRect.sizeDelta = Vector2.zero;

            return button;
        }

        private GameObject CreateLogTabContent(Transform parent)
        {
            GameObject contentObj = new GameObject("LogTabContent");
            contentObj.transform.SetParent(parent, false);

            RectTransform contentRect = contentObj.AddComponent<RectTransform>();
            contentRect.anchorMin = Vector2.zero;
            contentRect.anchorMax = Vector2.one;
            contentRect.sizeDelta = Vector2.zero;

            VerticalLayoutGroup layout = contentObj.AddComponent<VerticalLayoutGroup>();
            layout.spacing = 15;
            layout.padding = new RectOffset(20, 20, 20, 20);
            layout.childControlHeight = false;
            layout.childControlWidth = true;
            layout.childForceExpandHeight = false;
            layout.childForceExpandWidth = true;

            // 創建 Log 控制 Toggle
            CreateLogToggle(contentObj.transform, "調試日誌 (Debug)", "EnableDebugLog");
            CreateLogToggle(contentObj.transform, "資訊日誌 (Info)", "EnableInfoLog");
            CreateLogToggle(contentObj.transform, "警告日誌 (Warning)", "EnableWarningLog");
            CreateLogToggle(contentObj.transform, "錯誤日誌 (Error)", "EnableErrorLog");

            return contentObj;
        }

        private GameObject CreateLanguageTabContent(Transform parent)
        {
            GameObject contentObj = new GameObject("LanguageTabContent");
            contentObj.transform.SetParent(parent, false);

            RectTransform contentRect = contentObj.AddComponent<RectTransform>();
            contentRect.anchorMin = Vector2.zero;
            contentRect.anchorMax = Vector2.one;
            contentRect.sizeDelta = Vector2.zero;

            VerticalLayoutGroup layout = contentObj.AddComponent<VerticalLayoutGroup>();
            layout.spacing = 15;
            layout.padding = new RectOffset(20, 20, 20, 20);
            layout.childControlHeight = false;
            layout.childControlWidth = true;
            layout.childForceExpandHeight = false;
            layout.childForceExpandWidth = true;

            // 語言選項標題
            TextMeshProUGUI titleText = CreateText(contentObj.transform, "選擇語言", 0);
            titleText.fontSize = 22;
            titleText.fontStyle = FontStyles.Bold;
            titleText.alignment = TextAlignmentOptions.Center;
            titleText.gameObject.GetComponent<RectTransform>().sizeDelta = new Vector2(0, 30);

            // 目前只有繁體中文
            CreateLanguageOption(contentObj.transform, "繁體中文", "zh-TW", true);

            // 預留空間說明
            TextMeshProUGUI noteText = CreateText(contentObj.transform, "（其他語言選項將在未來版本中添加）", 0);
            noteText.fontSize = 16;
            noteText.color = new Color(0.7f, 0.7f, 0.7f, 1f);
            noteText.alignment = TextAlignmentOptions.Center;
            noteText.gameObject.GetComponent<RectTransform>().sizeDelta = new Vector2(0, 25);

            return contentObj;
        }

        private void CreateLanguageOption(Transform parent, string languageName, string languageCode, bool isSelected)
        {
            GameObject optionObj = new GameObject($"LanguageOption_{languageCode}");
            optionObj.transform.SetParent(parent, false);

            RectTransform optionRect = optionObj.AddComponent<RectTransform>();
            optionRect.sizeDelta = new Vector2(0, 40);

            HorizontalLayoutGroup layout = optionObj.AddComponent<HorizontalLayoutGroup>();
            layout.spacing = 10;
            layout.padding = new RectOffset(0, 0, 0, 0);
            layout.childControlHeight = true;
            layout.childControlWidth = false;
            layout.childForceExpandHeight = true;
            layout.childForceExpandWidth = false;

            // 語言名稱
            TextMeshProUGUI nameText = CreateText(optionObj.transform, languageName, 0);
            RectTransform nameRect = nameText.gameObject.GetComponent<RectTransform>();
            nameRect.sizeDelta = new Vector2(200, 0);
            nameText.alignment = TextAlignmentOptions.MidlineLeft;
            nameText.fontSize = 18;
            
            // 確保文字垂直置中
            nameRect.anchorMin = new Vector2(0, 0.5f);
            nameRect.anchorMax = new Vector2(0, 0.5f);
            nameRect.pivot = new Vector2(0, 0.5f);

            // 選中標記（目前只有一個選項，所以總是選中）
            if (isSelected)
            {
                TextMeshProUGUI checkText = CreateText(optionObj.transform, "✓", 0);
                RectTransform checkRect = checkText.gameObject.GetComponent<RectTransform>();
                checkRect.sizeDelta = new Vector2(30, 0);
                checkText.color = new Color(112f/255f, 204f/255f, 224f/255f, 1f);
                checkText.fontSize = 24;
                checkText.fontStyle = FontStyles.Bold;
                checkText.alignment = TextAlignmentOptions.Center;
                
                // 確保打勾標記垂直置中
                checkRect.anchorMin = new Vector2(1f, 0.5f);
                checkRect.anchorMax = new Vector2(1f, 0.5f);
                checkRect.pivot = new Vector2(0.5f, 0.5f);
            }
        }

        private void SwitchTab(string tabName)
        {
            _currentTab = tabName;
            
            // 更新頁籤按鈕顏色
            if (_logTabButton != null && _languageTabButton != null)
            {
                bool logSelected = tabName == "Log";
                bool langSelected = tabName == "Language";
                
                ColorBlock logColors = _logTabButton.colors;
                logColors.normalColor = logSelected 
                    ? new Color(112f/255f, 204f/255f, 224f/255f, 1f)
                    : new Color(0.3f, 0.3f, 0.3f, 1f);
                _logTabButton.colors = logColors;

                ColorBlock langColors = _languageTabButton.colors;
                langColors.normalColor = langSelected 
                    ? new Color(112f/255f, 204f/255f, 224f/255f, 1f)
                    : new Color(0.3f, 0.3f, 0.3f, 1f);
                _languageTabButton.colors = langColors;
            }

            // 切換內容顯示
            if (_logTabContent != null && _languageTabContent != null)
            {
                _logTabContent.SetActive(tabName == "Log");
                _languageTabContent.SetActive(tabName == "Language");
            }

            Logger.Debug($"Switched to tab: {tabName}");
        }

        private void CreateLogToggle(Transform parent, string label, string settingKey)
        {
            GameObject toggleContainer = new GameObject($"LogToggle_{settingKey}");
            toggleContainer.transform.SetParent(parent, false);

            RectTransform containerRect = toggleContainer.AddComponent<RectTransform>();
            containerRect.sizeDelta = new Vector2(0, 35);

            HorizontalLayoutGroup layout = toggleContainer.AddComponent<HorizontalLayoutGroup>();
            layout.spacing = 10;
            layout.padding = new RectOffset(0, 0, 0, 0);
            layout.childControlHeight = true;
            layout.childControlWidth = false;
            layout.childForceExpandHeight = true;
            layout.childForceExpandWidth = false;

            // 標籤文字
            TextMeshProUGUI labelText = CreateText(toggleContainer.transform, label, 0);
            labelText.gameObject.GetComponent<RectTransform>().sizeDelta = new Vector2(300, 0);
            labelText.alignment = TextAlignmentOptions.MidlineLeft;
            labelText.fontSize = 16;

            // Toggle
            GameObject toggleObj = new GameObject("Toggle");
            toggleObj.transform.SetParent(toggleContainer.transform, false);

            RectTransform toggleRect = toggleObj.AddComponent<RectTransform>();
            toggleRect.sizeDelta = new Vector2(50, 30);

            Toggle toggle = toggleObj.AddComponent<Toggle>();
            
            // 根據設定鍵設置初始值
            var settings = EquipmentSkinDataManager.Instance.AppSettings;
            bool initialValue = settingKey switch
            {
                "EnableDebugLog" => settings.EnableDebugLog,
                "EnableInfoLog" => settings.EnableInfoLog,
                "EnableWarningLog" => settings.EnableWarningLog,
                "EnableErrorLog" => settings.EnableErrorLog,
                _ => false
            };
            toggle.isOn = initialValue;

            toggle.onValueChanged.AddListener((value) => OnLogSettingChanged(settingKey, value));

            // Toggle 背景
            GameObject bgObj = new GameObject("Background");
            bgObj.transform.SetParent(toggleObj.transform, false);

            RectTransform bgRect = bgObj.AddComponent<RectTransform>();
            bgRect.anchorMin = Vector2.zero;
            bgRect.anchorMax = Vector2.one;
            bgRect.sizeDelta = Vector2.zero;

            Image bgImage = bgObj.AddComponent<Image>();
            bgImage.color = new Color(0.2f, 0.2f, 0.2f, 1f);

            // Toggle 勾選標記
            GameObject checkmarkObj = new GameObject("Checkmark");
            checkmarkObj.transform.SetParent(bgObj.transform, false);

            RectTransform checkmarkRect = checkmarkObj.AddComponent<RectTransform>();
            checkmarkRect.anchorMin = new Vector2(0.2f, 0.2f);
            checkmarkRect.anchorMax = new Vector2(0.8f, 0.8f);
            checkmarkRect.sizeDelta = Vector2.zero;

            Image checkmarkImage = checkmarkObj.AddComponent<Image>();
            checkmarkImage.color = new Color(112f/255f, 204f/255f, 224f/255f, 1f);

            toggle.graphic = checkmarkImage;
            toggle.targetGraphic = bgImage;
        }

        private void OnLogSettingChanged(string settingKey, bool value)
        {
            try
            {
                var settings = EquipmentSkinDataManager.Instance.AppSettings;
                switch (settingKey)
                {
                    case "EnableDebugLog":
                        settings.EnableDebugLog = value;
                        break;
                    case "EnableInfoLog":
                        settings.EnableInfoLog = value;
                        break;
                    case "EnableWarningLog":
                        settings.EnableWarningLog = value;
                        break;
                    case "EnableErrorLog":
                        settings.EnableErrorLog = value;
                        break;
                }

                // 立即保存設定
                DataPersistence.SaveConfig();
                Logger.Info($"Log setting changed: {settingKey} = {value}");
            }
            catch (Exception e)
            {
                Logger.Error($"Error changing log setting: {settingKey}", e);
            }
        }

        private void ToggleSettingsPanel()
        {
            if (_settingsPanel != null && _backgroundPanel != null)
            {
                _isSettingsPanelVisible = !_isSettingsPanelVisible;
                
                if (_isSettingsPanelVisible)
                {
                    // 顯示設定面板，隱藏主面板
                    _settingsPanel.SetActive(true);
                    _backgroundPanel.SetActive(false);
                    Logger.Debug("Settings panel opened, main panel hidden");
                }
                else
                {
                    // 隱藏設定面板，顯示主面板
                    _settingsPanel.SetActive(false);
                    _backgroundPanel.SetActive(true);
                    Logger.Debug("Settings panel closed, main panel shown");
                }
            }
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

        private float _lastUpdateTime = 0f;
        private const float UPDATE_INTERVAL = 0.5f; // 每 0.5 秒更新一次

        void Update()
        {
            // 當 UI 開啟時，監聽 ESC 鍵關閉
            if (_isUIVisible && Input.GetKeyDown(KeyCode.Escape))
            {
                HideUI();
            }

            // 當 UI 開啟時，定期更新當前裝備顯示（避免每幀都更新）
            if (_isUIVisible && Time.time - _lastUpdateTime > UPDATE_INTERVAL)
            {
                UpdateCurrentEquipmentDisplay();
                _lastUpdateTime = Time.time;
            }
        }

        /// <summary>
        /// 更新所有槽位的當前裝備 ID 顯示
        /// </summary>
        private void UpdateCurrentEquipmentDisplay()
        {
            try
            {
                // 根據當前選中的角色類型獲取對應的角色
                CharacterMainControl? targetCharacter = null;
                var currentType = EquipmentSkinDataManager.Instance.CurrentCharacterType;
                
                if (currentType == CharacterType.Player)
                {
                    targetCharacter = LevelManager.Instance?.MainCharacter;
                }
                else if (currentType == CharacterType.Pet)
                {
                    targetCharacter = LevelManager.Instance?.PetCharacter;
                }

                if (targetCharacter == null) return;

                var equipmentController = targetCharacter.GetComponent<CharacterEquipmentController>();
                if (equipmentController == null) return;

                // 遊戲使用獨立欄位而非字典，逐個取得
                foreach (var kvp in _slotUIElements)
                {
                    var slotType = kvp.Key;
                    var elements = kvp.Value;

                    Slot? slot = GetSlotFromController(equipmentController, slotType);
                    if (slot != null && slot.Content != null)
                    {
                        elements.CurrentEquipmentText.text = $"當前: {slot.Content.TypeID}";
                    }
                    else
                    {
                        elements.CurrentEquipmentText.text = "當前: --";
                    }
                }
            }
            catch (Exception ex)
            {
                Logger.Error("Error updating current equipment display", ex);
            }
        }

        /// <summary>
        /// 從 CharacterEquipmentController 取得指定槽位的 Slot
        /// </summary>
        private Slot? GetSlotFromController(CharacterEquipmentController controller, EquipmentSlotType slotType)
        {
            string? fieldName = slotType switch
            {
                EquipmentSlotType.Armor => "armorSlot",
                EquipmentSlotType.Helmet => "helmatSlot",
                EquipmentSlotType.FaceMask => "faceMaskSlot",
                EquipmentSlotType.Backpack => "backpackSlot",
                EquipmentSlotType.Headset => "headsetSlot",
                _ => null
            };

            if (string.IsNullOrEmpty(fieldName)) return null;

            return Traverse.Create(controller).Field(fieldName).GetValue<Slot>();
        }

        /// <summary>
        /// 切換 UI 顯示狀態
        /// - 如果 UI 已關閉：打開第一層（主面板）
        /// - 如果 UI 已打開（無論在第一層還是第二層）：關閉整個 UI
        /// </summary>
        public void ToggleUI()
        {
            _isUIVisible = !_isUIVisible;
            if (_uiPanel != null)
            {
                _uiPanel.SetActive(_isUIVisible);
                if (_isUIVisible)
                {
                    // UI 關閉時按 F7：打開第一層（主面板）
                    ShowUI();
                }
                else
                {
                    // UI 已打開時按 F7：關閉整個 UI（無論當前在哪一層）
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
                
                // 每次從關閉狀態打開 UI 時，都顯示第一層（主面板）
                // 這確保了無論之前在哪一層，關閉後重新打開都會回到第一層
                if (_backgroundPanel != null && _settingsPanel != null)
                {
                    _backgroundPanel.SetActive(true);
                    _settingsPanel.SetActive(false);
                    _isSettingsPanelVisible = false;
                    Logger.Debug("ShowUI - Reset to main panel");
                }
                
                // 確保載入最新配置
                Logger.Debug("ShowUI - Loading config...");
                DataPersistence.LoadConfig();
                
                // 顯示當前配置狀態
                var profile = EquipmentSkinDataManager.Instance.CurrentProfile;
                Logger.Debug($"ShowUI - Current profile: {profile?.ProfileName}");
                Logger.Debug($"ShowUI - SlotConfigs count: {profile?.SlotConfigs?.Count ?? 0}");
                if (profile?.SlotConfigs != null)
                {
                    foreach (var kvp in profile.SlotConfigs)
                    {
                        Logger.Debug($"ShowUI - {kvp.Key}: SkinID={kvp.Value.SkinItemTypeID}, UseSkin={kvp.Value.UseSkin}");
                    }
                }
                
                RefreshUI();
                
                // 使用遊戲的輸入管理系統（和物品欄一樣的方式）
                InputManager.DisableInput(_uiPanel);
                
                Logger.Debug("UI opened - Input disabled via InputManager");
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
                
                Logger.Debug("UI closed - Input restored");
            }
        }

        private void RefreshUI()
        {
            try
            {
                var profile = EquipmentSkinDataManager.Instance.CurrentProfile;
                if (profile == null || profile.SlotConfigs == null)
                {
                    Logger.Error("Profile or SlotConfigs is null!");
                    return;
                }

                foreach (var kvp in _slotUIElements)
                {
                    var slotType = kvp.Key;
                    var elements = kvp.Value;
                    
                    if (!profile.SlotConfigs.ContainsKey(slotType))
                    {
                        Logger.Warning($"SlotType {slotType} not found in profile!");
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
                    
                    Logger.Debug($"RefreshUI - Slot {slotType}: SkinID={config.SkinItemTypeID}, UseSkin={config.UseSkin}");
                }
            }
            catch (Exception e)
            {
                Logger.Error("Error in RefreshUI", e);
                Logger.Error($"Stack trace: {e.StackTrace}");
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
                    Logger.Info($"Slot {slotType} UseSkin changed to: {value}");
                    Logger.Debug($"Current config: SkinID={profile.SlotConfigs[slotType].SkinItemTypeID}");
                    
                    // 立即應用變更（觸發全身重新渲染）
                    RefreshAllEquipment();
                }
                else
                {
                    Logger.Error($"Cannot toggle slot {slotType} - profile or config is null");
                }
            }
            catch (Exception e)
            {
                Logger.Error("Error in OnToggleChanged", e);
            }
        }


        private void OnSkinItemChanged(EquipmentSlotType slotType, string value)
        {
            try
            {
                var profile = EquipmentSkinDataManager.Instance.CurrentProfile;
                if (profile == null || profile.SlotConfigs == null || !profile.SlotConfigs.ContainsKey(slotType))
                {
                    Logger.Error($"Cannot set skin item for slot {slotType}");
                    return;
                }

                if (string.IsNullOrEmpty(value))
                {
                    // 空欄 = 不套用任何外觀（保持原樣）
                    profile.SlotConfigs[slotType].SkinItemTypeID = 0;
                    Logger.Debug($"Slot {slotType} skin cleared (will use original visual)");
                    
                    // 如果已啟用，立即應用變更（全身重新渲染）
                    if (profile.SlotConfigs[slotType].UseSkin)
                    {
                        RefreshAllEquipment();
                    }
                }
                else if (int.TryParse(value, out int itemID))
                {
                    profile.SlotConfigs[slotType].SkinItemTypeID = itemID;
                    Logger.Debug($"Slot {slotType} skin item set to: {itemID} (including -1 for hide)");
                    
                    // 如果已啟用，立即應用變更（全身重新渲染）
                    if (profile.SlotConfigs[slotType].UseSkin)
                    {
                        RefreshAllEquipment();
                    }
                }
            }
            catch (Exception e)
            {
                Logger.Error("Error in OnSkinItemChanged", e);
            }
        }


        /// <summary>
        /// 立即刷新所有裝備視覺（觸發全身重新渲染）
        /// </summary>
        private void RefreshAllEquipment()
        {
            HarmonyPatches.ForceRefreshAllEquipment();
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
                    Logger.Warning("Cannot find player CharacterMainControl");
                    return;
                }

                // 透過 Harmony Traverse 取得 private 字段 characterItem（避免直接引用 System.Reflection）
                var characterItem = Traverse.Create(player)
                                            .Field("characterItem")
                                            .GetValue<object>();
                if (characterItem == null)
                {
                    Logger.Warning("Cannot find characterItem field on player");
                    return;
                }

                // 取得 Slots 屬性
                var itemType = characterItem.GetType();
                var slots = Traverse.Create(characterItem).Property("Slots").GetValue();
                if (slots == null)
                {
                    Logger.Warning("Slots is null");
                    return;
                }

                // 根據槽位類型取得對應的 Slot
                string? slotKey = GetSlotKeyFromType(slotType);
                if (string.IsNullOrEmpty(slotKey))
                {
                    Logger.Warning($"Unknown slot type: {slotType}");
                    return;
                }

                int slotHash = slotKey.GetHashCode();
                var slotTraverse = Traverse.Create(slots).Method("GetSlot", new object[] { slotHash });
                if (slotTraverse == null)
                {
                    Logger.Warning("Cannot find GetSlot(int) method on Slots");
                    return;
                }

                var slot = slotTraverse.GetValue();
                if (slot == null)
                {
                    Logger.Warning($"Cannot find slot: {slotKey}");
                    return;
                }

                // 觸發槽位內容變更事件，讓遊戲重新渲染這個槽位
                var forceInvokeMethod = slot.GetType().GetMethod("ForceInvokeSlotContentChangedEvent");
                if (forceInvokeMethod != null)
                {
                    forceInvokeMethod.Invoke(slot, null);
                    Logger.Info($"Refreshed visual for slot: {slotType}");
                }
            }
            catch (Exception e)
            {
                Logger.Error("Error refreshing equipment visual", e);
                Logger.Error($"Stack trace: {e.StackTrace}");
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
            Logger.Debug("Save button clicked!");
            
            // 先保存配置到文件
            try
            {
                DataPersistence.SaveConfig();
                Logger.Info("Configuration saved to file!");
            }
            catch (Exception e)
            {
                Logger.Error("Failed to save config", e);
            }
            
            // 立即應用所有槽位的設定（全身重新渲染）
            RefreshAllEquipment();
            
            Logger.Info("Configuration applied!");
            
            // 保存後自動關閉介面
            HideUI();
        }

        private void OnClearSlotClicked(EquipmentSlotType slotType)
        {
            Logger.Debug($"Clear slot: {slotType}");
            
            var profile = EquipmentSkinDataManager.Instance.CurrentProfile;
            if (profile != null && profile.SlotConfigs != null && profile.SlotConfigs.ContainsKey(slotType))
            {
                // 清空設定
                profile.SlotConfigs[slotType].SkinItemTypeID = 0;
                profile.SlotConfigs[slotType].UseSkin = false;
                
                // 刷新 UI
                RefreshUI();
                
                // 立即應用變更（全身重新渲染）
                RefreshAllEquipment();
                
                Logger.Info($"Slot {slotType} cleared!");
            }
        }

        private void OnResetClicked()
        {
            Logger.Debug("Reset button clicked!");
            EquipmentSkinDataManager.Instance.ResetProfile();
            RefreshUI();
            Logger.Info("Configuration reset!");
        }

        private void OnCloseClicked()
        {
            Logger.Debug("Close button clicked!");
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
                Logger.Debug("Created EventSystem for UI interaction");
            }
            else
            {
                Logger.Debug("EventSystem already exists");
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
            Texture2D texture = new Texture2D(width, height, TextureFormat.RGBA32, true); // 啟用 mipmap
            texture.filterMode = FilterMode.Trilinear; // 三線性過濾，最平滑
            texture.wrapMode = TextureWrapMode.Clamp;
            Color[] pixels = new Color[width * height];
            
            // 使用超採樣抗鋸齒（每個像素採樣 4 次）
            for (int y = 0; y < height; y++)
            {
                for (int x = 0; x < width; x++)
                {
                    float totalAlpha = 0f;
                    int samples = 4; // 2x2 超採樣
                    
                    for (int sy = 0; sy < 2; sy++)
                    {
                        for (int sx = 0; sx < 2; sx++)
                        {
                            float px = x + (sx + 0.5f) / 2f;
                            float py = y + (sy + 0.5f) / 2f;
                            
                            float alpha = CalculateAlpha(px, py, width, height, cornerRadius);
                            totalAlpha += alpha;
                        }
                    }
                    
                    pixels[y * width + x] = new Color(1f, 1f, 1f, totalAlpha / samples);
                }
            }
            
            texture.SetPixels(pixels);
            texture.Apply(true); // 生成 mipmap
            
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
        
        /// <summary>
        /// 計算指定位置的 alpha 值（用於圓角）
        /// </summary>
        private float CalculateAlpha(float x, float y, int width, int height, int cornerRadius)
        {
            // 檢查是否在四個角落區域
            bool inTopLeft = x < cornerRadius && y > height - cornerRadius;
            bool inTopRight = x > width - cornerRadius && y > height - cornerRadius;
            bool inBottomLeft = x < cornerRadius && y < cornerRadius;
            bool inBottomRight = x > width - cornerRadius && y < cornerRadius;
            
            if (!inTopLeft && !inTopRight && !inBottomLeft && !inBottomRight)
            {
                return 1f; // 不在角落，完全不透明
            }
            
            // 計算到角落中心的距離
            float dx = 0, dy = 0;
            
            if (inTopLeft)
            {
                dx = cornerRadius - x;
                dy = y - (height - cornerRadius);
            }
            else if (inTopRight)
            {
                dx = x - (width - cornerRadius);
                dy = y - (height - cornerRadius);
            }
            else if (inBottomLeft)
            {
                dx = cornerRadius - x;
                dy = cornerRadius - y;
            }
            else if (inBottomRight)
            {
                dx = x - (width - cornerRadius);
                dy = cornerRadius - y;
            }
            
            float distance = Mathf.Sqrt(dx * dx + dy * dy);
            
            // 平滑過渡（使用 smoothstep）
            if (distance > cornerRadius)
                return 0f;
            else if (distance < cornerRadius - 1.5f)
                return 1f;
            else
            {
                // smoothstep 函數提供更平滑的過渡
                float t = (distance - (cornerRadius - 1.5f)) / 1.5f;
                return 1f - (t * t * (3f - 2f * t));
            }
        }
    }
}
