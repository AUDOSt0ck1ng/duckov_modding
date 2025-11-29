using UnityEngine;
using UnityEngine.UI;
using UnityEngine.EventSystems;
using ItemStatsSystem;
using Duckov;

namespace EasyPanel
{
	/// <summary>
	/// 圓盤格子 - 支持接收遊戲原生的拖曳系統
	/// 使用 ItemReference 精確追蹤物品位置
	/// </summary>
	public class RingSlot : MonoBehaviour, IDropHandler, IPointerEnterHandler, IPointerExitHandler, IPointerClickHandler
	{
		private int slotIndex;
		private RingMenuPanel parentPanel;
		private ItemReference itemReference = new ItemReference();

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
		/// 設置道具
		/// </summary>
		public void SetItem(Item item)
		{
			// 從物品創建引用
			if (item != null)
			{
				itemReference = ItemReference.FromItem(item);
				Debug.Log($"[RingSlot {slotIndex}] 設置物品: {item.DisplayName}, {itemReference}");
			}
			else
			{
				itemReference.Clear();
			}

			Refresh();
		}

		/// <summary>
		/// 獲取道具 - 使用 ItemReference 智能查找
		/// </summary>
		public Item GetItem()
		{
			return itemReference.GetItem();
		}

		/// <summary>
		/// 獲取物品引用 (用於保存)
		/// </summary>
		public ItemReference GetItemReference()
		{
			return itemReference;
		}

		/// <summary>
		/// 設置物品引用 (用於載入)
		/// </summary>
		public void SetItemReference(ItemReference reference)
		{
			if (reference != null)
			{
				itemReference = reference.Clone();
			}
			else
			{
				itemReference = new ItemReference();
			}

			Refresh();
		}

		/// <summary>
		/// 刷新顯示
		/// </summary>
		public void Refresh()
		{
			// 使用引用獲取物品
			Item displayItem = GetItem();

			if (displayItem != null && !displayItem.Equals(null))
			{
				// 顯示圖標
				if (displayItem.Icon != null)
				{
					iconImage.sprite = displayItem.Icon;
					iconImage.enabled = true;
					iconImage.color = Color.white;
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
			else if (!itemReference.IsEmpty)
			{
				// 有配置但無法找到物品 - 顯示為灰色佔位符
				// TODO: 從資料庫獲取物品圖標
				iconImage.enabled = false;
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
			if (eventData.button == PointerEventData.InputButton.Right)
			{
				// 右鍵清除格子
				SetItem(null);
				parentPanel.SetSlotItem(slotIndex, null);
				Debug.Log($"[RingSlot {slotIndex}] 已清除");
			}
		}

		/// <summary>
		/// 使用格子中的物品
		/// </summary>
		public void UseItem()
		{
			// 如果沒有配置任何物品
			if (itemReference.IsEmpty)
			{
				Debug.Log($"[RingSlot {slotIndex}] 沒有配置任何物品");
				return;
			}

			// 使用引用獲取物品
			Item itemToUse = GetItem();

			if (itemToUse == null)
			{
				Debug.Log($"[RingSlot {slotIndex}] 無法找到物品 ({itemReference})");
				Refresh();
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
	}
}
