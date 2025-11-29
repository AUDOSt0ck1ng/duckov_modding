using System;
using UnityEngine;
using ItemStatsSystem;
using Duckov;

namespace EasyPanel
{
	/// <summary>
	/// 物品引用 - 用於精確定位物品
	/// 保存三個資訊:
	/// 1. TypeID: 物品類型ID (用於驗證和 fallback)
	/// 2. SlotKey: 如果物品在裝備槽 (如 "PrimaryWeapon", "SecondaryWeapon", "Armor" 等)
	/// 3. BackpackIndex: 如果物品在背包的索引
	/// </summary>
	[Serializable]
	public class ItemReference
	{
		[SerializeField]
		public int typeID = -1;

		[SerializeField]
		public string slotKey = "";

		[SerializeField]
		public int backpackIndex = -1;

		/// <summary>
		/// 是否為空引用
		/// </summary>
		public bool IsEmpty => typeID <= 0;

		/// <summary>
		/// 是否為裝備槽引用
		/// </summary>
		public bool IsEquipmentSlot => !string.IsNullOrEmpty(slotKey);

		/// <summary>
		/// 是否為背包引用
		/// </summary>
		public bool IsBackpackItem => backpackIndex >= 0;

		/// <summary>
		/// 從物品創建引用
		/// </summary>
		public static ItemReference FromItem(Item item)
		{
			if (item == null)
			{
				return new ItemReference();
			}

			ItemReference reference = new ItemReference
			{
				typeID = item.TypeID
			};

			// 檢查物品是否在裝備槽中
			if (item.PluggedIntoSlot != null && !string.IsNullOrEmpty(item.PluggedIntoSlot.Key))
			{
				reference.slotKey = item.PluggedIntoSlot.Key;
				Debug.Log($"[ItemReference] 創建裝備槽引用: TypeID={item.TypeID}, Slot={reference.slotKey}");
			}
			// 檢查物品是否在背包中
			else if (item.InInventory != null)
			{
				reference.backpackIndex = item.InInventory.GetIndex(item);
				Debug.Log($"[ItemReference] 創建背包引用: TypeID={item.TypeID}, Index={reference.backpackIndex}");
			}
			else
			{
				Debug.LogWarning($"[ItemReference] 物品 {item.DisplayName} (TypeID={item.TypeID}) 既不在裝備槽也不在背包中");
			}

			return reference;
		}

		/// <summary>
		/// 獲取引用的物品 - 使用智能查找邏輯
		/// </summary>
		public Item GetItem()
		{
			if (IsEmpty)
			{
				return null;
			}

			// 獲取主角物品和背包
			var master = CharacterMainControl.Main;
			if (master == null || master.CharacterItem == null)
			{
				Debug.LogWarning("[ItemReference] 無法獲取主角");
				return null;
			}

			Item characterItem = master.CharacterItem;
			Inventory mainInventory = characterItem.Inventory;

			// 優先級 1: 如果有槽位 Key，精確從裝備槽查找
			if (IsEquipmentSlot && characterItem.Slots != null)
			{
				var slot = characterItem.Slots.GetSlot(slotKey);
				if (slot != null && slot.Content != null)
				{
					// 驗證 TypeID 是否匹配
					if (slot.Content.TypeID == typeID)
					{
						Debug.Log($"[ItemReference] ✓ 從裝備槽 {slotKey} 找到物品: {slot.Content.DisplayName}");
						return slot.Content;
					}
					else
					{
						Debug.LogWarning($"[ItemReference] 裝備槽 {slotKey} 的物品 TypeID 不匹配 (期望={typeID}, 實際={slot.Content.TypeID})");
					}
				}
				else
				{
					Debug.Log($"[ItemReference] 裝備槽 {slotKey} 為空或不存在");
				}
			}

			// 優先級 2: 如果有背包索引,精確從背包查找
			if (IsBackpackItem && mainInventory != null)
			{
				if (backpackIndex >= 0 && backpackIndex < mainInventory.Capacity)
				{
					Item item = mainInventory.GetItemAt(backpackIndex);
					if (item != null)
					{
						// 驗證 TypeID 是否匹配
						if (item.TypeID == typeID)
						{
							Debug.Log($"[ItemReference] ✓ 從背包索引 {backpackIndex} 找到物品: {item.DisplayName}");
							return item;
						}
						else
						{
							Debug.LogWarning($"[ItemReference] 背包索引 {backpackIndex} 的物品 TypeID 不匹配 (期望={typeID}, 實際={item.TypeID})");
						}
					}
					else
					{
						Debug.Log($"[ItemReference] 背包索引 {backpackIndex} 為空");
					}
				}
			}

			// 優先級 3: Fallback - 用 TypeID 在背包中查找第一個匹配的物品
			if (mainInventory != null)
			{
				for (int i = 0; i < mainInventory.Content.Count; i++)
				{
					Item item = mainInventory.Content[i];
					if (item != null && item.TypeID == typeID)
					{
						Debug.Log($"[ItemReference] ⚠ Fallback: 用 TypeID 在背包索引 {i} 找到物品: {item.DisplayName}");
						return item;
					}
				}
			}

			Debug.Log($"[ItemReference] ✗ 無法找到物品 (TypeID={typeID}, Slot={slotKey}, Index={backpackIndex})");
			return null;
		}

		/// <summary>
		/// 清空引用
		/// </summary>
		public void Clear()
		{
			typeID = -1;
			slotKey = "";
			backpackIndex = -1;
		}

		/// <summary>
		/// 複製引用
		/// </summary>
		public ItemReference Clone()
		{
			return new ItemReference
			{
				typeID = this.typeID,
				slotKey = this.slotKey,
				backpackIndex = this.backpackIndex
			};
		}

		public override string ToString()
		{
			if (IsEmpty)
				return "ItemReference(Empty)";
			if (IsEquipmentSlot)
				return $"ItemReference(TypeID={typeID}, Slot={slotKey})";
			if (IsBackpackItem)
				return $"ItemReference(TypeID={typeID}, BackpackIndex={backpackIndex})";
			return $"ItemReference(TypeID={typeID}, NoLocation)";
		}
	}
}
