using System.Collections.Generic;
using ItemStatsSystem;

namespace EquipmentSkinSystem
{
    /// <summary>
    /// 物品資訊提供者介面
    /// 提供取得全物品資訊的方法
    /// </summary>
    public interface IItemInfoProvider
    {
        /// <summary>
        /// 取得所有物品的 TypeID 列表（包含動態添加的物品）
        /// </summary>
        /// <returns>所有物品的 TypeID 陣列</returns>
        int[] GetAllItemTypeIDs();

        /// <summary>
        /// 取得所有物品的資訊（包含 TypeID 和 MetaData）
        /// </summary>
        /// <returns>物品資訊列表，每個項目包含 TypeID 和 MetaData</returns>
        List<ItemInfo> GetAllItemInfos();

        /// <summary>
        /// 根據 TypeID 取得物品的 MetaData
        /// </summary>
        /// <param name="typeID">物品的 TypeID</param>
        /// <returns>物品的 MetaData，如果找不到則返回預設值</returns>
        ItemMetaData GetItemMetaData(int typeID);

        /// <summary>
        /// 檢查指定的 TypeID 是否存在（包含動態添加的物品）
        /// </summary>
        /// <param name="typeID">要檢查的 TypeID</param>
        /// <returns>如果存在則返回 true，否則返回 false</returns>
        bool ItemExists(int typeID);

        /// <summary>
        /// 根據標籤名稱查詢物品（使用 ItemFilter）
        /// </summary>
        /// <param name="tagName">標籤名稱（例如 "Armor", "Helmat", "FaceMask", "Backpack", "Headset", "Weapon" 等）</param>
        /// <returns>符合標籤的物品資訊列表</returns>
        List<ItemInfo> GetItemsByTag(string tagName);

        /// <summary>
        /// 根據類別名稱查詢物品（使用第一個標籤作為類別）
        /// </summary>
        /// <param name="categoryName">類別名稱（對應 ItemMetaData.Catagory）</param>
        /// <returns>符合類別的物品資訊列表</returns>
        List<ItemInfo> GetItemsByCategory(string categoryName);

        /// <summary>
        /// 根據多個標籤查詢物品（使用 ItemFilter）
        /// </summary>
        /// <param name="requireTags">必須包含的標籤名稱列表</param>
        /// <param name="excludeTags">必須排除的標籤名稱列表（可選）</param>
        /// <returns>符合條件的物品資訊列表</returns>
        List<ItemInfo> GetItemsByTags(string[] requireTags, string[]? excludeTags = null);
    }

    /// <summary>
    /// 物品資訊結構
    /// </summary>
    public struct ItemInfo
    {
        /// <summary>
        /// 物品的 TypeID
        /// </summary>
        public int TypeID;

        /// <summary>
        /// 物品的 MetaData
        /// </summary>
        public ItemMetaData MetaData;

        /// <summary>
        /// 是否為動態添加的物品（Mod 物品）
        /// </summary>
        public bool IsDynamic;

        public ItemInfo(int typeID, ItemMetaData metaData, bool isDynamic = false)
        {
            TypeID = typeID;
            MetaData = metaData;
            IsDynamic = isDynamic;
        }
    }
}
