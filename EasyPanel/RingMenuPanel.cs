using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.EventSystems;
using ItemStatsSystem;
using Duckov;
using Saves;

namespace EasyPanel
{
	/// <summary>
	/// 環形面板主控制器 - 圓盤式UI
	/// </summary>
	public class RingMenuPanel : UIPanel
	{
		[Header("圓盤設置")]
		[SerializeField]
		private float diskRadius = 180f; // 圓盤半徑

		[SerializeField]
		private int slotCount = 8; // 格子數量

		[SerializeField]
		private float slotSize = 70f; // 每個格子的大小

		private GameObject diskBackground; // 圓盤背景
		private List<RingSlot> slots = new List<RingSlot>();
		private int selectedSlotIndex = -1;
		private bool isOpen = false;
		private bool configLoaded = false; // 配置是否已載入
		private RingMenuSettings settings; // 設定

		// 單例模式
		private static RingMenuPanel instance;
		public static RingMenuPanel Instance => instance;

	private void Awake()
	{
		if (instance == null)
		{
			instance = this;
		}
		else
		{
			Destroy(gameObject);
			return;
		}

		// 載入設定
		settings = RingMenuSettings.Load();
		Debug.Log($"[EasyPanel] 設定已載入，選取半徑: {settings.SelectionRadius}");

		// 訂閱 LevelManager 初始化完成事件
		LevelManager.OnAfterLevelInitialized += OnLevelInitialized;
	}

	private void OnDestroy()
	{
		// 取消訂閱事件
		LevelManager.OnAfterLevelInitialized -= OnLevelInitialized;
	}

	/// <summary>
	/// 場景完全初始化後的回調
	/// 在此時載入配置,確保角色、背包、裝備都已準備好
	/// </summary>
	private void OnLevelInitialized()
	{
		Debug.Log("[EasyPanel] LevelManager 初始化完成，開始載入格子配置");

		// 場景初始化後重新載入配置
		if (configLoaded && slots != null && slots.Count > 0)
		{
			LoadSlotConfiguration();
			RefreshAllSlots();
			Debug.Log("[EasyPanel] 格子配置已重新載入");
		}
	}

	/// <summary>
	/// 初始化 UI（運行時創建圓盤）
	/// </summary>
	public void InitializeUI()
	{
		// 創建圓盤背景
		CreateDiskBackground();

		// 創建中心設定按鈕
		CreateCenterButton();

		// 創建格子
		CreateSlots();

		// 立即載入配置
		LoadSlotConfiguration();
		configLoaded = true;

		// 初始化時隱藏
		gameObject.SetActive(false);

		Debug.Log($"[EasyPanel] 圓盤UI已初始化，共{slotCount}個格子");
	}

		/// <summary>
		/// 創建圓盤背景
		/// </summary>
		private void CreateDiskBackground()
		{
			diskBackground = new GameObject("DiskBackground");
			diskBackground.transform.SetParent(transform, false);

			var bgRect = diskBackground.AddComponent<RectTransform>();
			bgRect.anchorMin = new Vector2(0.5f, 0.5f);
			bgRect.anchorMax = new Vector2(0.5f, 0.5f);
			bgRect.pivot = new Vector2(0.5f, 0.5f);
			bgRect.anchoredPosition = Vector2.zero;
			bgRect.sizeDelta = new Vector2(diskRadius * 2.5f, diskRadius * 2.5f);

			// 不添加背景圖片，保持完全透明
			// 只添加圓形外框輪廓線
			var bgImage = diskBackground.AddComponent<Image>();
			bgImage.color = new Color(0f, 0f, 0f, 0f); // 完全透明背景
			bgImage.raycastTarget = false; // 不接收鼠標事件

		}

		/// <summary>
		/// 創建中心設定按鈕
		/// </summary>
		private void CreateCenterButton()
		{
			GameObject centerButton = new GameObject("CenterButton");
			centerButton.transform.SetParent(diskBackground.transform, false);

			var buttonRect = centerButton.AddComponent<RectTransform>();
			buttonRect.anchorMin = new Vector2(0.5f, 0.5f);
			buttonRect.anchorMax = new Vector2(0.5f, 0.5f);
			buttonRect.pivot = new Vector2(0.5f, 0.5f);
			buttonRect.anchoredPosition = Vector2.zero;
			buttonRect.sizeDelta = new Vector2(60f, 60f); // 直徑60的圓形按鈕

			// 添加圓形背景
			var buttonImage = centerButton.AddComponent<Image>();
			buttonImage.color = new Color(0.4f, 0.4f, 0.4f, 0.6f); // 半透明灰色

			// 添加外框
			var outline = centerButton.AddComponent<Outline>();
			outline.effectColor = new Color(0.9f, 0.7f, 0.3f, 0.8f); // 金色輪廓
			outline.effectDistance = new Vector2(2, 2);

			// 添加設定圖標文字 (齒輪符號)
			GameObject iconText = new GameObject("Icon");
			iconText.transform.SetParent(centerButton.transform, false);

			var textRect = iconText.AddComponent<RectTransform>();
			textRect.anchorMin = Vector2.zero;
			textRect.anchorMax = Vector2.one;
			textRect.sizeDelta = Vector2.zero;

			var text = iconText.AddComponent<Text>();
			text.font = Resources.GetBuiltinResource<Font>("Arial.ttf");
			text.fontSize = 30;
			text.text = "⚙"; // 齒輪符號
			text.alignment = TextAnchor.MiddleCenter;
			text.color = Color.white;
		}

		/// <summary>
		/// 創建格子
		/// </summary>
		private void CreateSlots()
		{
			float angleStep = 360f / slotCount;
			float startAngle = 90f; // 從正上方開始

			for (int i = 0; i < slotCount; i++)
			{
				float angle = startAngle - (angleStep * i);
				float radian = angle * Mathf.Deg2Rad;

				// 計算位置
				Vector2 position = new Vector2(
					Mathf.Cos(radian) * diskRadius,
					Mathf.Sin(radian) * diskRadius
				);

				// 創建格子
				GameObject slotObj = new GameObject($"Slot_{i}");
				slotObj.transform.SetParent(diskBackground.transform, false);

				var slotRect = slotObj.AddComponent<RectTransform>();
				slotRect.anchorMin = new Vector2(0.5f, 0.5f);
				slotRect.anchorMax = new Vector2(0.5f, 0.5f);
				slotRect.pivot = new Vector2(0.5f, 0.5f);
				slotRect.anchoredPosition = position;
				slotRect.sizeDelta = new Vector2(slotSize, slotSize);

				// 添加格子組件
				var slot = slotObj.AddComponent<RingSlot>();
				slot.Initialize(i, this);

				slots.Add(slot);
			}
		}

		/// <summary>
		/// 顯示/隱藏圓盤（切換）
		/// </summary>
		public static void Toggle()
		{
			if (instance == null) return;

			if (instance.isOpen)
			{
				Hide();
			}
			else
			{
				Show();
			}
		}

		/// <summary>
		/// 顯示圓盤
		/// </summary>
		public static void Show()
		{
			if (instance != null && !instance.isOpen)
			{
				instance.ShowPanel();
			}
		}

		/// <summary>
		/// 隱藏圓盤
		/// </summary>
		public static void Hide()
		{
			if (instance != null && instance.isOpen)
			{
				instance.HidePanel();
			}
		}

	/// <summary>
	/// 使用選中格子的道具
	/// </summary>
	public static void UseSelectedSlot()
	{
		if (instance != null)
		{
			Debug.Log($"[EasyPanel] UseSelectedSlot 被呼叫，selectedSlotIndex = {instance.selectedSlotIndex}");

			// 註解：中央按鈕功能暫時停用
			// if (instance.selectedSlotIndex == -2)
			// {
			// 	// 打開設定介面
			// 	instance.OpenSettingsUI();
			// }

			if (instance.selectedSlotIndex >= 0)
			{
				instance.UseSlot(instance.selectedSlotIndex);
			}
			else
			{
				Debug.Log("[EasyPanel] 沒有選中任何格子");
			}
		}
	}

	protected override void OnOpen()
	{
		base.OnOpen();
		isOpen = true;

		// 不暫停遊戲輸入，只顯示鼠標
		Cursor.visible = true;
		Cursor.lockState = CursorLockMode.None;

		// 每次打開都刷新所有格子的顯示（因為背包內容可能變化）
		RefreshAllSlots();

		Debug.Log("[EasyPanel] 面板已打開，格子已刷新");
	}

		protected override void OnClose()
		{
			base.OnClose();
			isOpen = false;

			// 恢復鼠標狀態
			if (InputManager.InputActived)
			{
				Cursor.visible = false;
				Cursor.lockState = CursorLockMode.Locked;
			}
		}

		private void ShowPanel()
		{
			gameObject.SetActive(true);
			OnOpen();
		}

		private void HidePanel()
		{
			OnClose();
			gameObject.SetActive(false);
		}

		private void Update()
		{
			if (isOpen)
			{
				// 根據鼠標位置選擇格子
				UpdateSlotSelection();
			}
		}

		/// <summary>
		/// 根據鼠標位置更新選擇
		/// </summary>
		private void UpdateSlotSelection()
		{
			// 將鼠標位置轉換為相對於圓盤中心的向量
			RectTransform diskRect = diskBackground.GetComponent<RectTransform>();
			if (RectTransformUtility.ScreenPointToLocalPointInRectangle(
				diskRect, UnityEngine.Input.mousePosition, null, out Vector2 localPoint))
			{
				// 計算角度
				float angle = Mathf.Atan2(localPoint.y, localPoint.x) * Mathf.Rad2Deg;
				// 轉換為 0-360 度
				if (angle < 0) angle += 360f;

				// 調整角度使正上方為 0 度
				angle = (450f - angle) % 360f;

				// 計算應該選擇哪個格子
				float anglePerSlot = 360f / slotCount;
				int newIndex = Mathf.FloorToInt((angle + anglePerSlot / 2f) / anglePerSlot) % slotCount;

			// 檢查鼠標是否在圓盤範圍內
			float distance = localPoint.magnitude;

			// 註解：中央按鈕功能暫時停用
			// if (distance <= 30f)
			// {
			// 	// 在中心設定區域
			// 	if (selectedSlotIndex != -2)
			// 	{
			// 		SelectSlot(-2); // -2 代表中心設定區域
			// 	}
			// }
			// else

			if (distance <= 200f && distance > 30f)
			{
				// 在有效範圍內，選擇格子
				if (newIndex != selectedSlotIndex)
				{
					SelectSlot(newIndex);
				}
			}
			else
			{
				// 超出範圍，取消選擇
				if (selectedSlotIndex >= 0)
				{
					SelectSlot(-1);
				}
			}
			}
		}

		/// <summary>
		/// 選擇格子
		/// </summary>
		private void SelectSlot(int index)
		{
			// 取消之前的選中
			if (selectedSlotIndex >= 0 && selectedSlotIndex < slots.Count)
			{
				slots[selectedSlotIndex].SetSelected(false);
			}

			// 選中新格子
			selectedSlotIndex = index;
			if (selectedSlotIndex >= 0 && selectedSlotIndex < slots.Count)
			{
				slots[selectedSlotIndex].SetSelected(true);
				Debug.Log($"[EasyPanel] 選中格子 {selectedSlotIndex}");
			}
			else if (selectedSlotIndex == -2)
			{
				Debug.Log("[EasyPanel] 選中中心設定區域");
			}
			else if (selectedSlotIndex == -1)
			{
				Debug.Log("[EasyPanel] 取消選擇");
			}
		}

		/// <summary>
		/// 使用格子中的道具
		/// </summary>
		private void UseSlot(int slotIndex)
		{
			if (slotIndex < 0 || slotIndex >= slots.Count) return;

			var slot = slots[slotIndex];
			slot.UseItem();
		}

		/// <summary>
		/// 設置格子的道具（拖曳時調用）
		/// </summary>
		public void SetSlotItem(int slotIndex, Item item)
		{
			if (slotIndex < 0 || slotIndex >= slots.Count) return;

			slots[slotIndex].SetItem(item);
			SaveSlotConfiguration();

			Debug.Log($"[EasyPanel] 格子 {slotIndex} 設置為: {(item != null ? item.DisplayName : "空")}");
		}

	/// <summary>
	/// 刷新所有格子
	/// </summary>
	private void RefreshAllSlots()
	{
		foreach (var slot in slots)
		{
			slot.Refresh();
		}
	}

	/// <summary>
	/// 保存格子配置 - 使用 ES3 SavesSystem
	/// </summary>
	private void SaveSlotConfiguration()
	{
		try
		{
			List<ItemReference> references = new List<ItemReference>();

			for (int i = 0; i < slots.Count; i++)
			{
				ItemReference reference = slots[i].GetItemReference();
				references.Add(reference);

				if (!reference.IsEmpty)
				{
					Debug.Log($"[EasyPanel] 保存格子 {i}: {reference}");
				}
			}

			RadialMenuData data = RadialMenuData.FromSlotReferences(references);

			// 使用 SavesSystem 保存到遊戲存檔
			SavesSystem.Save("EasyPanel/RadialMenu", data);

			int nonEmpty = data.GetNonEmptySlotCount();
			Debug.Log($"[EasyPanel] ✓ 格子配置已保存到遊戲存檔，共 {nonEmpty}/{data.slotCount} 個有效配置");
		}
		catch (System.Exception e)
		{
			Debug.LogError($"[EasyPanel] 保存格子配置失敗: {e.Message}\n{e.StackTrace}");
		}
	}

	/// <summary>
	/// 載入格子配置 - 使用 ES3 SavesSystem
	/// </summary>
	private void LoadSlotConfiguration()
	{
		Debug.Log("[EasyPanel] 開始載入格子配置...");

		try
		{
			// 從 SavesSystem 載入
			RadialMenuData data = SavesSystem.Load<RadialMenuData>("EasyPanel/RadialMenu");

			if (data == null)
			{
				Debug.Log("[EasyPanel] 未找到保存的配置");
				return;
			}

			if (!data.Validate())
			{
				Debug.LogWarning("[EasyPanel] 載入的配置驗證失敗");
				return;
			}

			Debug.Log($"[EasyPanel] 成功載入配置: {data}");

			List<ItemReference> references = data.ToSlotReferences();

			int loadedCount = 0;
			for (int i = 0; i < references.Count && i < slots.Count; i++)
			{
				ItemReference reference = references[i];

				if (!reference.IsEmpty)
				{
					slots[i].SetItemReference(reference);
					loadedCount++;
					Debug.Log($"[EasyPanel] ✓ 格子 {i} 載入: {reference}");
				}
			}

			Debug.Log($"[EasyPanel] ✓ 格子配置載入完成，共載入 {loadedCount}/{references.Count} 個配置");
		}
		catch (System.Exception e)
		{
			Debug.LogError($"[EasyPanel] 載入格子配置失敗: {e.Message}\n{e.StackTrace}");
		}
	}

	/// <summary>
	/// 打開設定介面
	/// </summary>
	private void OpenSettingsUI()
	{
		Debug.Log("[EasyPanel] 打開設定介面（待實現完整UI）");

		// 尋找或創建設定UI
		var settingsUI = FindObjectOfType<RingMenuSettingsUI>();
		if (settingsUI != null)
		{
			settingsUI.gameObject.SetActive(true);
		}
		else
		{
			Debug.LogWarning("[EasyPanel] 找不到 RingMenuSettingsUI 組件");
		}
	}

	}
}
