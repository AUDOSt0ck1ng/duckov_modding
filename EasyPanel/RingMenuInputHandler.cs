using UnityEngine;
using UnityEngine.InputSystem;
using ItemStatsSystem;
using Duckov;

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
