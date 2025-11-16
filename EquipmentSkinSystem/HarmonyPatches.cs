using System;
using System.Collections.Generic;
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
                    Logger.Debug("ChangeEquipmentModel Prefix triggered!");
                    
                    // 檢查是否為目標角色（玩家或狗）
                    CharacterType characterType;
                    if (!IsTargetCharacter(__instance, out characterType))
                    {
                        Logger.Debug("Not target character, skip");
                        return true;
                    }
                    
                    Logger.Debug($"Target character: {characterType}");

                    // 無效槽位，交由遊戲處理
                    if (slot == null || socket == null)
                    {
                        Logger.Debug("Invalid slot or socket, skip");
                        return true;
                    }

                    Logger.Debug($"Processing slot: {slot.Key}, Content: {(slot.Content != null ? slot.Content.TypeID.ToString() : "NULL")}");

                    // 取得該槽位的外觀配置
                    var slotType = GetSlotTypeFromKey(slot.Key);
                    if (!slotType.HasValue)
                        return true; // 不是我們管理的槽位，執行原方法

                    var config = GetSlotConfig(slotType.Value, characterType);
                    Logger.Debug($"[{characterType}] Slot {slotType.Value} config: UseSkin={config.UseSkin}, SkinID={config.SkinItemTypeID}");
                    
                    // 未啟用外觀覆蓋：讓遊戲自己處理
                    if (!config.UseSkin)
                    {
                        Logger.Debug($"[{characterType}] Not using skin for {slotType.Value}, let game handle it");
                        return true; // 執行遊戲原方法
                    }

                    // 槽位是空的（脫下裝備）：讓遊戲自己處理，避免清空後無法恢復
                    if (slot.Content == null)
                    {
                        Logger.Debug($"[{characterType}] Slot {slotType.Value} is empty, let game handle it");
                        return true; // 讓遊戲處理空槽位
                    }

                    // 已啟用外觀覆蓋且槽位有內容：完全接管渲染
                    // 先清空整個 Socket（避免殘留）
                    ClearEntireSocket(socket);

                    // 如果是 HelmatSocket，需要重新渲染頭盔和耳機
                    if (slotType.Value == EquipmentSlotType.Helmet || slotType.Value == EquipmentSlotType.Headset)
                    {
                        RenderHelmatSocketSlots(__instance, socket, characterType);
                        RefreshFacialFeaturesIfNeeded(slot, __instance);
                        return false;
                    }

                    // 已啟用外觀覆蓋：總是套用造型系統
                    if (config.SkinItemTypeID == -1)
                    {
                        // ID = -1：隱藏外觀（不渲染）
                        Logger.Debug($"Hiding equipment for {slotType.Value}");
                        RefreshFacialFeaturesIfNeeded(slot, __instance);
                        return false;
                    }
                    else if (config.SkinItemTypeID > 0)
                    {
                        // ID > 0：使用設定的外觀 ID
                        Logger.Debug($"Rendering skin {config.SkinItemTypeID} for {slotType.Value}");
                        RenderEquipment(config.SkinItemTypeID, socket);
                        RefreshFacialFeaturesIfNeeded(slot, __instance);
                        return false;
                    }
                    else
                    {
                        // ID = 0 或空：使用原始裝備的 ID 作為外觀（總是套用造型系統）
                        Logger.Debug($"Rendering original equipment ID {slot.Content.TypeID} as skin for {slotType.Value}");
                        RenderEquipment(slot.Content.TypeID, socket);
                        RefreshFacialFeaturesIfNeeded(slot, __instance);
                        return false;
                    }
                }
                catch (Exception e)
                {
                    Logger.Error("Error in ChangeEquipmentModel patch", e);
                    Logger.Error($"Stack trace: {e.StackTrace}");
                }

                // 出錯時執行原方法
                return true;
            }

            /// <summary>
            /// 檢查是否為目標角色（玩家或狗）
            /// </summary>
            private static bool IsTargetCharacter(CharacterEquipmentController controller, out CharacterType characterType)
            {
                characterType = CharacterType.Player;

                try
                {
                    // 取得 CharacterMainControl
                    var cm = Traverse.Create(controller)
                                     .Field("characterMainControl")
                                     .GetValue<CharacterMainControl>();

                    if (cm == null)
                        return false;

                    // 檢查是否為玩家角色
                    var mainCharacter = LevelManager.Instance?.MainCharacter;
                    if (mainCharacter != null && cm == mainCharacter)
                    {
                        characterType = CharacterType.Player;
                        return true;
                    }

                    // 檢查是否為狗
                    var petCharacter = LevelManager.Instance?.PetCharacter;
                    if (petCharacter != null && cm == petCharacter)
                    {
                        characterType = CharacterType.Pet;
                        return true;
                    }

                    return false; // 不是玩家也不是狗
                }
                catch (Exception ex)
                {
                    Logger.Error("Error checking target character", ex);
                    return false; // 出錯時不攔截，讓遊戲正常運作
                }
            }

            /// <summary>
            /// 檢查是否為玩家角色（向後相容）
            /// </summary>
            private static bool IsPlayerCharacter(CharacterEquipmentController controller)
            {
                CharacterType charType;
                return IsTargetCharacter(controller, out charType) && charType == CharacterType.Player;
            }

            /// <summary>
            /// 清空整個 Socket 的所有子物件
            /// </summary>
            private static void ClearEntireSocket(Transform socket)
            {
                if (socket == null) return;

                for (int i = socket.childCount - 1; i >= 0; i--)
                {
                    GameObject.Destroy(socket.GetChild(i).gameObject);
                }
                Logger.Debug("Cleared entire socket");
            }

            /// <summary>
            /// 渲染 HelmatSocket 上的所有槽位（頭盔 + 耳機）
            /// 因為它們共用同一個 Socket，必須一起處理
            /// </summary>
            private static void RenderHelmatSocketSlots(CharacterEquipmentController controller, Transform socket, CharacterType characterType)
            {
                // 取得頭盔和耳機槽位
                var helmatSlot = Traverse.Create(controller).Field("helmatSlot").GetValue<Slot>();
                var headsetSlot = Traverse.Create(controller).Field("headsetSlot").GetValue<Slot>();

                var helmatConfig = GetSlotConfig(EquipmentSlotType.Helmet, characterType);
                var headsetConfig = GetSlotConfig(EquipmentSlotType.Headset, characterType);

                // 渲染頭盔
                if (helmatSlot != null && helmatSlot.Content != null)
                {
                    if (helmatConfig.UseSkin)
                    {
                        if (helmatConfig.SkinItemTypeID == -1)
                        {
                            Logger.Debug("Helmet hidden (ID=-1)");
                        }
                        else if (helmatConfig.SkinItemTypeID > 0)
                        {
                            Logger.Debug($"Rendering helmet skin {helmatConfig.SkinItemTypeID}");
                            RenderEquipment(helmatConfig.SkinItemTypeID, socket);
                        }
                        else
                        {
                            Logger.Debug($"Rendering original helmet {helmatSlot.Content.TypeID}");
                            RenderEquipment(helmatSlot.Content.TypeID, socket);
                        }
                    }
                    else
                    {
                        // 未啟用：渲染原本裝備
                        Logger.Debug($"Rendering original helmet {helmatSlot.Content.TypeID} (skin disabled)");
                        RenderEquipment(helmatSlot.Content.TypeID, socket);
                    }
                }

                // 渲染耳機
                if (headsetSlot != null && headsetSlot.Content != null)
                {
                    if (headsetConfig.UseSkin)
                    {
                        if (headsetConfig.SkinItemTypeID == -1)
                        {
                            Logger.Debug("Headset hidden (ID=-1)");
                        }
                        else if (headsetConfig.SkinItemTypeID > 0)
                        {
                            Logger.Debug($"Rendering headset skin {headsetConfig.SkinItemTypeID}");
                            RenderEquipment(headsetConfig.SkinItemTypeID, socket);
                        }
                        else
                        {
                            Logger.Debug($"Rendering original headset {headsetSlot.Content.TypeID}");
                            RenderEquipment(headsetSlot.Content.TypeID, socket);
                        }
                    }
                    else
                    {
                        // 未啟用：渲染原本裝備
                        Logger.Debug($"Rendering original headset {headsetSlot.Content.TypeID} (skin disabled)");
                        RenderEquipment(headsetSlot.Content.TypeID, socket);
                    }
                }
            }

            /// <summary>
            /// 取得槽位配置（根據角色類型）
            /// </summary>
            private static SlotSkinConfig GetSlotConfig(EquipmentSlotType slotType, CharacterType characterType)
            {
                var profile = characterType == CharacterType.Player 
                    ? EquipmentSkinDataManager.Instance.PlayerProfile 
                    : EquipmentSkinDataManager.Instance.PetProfile;
                    
                if (profile.SlotConfigs.TryGetValue(slotType, out var config))
                    return config;
                
                // 返回預設配置（未啟用）
                return new SlotSkinConfig(slotType);
            }

            /// <summary>
            /// 渲染裝備（統一方法，根據 TypeID 渲染）
            /// </summary>
            private static void RenderEquipment(int itemTypeID, Transform socket)
            {
                Item item = ItemAssetsCollection.InstantiateSync(itemTypeID);
                if (item == null)
                {
                    Logger.Warning($"Failed to instantiate item {itemTypeID}");
                    return;
                }

                ItemAgent agent = item.AgentUtilities.CreateAgent(
                    CharacterEquipmentController.equipmentModelHash,
                    ItemAgent.AgentTypes.equipment
                );

                if (agent != null)
                {
                    agent.transform.SetParent(socket, worldPositionStays: false);
                    agent.transform.localRotation = Quaternion.identity;
                    agent.transform.localPosition = Vector3.zero;
                    Logger.Debug($"Rendered equipment {itemTypeID}");
                }
                else
                {
                    GameObject.Destroy(item.gameObject);
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
                    Logger.Error("Error refreshing facial features", ex);
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
                    Logger.Debug($"ChangeEquipmentModel called! Slot: {(slot != null ? "Valid" : "NULL")}, Content: {(slot?.Content != null ? "Valid" : "NULL")}");
                    
                    if (slot != null && slot.Content != null)
                    {
                        Logger.Debug($"📦 裝備變更 - 物品 ID: {slot.Content.TypeID}, 名稱: {slot.Content.name}");
                    }
                    else if (slot != null && slot.Content == null)
                    {
                        Logger.Debug("📦 裝備移除 - 槽位已清空");
                    }
                }
                catch (Exception e)
                {
                    Logger.Error("Error logging equipment change", e);
                    Logger.Error($"Stack: {e.StackTrace}");
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
                    Logger.Warning("ForceRefreshMouthVisibility: characterMainControl not found");
                    return;
                }

                var model = cm.characterModel;
                if (model == null)
                {
                    Logger.Warning("ForceRefreshMouthVisibility: characterModel is null");
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
                    Logger.Debug($"ForceRefreshMouthVisibility: invoked {methodName}");
                }
                else
                {
                    Logger.Warning($"ForceRefreshMouthVisibility: method {methodName} not found");
                }

                // 如果是頭盔，同時強制刷新耳機（耳機模型也掛在 HelmatSocket 上）
                // 但如果正在全局刷新，則跳過（避免重複渲染）
                if (slotType == EquipmentSlotType.Helmet && !_isRefreshingAll)
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
                            Logger.Debug("ForceRefreshMouthVisibility: refreshed headset model");
                        }
                    }
                }
            }
            catch (Exception e)
            {
                Logger.Error("ForceRefreshMouthVisibility error", e);
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

        private static bool _isRefreshingAll = false; // 標記是否正在全局刷新

        /// <summary>
        /// 強制重新渲染所有裝備槽位（用於 UI 切換外觀後）
        /// </summary>
        public static void ForceRefreshAllEquipment()
        {
            try
            {
                _isRefreshingAll = true; // 設置標記，避免 ForceRefreshMouthVisibility 重複渲染耳機

                // 刷新玩家裝備
                var mainCharacter = LevelManager.Instance?.MainCharacter;
                if (mainCharacter != null)
                {
                    RefreshCharacterEquipment(mainCharacter, "Player");
                }
                
                // 刷新狗的裝備
                var petCharacter = LevelManager.Instance?.PetCharacter;
                if (petCharacter != null)
                {
                    RefreshCharacterEquipment(petCharacter, "Pet");
                }

                Logger.Info("All equipment refreshed");
            }
            catch (Exception e)
            {
                Logger.Error("Error refreshing all equipment", e);
                Logger.Error($"Stack trace: {e.StackTrace}");
            }
            finally
            {
                _isRefreshingAll = false; // 清除標記
            }
        }

        /// <summary>
        /// 刷新單個角色的所有裝備
        /// </summary>
        private static void RefreshCharacterEquipment(CharacterMainControl character, string characterName)
        {
            var controller = character.GetComponent<CharacterEquipmentController>();
            if (controller == null)
            {
                Logger.Warning($"{characterName} has no CharacterEquipmentController");
                return;
            }

            // 取得所有裝備槽位
            var armorSlot = Traverse.Create(controller).Field("armorSlot").GetValue<Slot>();
            var helmatSlot = Traverse.Create(controller).Field("helmatSlot").GetValue<Slot>();
            var faceMaskSlot = Traverse.Create(controller).Field("faceMaskSlot").GetValue<Slot>();
            var backpackSlot = Traverse.Create(controller).Field("backpackSlot").GetValue<Slot>();
            var headsetSlot = Traverse.Create(controller).Field("headsetSlot").GetValue<Slot>();

            // 檢查是否有任何裝備已加載（避免在裝備還沒加載時就清空）
            bool hasAnyEquipment = (armorSlot != null && armorSlot.Content != null) ||
                                   (helmatSlot != null && helmatSlot.Content != null) ||
                                   (faceMaskSlot != null && faceMaskSlot.Content != null) ||
                                   (backpackSlot != null && backpackSlot.Content != null) ||
                                   (headsetSlot != null && headsetSlot.Content != null);

            if (!hasAnyEquipment)
            {
                Logger.Debug($"{characterName} has no equipment loaded yet, skipping refresh");
                return;
            }

            // 強制觸發每個槽位的渲染方法（只刷新有裝備的槽位）
            if (armorSlot != null && armorSlot.Content != null)
                Traverse.Create(controller).Method("ChangeArmorModel", armorSlot).GetValue();
            
            if (backpackSlot != null && backpackSlot.Content != null)
                Traverse.Create(controller).Method("ChangeBackpackModel", backpackSlot).GetValue();
            
            if (helmatSlot != null && helmatSlot.Content != null)
                Traverse.Create(controller).Method("ChangeHelmatModel", helmatSlot).GetValue();
            
            if (faceMaskSlot != null && faceMaskSlot.Content != null)
                Traverse.Create(controller).Method("ChangeFaceMaskModel", faceMaskSlot).GetValue();
            
            if (headsetSlot != null && headsetSlot.Content != null)
                Traverse.Create(controller).Method("ChangeHeadsetModel", headsetSlot).GetValue();

            Logger.Info($"{characterName} equipment refreshed");
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

                Logger.Debug($"Detecting slot for item {item.TypeID}");
            }
            catch (Exception e)
            {
                Logger.Error("Error detecting slot type", e);
            }

            return null;
        }

    }
}
