using UnityEngine;
using UnityEngine.InputSystem;

namespace EasyPanel
{
	/// <summary>
	/// 環形面板輸入處理器
	/// 負責監聽快捷鍵並控制環形面板的顯示/隱藏
	/// 支持自定義按鍵並持久化保存
	/// </summary>
	public class RingMenuInputHandler : MonoBehaviour
	{
		private RingMenuSettings settings;
		private bool isMenuOpen = false;
		private PlayerInput playerInput;

		private void Awake()
		{
			// 載入設置
			settings = RingMenuSettings.Load();
		Debug.Log($"[EasyPanel] InputHandler 載入設定: {settings.OpenMenuKeyName}");
		}

		private void Start()
		{
			// 獲取 PlayerInput 組件
			playerInput = GetComponent<PlayerInput>();
			if (playerInput == null)
			{
				Debug.LogWarning("RingMenuInputHandler: 找不到 PlayerInput 組件");
			}
		}

	private void Update()
	{
		HandleRingMenuInput();
		HandleDebugKeys();
		HandleQuickBindKeys();
	}

	/// <summary>
	/// 處理快速綁定按鍵（Ctrl + 數字鍵）
	/// </summary>
	private void HandleQuickBindKeys()
	{
		if (!InputManager.InputActived) return;

		Keyboard keyboard = Keyboard.current;
		if (keyboard == null) return;

		// 必須按住 Ctrl
		if (!keyboard[Key.LeftCtrl].isPressed && !keyboard[Key.RightCtrl].isPressed)
			return;

		// 檢查數字鍵 1-8
		for (int i = 0; i < 8; i++)
		{
			Key numberKey = Key.Digit1 + i;
			if (keyboard[numberKey].wasPressedThisFrame)
			{
				BindCurrentItemToSlot(i);
				break;
			}
		}
	}

	/// <summary>
	/// 綁定當前手持物品到指定格子
	/// </summary>
	private void BindCurrentItemToSlot(int slotIndex)
	{
		CharacterMainControl player = CharacterMainControl.Main;
		if (player == null)
		{
			Debug.LogWarning("[EasyPanel] 找不到玩家角色");
			return;
		}

		Item currentItem = player.CurrentItem;
		if (currentItem == null)
		{
			Debug.Log($"[EasyPanel] Ctrl+{slotIndex + 1}: 當前沒有手持物品");
			return;
		}

		Debug.Log($"[EasyPanel] Ctrl+{slotIndex + 1}: 綁定物品 {currentItem.DisplayName} (TypeID: {currentItem.TypeID}) 到格子 {slotIndex}");

		// 直接調用面板的設置方法
		RingMenuPanel.Instance?.SetSlotItem(slotIndex, currentItem);
	}

	/// <summary>
	/// 處理調試按鍵（用於測試）
	/// </summary>
	private void HandleDebugKeys()
	{
		if (!InputManager.InputActived) return;

		Keyboard keyboard = Keyboard.current;
		if (keyboard == null) return;

		// F5: 手動觸發保存測試
		if (keyboard[Key.F5].wasPressedThisFrame)
		{
			Debug.Log("[EasyPanel Debug] F5 按下 - 測試保存虛擬數據");
			TestSaveConfiguration();
		}

		// F6: 手動觸發載入測試
		if (keyboard[Key.F6].wasPressedThisFrame)
		{
			Debug.Log("[EasyPanel Debug] F6 按下 - 測試載入配置");
			TestLoadConfiguration();
		}

		// F7: 清除所有配置
		if (keyboard[Key.F7].wasPressedThisFrame)
		{
			Debug.Log("[EasyPanel Debug] F7 按下 - 清除所有配置");
			PlayerPrefs.DeleteKey("EasyPanel_SlotConfigs");
			PlayerPrefs.Save();
			Debug.Log("[EasyPanel Debug] ✓ 配置已清除");
		}
	}

	/// <summary>
	/// 測試保存配置
	/// </summary>
	private void TestSaveConfiguration()
	{
		// 創建測試數據
		var testConfig = new RingMenuPanel.SlotConfigsList
		{
			configs = new RingMenuPanel.SlotConfig[]
			{
				new RingMenuPanel.SlotConfig { slotIndex = 0, itemTypeID = "999" },
				new RingMenuPanel.SlotConfig { slotIndex = 1, itemTypeID = "888" },
				new RingMenuPanel.SlotConfig { slotIndex = 2, itemTypeID = "777" }
			}
		};

		string json = JsonUtility.ToJson(testConfig);
		Debug.Log($"[EasyPanel Debug] 測試 JSON: {json}");

		PlayerPrefs.SetString("EasyPanel_SlotConfigs", json);
		PlayerPrefs.Save();

		// 驗證
		string saved = PlayerPrefs.GetString("EasyPanel_SlotConfigs", "");
		Debug.Log($"[EasyPanel Debug] ✓ 已保存，驗證讀取: {saved}");
	}

	/// <summary>
	/// 測試載入配置
	/// </summary>
	private void TestLoadConfiguration()
	{
		if (!PlayerPrefs.HasKey("EasyPanel_SlotConfigs"))
		{
			Debug.LogWarning("[EasyPanel Debug] PlayerPrefs 中沒有配置");
			return;
		}

		string json = PlayerPrefs.GetString("EasyPanel_SlotConfigs");
		Debug.Log($"[EasyPanel Debug] 讀取到的 JSON: {json}");

		try
		{
			var config = JsonUtility.FromJson<RingMenuPanel.SlotConfigsList>(json);
			if (config != null && config.configs != null)
			{
				Debug.Log($"[EasyPanel Debug] ✓ 成功解析，共 {config.configs.Length} 個配置");
				foreach (var item in config.configs)
				{
					Debug.Log($"[EasyPanel Debug]   - 格子 {item.slotIndex}: TypeID = {item.itemTypeID}");
				}
			}
			else
			{
				Debug.LogWarning("[EasyPanel Debug] 解析失敗或配置為空");
			}
		}
		catch (System.Exception e)
		{
			Debug.LogError($"[EasyPanel Debug] 解析錯誤: {e.Message}");
		}
	}

	/// <summary>
	/// 處理環形面板輸入
	/// </summary>
	private void HandleRingMenuInput()
	{
		// 檢查輸入是否可用
		if (!InputManager.InputActived)
		{
			return;
		}

		// 如果有活動的 View，不處理
		if (Duckov.UI.View.ActiveView != null)
		{
			return;
		}

		Keyboard keyboard = Keyboard.current;
		if (keyboard == null)
		{
			return;
		}

		// 獲取當前設置的按鍵
		Key openMenuKey = settings.GetOpenMenuKey();

		// 按下按鍵
		if (keyboard[openMenuKey].wasPressedThisFrame)
		{
			Debug.Log($"[EasyPanel] 按鍵被按下: {openMenuKey}, 面板狀態: {isMenuOpen}");

			if (!isMenuOpen)
			{
				// 面板未開啟：開啟面板
				OpenRingMenu();
				Debug.Log("[EasyPanel] 面板已打開，請移動滑鼠到想要的格子");
			}
			else
			{
				// 面板已開啟：使用當前選中的格子（如果有），然後關閉
				Debug.Log("[EasyPanel] 面板已開啟，準備使用選中的格子並關閉");
				RingMenuPanel.UseSelectedSlot();
				CloseRingMenu();
			}
		}
	}

		/// <summary>
		/// 切換圓盤顯示/隱藏
		/// </summary>
		private void ToggleRingMenu()
		{
			if (isMenuOpen)
			{
				CloseRingMenu();
			}
			else
			{
				OpenRingMenu();
			}
		}

		/// <summary>
		/// 打開圓盤面板
		/// </summary>
		private void OpenRingMenu()
		{
			if (!isMenuOpen)
			{
				RingMenuPanel.Toggle();
				isMenuOpen = true;
			}
		}

		/// <summary>
		/// 關閉圓盤面板
		/// </summary>
		private void CloseRingMenu()
		{
			if (isMenuOpen)
			{
				RingMenuPanel.Toggle();
				isMenuOpen = false;
			}
		}

		/// <summary>
		/// 外部調用：強制關閉面板
		/// </summary>
		public void ForceClose()
		{
			if (isMenuOpen)
			{
				CloseRingMenu();
			}
		}

		/// <summary>
		/// 獲取當前設置
		/// </summary>
		public RingMenuSettings GetSettings()
		{
			return settings;
		}

		/// <summary>
		/// 更新設置並保存
		/// </summary>
		public void UpdateSettings(RingMenuSettings newSettings)
		{
			settings = newSettings;
			settings.Save();
		}

		/// <summary>
		/// 設置快捷鍵
		/// </summary>
		public void SetOpenMenuKey(string keyName)
		{
			if (RingMenuSettings.IsValidKeyName(keyName))
			{
				settings.OpenMenuKeyName = keyName;
				Debug.Log($"環形面板快捷鍵已更改為: {keyName}");
			}
			else
			{
				Debug.LogWarning($"無效的按鍵名稱: {keyName}");
			}
		}

		/// <summary>
		/// 設置是否按住顯示
		/// </summary>
		public void SetHoldToShow(bool hold)
		{
			settings.HoldToShow = hold;
		}

		/// <summary>
		/// 設置是否使用滑鼠確認
		/// </summary>
		public void SetUseMouseConfirm(bool use)
		{
			settings.UseMouseConfirm = use;
		}

		/// <summary>
		/// 重新載入設定
		/// </summary>
		public void ReloadSettings()
		{
			settings = RingMenuSettings.Load();
			Debug.Log($"[EasyPanel] InputHandler 設定已重新載入，快捷鍵: {settings.OpenMenuKeyName}");
		}

		private void OnDisable()
		{
			// 組件禁用時關閉面板
			if (isMenuOpen)
			{
				RingMenuPanel.Hide();
				isMenuOpen = false;
			}
		}
	}
}
