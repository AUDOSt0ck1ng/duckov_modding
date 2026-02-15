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
        [HarmonyPatch(typeof(CharacterEquipmentController), "ChangeEquipmentModel", new Type[] { typeof(Slot), typeof(Transform) })]
        public static class ChangeEquipmentModelPatch
        {
            [HarmonyPrefix]
            public static bool Prefix(Slot slot, Transform socket, CharacterEquipmentController __instance, out Slot __state)
            {
                __state = slot;  // 保存 slot 給 Postfix 使用
                try
                {

                    // ====================================================================
                    // 關鍵修正：所有驗證檢查必須在任何操作（如清空 socket）之前完成
                    // 如果不是目標場景/角色，直接返回 true，不做任何操作
                    // ====================================================================

                    // 檢查 1：場景類型（過場動畫場景不處理）
                    if (!IsValidGameplayScene())
                    {
                        Logger.Debug("Not a valid gameplay scene (cutscene/transition detected), let game handle it");
                        return true; // 直接讓遊戲處理，不做任何操作
                    }

                    // 檢查 2：是否為目標角色（玩家或狗）
                    CharacterType characterType;
                    if (!IsTargetCharacter(__instance, out characterType))
                    {
                        Logger.Debug("Not target character, let game handle it");
                        return true; // 直接讓遊戲處理，不做任何操作
                    }

                    // 檢查 3：槽位和 socket 有效性
                    if (slot == null || socket == null || socket.gameObject == null)
                    {
                        Logger.Debug("Invalid slot or socket, let game handle it");
                        return true;
                    }

                    // 檢查 4：Socket 是否在有效的 CharacterModel 下
                    var cm = Traverse.Create(__instance).Field("characterMainControl").GetValue<CharacterMainControl>();
                    if (cm == null || cm.characterModel == null)
                    {
                        Logger.Debug("CharacterModel is null, cannot verify socket validity, let game handle it");
                        return true;
                    }

                    // 檢查 5：驗證 socket 確實屬於這個 characterModel
                    if (!IsSocketValidForCharacterModel(socket, cm.characterModel))
                    {
                        Logger.Warning($"Socket '{socket.name}' is not valid for current CharacterModel, let game handle it (might be in cutscene)");
                        return true;
                    }

                    // 檢查 6：是否為我們管理的槽位
                    var slotType = GetSlotTypeFromKey(slot.Key);
                    if (!slotType.HasValue)
                    {
                        Logger.Debug("Not a managed slot type, let game handle it");
                        return true; // 不是我們管理的槽位，執行原方法
                    }

                    // ====================================================================
                    // 所有檢查通過，開始處理邏輯
                    // ====================================================================

                    var config = GetSlotConfig(slotType.Value, characterType);

                    // 特殊處理：頭盔、耳機、面部是特例，全部由我們接管（因為遊戲本身有渲染問題）
                    // 當其中任一槽位變更時（包括脫下），都應該重新渲染所有三個槽位
                    if (slotType.Value == EquipmentSlotType.Helmet || slotType.Value == EquipmentSlotType.Headset || slotType.Value == EquipmentSlotType.FaceMask)
                    {
                        Logger.Debug($"[{characterType}] Helmet/headset/faceMask slot changed (Content: {slot.Content != null}), scheduling delayed render");

                        // 關鍵修復：延遲渲染到下一幀，避免在 Item.OnDestroy() 過程中操作 socket
                        // 先清空 socket（這個是安全的）
                        ClearEntireSocket(socket);

                        // 使用 Coroutine 延遲渲染
                        if (ModBehaviour.Instance != null)
                        {
                            ModBehaviour.Instance.StartCoroutine(DelayedRenderHelmetSlots(__instance, socket, characterType, slot, slotType.Value));
                        }
                        else
                        {
                            // 如果 ModBehaviour 不可用，直接渲染（風險較高但至少能工作）
                            Logger.Warning("ModBehaviour.Instance is null, rendering immediately (might cause issues)");
                            RenderHelmetHeadsetFaceMaskSlots(__instance, socket, characterType, slot, slotType.Value);
                            ForceUpdateHairAndMouth(__instance, characterType);
                        }

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
        /// 檢查是否為有效的遊戲場景（基地或副本地圖）
        /// 過場動畫、過渡場景等會返回 false
        /// </summary>
        private static bool IsValidGameplayScene()
        {
            try
            {
                // 關鍵檢查 1：場景名稱（最直接的方式）
                string currentSceneName = UnityEngine.SceneManagement.SceneManager.GetActiveScene().name;
                if (!string.IsNullOrEmpty(currentSceneName))
                {
                    string sceneLower = currentSceneName.ToLower();

                    // 排除已知的非遊戲場景
                    if (sceneLower.Contains("loading") ||
                        sceneLower.Contains("cutscene") ||
                        sceneLower.Contains("menu") ||
                        sceneLower.Contains("transition"))
                    {
                        Logger.Debug($"Scene check: '{currentSceneName}' is a non-gameplay scene, skipping equipment render");
                        return false;
                    }
                }

                // 關鍵檢查 2：是否有正在播放的過場動畫
                var cutScene = UnityEngine.Object.FindFirstObjectByType<CutScene>();
                if (cutScene != null)
                {
                    // 使用 Traverse 檢查 private 字段 playing
                    bool isPlaying = Traverse.Create(cutScene).Field("playing").GetValue<bool>();
                    if (isPlaying)
                    {
                        Logger.Debug("Scene check: CutScene is playing, skipping equipment render");
                        return false;
                    }
                }

                // 檢查是否有 LevelManager
                if (LevelManager.Instance == null)
                {
                    Logger.Debug("Scene check: LevelManager.Instance is null, skipping equipment render");
                    return false;
                }

                // 關鍵：必須是基地或副本地圖其中之一
                bool isBaseLevel = LevelManager.Instance.IsBaseLevel;
                bool isRaidMap = LevelManager.Instance.IsRaidMap;

                // 保守策略：必須明確是基地或副本地圖，才允許渲染
                // 如果兩個都是 false（過場動畫、載入畫面、或 LevelConfig 不存在），則不渲染
                bool isValidGameplay = isBaseLevel || isRaidMap;

                if (!isValidGameplay)
                {
                    Logger.Debug($"Scene check: Not a gameplay scene (IsBaseLevel={isBaseLevel}, IsRaidMap={isRaidMap}), skipping equipment render");
                    return false;
                }

                Logger.Debug($"Scene check: Valid gameplay scene '{currentSceneName}' (IsBaseLevel={isBaseLevel}, IsRaidMap={isRaidMap}), allowing equipment render");
                return true;
            }
            catch (Exception e)
            {
                Logger.Error("Error checking if valid gameplay scene", e);
                return false; // 出錯時不渲染，讓遊戲處理
            }
        }

        /// <summary>
        /// 驗證 Socket 是否屬於給定的 CharacterModel
        /// 防止在過場動畫中渲染到錯誤的 Socket 上
        /// </summary>
        private static bool IsSocketValidForCharacterModel(Transform socket, CharacterModel characterModel)
        {
            if (socket == null || characterModel == null)
                return false;

            try
            {
                // 檢查 socket 是否是 characterModel 的子物件
                Transform current = socket;
                while (current != null)
                {
                    if (current == characterModel.transform)
                    {
                        return true; // socket 是 characterModel 的子物件
                    }
                    current = current.parent;
                }

                // socket 不屬於這個 characterModel
                Logger.Debug($"Socket '{socket.name}' is not a child of CharacterModel '{characterModel.name}'");
                return false;
            }
            catch (Exception ex)
            {
                Logger.Error("Error validating socket", ex);
                return false;
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

                // 檢查 CharacterModel 是否存在（過場動畫中可能沒有）
                if (cm.characterModel == null)
                {
                    Logger.Debug("CharacterModel is null, skipping rendering (might be in cutscene or transitional scene)");
                    return false;
                }

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
                    Logger.Debug("Identified as Pet character");
                    return true;
                }

                Logger.Debug($"Not target character (not MainCharacter and not PetCharacter)");
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
            if (socket == null || socket.gameObject == null)
                return;

            try
            {
                for (int i = socket.childCount - 1; i >= 0; i--)
                {
                    var child = socket.GetChild(i);
                    if (child != null && child.gameObject != null)
                    {
                        GameObject.Destroy(child.gameObject);
                    }
                }
                Logger.Debug("Cleared entire socket");
            }
            catch (System.Exception ex)
            {
                Logger.Warning($"Error while clearing socket: {ex.Message}");
            }
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
        /// 延遲渲染頭盔槽位（Coroutine）
        /// 用於避免在 Item.OnDestroy() 過程中操作 socket
        /// </summary>
        private static System.Collections.IEnumerator DelayedRenderHelmetSlots(CharacterEquipmentController controller, Transform socket, CharacterType characterType, Slot currentSlot, EquipmentSlotType currentSlotType)
        {
            // 等待一幀，讓 OnDestroy() 完成
            yield return null;

            // 再次驗證 socket 和 controller 仍然有效
            if (socket == null || IsGameObjectBeingDestroyed(socket.gameObject))
            {
                Logger.Warning("Socket was destroyed during delayed render, skipping");
                yield break;
            }

            if (controller == null)
            {
                Logger.Warning("Controller was destroyed during delayed render, skipping");
                yield break;
            }

            try
            {
                Logger.Debug($"[Delayed Render] Executing delayed render for {currentSlotType}");
                RenderHelmetHeadsetFaceMaskSlots(controller, socket, characterType, currentSlot, currentSlotType);
                ForceUpdateHairAndMouth(controller, characterType);
            }
            catch (System.Exception ex)
            {
                Logger.Error($"Error in delayed helmet render: {ex.Message}", ex);
            }
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
        /// 檢查 GameObject 是否正在被銷毀或已被銷毀
        /// Unity 的特殊機制：正在銷毀的物件 == null 可能返回 false
        /// </summary>
        private static bool IsGameObjectBeingDestroyed(GameObject obj)
        {
            if (obj == null)
                return true;

            try
            {
                // 嘗試訪問 name 屬性，如果物件正在被銷毀會拋出異常
                var _ = obj.name;
                // 檢查 activeInHierarchy - 如果父物件正在被銷毀，這個會返回 false
                if (!obj.activeInHierarchy && obj.activeSelf)
                {
                    // 物件本身是 active 但在 hierarchy 中不是 active = 父物件可能被銷毀
                    return true;
                }
                return false;
            }
            catch
            {
                return true;
            }
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
            // 檢查 itemTypeID 是否有效
            if (itemTypeID <= 0)
            {
                Logger.Warning($"Invalid itemTypeID: {itemTypeID}, cannot render equipment");
                return;
            }

            // 檢查 socket 是否有效且未被銷毀
            if (socket == null || IsGameObjectBeingDestroyed(socket.gameObject))
            {
                Logger.Warning("Socket is null or being destroyed, cannot render equipment");
                return;
            }

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
                ItemAgent prefab = actualSlot.Content.AgentUtilities.GetPrefab(CharacterEquipmentController.equipmentModelHash);
                if (prefab != null)
                {
                    agent = actualSlot.Content.AgentUtilities.CreateAgent(prefab, ItemAgent.AgentTypes.equipment);
                }
                else if ((bool)actualSlot.Content.ItemGraphic)
                {
                    // 後備方案：使用 ItemGraphic（與遊戲原始代碼相同的邏輯）
                    agent = ItemGraphicInfo.CreateAGraphic(actualSlot.Content, socket).gameObject.AddComponent<DuckovItemAgent>();
                    actualSlot.Content.AgentUtilities.BindNewAgent(agent, ItemAgent.AgentTypes.equipment);
                }

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
                    ItemAgent prefab = tempItem.AgentUtilities.GetPrefab(CharacterEquipmentController.equipmentModelHash);
                    if (prefab != null)
                    {
                        agent = tempItem.AgentUtilities.CreateAgent(prefab, ItemAgent.AgentTypes.equipment);
                    }
                    else if ((bool)tempItem.ItemGraphic)
                    {
                        // 後備方案：使用 ItemGraphic（與遊戲原始代碼相同的邏輯）
                        agent = ItemGraphicInfo.CreateAGraphic(tempItem, socket).gameObject.AddComponent<DuckovItemAgent>();
                        tempItem.AgentUtilities.BindNewAgent(agent, ItemAgent.AgentTypes.equipment);
                    }
                }
            }

            if (agent != null)
            {
                // 再次檢查 socket 是否仍然有效（防止在創建 agent 過程中被銷毀）
                if (socket == null || IsGameObjectBeingDestroyed(socket.gameObject))
                {
                    Logger.Warning("Socket was destroyed during agent creation, destroying agent");
                    GameObject.Destroy(agent.gameObject);
                    return;
                }

                try
                {
                    agent.transform.SetParent(socket, worldPositionStays: false);
                    agent.transform.localRotation = Quaternion.identity;
                    agent.transform.localPosition = Vector3.zero;
                }
                catch (System.Exception ex)
                {
                    Logger.Error($"Failed to set agent parent: {ex.Message}");
                    GameObject.Destroy(agent.gameObject);
                }
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

                // 嘗試使用 Agent Prefab
                ItemAgent skinAgentPrefab = skinItem.AgentUtilities.GetPrefab(CharacterEquipmentController.equipmentModelHash);
                if (skinAgentPrefab != null)
                {
                    // 實例化外觀 Agent 來獲取視覺模型
                    ItemAgent skinAgent = UnityEngine.Object.Instantiate(skinAgentPrefab);

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

                    // 銷毀臨時 Agent
                    GameObject.Destroy(skinAgent.gameObject);
                }
                else if ((bool)skinItem.ItemGraphic)
                {
                    // 後備方案：使用 ItemGraphic
                    ItemGraphicInfo graphic = ItemGraphicInfo.CreateAGraphic(skinItem, agent.transform);
                    if (graphic != null)
                    {
                        graphic.transform.SetParent(agent.transform, false);
                        graphic.transform.localPosition = Vector3.zero;
                        graphic.transform.localRotation = Quaternion.identity;
                    }
                }
                else
                {
                    Logger.Warning($"Skin item {skinItemTypeID} has neither AgentPrefab nor ItemGraphic");
                }

                // 銷毀臨時 Item
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
        /// 診斷補丁：攔截所有裝備變更方法來找出實際被調用的方法
        /// </summary>
        [HarmonyPatch(typeof(CharacterEquipmentController), "ChangeArmorModel")]
        public static class DiagnosticPatch_ChangeArmorModel
        {
            [HarmonyPrefix]
            public static void Prefix(Slot slot)
            {
                Logger.Debug($"🔍 DIAGNOSTIC: ChangeArmorModel called! Slot: {(slot != null ? "Valid" : "NULL")}, Content: {(slot?.Content != null ? slot.Content.TypeID.ToString() : "NULL")}");
            }
        }

        [HarmonyPatch(typeof(CharacterEquipmentController), "ChangeHelmatModel")]
        public static class DiagnosticPatch_ChangeHelmatModel
        {
            [HarmonyPrefix]
            public static void Prefix(Slot slot)
            {
                Logger.Debug($"🔍 DIAGNOSTIC: ChangeHelmatModel called! Slot: {(slot != null ? "Valid" : "NULL")}, Content: {(slot?.Content != null ? slot.Content.TypeID.ToString() : "NULL")}");
            }
        }

        [HarmonyPatch(typeof(CharacterEquipmentController), "ChangeHeadsetModel")]
        public static class DiagnosticPatch_ChangeHeadsetModel
        {
            [HarmonyPrefix]
            public static void Prefix(Slot slot)
            {
                Logger.Debug($"🔍 DIAGNOSTIC: ChangeHeadsetModel called! Slot: {(slot != null ? "Valid" : "NULL")}, Content: {(slot?.Content != null ? slot.Content.TypeID.ToString() : "NULL")}");
            }
        }

        [HarmonyPatch(typeof(CharacterEquipmentController), "ChangeBackpackModel")]
        public static class DiagnosticPatch_ChangeBackpackModel
        {
            [HarmonyPrefix]
            public static void Prefix(Slot slot)
            {
                Logger.Debug($"🔍 DIAGNOSTIC: ChangeBackpackModel called! Slot: {(slot != null ? "Valid" : "NULL")}, Content: {(slot?.Content != null ? slot.Content.TypeID.ToString() : "NULL")}");
            }
        }

        [HarmonyPatch(typeof(CharacterEquipmentController), "ChangeFaceMaskModel")]
        public static class DiagnosticPatch_ChangeFaceMaskModel
        {
            [HarmonyPrefix]
            public static void Prefix(Slot slot)
            {
                Logger.Debug($"🔍 DIAGNOSTIC: ChangeFaceMaskModel called! Slot: {(slot != null ? "Valid" : "NULL")}, Content: {(slot?.Content != null ? slot.Content.TypeID.ToString() : "NULL")}");
            }
        }

        [HarmonyPatch(typeof(CharacterEquipmentController), "ChangeEquipmentModel")]
        public static class DiagnosticPatch_ChangeEquipmentModel
        {
            [HarmonyPrefix]
            public static void Prefix(Slot slot, Transform socket)
            {
                Logger.Debug($"🔍 DIAGNOSTIC: ChangeEquipmentModel called! Slot: {(slot != null ? "Valid" : "NULL")}, Content: {(slot?.Content != null ? slot.Content.TypeID.ToString() : "NULL")}, Socket: {(socket != null ? socket.name : "NULL")}");
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

                // 關鍵：先檢查是否為有效的遊戲場景
                if (!IsValidGameplayScene())
                {
                    Logger.Warning("ForceRefreshAllEquipment: Not a valid gameplay scene, aborting refresh");
                    return;
                }

                _isRefreshingAll = true; // 設置標記，避免 ForceRefreshMouthVisibility 重複渲染耳機

                // 刷新玩家裝備
                var mainCharacter = LevelManager.Instance?.MainCharacter;
                if (mainCharacter != null)
                {
                    Logger.Info("Refreshing Player equipment...");
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
                    Logger.Info("Refreshing Pet equipment...");
                    RefreshCharacterEquipment(petCharacter, "Pet");
                }
                else
                {
                    Logger.Info("PetCharacter is null (no pet in current scene)");
                }

                Logger.Info("✅ === All equipment refreshed ===");
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
                Traverse.Create(controller).Method("ChangeArmorModel", new object[] { armorSlot }).GetValue();

            Logger.Debug($"[{characterName}] Triggering ChangeBackpackModel...");
            if (backpackSlot != null)
                Traverse.Create(controller).Method("ChangeBackpackModel", new object[] { backpackSlot }).GetValue();

            Logger.Debug($"[{characterName}] Triggering ChangeHelmatModel...");
            if (helmatSlot != null)
            {
                try
                {
                    Traverse.Create(controller).Method("ChangeHelmatModel", new object[] { helmatSlot }).GetValue();
                }
                catch (Exception ex)
                {
                    Logger.Error($"[{characterName}] Error calling ChangeHelmatModel: {ex.Message}");
                }
            }
            else
            {
                Logger.Warning($"[{characterName}] helmatSlot is null, skipping ChangeHelmatModel");
            }

            Logger.Debug($"[{characterName}] Triggering ChangeFaceMaskModel...");
            if (faceMaskSlot != null)
                Traverse.Create(controller).Method("ChangeFaceMaskModel", new object[] { faceMaskSlot }).GetValue();

            Logger.Debug($"[{characterName}] Triggering ChangeHeadsetModel...");
            if (headsetSlot != null)
                Traverse.Create(controller).Method("ChangeHeadsetModel", new object[] { headsetSlot }).GetValue();

            Logger.Info($"✅ {characterName} equipment refreshed");
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
