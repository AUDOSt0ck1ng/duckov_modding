using UnityEngine;
using UnityEngine.UI;
using UnityEngine.EventSystems;
using ItemStatsSystem;

namespace EasyPanel
{
	/// <summary>
	/// 圓盤格子 - 支持接收遊戲原生的拖曳系統
	/// </summary>
	public class RingSlot : MonoBehaviour, IDropHandler, IPointerEnterHandler, IPointerExitHandler, IPointerClickHandler
	{
		private int slotIndex;
		private RingMenuPanel parentPanel;
		private Item currentItem;

		// 存儲物品 TypeID 用於消耗品的持久化
		private int storedItemTypeID = -1;
		private bool isEquipmentSlot = false; // 是否為裝備槽（存儲 instance）

		private Image backgroundImage;
		private Image iconImage;
		private Text countText;
		private GameObject iconObject;
		private GameObject countObject;

		private bool isSelected = false;
		private bool isHovering = false;

		private Color normalColor = new Color(0.2f, 0.2f, 0.2f, 0.3f);
		private Color selectedColor = new Color(0.8f, 0.6f, 0.2f, 0.6f);
		private Color hoverColor = new Color(0.4f, 0.4f, 0.4f, 0.5f);
		private Color canDropColor = new Color(0.2f, 0.8f, 0.2f, 0.5f);

		/// <summary>
		/// 初始化格子
		/// </summary>
		public void Initialize(int index, RingMenuPanel panel)
		{
			slotIndex = index;
			parentPanel = panel;

			CreateUI();
			RegisterDragEvents();
		}

		/// <summary>
		/// 創建UI元素
		/// </summary>
		private void CreateUI()
		{
			// 背景
			backgroundImage = gameObject.AddComponent<Image>();
			backgroundImage.color = normalColor;

			// 添加外框
			var outline = gameObject.AddComponent<Outline>();
			outline.effectColor = new Color(0.8f, 0.8f, 0.8f, 0.5f);
			outline.effectDistance = new Vector2(2, 2);

			// 創建圖標容器
			iconObject = new GameObject("Icon");
			iconObject.transform.SetParent(transform, false);

			var iconRect = iconObject.AddComponent<RectTransform>();
			iconRect.anchorMin = new Vector2(0.5f, 0.5f);
			iconRect.anchorMax = new Vector2(0.5f, 0.5f);
			iconRect.pivot = new Vector2(0.5f, 0.5f);
			iconRect.sizeDelta = new Vector2(60, 60);
			iconRect.anchoredPosition = Vector2.zero;

			iconImage = iconObject.AddComponent<Image>();
			iconImage.enabled = false;

			// 創建數量文字
			countObject = new GameObject("Count");
			countObject.transform.SetParent(transform, false);

			var countRect = countObject.AddComponent<RectTransform>();
			countRect.anchorMin = new Vector2(1, 0);
			countRect.anchorMax = new Vector2(1, 0);
			countRect.pivot = new Vector2(1, 0);
			countRect.sizeDelta = new Vector2(30, 20);
			countRect.anchoredPosition = new Vector2(-5, 5);

			countText = countObject.AddComponent<Text>();
			countText.font = Resources.GetBuiltinResource<Font>("Arial.ttf");
			countText.fontSize = 14;
			countText.alignment = TextAnchor.LowerRight;
			countText.color = Color.white;
			countObject.SetActive(false);

			// 添加格子編號
			GameObject numberObj = new GameObject("Number");
			numberObj.transform.SetParent(transform, false);

			var numberRect = numberObj.AddComponent<RectTransform>();
			numberRect.anchorMin = new Vector2(0, 1);
			numberRect.anchorMax = new Vector2(0, 1);
			numberRect.pivot = new Vector2(0, 1);
			numberRect.sizeDelta = new Vector2(20, 20);
			numberRect.anchoredPosition = new Vector2(5, -5);

			var numberText = numberObj.AddComponent<Text>();
			numberText.font = Resources.GetBuiltinResource<Font>("Arial.ttf");
			numberText.fontSize = 12;
			numberText.text = (slotIndex + 1).ToString();
			numberText.alignment = TextAnchor.UpperLeft;
			numberText.color = new Color(1f, 1f, 1f, 0.6f);
		}

		/// <summary>
		/// 註冊拖曳事件（監聽遊戲的拖曳系統）
		/// </summary>
		private void RegisterDragEvents()
		{
			// 訂閱遊戲的拖曳事件
			// IItemDragSource.OnStartDragItem 和 OnEndDragItem
			// 這些是遊戲原生的靜態事件
		}

	/// <summary>
	/// 設置道具
	/// </summary>
	public void SetItem(Item item)
	{
		currentItem = item;

		// 記錄 TypeID 用於持久化
		if (item != null)
		{
			storedItemTypeID = item.TypeID;
			// 判斷是否為裝備類物品
			isEquipmentSlot = IsEquipmentItem(item);
			Debug.Log($"[RingSlot {slotIndex}] 設置物品: {item.DisplayName}, TypeID: {storedItemTypeID}, 是裝備: {isEquipmentSlot}");
		}
		else
		{
			storedItemTypeID = -1;
			isEquipmentSlot = false;
		}

		Refresh();
	}

	/// <summary>
	/// 獲取道具（如果是消耗品且 instance 已失效，會嘗試從背包找同類型）
	/// </summary>
	public Item GetItem()
	{
		// 如果沒有存儲任何物品
		if (storedItemTypeID == -1)
		{
			return null;
		}

		// 如果是裝備類，返回原始 instance（可能為 null）
		if (isEquipmentSlot)
		{
			return currentItem;
		}

		// 如果是消耗品，嘗試從背包中找到同類型的物品
		if (currentItem == null || currentItem.Equals(null))
		{
			currentItem = FindItemByTypeID(storedItemTypeID);
			if (currentItem != null)
			{
				Debug.Log($"[RingSlot {slotIndex}] 從背包找到同類型物品: {currentItem.DisplayName}");
			}
		}

		return currentItem;
	}

	/// <summary>
	/// 獲取存儲的物品 TypeID
	/// </summary>
	public int GetStoredTypeID()
	{
		return storedItemTypeID;
	}

	/// <summary>
	/// 從 TypeID 設置物品（用於載入配置）
	/// </summary>
	public void SetItemByTypeID(int typeID)
	{
		storedItemTypeID = typeID;
		if (typeID > 0)
		{
			currentItem = FindItemByTypeID(typeID);
			if (currentItem != null)
			{
				isEquipmentSlot = IsEquipmentItem(currentItem);
			}
		}
		Refresh();
	}

	/// <summary>
	/// 刷新顯示
	/// </summary>
	public void Refresh()
	{
		// 嘗試獲取物品（會自動從背包查找）
		Item displayItem = GetItem();

		if (displayItem != null && !displayItem.Equals(null))
		{
			// 顯示圖標
			if (displayItem.Icon != null)
			{
				iconImage.sprite = displayItem.Icon;
				iconImage.enabled = true;
				iconImage.color = Color.white; // 正常顏色
			}
			else
			{
				iconImage.enabled = false;
			}

			// 顯示數量
			if (displayItem.Stackable && displayItem.StackCount > 1)
			{
				countText.text = displayItem.StackCount.ToString();
				countObject.SetActive(true);
			}
			else
			{
				countObject.SetActive(false);
			}
		}
		else if (storedItemTypeID > 0)
		{
			// 有配置但背包中沒有可用物品 - 顯示為灰色或佔位符
			// 嘗試從資料庫獲取物品資訊來顯示圖示
			Item templateItem = GetItemTemplate(storedItemTypeID);
			if (templateItem != null && templateItem.Icon != null)
			{
				iconImage.sprite = templateItem.Icon;
				iconImage.enabled = true;
				iconImage.color = new Color(0.5f, 0.5f, 0.5f, 0.5f); // 灰色半透明
			}
			else
			{
				iconImage.enabled = false;
			}

			countObject.SetActive(false);
		}
		else
		{
			// 完全空的格子
			iconImage.enabled = false;
			countObject.SetActive(false);
		}
	}

		/// <summary>
		/// 設置選中狀態
		/// </summary>
		public void SetSelected(bool selected)
		{
			isSelected = selected;
			UpdateVisuals();
		}

		/// <summary>
		/// 更新視覺效果
		/// </summary>
		private void UpdateVisuals()
		{
			if (isSelected)
			{
				backgroundImage.color = selectedColor;
				transform.localScale = Vector3.one * 1.2f;
			}
			else if (isHovering)
			{
				backgroundImage.color = hoverColor;
				transform.localScale = Vector3.one * 1.1f;
			}
			else
			{
				backgroundImage.color = normalColor;
				transform.localScale = Vector3.one;
			}
		}

	/// <summary>
	/// 處理道具拖放（接收遊戲原生的拖曳）
	/// </summary>
	public void OnDrop(PointerEventData eventData)
	{
		Debug.Log($"[RingSlot {slotIndex}] OnDrop 被觸發！");

		if (eventData.button != PointerEventData.InputButton.Left)
		{
			Debug.Log($"[RingSlot {slotIndex}] 不是左鍵拖曳，忽略");
			return;
		}

		Debug.Log($"[RingSlot {slotIndex}] pointerDrag: {eventData.pointerDrag?.name}");

		// 獲取拖曳源（使用遊戲的接口）
		var dragSource = eventData.pointerDrag?.GetComponent(System.Type.GetType("IItemDragSource, TeamSoda.Duckov.Core"));
		if (dragSource == null)
		{
			Debug.Log($"[RingSlot {slotIndex}] 未找到 IItemDragSource");
			return;
		}

		Debug.Log($"[RingSlot {slotIndex}] 找到 IItemDragSource: {dragSource.GetType().Name}");

		// 通過反射獲取道具
		var getItemMethod = dragSource.GetType().GetMethod("GetItem");
		if (getItemMethod == null)
		{
			Debug.Log($"[RingSlot {slotIndex}] 未找到 GetItem 方法");
			return;
		}

		Item item = getItemMethod.Invoke(dragSource, null) as Item;
		if (item == null)
		{
			Debug.Log($"[RingSlot {slotIndex}] 獲取的道具為 null");
			return;
		}

		Debug.Log($"[RingSlot {slotIndex}] 準備設置道具: {item.DisplayName} (TypeID: {item.TypeID})");

		// 設置道具到格子
		SetItem(item);
		parentPanel.SetSlotItem(slotIndex, item);

		Debug.Log($"[RingSlot {slotIndex}] ✓ 成功拖曳道具: {item.DisplayName}");

		// 恢復視覺效果
		backgroundImage.color = normalColor;
	}

		/// <summary>
		/// 鼠標進入
		/// </summary>
		public void OnPointerEnter(PointerEventData eventData)
		{
			isHovering = true;

			// 如果正在拖曳道具，顯示可放置提示
			if (eventData.pointerDrag != null)
			{
				backgroundImage.color = canDropColor;
			}
			else if (!isSelected)
			{
				backgroundImage.color = hoverColor;
			}
		}

		/// <summary>
		/// 鼠標離開
		/// </summary>
		public void OnPointerExit(PointerEventData eventData)
		{
			isHovering = false;

			if (!isSelected)
			{
				backgroundImage.color = normalColor;
			}
		}

		/// <summary>
		/// 點擊格子
		/// </summary>
		public void OnPointerClick(PointerEventData eventData)
		{
			if (eventData.button == PointerEventData.InputButton.Left)
			{
				// 左鍵點擊空格子，嘗試放入當前選中的物品
				if (currentItem == null)
				{
					TryPlaceSelectedItem();
				}
			}
			else if (eventData.button == PointerEventData.InputButton.Right)
			{
				// 右鍵清除格子
				if (currentItem != null)
				{
					SetItem(null);
					parentPanel.SetSlotItem(slotIndex, null);
					Debug.Log($"[RingSlot {slotIndex}] 已清除");
				}
			}
		}

	/// <summary>
	/// 使用格子中的物品（由外部呼叫）
	/// </summary>
	public void UseItem()
	{
		// 如果沒有存儲任何物品配置
		if (storedItemTypeID == -1)
		{
			Debug.Log($"[RingSlot {slotIndex}] 沒有配置任何物品");
			return;
		}

		// 嘗試獲取物品（會自動從背包尋找）
		Item itemToUse = GetItem();

		if (itemToUse == null)
		{
			Debug.Log($"[RingSlot {slotIndex}] 背包中沒有可用的物品 (TypeID: {storedItemTypeID})");
			// 不清空配置，保留 TypeID
			Refresh(); // 更新顯示（可能顯示為灰色或無圖示）
			return;
		}

		// 獲取主角
		CharacterMainControl mainCharacter = CharacterMainControl.Main;
		if (mainCharacter == null)
		{
			Debug.LogWarning("[RingSlot] 找不到主角控制器");
			return;
		}

		// 判斷物品類型並處理
		if (itemToUse.UsageUtilities != null && itemToUse.UsageUtilities.IsUsable(itemToUse, mainCharacter))
		{
			// 消耗型物品：使用
			mainCharacter.UseItem(itemToUse);
			Debug.Log($"[RingSlot {slotIndex}] 使用道具: {itemToUse.DisplayName}");

			// 使用後，currentItem 可能失效，但保留 TypeID
			// 下次使用時會自動從背包找下一個同類型物品
		}
		else if (itemToUse.GetBool("IsSkill"))
		{
			// 技能：切換
			mainCharacter.ChangeHoldItem(itemToUse);
			Debug.Log($"[RingSlot {slotIndex}] 切換技能: {itemToUse.DisplayName}");
		}
		else if (itemToUse.HasHandHeldAgent)
		{
			// 武器/裝備：裝備
			mainCharacter.ChangeHoldItem(itemToUse);
			Debug.Log($"[RingSlot {slotIndex}] 裝備物品: {itemToUse.DisplayName}");
		}
		else
		{
			Debug.Log($"[RingSlot {slotIndex}] 無法使用此物品: {itemToUse.DisplayName}");
		}
	}

	/// <summary>
	/// 嘗試放入當前選中的物品（使用遊戲的選擇系統）
	/// </summary>
	private void TryPlaceSelectedItem()
	{
		// 獲取 ItemUIUtilities.SelectedItem
		var itemUIUtilitiesType = System.Type.GetType("Duckov.UI.ItemUIUtilities, TeamSoda.Duckov.Core");
		if (itemUIUtilitiesType == null)
		{
			Debug.Log("[RingSlot] 未找到 ItemUIUtilities");
			return;
		}

		var selectedItemProperty = itemUIUtilitiesType.GetProperty("SelectedItem",
			System.Reflection.BindingFlags.Public | System.Reflection.BindingFlags.Static);
		if (selectedItemProperty == null)
		{
			Debug.Log("[RingSlot] 未找到 SelectedItem 屬性");
			return;
		}

		Item selectedItem = selectedItemProperty.GetValue(null) as Item;
		if (selectedItem != null)
		{
			SetItem(selectedItem);
			parentPanel.SetSlotItem(slotIndex, selectedItem);
			Debug.Log($"[RingSlot {slotIndex}] 通過選擇放入道具: {selectedItem.DisplayName}");

			// 清除選擇
			var selectMethod = itemUIUtilitiesType.GetMethod("Select",
				System.Reflection.BindingFlags.Public | System.Reflection.BindingFlags.Static);
			if (selectMethod != null)
			{
				selectMethod.Invoke(null, new object[] { null });
			}
		}
	}

	/// <summary>
	/// 判斷是否為裝備類物品（需要保存 instance）
	/// </summary>
	private bool IsEquipmentItem(Item item)
	{
		if (item == null) return false;

		// 武器、防具等有 HandHeldAgent 的物品視為裝備
		if (item.HasHandHeldAgent)
		{
			return true;
		}

		// 技能視為裝備
		if (item.GetBool("IsSkill"))
		{
			return true;
		}

		// 其他物品（消耗品、投擲物等）不是裝備
		return false;
	}

	/// <summary>
	/// 從玩家背包中查找指定 TypeID 的物品
	/// </summary>
	private Item FindItemByTypeID(int typeID)
	{
		CharacterMainControl mainCharacter = CharacterMainControl.Main;
		if (mainCharacter == null)
		{
			return null;
		}

		// 獲取玩家的物品容器（背包）
		// 使用反射訪問 ItemContainer 或 Inventory
		try
		{
			var itemContainerProperty = mainCharacter.GetType().GetProperty("ItemContainer");
			if (itemContainerProperty == null)
			{
				itemContainerProperty = mainCharacter.GetType().GetProperty("Inventory");
			}

			if (itemContainerProperty == null)
			{
				Debug.LogWarning("[RingSlot] 找不到 ItemContainer 或 Inventory 屬性");
				return null;
			}

			var itemContainer = itemContainerProperty.GetValue(mainCharacter);
			if (itemContainer == null)
			{
				return null;
			}

			// 獲取 Items 集合
			var itemsProperty = itemContainer.GetType().GetProperty("Items");
			if (itemsProperty == null)
			{
				Debug.LogWarning("[RingSlot] 找不到 Items 屬性");
				return null;
			}

			var items = itemsProperty.GetValue(itemContainer) as System.Collections.IEnumerable;
			if (items == null)
			{
				return null;
			}

			// 遍歷背包中的所有物品
			foreach (var obj in items)
			{
				Item item = obj as Item;
				if (item != null && !item.Equals(null) && item.TypeID == typeID)
				{
					return item;
				}
			}
		}
		catch (System.Exception e)
		{
			Debug.LogWarning($"[RingSlot] 查找物品時發生錯誤: {e.Message}");
		}

		return null;
	}

	/// <summary>
	/// 從物品資料庫獲取物品模板（用於顯示圖示）
	/// </summary>
	private Item GetItemTemplate(int typeID)
	{
		// 暫時簡化實作：如果背包中沒有，就不顯示圖示
		// 未來可以透過反射或其他方式訪問遊戲的物品資料庫
		return null;
	}
}



}
