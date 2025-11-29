using System;
using System.Collections.Generic;
using UnityEngine;

namespace EasyPanel
{
	/// <summary>
	/// 環形選單持久化資料
	/// 使用 ES3 SavesSystem 保存到遊戲存檔中
	/// </summary>
	[Serializable]
	public class RadialMenuData
	{
		/// <summary>
		/// 單個槽位的配置資料
		/// </summary>
		[Serializable]
		public class SlotData
		{
			[SerializeField]
			public int slotIndex;

			[SerializeField]
			public int typeID = -1;

			[SerializeField]
			public string slotKey = "";

			[SerializeField]
			public int backpackIndex = -1;

			public SlotData()
			{
			}

			public SlotData(int index, ItemReference reference)
			{
				slotIndex = index;
				if (reference != null && !reference.IsEmpty)
				{
					typeID = reference.typeID;
					slotKey = reference.slotKey;
					backpackIndex = reference.backpackIndex;
				}
			}

			public ItemReference ToItemReference()
			{
				return new ItemReference
				{
					typeID = typeID,
					slotKey = slotKey,
					backpackIndex = backpackIndex
				};
			}

			public bool IsEmpty()
			{
				return typeID <= 0;
			}
		}

		[SerializeField]
		public List<SlotData> slots = new List<SlotData>();

		[SerializeField]
		public int slotCount = 8;

		/// <summary>
		/// 從槽位引用列表創建保存資料
		/// </summary>
		public static RadialMenuData FromSlotReferences(List<ItemReference> references)
		{
			RadialMenuData data = new RadialMenuData
			{
				slotCount = references.Count
			};

			for (int i = 0; i < references.Count; i++)
			{
				data.slots.Add(new SlotData(i, references[i]));
			}

			return data;
		}

		/// <summary>
		/// 轉換為槽位引用列表
		/// </summary>
		public List<ItemReference> ToSlotReferences()
		{
			List<ItemReference> references = new List<ItemReference>();

			// 確保列表大小正確
			for (int i = 0; i < slotCount; i++)
			{
				if (i < slots.Count)
				{
					references.Add(slots[i].ToItemReference());
				}
				else
				{
					references.Add(new ItemReference());
				}
			}

			return references;
		}

		/// <summary>
		/// 驗證資料完整性
		/// </summary>
		public bool Validate()
		{
			if (slotCount <= 0 || slotCount > 32)
			{
				Debug.LogWarning($"[RadialMenuData] 槽位數量異常: {slotCount}");
				return false;
			}

			if (slots == null)
			{
				Debug.LogWarning("[RadialMenuData] 槽位資料為 null");
				return false;
			}

			return true;
		}

		/// <summary>
		/// 獲取非空槽位數量
		/// </summary>
		public int GetNonEmptySlotCount()
		{
			int count = 0;
			foreach (var slot in slots)
			{
				if (!slot.IsEmpty())
				{
					count++;
				}
			}
			return count;
		}

		public override string ToString()
		{
			int nonEmpty = GetNonEmptySlotCount();
			return $"RadialMenuData(SlotCount={slotCount}, NonEmpty={nonEmpty}/{slots.Count})";
		}
	}
}
