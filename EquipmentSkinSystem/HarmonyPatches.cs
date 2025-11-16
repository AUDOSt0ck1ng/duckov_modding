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
                    Debug.Log($"[EquipmentSkinSystem] ChangeEquipmentModel Prefix triggered!");
                    
                    // 只處理玩家角色
                    if (!IsPlayerCharacter(__instance))
                    {
                        Debug.Log($"[EquipmentSkinSystem] Not player character, skip");
                        return true;
                    }

                    // 無效槽位，交由遊戲處理
                    if (slot == null || socket == null)
                    {
                        Debug.Log($"[EquipmentSkinSystem] Invalid slot or socket, skip");
                        return true;
                    }

                    Debug.Log($"[EquipmentSkinSystem] Processing slot: {slot.Key}, Content: {(slot.Content != null ? slot.Content.TypeID.ToString() : "NULL")}");

                    // 槽位是空的（脫下裝備）
                    if (slot.Content == null)
                    {
                        ClearSocket(socket);
                        RefreshFacialFeaturesIfNeeded(slot, __instance);
                        return false; // 不渲染任何東西
                    }

                    // 取得該槽位的外觀配置
                    var slotType = GetSlotTypeFromKey(slot.Key);
                    if (!slotType.HasValue)
                        return true; // 不是我們管理的槽位，執行原方法

                    var config = GetSlotConfig(slotType.Value);
                    Debug.Log($"[EquipmentSkinSystem] Slot {slotType.Value} config: UseSkin={config.UseSkin}, SkinID={config.SkinItemTypeID}");
                    
                    // 未啟用外觀覆蓋：讓遊戲自己處理
                    if (!config.UseSkin)
                    {
                        Debug.Log($"[EquipmentSkinSystem] Not using skin for {slotType.Value}, let game handle it");
                        return true; // 執行遊戲原方法
                    }

                    // 確定要攔截了，先清除舊模型
                    ClearSocket(socket);

                    // 已啟用外觀覆蓋
                    if (config.SkinItemTypeID == -1)
                    {
                        // ID = -1：隱藏外觀（不渲染）
                        Debug.Log($"[EquipmentSkinSystem] Hiding equipment for {slotType.Value}");
                        RefreshFacialFeaturesIfNeeded(slot, __instance);
                        return false;
                    }
                    else if (config.SkinItemTypeID > 0)
                    {
                        // ID > 0：替換外觀
                        Debug.Log($"[EquipmentSkinSystem] Rendering skin {config.SkinItemTypeID} for {slotType.Value}");
                        RenderSkinEquipment(config.SkinItemTypeID, socket);
                        RefreshFacialFeaturesIfNeeded(slot, __instance);
                        return false;
                    }
                    else
                    {
                        // ID = 0 或其他：使用原始裝備
                        Debug.Log($"[EquipmentSkinSystem] Rendering original equipment (ID=0) for {slotType.Value}");
                        RenderOriginalEquipment(slot, socket);
                        RefreshFacialFeaturesIfNeeded(slot, __instance);
                        return false;
                    }
                }
                catch (Exception e)
                {
                    Debug.LogError($"[EquipmentSkinSystem] Error in ChangeEquipmentModel patch: {e.Message}");
                    Debug.LogError($"[EquipmentSkinSystem] Stack trace: {e.StackTrace}");
                }
                
                // 出錯時執行原方法
                return true;
            }

            /// <summary>
            /// 檢查是否為玩家角色
            /// </summary>
            private static bool IsPlayerCharacter(CharacterEquipmentController controller)
            {
                try
                {
                    // 取得 CharacterMainControl
                    var cm = Traverse.Create(controller)
                                     .Field("characterMainControl")
                                     .GetValue<CharacterMainControl>();
                    
                    if (cm == null)
                        return false;

                    // 檢查是否為玩家角色（透過 LevelManager 比對）
                    var mainCharacter = LevelManager.Instance?.MainCharacter;
                    return mainCharacter != null && cm == mainCharacter;
                }
                catch (Exception ex)
                {
                    Debug.LogError($"[EquipmentSkinSystem] Error checking player character: {ex.Message}");
                    return false; // 出錯時不攔截，讓遊戲正常運作
                }
            }

            /// <summary>
            /// 清除 socket 上的所有子物件
            /// </summary>
            private static void ClearSocket(Transform socket)
            {
                for (int i = socket.childCount - 1; i >= 0; i--)
                {
                    GameObject.Destroy(socket.GetChild(i).gameObject);
                }
            }

            /// <summary>
            /// 取得槽位配置
            /// </summary>
            private static SlotSkinConfig GetSlotConfig(EquipmentSlotType slotType)
            {
                var profile = EquipmentSkinDataManager.Instance.CurrentProfile;
                if (profile.SlotConfigs.TryGetValue(slotType, out var config))
                    return config;
                
                // 返回預設配置（未啟用）
                return new SlotSkinConfig(slotType);
            }

            /// <summary>
            /// 渲染原始裝備
            /// </summary>
            private static void RenderOriginalEquipment(Slot slot, Transform socket)
            {
                ItemAgent agent = slot.Content.AgentUtilities.CreateAgent(
                    CharacterEquipmentController.equipmentModelHash,
                    ItemAgent.AgentTypes.equipment
                );

                if (agent != null)
                {
                    agent.transform.SetParent(socket, worldPositionStays: false);
                    agent.transform.localRotation = Quaternion.identity;
                    agent.transform.localPosition = Vector3.zero;
                }
            }

            /// <summary>
            /// 渲染替換外觀
            /// </summary>
            private static void RenderSkinEquipment(int skinItemID, Transform socket)
            {
                Item skinItem = ItemAssetsCollection.InstantiateSync(skinItemID);
                if (skinItem == null)
                {
                    Debug.LogWarning($"[EquipmentSkinSystem] Failed to instantiate skin item {skinItemID}");
                    return;
                }

                ItemAgent skinAgent = skinItem.AgentUtilities.CreateAgent(
                    CharacterEquipmentController.equipmentModelHash,
                    ItemAgent.AgentTypes.equipment
                );

                if (skinAgent != null)
                {
                    skinAgent.transform.SetParent(socket, worldPositionStays: false);
                    skinAgent.transform.localRotation = Quaternion.identity;
                    skinAgent.transform.localPosition = Vector3.zero;
                }
                else
                {
                    GameObject.Destroy(skinItem.gameObject);
                }
            }

            /// <summary>
            /// 如果是頭盔或面罩，刷新嘴巴與耳機顯示
            /// </summary>
            private static void RefreshFacialFeaturesIfNeeded(Slot slot, CharacterEquipmentController controller)
            {
                try
                {
                    var slotType = GetSlotTypeFromKey(slot.Key);
                    if (slotType.HasValue && IsHeadSlot(slotType.Value))
                    {
                        ForceRefreshMouthVisibility(controller, slotType.Value);
                    }
                }
                catch (Exception ex)
                {
                    Debug.LogError($"[EquipmentSkinSystem] Error refreshing facial features: {ex.Message}");
                }
            }

            /// <summary>
            /// 判斷是否為頭部槽位（需要刷新面部特徵）
            /// </summary>
            private static bool IsHeadSlot(EquipmentSlotType slotType)
            {
                return slotType == EquipmentSlotType.Helmet || slotType == EquipmentSlotType.FaceMask;
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

        /// <summary>
        /// 強制重新渲染所有裝備槽位（用於 UI 切換外觀後）
        /// </summary>
        public static void ForceRefreshAllEquipment()
        {
            try
            {
                var mainCharacter = LevelManager.Instance?.MainCharacter;
                if (mainCharacter == null)
                {
                    Debug.LogWarning("[EquipmentSkinSystem] Cannot refresh: MainCharacter not found");
                    return;
                }

                var controller = mainCharacter.GetComponent<CharacterEquipmentController>();
                if (controller == null)
                {
                    Debug.LogWarning("[EquipmentSkinSystem] Cannot refresh: CharacterEquipmentController not found");
                    return;
                }

                // 取得所有裝備槽位
                var armorSlot = Traverse.Create(controller).Field("armorSlot").GetValue<Slot>();
                var helmatSlot = Traverse.Create(controller).Field("helmatSlot").GetValue<Slot>();
                var faceMaskSlot = Traverse.Create(controller).Field("faceMaskSlot").GetValue<Slot>();
                var backpackSlot = Traverse.Create(controller).Field("backpackSlot").GetValue<Slot>();
                var headsetSlot = Traverse.Create(controller).Field("headsetSlot").GetValue<Slot>();

                // 強制觸發每個槽位的渲染方法
                if (armorSlot != null)
                    Traverse.Create(controller).Method("ChangeArmorModel", armorSlot).GetValue();
                
                if (helmatSlot != null)
                    Traverse.Create(controller).Method("ChangeHelmatModel", helmatSlot).GetValue();
                
                if (faceMaskSlot != null)
                    Traverse.Create(controller).Method("ChangeFaceMaskModel", faceMaskSlot).GetValue();
                
                if (backpackSlot != null)
                    Traverse.Create(controller).Method("ChangeBackpackModel", backpackSlot).GetValue();
                
                if (headsetSlot != null)
                    Traverse.Create(controller).Method("ChangeHeadsetModel", headsetSlot).GetValue();

                Debug.Log("[EquipmentSkinSystem] ✅ All equipment refreshed");
            }
            catch (Exception e)
            {
                Debug.LogError($"[EquipmentSkinSystem] Error refreshing all equipment: {e.Message}");
                Debug.LogError($"[EquipmentSkinSystem] Stack trace: {e.StackTrace}");
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

