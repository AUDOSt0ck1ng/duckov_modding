using UnityEngine;
using UnityEngine.UI;
using ItemStatsSystem;

namespace EasyPanel
{
	/// <summary>
	/// 環形面板中的單個選項
	/// 顯示道具圖標和名稱，並處理選中狀態
	/// </summary>
	public class RingMenuItem : MonoBehaviour
	{
		[Header("UI 組件")]
		[SerializeField]
		[Tooltip("道具圖標")]
		private Image itemIcon;

		[SerializeField]
		[Tooltip("道具名稱文字")]
		private Text itemNameText;

		[SerializeField]
		[Tooltip("數量文字")]
		private Text itemCountText;

		[SerializeField]
		[Tooltip("快捷鍵編號文字")]
		private Text shortcutNumberText;

		[SerializeField]
		[Tooltip("選中時的邊框")]
		private Image selectionBorder;

		[SerializeField]
		[Tooltip("背景圖片")]
		private Image backgroundImage;

		[Header("視覺設置")]
		[SerializeField]
		[Tooltip("選中時的縮放比例")]
		private float selectedScale = 1.2f;

		[SerializeField]
		[Tooltip("選中時的顏色")]
		private Color selectedColor = Color.yellow;

		[SerializeField]
		[Tooltip("未選中時的顏色")]
		private Color normalColor = Color.white;

		private Item item;
		private int shortcutIndex;
		private bool isSelected = false;

		/// <summary>
		/// 初始化選項（動態UI版本）
		/// </summary>
		public void InitializeDynamic(Item item, int shortcutIndex, Image iconImage, Image bgImage)
		{
			this.item = item;
			this.shortcutIndex = shortcutIndex;
			this.itemIcon = iconImage;
			this.backgroundImage = bgImage;
		}

		/// <summary>
		/// 初始化選項
		/// </summary>
		/// <param name="item">道具數據</param>
		/// <param name="shortcutIndex">快捷欄索引</param>
		public void Initialize(Item item, int shortcutIndex)
		{
			this.item = item;
			this.shortcutIndex = shortcutIndex;

			UpdateDisplay();
		}

		/// <summary>
		/// 設置選中狀態
		/// </summary>
		public void SetSelected(bool selected)
		{
			isSelected = selected;

			// 更新視覺效果
			if (selectionBorder != null)
			{
				selectionBorder.enabled = selected;
			}

			if (backgroundImage != null)
			{
				backgroundImage.color = selected ? selectedColor : normalColor;
			}

			// 縮放效果
			transform.localScale = selected ? Vector3.one * selectedScale : Vector3.one;
		}

		/// <summary>
		/// 獲取道具數據
		/// </summary>
		public Item GetItem()
		{
			return item;
		}

		/// <summary>
		/// 更新顯示
		/// </summary>
		private void UpdateDisplay()
		{
			if (item == null)
			{
				return;
			}

			// 設置圖標
			if (itemIcon != null)
			{
				Sprite icon = GetItemIcon(item);
				if (icon != null)
				{
					itemIcon.sprite = icon;
					itemIcon.enabled = true;
				}
				else
				{
					itemIcon.enabled = false;
				}
			}

			// 設置名稱
			if (itemNameText != null)
			{
				string itemName = item.GetString("LocalizedName", item.ToString());
				itemNameText.text = itemName;
			}

			// 設置數量
			if (itemCountText != null)
			{
				if (item.Stackable && item.StackCount > 1)
				{
					itemCountText.text = item.StackCount.ToString();
					itemCountText.enabled = true;
				}
				else
				{
					itemCountText.enabled = false;
				}
			}

			// 設置快捷鍵編號
			if (shortcutNumberText != null)
			{
				shortcutNumberText.text = (shortcutIndex + 1).ToString();
			}
		}

		/// <summary>
		/// 獲取道具圖標
		/// </summary>
		private Sprite GetItemIcon(Item item)
		{
			// 嘗試從道具獲取圖標
			if (item.Icon != null)
			{
				return item.Icon;
			}

			// 如果道具有代理，嘗試從代理獲取
			if (item.HasHandHeldAgent)
			{
				// 可以嘗試從 ItemAgent 獲取圖標
				// 這部分需要根據實際情況調整
			}

			return null;
		}

		private void OnDestroy()
		{
			// 清理資源
			item = null;
		}
	}
}
