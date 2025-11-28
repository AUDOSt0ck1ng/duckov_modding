using System.Collections.Generic;
using System.Linq;
using Duckov.Utilities;
using HarmonyLib;
using ItemStatsSystem;

namespace EquipmentSkinSystem
{
    /// <summary>
    /// 物品資訊提供者實作
    /// 使用 ItemAssetsCollection 來取得全物品資訊
    /// </summary>
    public class ItemInfoProvider : IItemInfoProvider
    {
        private static ItemInfoProvider? _instance;

        /// <summary>
        /// 取得單例實例
        /// </summary>
        public static ItemInfoProvider Instance
        {
            get
            {
                if (_instance == null)
                {
                    _instance = new ItemInfoProvider();
                }
                return _instance;
            }
        }

        /// <summary>
        /// 取得所有物品的 TypeID 列表（包含動態添加的物品）
        /// </summary>
        public int[] GetAllItemTypeIDs()
        {
            try
            {
                var collection = ItemAssetsCollection.Instance;
                if (collection == null)
                {
                    Logger.Warning("ItemAssetsCollection.Instance is null");
                    return new int[0];
                }

                // 取得所有基本物品的 TypeID
                var baseTypeIDs = new List<int>();
                if (collection.entries != null)
                {
                    foreach (var entry in collection.entries)
                    {
                        if (entry != null && entry.typeID > 0)
                        {
                            baseTypeIDs.Add(entry.typeID);
                        }
                    }
                }

                // 取得所有動態物品的 TypeID
                // 由於無法直接存取 dynamicDic，我們使用 GetAllTypeIds 來取得所有物品
                // 然後找出不在 entries 中的 TypeID（這些就是動態物品）
                var itemFilter = new ItemFilter
                {
                    minQuality = 0,
                    maxQuality = 999  // 設定一個足夠大的值來包含所有品質的物品
                };
                var allTypeIDsWithFilter = ItemAssetsCollection.GetAllTypeIds(itemFilter);

                var dynamicTypeIDs = new List<int>();
                if (allTypeIDsWithFilter != null)
                {
                    var baseTypeIDSet = new System.Collections.Generic.HashSet<int>(baseTypeIDs);
                    foreach (var typeID in allTypeIDsWithFilter)
                    {
                        if (!baseTypeIDSet.Contains(typeID))
                        {
                            dynamicTypeIDs.Add(typeID);
                        }
                    }
                }

                // 合併並去重
                var allTypeIDs = baseTypeIDs.Union(dynamicTypeIDs).ToArray();

                Logger.Debug($"GetAllItemTypeIDs: Found {baseTypeIDs.Count} base items and {dynamicTypeIDs.Count} dynamic items, total {allTypeIDs.Length} items");

                return allTypeIDs;
            }
            catch (System.Exception e)
            {
                Logger.Error("Error in GetAllItemTypeIDs", e);
                return new int[0];
            }
        }

        /// <summary>
        /// 取得所有物品的資訊（包含 TypeID 和 MetaData）
        /// </summary>
        public List<ItemInfo> GetAllItemInfos()
        {
            var itemInfos = new List<ItemInfo>();

            try
            {
                var collection = ItemAssetsCollection.Instance;
                if (collection == null)
                {
                    Logger.Warning("ItemAssetsCollection.Instance is null");
                    return itemInfos;
                }

                // 取得所有基本物品
                if (collection.entries != null)
                {
                    foreach (var entry in collection.entries)
                    {
                        if (entry != null && entry.typeID > 0)
                        {
                            var metaData = entry.metaData;
                            itemInfos.Add(new ItemInfo(entry.typeID, metaData, false));
                        }
                    }
                }

                // 取得所有動態物品
                // 使用 GetAllTypeIds 來找出所有 TypeID，然後檢查哪些不在 entries 中
                var itemFilter = new ItemFilter
                {
                    minQuality = 0,
                    maxQuality = 999  // 設定一個足夠大的值來包含所有品質的物品
                };
                var allTypeIDsWithFilter = ItemAssetsCollection.GetAllTypeIds(itemFilter);

                if (allTypeIDsWithFilter != null)
                {
                    var baseTypeIDSet = new System.Collections.Generic.HashSet<int>();
                    foreach (var entry in collection.entries ?? new List<ItemAssetsCollection.Entry>())
                    {
                        if (entry != null && entry.typeID > 0)
                        {
                            baseTypeIDSet.Add(entry.typeID);
                        }
                    }

                    foreach (var typeID in allTypeIDsWithFilter)
                    {
                        if (!baseTypeIDSet.Contains(typeID))
                        {
                            // 這是動態物品
                            var metaData = ItemAssetsCollection.GetMetaData(typeID);
                            itemInfos.Add(new ItemInfo(typeID, metaData, true));
                        }
                    }
                }

                Logger.Debug($"GetAllItemInfos: Found {itemInfos.Count} items");
            }
            catch (System.Exception e)
            {
                Logger.Error("Error in GetAllItemInfos", e);
            }

            return itemInfos;
        }

        /// <summary>
        /// 根據 TypeID 取得物品的 MetaData
        /// </summary>
        public ItemMetaData GetItemMetaData(int typeID)
        {
            try
            {
                return ItemAssetsCollection.GetMetaData(typeID);
            }
            catch (System.Exception e)
            {
                Logger.Error($"Error in GetItemMetaData for typeID {typeID}", e);
                return default(ItemMetaData);
            }
        }

        /// <summary>
        /// 檢查指定的 TypeID 是否存在（包含動態添加的物品）
        /// </summary>
        public bool ItemExists(int typeID)
        {
            try
            {
                // 先檢查基本物品
                var collection = ItemAssetsCollection.Instance;
                if (collection != null && collection.entries != null)
                {
                    if (collection.entries.Any(e => e != null && e.typeID == typeID))
                    {
                        return true;
                    }
                }

                // 再檢查動態物品（使用 TryGetDynamicEntry 方法）
                if (ItemAssetsCollection.TryGetDynamicEntry(typeID, out var dynamicEntry))
                {
                    if (dynamicEntry != null && dynamicEntry.prefab != null)
                    {
                        return true;
                    }
                }

                return false;
            }
            catch (System.Exception e)
            {
                Logger.Error($"Error in ItemExists for typeID {typeID}", e);
                return false;
            }
        }

        /// <summary>
        /// 根據標籤名稱查詢物品（使用 ItemFilter）
        /// </summary>
        public List<ItemInfo> GetItemsByTag(string tagName)
        {
            return GetItemsByTags(new[] { tagName }, null);
        }

        /// <summary>
        /// 根據類別名稱查詢物品（使用第一個標籤作為類別）
        /// </summary>
        public List<ItemInfo> GetItemsByCategory(string categoryName)
        {
            var itemInfos = new List<ItemInfo>();

            try
            {
                var allItems = GetAllItemInfos();
                foreach (var item in allItems)
                {
                    // ItemMetaData.Catagory 返回 tags[0].name
                    if (item.MetaData.Catagory == categoryName)
                    {
                        itemInfos.Add(item);
                    }
                }

                Logger.Debug($"GetItemsByCategory '{categoryName}': Found {itemInfos.Count} items");
            }
            catch (System.Exception e)
            {
                Logger.Error($"Error in GetItemsByCategory for category '{categoryName}'", e);
            }

            return itemInfos;
        }

        /// <summary>
        /// 根據多個標籤查詢物品（使用 ItemFilter）
        /// </summary>
        public List<ItemInfo> GetItemsByTags(string[] requireTags, string[]? excludeTags = null)
        {
            var itemInfos = new List<ItemInfo>();

            try
            {
                // 使用 ItemFilter 來查詢物品
                var itemFilter = new ItemFilter
                {
                    minQuality = 0,
                    maxQuality = 999
                };

                // 轉換標籤名稱到 Tag 物件
                if (requireTags != null && requireTags.Length > 0)
                {
                    var tagList = new List<Tag>();
                    foreach (var tagName in requireTags)
                    {
                        var tag = TagUtilities.TagFromString(tagName);
                        if (tag != null)
                        {
                            tagList.Add(tag);
                        }
                        else
                        {
                            Logger.Warning($"Tag '{tagName}' not found");
                        }
                    }
                    if (tagList.Count > 0)
                    {
                        itemFilter.requireTags = tagList.ToArray();
                    }
                }

                if (excludeTags != null && excludeTags.Length > 0)
                {
                    var excludeTagList = new List<Tag>();
                    foreach (var tagName in excludeTags)
                    {
                        var tag = TagUtilities.TagFromString(tagName);
                        if (tag != null)
                        {
                            excludeTagList.Add(tag);
                        }
                        else
                        {
                            Logger.Warning($"Exclude tag '{tagName}' not found");
                        }
                    }
                    if (excludeTagList.Count > 0)
                    {
                        itemFilter.excludeTags = excludeTagList.ToArray();
                    }
                }

                // 使用 GetAllTypeIds 來取得符合條件的 TypeID
                var typeIDs = ItemAssetsCollection.GetAllTypeIds(itemFilter);

                if (typeIDs != null)
                {
                    foreach (var typeID in typeIDs)
                    {
                        var metaData = ItemAssetsCollection.GetMetaData(typeID);
                        // 判斷是否為動態物品（簡單判斷：如果不在 entries 中就是動態物品）
                        var collection = ItemAssetsCollection.Instance;
                        bool isDynamic = false;
                        if (collection != null && collection.entries != null)
                        {
                            isDynamic = !collection.entries.Any(e => e != null && e.typeID == typeID);
                        }
                        itemInfos.Add(new ItemInfo(typeID, metaData, isDynamic));
                    }
                }

                Logger.Debug($"GetItemsByTags: Found {itemInfos.Count} items with tags [{string.Join(", ", requireTags ?? new string[0])}]");
            }
            catch (System.Exception e)
            {
                Logger.Error($"Error in GetItemsByTags", e);
            }

            return itemInfos;
        }
    }
}
