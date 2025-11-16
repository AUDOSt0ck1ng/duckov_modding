using System;
using HarmonyLib;
using UnityEngine;
using ItemStatsSystem;
using ItemStatsSystem.Items;

namespace EquipmentSkinSystem
{
    /// <summary>
    /// Harmony 補丁集合
    /// 用於攔截和修改裝備渲染邏輯
    /// </summary>
    public static class HarmonyPatches
    {
        /// <summary>
        /// 攔截裝備外觀更新的核心方法
        /// 這是所有裝備外觀變更的最終調用點
        /// </summary>
        [HarmonyPatch(typeof(CharacterEquipmentController), "ChangeEquipmentModel")]
        public static class ChangeEquipmentModelPatch
        {
            [HarmonyPrefix]
            public static bool Prefix(Slot slot, Transform socket, CharacterEquipmentController __instance)
            {
                try
                {
                    // 檢查槽位是否有效
                    if (slot == null || socket == null)
                    {
                        return true; // 執行原方法
                    }

                    // 如果槽位是空的（脫下裝備），讓遊戲自己處理
                    if (slot.Content == null)
                    {
                        Debug.Log($"[EquipmentSkinSystem] Slot is empty, let game handle it");
                        return true; // 執行原方法，讓遊戲清除裝備
                    }

                    Item actualItem = slot.Content;

                    // 檢查這個槽位是否有外觀覆蓋
                    int skinItemID = TryGetSkinForSlot(slot);

                    // 如果有外觀覆蓋（-1 或 >0），先清除舊裝備模型（只刪除 ItemAgent）
                    if (skinItemID == -1 || skinItemID > 0)
                    {
                        for (int i = socket.childCount - 1; i >= 0; i--)
                        {
                            Transform child = socket.GetChild(i);
                            if (child.GetComponent<ItemAgent>() != null)
                            {
                                GameObject.Destroy(child.gameObject);
                            }
                        }
                    }

                    // 特殊處理：如果 skinItemID 是 -1，代表要隱藏外觀
                    if (skinItemID == -1)
                    {
                        Debug.Log($"[EquipmentSkinSystem] Hiding visual for item {actualItem.TypeID}");

                        // 嘗試重新啟用嘴巴 / 頭部相關部件
                        try
                        {
                            var slotType = GetSlotTypeFromKey(slot.Key);
                            if (slotType.HasValue &&
                                (slotType.Value == EquipmentSlotType.Helmet ||
                                 slotType.Value == EquipmentSlotType.FaceMask))
                            {
                                ForceRefreshMouthVisibility(__instance, slotType.Value);
                            }
                        }
                        catch (Exception mouthEx)
                        {
                            Debug.LogError($"[EquipmentSkinSystem] Error while refreshing mouth visibility: {mouthEx.Message}");
                        }

                        // 不創建新的裝備模型，直接結束
                        return false;
                    }

                    if (skinItemID > 0)
                    {
                        Debug.Log($"[EquipmentSkinSystem] Applying skin: Item {actualItem.TypeID} -> Skin {skinItemID}");
                        
                        // 創建外觀物品
                        Item skinItem = ItemAssetsCollection.InstantiateSync(skinItemID);
                        
                        if (skinItem != null)
                        {
                            // 使用外觀物品創建裝備 Agent（視覺模型）
                            ItemAgent skinAgent = skinItem.AgentUtilities.CreateAgent(
                                CharacterEquipmentController.equipmentModelHash,
                                ItemAgent.AgentTypes.equipment
                            );
                            
                            if (skinAgent != null)
                            {
                                // 將外觀模型附加到角色的 socket 上
                                skinAgent.transform.SetParent(socket, worldPositionStays: false);
                                skinAgent.transform.localRotation = Quaternion.identity;
                                skinAgent.transform.localPosition = Vector3.zero;
                                
                                Debug.Log($"[EquipmentSkinSystem] Skin applied successfully!");
                                
                                // 跳過原方法，使用我們的外觀
                                return false;
                            }
                            else
                            {
                                Debug.LogWarning($"[EquipmentSkinSystem] Failed to create agent for skin item {skinItemID}");
                                // 清理創建的物品
                                GameObject.Destroy(skinItem.gameObject);
                            }
                        }
                        else
                        {
                            Debug.LogWarning($"[EquipmentSkinSystem] Failed to instantiate skin item {skinItemID}");
                        }
                    }
                }
                catch (Exception e)
                {
                    Debug.LogError($"[EquipmentSkinSystem] Error in ChangeEquipmentModel patch: {e.Message}");
                    Debug.LogError($"[EquipmentSkinSystem] Stack trace: {e.StackTrace}");
                }
                
                // 沒有外觀覆蓋或出錯，執行原方法
                return true;
            }
        }

        /// <summary>
        /// 攔截裝備外觀更新方法，記錄物品 ID
        /// 因為所有槽位都會調用 ChangeEquipmentModel，所以在這裡記錄就夠了
        /// </summary>
        [HarmonyPatch(typeof(CharacterEquipmentController), "ChangeEquipmentModel")]
        public static class EquipmentChangeLogger
        {
            [HarmonyPrefix]
            public static void LogPrefix(Slot slot, Transform socket)
            {
                try
                {
                    Debug.Log($"[EquipmentSkinSystem] ChangeEquipmentModel called! Slot: {(slot != null ? "Valid" : "NULL")}, Content: {(slot?.Content != null ? "Valid" : "NULL")}");
                    
                    if (slot != null && slot.Content != null)
                    {
                        Debug.Log($"[EquipmentSkinSystem] 📦 裝備變更 - 物品 ID: {slot.Content.TypeID}, 名稱: {slot.Content.name}");
                    }
                    else if (slot != null && slot.Content == null)
                    {
                        Debug.Log($"[EquipmentSkinSystem] 📦 裝備移除 - 槽位已清空");
                    }
                }
                catch (Exception e)
                {
                    Debug.LogError($"[EquipmentSkinSystem] Error logging equipment change: {e.Message}");
                    Debug.LogError($"[EquipmentSkinSystem] Stack: {e.StackTrace}");
                }
            }
        }

        /// <summary>
        /// 根據槽位獲取外觀覆蓋 ID（簡化版）
        /// </summary>
        private static int TryGetSkinForSlot(Slot slot)
        {
            try
            {
                if (slot == null) return 0;

                var profile = EquipmentSkinDataManager.Instance.CurrentProfile;
                
                // 根據槽位 Key 映射到 EquipmentSlotType
                EquipmentSlotType? slotType = GetSlotTypeFromKey(slot.Key);
                if (!slotType.HasValue)
                {
                    return 0; // 不是我們管理的槽位
                }
                
                // 查找對應的配置
                if (!profile.SlotConfigs.TryGetValue(slotType.Value, out var config))
                {
                    return 0;
                }
                
                // 只有在「啟用外觀」開關打開時才套用
                if (!config.UseSkin)
                {
                    return 0;
                }
                
                // 返回 Skin ID
                // -1 = 隱藏外觀
                // 正數 = 替換外觀
                // 0 = 不套用
                if (config.SkinItemTypeID == -1)
                {
                    Debug.Log($"[EquipmentSkinSystem] Hiding visual for slot {slotType.Value}");
                    return -1;
                }
                else if (config.SkinItemTypeID > 0)
                {
                    Debug.Log($"[EquipmentSkinSystem] Replacing slot {slotType.Value} with skin {config.SkinItemTypeID}");
                    return config.SkinItemTypeID;
                }
            }
            catch (Exception e)
            {
                Debug.LogError($"[EquipmentSkinSystem] Error in TryGetSkinForSlot: {e.Message}");
            }
            
            return 0; // 0 代表不套用任何外觀
        }

        /// <summary>
        /// 在隱藏頭盔 / 面罩時，強制刷新嘴巴與耳機顯示狀態
        /// - 利用 CharacterModel 的 OnHelmatSlotContentChange / OnFaceMaskSlotContentChange 來刷新嘴巴 / 頭髮
        /// - 如果是頭盔槽位，同時強制讓耳機重新渲染一次
        /// </summary>
        private static void ForceRefreshMouthVisibility(CharacterEquipmentController controller, EquipmentSlotType slotType)
        {
            try
            {
                if (controller == null) return;

                // 取得 CharacterMainControl（private 字段）
                var cm = Traverse.Create(controller)
                                 .Field("characterMainControl")
                                 .GetValue<CharacterMainControl>();
                if (cm == null)
                {
                    Debug.LogWarning("[EquipmentSkinSystem] ForceRefreshMouthVisibility: characterMainControl not found");
                    return;
                }

                var model = cm.characterModel;
                if (model == null)
                {
                    Debug.LogWarning("[EquipmentSkinSystem] ForceRefreshMouthVisibility: characterModel is null");
                    return;
                }

                // 建一個 Content 為 null 的臨時 Slot，讓遊戲邏輯認為「這個槽位沒有裝備」
                var tempSlot = new Slot();

                string methodName = slotType == EquipmentSlotType.Helmet
                    ? "OnHelmatSlotContentChange"
                    : "OnFaceMaskSlotContentChange";

                var traverseModel = Traverse.Create(model);
                var methodTraverse = traverseModel.Method(methodName, new object[] { tempSlot });
                if (methodTraverse != null)
                {
                    methodTraverse.GetValue();
                    Debug.Log($"[EquipmentSkinSystem] ForceRefreshMouthVisibility: invoked {methodName}");
                }
                else
                {
                    Debug.LogWarning($"[EquipmentSkinSystem] ForceRefreshMouthVisibility: method {methodName} not found");
                }

                // 如果是頭盔，同時強制刷新耳機（耳機模型也掛在 HelmatSocket 上）
                if (slotType == EquipmentSlotType.Helmet)
                {
                    var headsetSlot = Traverse.Create(controller)
                                              .Field("headsetSlot")
                                              .GetValue<Slot>();

                    if (headsetSlot != null && headsetSlot.Content != null)
                    {
                        var changeHeadsetTraverse = Traverse.Create(controller).Method("ChangeHeadsetModel", new object[] { headsetSlot });
                        if (changeHeadsetTraverse != null)
                        {
                            changeHeadsetTraverse.GetValue();
                            Debug.Log("[EquipmentSkinSystem] ForceRefreshMouthVisibility: refreshed headset model");
                        }
                    }
                }
            }
            catch (Exception e)
            {
                Debug.LogError($"[EquipmentSkinSystem] ForceRefreshMouthVisibility error: {e.Message}");
            }
        }

        /// <summary>
        /// 將遊戲的槽位 Key 映射到我們的 EquipmentSlotType
        /// </summary>
        private static EquipmentSlotType? GetSlotTypeFromKey(string key)
        {
            if (string.IsNullOrEmpty(key)) return null;
            
            switch (key)
            {
                case "Armor":
                    return EquipmentSlotType.Armor;
                case "Helmat": // 注意：遊戲裡拼錯了，是 Helmat 不是 Helmet
                    return EquipmentSlotType.Helmet;
                case "FaceMask":
                    return EquipmentSlotType.FaceMask;
                case "Backpack":
                    return EquipmentSlotType.Backpack;
                case "Headset":
                    return EquipmentSlotType.Headset;
                default:
                    return null;
            }
        }

    }

    /// <summary>
    /// 裝備槽位檢測輔助類
    /// 用於判斷物品屬於哪個裝備槽位
    /// </summary>
    public static class EquipmentSlotDetector
    {
        /// <summary>
        /// 根據物品 ID 或屬性判斷它屬於哪個槽位
        /// 注意：這需要根據實際遊戲的物品系統來實現
        /// </summary>
        public static EquipmentSlotType? DetectSlotType(Item item)
        {
            if (item == null) return null;

            try
            {
                // 這裡需要根據實際遊戲的物品分類系統來判斷
                // 可能的方法：
                // 1. 檢查物品的 TypeID 範圍
                // 2. 檢查物品的標籤或類別
                // 3. 檢查物品的屬性

                // 示例實現（需要根據實際情況調整）:
                /*
                if (item.HasTag("Head") || item.TypeID >= 1000 && item.TypeID < 2000)
                    return EquipmentSlotType.Head;
                else if (item.HasTag("Body") || item.TypeID >= 2000 && item.TypeID < 3000)
                    return EquipmentSlotType.Body;
                // ... 其他槽位的判斷
                */

                Debug.Log($"[EquipmentSkinSystem] Detecting slot for item {item.TypeID}");
            }
            catch (Exception e)
            {
                Debug.LogError($"[EquipmentSkinSystem] Error detecting slot type: {e.Message}");
            }

            return null;
        }

    }
}

