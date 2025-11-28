using System;
using System.Collections.Generic;
using HarmonyLib;
using ItemStatsSystem;
using ItemStatsSystem.Items;
using UnityEngine;

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
            public static bool Prefix(Slot slot, Transform socket, CharacterEquipmentController __instance, out Slot __state)
            {
                __state = slot;  // 保存 slot 給 Postfix 使用
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

                    // 特殊處理：頭盔、耳機、面部是特例，全部由我們接管（因為遊戲本身有渲染問題）
                    // 當其中任一槽位變更時（包括脫下），都應該重新渲染所有三個槽位
                    if (slotType.Value == EquipmentSlotType.Helmet || slotType.Value == EquipmentSlotType.Headset || slotType.Value == EquipmentSlotType.FaceMask)
                    {
                        Logger.Debug($"[{characterType}] Helmet/headset/faceMask slot changed (Content: {slot.Content != null}), re-rendering all three slots");
                        ClearEntireSocket(socket);
                        RenderHelmetHeadsetFaceMaskSlots(__instance, socket, characterType, slot, slotType.Value);

                        // 每次渲染後，強制重新判斷頭髮/嘴巴
                        ForceUpdateHairAndMouth(__instance, characterType);
                        return false; // 完全接管，不讓遊戲處理
                    }

                    // 其他槽位：根據配置決定是否接管
                    // 先清空整個 Socket（避免殘留）
                    ClearEntireSocket(socket);

                    // 未啟用外觀覆蓋：讓遊戲處理
                    if (!config.UseSkin)
                    {
                        if (slot.Content == null)
                        {
                            Logger.Debug($"[{characterType}] Slot {slotType.Value} is empty and UseSkin=false, let game handle it");
                        }
                        else
                        {
                            Logger.Debug($"[{characterType}] Not using skin for {slotType.Value}, let game handle it");
                        }
                        return true; // 執行遊戲原方法
                    }

                    // 已啟用外觀覆蓋：完全接管渲染
                    if (config.SkinItemTypeID == -1)
                    {
                        // ID = -1：隱藏外觀（不渲染）
                        Logger.Debug($"Hiding equipment for {slotType.Value}");
                        return false;
                    }
                    else if (config.SkinItemTypeID > 0)
                    {
                        // ID > 0：使用設定的外觀 ID（即使沒有裝備也渲染）
                        Logger.Debug($"Rendering skin {config.SkinItemTypeID} for {slotType.Value}");
                        RenderEquipment(config.SkinItemTypeID, socket, slot);
                        return false;
                    }
                    else
                    {
                        // ID = 0 或空：需要實際裝備才能渲染
                        if (slot.Content != null)
                        {
                            // 使用原始裝備的 ID 作為外觀（總是套用造型系統）
                            Logger.Debug($"Rendering original equipment ID {slot.Content.TypeID} as skin for {slotType.Value}");
                            RenderEquipment(slot.Content.TypeID, socket, slot);
                            return false;
                        }
                        else
                        {
                            // 沒有裝備且 ID = 0，讓遊戲處理
                            Logger.Debug($"[{characterType}] Slot {slotType.Value} is empty and SkinID=0, let game handle it");
                            return true;
                        }
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
            /// 在遊戲原方法執行後，強制重新判斷頭髮/嘴巴顯示（確保狀態正確）
            /// </summary>
            [HarmonyPostfix]
            public static void Postfix(Slot __state, CharacterEquipmentController __instance)
            {
                try
                {
                    if (__state == null)
                        return;

                    // 檢查是否為目標角色
                    CharacterType characterType;
                    if (!IsTargetCharacter(__instance, out characterType))
                        return;

                    // 只有頭部槽位需要重新判斷頭髮/嘴巴
                    var slotType = GetSlotTypeFromKey(__state.Key);
                    if (!slotType.HasValue || !IsHeadSlot(slotType.Value))
                        return;

                    Logger.Debug($"[Postfix] Force updating hair/mouth after game's original method for slot {slotType.Value}");

                    // 強制重新判斷頭髮/嘴巴（每次都重新讀取當前所有槽位的狀態）
                    ForceUpdateHairAndMouth(__instance, characterType);
                }
                catch (Exception ex)
                {
                    Logger.Error("Error in ChangeEquipmentModel Postfix", ex);
                }
            }
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
            if (socket == null)
                return;

            for (int i = socket.childCount - 1; i >= 0; i--)
            {
                GameObject.Destroy(socket.GetChild(i).gameObject);
            }
            Logger.Debug("Cleared entire socket");
        }

        /// <summary>
        /// 決定某個槽位要渲染的裝備 ID
        /// </summary>
        /// <returns>要渲染的 ItemTypeID，null 代表不渲染</returns>
        private static int? GetItemToRender(Slot slot, SlotSkinConfig config)
        {
            // 如果啟用造型
            if (config.UseSkin)
            {
                if (config.SkinItemTypeID == -1)
                    return null;  // 隱藏

                if (config.SkinItemTypeID > 0)
                    return config.SkinItemTypeID;  // 渲染幻化

                // SkinID = 0：退化成原裝
                return slot?.Content?.TypeID;
            }

            // 不啟用造型：渲染原裝
            return slot?.Content?.TypeID;
        }

        /// <summary>
        /// 渲染頭盔、耳機、面部
        /// 永遠保持 耳機->頭盔->面部 這樣的順序去渲染
        /// </summary>
        private static void RenderHelmetHeadsetFaceMaskSlots(CharacterEquipmentController controller, Transform socket, CharacterType characterType, Slot currentSlot, EquipmentSlotType currentSlotType)
        {
            // 取得頭盔、耳機、面部槽位
            var helmatSlot = Traverse.Create(controller).Field("helmatSlot").GetValue<Slot>();
            var headsetSlot = Traverse.Create(controller).Field("headsetSlot").GetValue<Slot>();
            var faceMaskSlot = Traverse.Create(controller).Field("faceMaskSlot").GetValue<Slot>();

            var helmatConfig = GetSlotConfig(EquipmentSlotType.Helmet, characterType);
            var headsetConfig = GetSlotConfig(EquipmentSlotType.Headset, characterType);
            var faceMaskConfig = GetSlotConfig(EquipmentSlotType.FaceMask, characterType);

            // 取得正確的 socket（頭盔和耳機使用 helmatSocket，面部使用 faceMaskSocket）
            var cm = Traverse.Create(controller).Field("characterMainControl").GetValue<CharacterMainControl>();
            Transform helmatSocket = null;
            Transform faceMaskSocket = null;
            if (cm != null && cm.characterModel != null)
            {
                helmatSocket = cm.characterModel.HelmatSocket;  // 頭盔和耳機使用這個 socket
                faceMaskSocket = cm.characterModel.FaceMaskSocket;  // 面部使用這個 socket
            }

            // 如果無法取得 helmatSocket，使用傳入的 socket 作為備用
            if (helmatSocket == null)
            {
                helmatSocket = socket;
            }

            // 決定使用哪個 Slot（如果當前觸發的就是目標槽位，優先使用 currentSlot）
            Slot headsetSlotToUse = (currentSlotType == EquipmentSlotType.Headset) ? currentSlot : headsetSlot;
            Slot helmetSlotToUse = (currentSlotType == EquipmentSlotType.Helmet) ? currentSlot : helmatSlot;
            Slot faceMaskSlotToUse = (currentSlotType == EquipmentSlotType.FaceMask) ? currentSlot : faceMaskSlot;

            // 決定要渲染的裝備 ID（null = 不渲染）
            int? headsetID = GetItemToRender(headsetSlotToUse, headsetConfig);
            int? helmetID = GetItemToRender(helmetSlotToUse, helmatConfig);
            int? faceMaskID = GetItemToRender(faceMaskSlotToUse, faceMaskConfig);

            Logger.Debug($"[Render] Headset={headsetID}, Helmet={helmetID}, FaceMask={faceMaskID}");

            // 清空所有 socket（避免殘留）
            ClearEntireSocket(helmatSocket);
            if (faceMaskSocket != null && faceMaskSocket != helmatSocket)
                ClearEntireSocket(faceMaskSocket);

            // 按順序渲染：耳機 → 頭盔 → 面罩（都不清空 socket，因為已經在上面統一清空了）
            if (headsetID.HasValue)
            {
                Logger.Debug($"Rendering headset ID {headsetID.Value}");
                RenderEquipment(headsetID.Value, helmatSocket, headsetSlotToUse, clearSocket: false);
            }

            if (helmetID.HasValue)
            {
                Logger.Debug($"Rendering helmet ID {helmetID.Value}");
                RenderEquipment(helmetID.Value, helmatSocket, helmetSlotToUse, clearSocket: false);
            }

            if (faceMaskID.HasValue && faceMaskSocket != null)
            {
                Logger.Debug($"Rendering face mask ID {faceMaskID.Value}");
                RenderEquipment(faceMaskID.Value, faceMaskSocket, faceMaskSlotToUse, clearSocket: false);
            }

        }

        /// <summary>
        /// 取得實際要用來判斷頭髮/嘴巴的 Item（考慮幻化）
        /// </summary>
        /// <returns>實際 Item（可能為 null = 沒裝備）</returns>
        private static Item GetEffectiveItem(Slot slot, SlotSkinConfig config)
        {
            if (slot == null)
            {
                Logger.Debug("[GetEffectiveItem] slot is null, return null");
                return null;
            }

            if (config.UseSkin)
            {
                if (config.SkinItemTypeID == -1)
                {
                    Logger.Debug("[GetEffectiveItem] UseSkin=true, SkinID=-1, return null (hide)");
                    return null;  // 隱藏 = 當作沒裝備
                }

                if (config.SkinItemTypeID > 0)
                {
                    var item = ItemAssetsCollection.InstantiateSync(config.SkinItemTypeID);
                    Logger.Debug($"[GetEffectiveItem] UseSkin=true, SkinID={config.SkinItemTypeID}, return skin item {item?.TypeID}");
                    return item;  // 幻化 Item
                }

                // SkinID = 0：退化成原裝
                Logger.Debug($"[GetEffectiveItem] UseSkin=true, SkinID=0, return original item {slot.Content?.TypeID}");
            }
            else
            {
                Logger.Debug($"[GetEffectiveItem] UseSkin=false, return original item {slot.Content?.TypeID}");
            }

            return slot.Content;  // 原裝或不啟用幻化
        }

        /// <summary>
        /// 根據頭盔和面罩的 ShowHair 和 ShowMouth 設定更新頭髮和嘴巴顯示狀態
        /// </summary>
        private static void UpdateHairAndMouthVisibility(
            CharacterEquipmentController controller,
            Slot helmatSlot,
            Slot faceMaskSlot,
            SlotSkinConfig helmatConfig,
            SlotSkinConfig faceMaskConfig,
            CharacterType characterType)
        {
            try
            {
                var cm = Traverse.Create(controller).Field("characterMainControl").GetValue<CharacterMainControl>();
                if (cm?.characterModel?.CustomFace == null)
                    return;

                var customFace = cm.characterModel.CustomFace;
                int showHairHash = "ShowHair".GetHashCode();
                int showMouthHash = "ShowMouth".GetHashCode();

                // 取得實際要判斷的 Item（考慮幻化）
                Item helmetItem = GetEffectiveItem(helmatSlot, helmatConfig);
                Item faceMaskItem = GetEffectiveItem(faceMaskSlot, faceMaskConfig);

                // 判斷頭髮顯示：只看頭盔（沒頭盔 = 顯示）
                bool showHair = (helmetItem == null)
                    ? true
                    : helmetItem.Constants.GetBool(showHairHash, defaultResult: false);

                // 判斷嘴巴顯示：頭盔和面罩都可能影響（兩者都允許才顯示）
                bool helmetShowMouth = (helmetItem == null)
                    ? true
                    : helmetItem.Constants.GetBool(showMouthHash, defaultResult: true);

                bool faceMaskShowMouth = (faceMaskItem == null)
                    ? true
                    : faceMaskItem.Constants.GetBool(showMouthHash, defaultResult: true);

                bool showMouth = helmetShowMouth && faceMaskShowMouth;

                Logger.Debug($"[Hair/Mouth] Helmet={helmetItem?.TypeID}, FaceMask={faceMaskItem?.TypeID}, ShowHair={showHair}, HelmetShowMouth={helmetShowMouth}, FaceMaskShowMouth={faceMaskShowMouth}, FinalShowMouth={showMouth}");

                // 套用
                if (customFace.hairSocket != null)
                    customFace.hairSocket.gameObject.SetActive(showHair);

                if (customFace.mouthPart?.socket != null)
                    customFace.mouthPart.socket.gameObject.SetActive(showMouth);

                // 清理臨時 Item（避免刪掉原始 slot.Content）
                if (helmetItem != null && helmetItem != helmatSlot?.Content)
                    GameObject.Destroy(helmetItem.gameObject);

                if (faceMaskItem != null && faceMaskItem != faceMaskSlot?.Content)
                    GameObject.Destroy(faceMaskItem.gameObject);
            }
            catch (Exception ex)
            {
                Logger.Error("Error updating hair/mouth visibility", ex);
            }
        }

        /// <summary>
        /// 檢查是否需要讓遊戲處理耳機（當耳機的 UseSkin 為 false 但頭盔的 UseSkin 為 true 時）
        /// </summary>
        private static bool ShouldLetGameHandleHeadset(CharacterEquipmentController controller, CharacterType characterType)
        {
            var headsetSlot = Traverse.Create(controller).Field("headsetSlot").GetValue<Slot>();
            var headsetConfig = GetSlotConfig(EquipmentSlotType.Headset, characterType);

            // 如果耳機不需要我們處理，讓遊戲處理
            return headsetSlot != null && headsetSlot.Content != null && !headsetConfig.UseSkin;
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
        /// <param name="itemTypeID">要渲染的物品 ID（外觀 ID）</param>
        /// <param name="socket">要掛載的 socket</param>
        /// <param name="actualSlot">實際裝備的 slot（如果有，Agent 會綁定到此 slot.Content 以保持功能正常）</param>
        /// <param name="clearSocket">是否清空 socket（預設為 true，當多個裝備共用同一個 socket 時設為 false）</param>
        private static void RenderEquipment(int itemTypeID, Transform socket, Slot actualSlot = null, bool clearSocket = true)
        {
            // 先清除 socket 上的現有物件（如果需要的話）
            if (clearSocket)
            {
                ClearEntireSocket(socket);
            }

            ItemAgent agent = null;

            // 如果有實際裝備的 slot，使用 slot.Content 的 Agent Prefab（保持功能正常）
            if (actualSlot != null && actualSlot.Content != null)
            {
                // 使用 slot.Content 的 Agent Prefab 創建 Agent（保持功能）
                // 這樣 selfAgent.Item 會指向原本的 Item，功能組件（如 SoulCollector）才能正常工作
                agent = actualSlot.Content.AgentUtilities.CreateAgent(
                    CharacterEquipmentController.equipmentModelHash,
                    ItemAgent.AgentTypes.equipment
                );

                if (agent == null)
                {
                    Logger.Warning($"Failed to create agent for item {actualSlot.Content.TypeID}");
                    return;
                }

                // 如果外觀 ID 與實際裝備 ID 不同，替換視覺模型
                if (itemTypeID != actualSlot.Content.TypeID)
                {
                    ReplaceAgentVisualModel(agent, itemTypeID);
                }
            }
            else
            {
                // 沒有實際裝備，使用外觀 Item 的 Prefab（僅視覺）
                Item tempItem = ItemAssetsCollection.InstantiateSync(itemTypeID);
                if (tempItem != null)
                {
                    agent = tempItem.AgentUtilities.CreateAgent(
                        CharacterEquipmentController.equipmentModelHash,
                        ItemAgent.AgentTypes.equipment
                    );
                }
            }

            if (agent != null)
            {
                agent.transform.SetParent(socket, worldPositionStays: false);
                agent.transform.localRotation = Quaternion.identity;
                agent.transform.localPosition = Vector3.zero;
            }
        }

        /// <summary>
        /// 替換 Agent 的視覺模型為外觀 Item 的模型
        /// 保留功能組件（如 SoulCollector），只替換視覺
        /// </summary>
        private static void ReplaceAgentVisualModel(ItemAgent agent, int skinItemTypeID)
        {
            try
            {
                // 創建外觀 Item 來獲取視覺模型
                Item skinItem = ItemAssetsCollection.InstantiateSync(skinItemTypeID);
                if (skinItem == null)
                {
                    Logger.Warning($"Failed to instantiate skin item {skinItemTypeID}");
                    return;
                }

                ItemAgent skinAgentPrefab = skinItem.AgentUtilities.GetPrefab(CharacterEquipmentController.equipmentModelHash);
                if (skinAgentPrefab == null)
                {
                    Logger.Warning($"Failed to get skin agent prefab for item {skinItemTypeID}");
                    GameObject.Destroy(skinItem.gameObject);
                    return;
                }

                // 實例化外觀 Agent 來獲取視覺模型
                ItemAgent skinAgent = UnityEngine.Object.Instantiate(skinAgentPrefab);

                // 隱藏原本的視覺子物件（保留功能組件在 agent 根物件上）
                var originalRenderers = agent.GetComponentsInChildren<Renderer>(true);
                foreach (var renderer in originalRenderers)
                {
                    // 只隱藏視覺子物件，不隱藏 agent 根物件上的組件
                    if (renderer.transform != agent.transform)
                    {
                        renderer.enabled = false;
                    }
                }

                // 複製外觀 Agent 的所有視覺子物件
                foreach (Transform skinChild in skinAgent.transform)
                {
                    // 跳過可能包含功能組件的子物件
                    if (skinChild.name.Contains("Collider") || skinChild.name.Contains("Trigger"))
                    {
                        continue;
                    }

                    var clonedChild = UnityEngine.Object.Instantiate(skinChild.gameObject, agent.transform);
                    clonedChild.name = skinChild.name;
                }

                // 銷毀臨時物件
                GameObject.Destroy(skinAgent.gameObject);
                GameObject.Destroy(skinItem.gameObject);
            }
            catch (Exception e)
            {
                Logger.Error($"Error replacing agent visual model: {e.Message}", e);
            }
        }


        /// <summary>
        /// 強制重新判斷並更新頭髮/嘴巴顯示（每次都重新讀取所有槽位狀態）
        /// </summary>
        private static void ForceUpdateHairAndMouth(CharacterEquipmentController controller, CharacterType characterType)
        {
            try
            {
                Logger.Debug($"[ForceUpdateHairAndMouth] Starting for {characterType}");

                // 重新取得所有槽位和配置
                var helmatSlot = Traverse.Create(controller).Field("helmatSlot").GetValue<Slot>();
                var faceMaskSlot = Traverse.Create(controller).Field("faceMaskSlot").GetValue<Slot>();
                var helmatConfig = GetSlotConfig(EquipmentSlotType.Helmet, characterType);
                var faceMaskConfig = GetSlotConfig(EquipmentSlotType.FaceMask, characterType);

                Logger.Debug($"[ForceUpdateHairAndMouth] Helmet: UseSkin={helmatConfig.UseSkin}, SkinID={helmatConfig.SkinItemTypeID}, HasContent={helmatSlot?.Content != null}");
                Logger.Debug($"[ForceUpdateHairAndMouth] FaceMask: UseSkin={faceMaskConfig.UseSkin}, SkinID={faceMaskConfig.SkinItemTypeID}, HasContent={faceMaskSlot?.Content != null}");

                // 重新判斷並更新頭髮/嘴巴
                UpdateHairAndMouthVisibility(controller, helmatSlot, faceMaskSlot, helmatConfig, faceMaskConfig, characterType);
            }
            catch (Exception ex)
            {
                Logger.Error("Error in ForceUpdateHairAndMouth", ex);
            }
        }

        /// <summary>
        /// 判斷是否為頭部槽位（需要刷新面部特徵）
        /// </summary>
        private static bool IsHeadSlot(EquipmentSlotType slotType)
        {
            return slotType == EquipmentSlotType.Helmet || slotType == EquipmentSlotType.FaceMask;
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
                if (controller == null)
                    return;

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
            if (string.IsNullOrEmpty(key))
                return null;

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
                Logger.Info("=== ForceRefreshAllEquipment called ===");
                _isRefreshingAll = true; // 設置標記，避免 ForceRefreshMouthVisibility 重複渲染耳機

                // 刷新玩家裝備
                var mainCharacter = LevelManager.Instance?.MainCharacter;
                if (mainCharacter != null)
                {
                    Logger.Debug("Refreshing Player equipment...");
                    RefreshCharacterEquipment(mainCharacter, "Player");
                }
                else
                {
                    Logger.Warning("MainCharacter is null, cannot refresh Player equipment");
                }

                // 刷新狗的裝備
                var petCharacter = LevelManager.Instance?.PetCharacter;
                if (petCharacter != null)
                {
                    Logger.Debug("Refreshing Pet equipment...");
                    RefreshCharacterEquipment(petCharacter, "Pet");
                }
                else
                {
                    Logger.Debug("PetCharacter is null (normal if no pet)");
                }

                Logger.Info("=== All equipment refreshed ===");
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

            // 不再檢查是否有原始裝備，因為即使沒有原始裝備，也可能有 Skin 設定需要渲染
            Logger.Debug($"[{characterName}] Refreshing all slots (including empty slots with skin settings)...");

            // 強制觸發每個槽位的渲染方法
            // 即使沒有原始裝備，只要有啟用造型也需要透過 ChangeXXXModel 讓 Harmony Prefix 介入並決定是否渲染 Skin
            Logger.Debug($"[{characterName}] Triggering ChangeArmorModel...");
            if (armorSlot != null)
                Traverse.Create(controller).Method("ChangeArmorModel", armorSlot).GetValue();

            Logger.Debug($"[{characterName}] Triggering ChangeBackpackModel...");
            if (backpackSlot != null)
                Traverse.Create(controller).Method("ChangeBackpackModel", backpackSlot).GetValue();

            Logger.Debug($"[{characterName}] Triggering ChangeHelmatModel...");
            if (helmatSlot != null)
                Traverse.Create(controller).Method("ChangeHelmatModel", helmatSlot).GetValue();

            Logger.Debug($"[{characterName}] Triggering ChangeFaceMaskModel...");
            if (faceMaskSlot != null)
                Traverse.Create(controller).Method("ChangeFaceMaskModel", faceMaskSlot).GetValue();

            Logger.Debug($"[{characterName}] Triggering ChangeHeadsetModel...");
            if (headsetSlot != null)
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
            if (item == null)
                return null;

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
