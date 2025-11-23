using System;
using System.Collections;
using System.IO;
using UnityEngine;
using UnityEngine.SceneManagement;
using HarmonyLib;
using ItemStatsSystem.Items;

namespace EquipmentSkinSystem
{
    /// <summary>
    /// 裝備外觀系統 Mod 主類
    /// </summary>
    public class ModBehaviour : Duckov.Modding.ModBehaviour
    {
        private SkinManagerUI? _skinManagerUI;
        private Harmony? _harmony;
        private const string HarmonyID = "com.equipmentskin.mod";

        void Awake()
        {
            Logger.Info("==========================================================");
            Logger.Info("=== Equipment Skin System Mod Loaded ===");
            Logger.Info("Version 1.0.0");
            Logger.Info("Awake called - Mod is loading...");
            Logger.Info("==========================================================");
        }

        void Start()
        {
            try
            {
                // 初始化配置讀取器（確保配置文件存在）
                ConfigReader.ReloadConfig();

                // 初始化數據管理器
                InitializeDataManager();

                // 初始化語言系統
                Localization.Initialize();

                // 訂閱遊戲的語言變更事件
                Localization.SubscribeToGameLanguageChange();

                // 應用 Harmony 補丁
                ApplyHarmonyPatches();

                // 訂閱關卡初始化完成事件，自動刷新裝備
                LevelManager.OnLevelInitialized += OnLevelInitialized;
                LevelManager.OnAfterLevelInitialized += OnAfterLevelInitialized;

                // 訂閱場景加載事件（用於同副本內換圖）
                SceneManager.sceneLoaded += OnSceneLoaded;

                Logger.Info("Initialization completed successfully!");
                Logger.Info("Press F7 to open UI (UI will be created on first use)");
                Logger.Info($"Config file location: {Path.Combine(Application.persistentDataPath, "EquipmentSkinSystem", "skin_config.json")}");
            }
            catch (Exception e)
            {
                Logger.Error("Initialization failed", e);
            }
        }

        /// <summary>
        /// 場景加載完成時觸發（用於同副本內換圖）
        /// </summary>
        private void OnSceneLoaded(Scene scene, LoadSceneMode mode)
        {
            try
            {
                Logger.Debug($"Scene loaded: {scene.name}, mode: {mode}");
                // 延遲刷新，確保場景完全加載
                StartCoroutine(WaitForEquipmentAndRefresh());
            }
            catch (Exception e)
            {
                Logger.Error("Error in OnSceneLoaded", e);
            }
        }

        /// <summary>
        /// 關卡初始化完成時觸發
        /// </summary>
        private void OnLevelInitialized()
        {
            try
            {
                Logger.Debug("Level initialized, waiting for equipment to load...");
                // 使用協程等待裝備加載完成後再刷新
                StartCoroutine(WaitForEquipmentAndRefresh());
            }
            catch (Exception e)
            {
                Logger.Error("Error in OnLevelInitialized", e);
            }
        }

        /// <summary>
        /// 關卡初始化後觸發（更晚的時機）
        /// </summary>
        private void OnAfterLevelInitialized()
        {
            try
            {
                Logger.Debug("After level initialized, waiting for equipment to load...");
                // 再次嘗試刷新，確保裝備正確渲染
                StartCoroutine(WaitForEquipmentAndRefresh());
            }
            catch (Exception e)
            {
                Logger.Error("Error in OnAfterLevelInitialized", e);
            }
        }

        /// <summary>
        /// 等待裝備加載完成後刷新
        /// </summary>
        private IEnumerator WaitForEquipmentAndRefresh()
        {
            // 先等待一小段時間，讓角色初始化
            yield return new WaitForSeconds(0.5f);
            
            // 最多等待 5 秒，每 0.2 秒檢查一次裝備是否已加載
            int maxAttempts = 25;
            int attempts = 0;
            
            while (attempts < maxAttempts)
            {
                bool equipmentReady = CheckEquipmentReady();
                
                if (equipmentReady)
                {
                    try
                    {
                        HarmonyPatches.ForceRefreshAllEquipment();
                        Logger.Info("Equipment refreshed after level initialization");
                        yield break; // 裝備已加載，退出協程
                    }
                    catch (Exception e)
                    {
                        Logger.Error("Error refreshing equipment after level initialization", e);
                        yield break;
                    }
                }
                
                attempts++;
                yield return new WaitForSeconds(0.2f);
            }
            
            // 如果超時，仍然嘗試刷新一次（可能裝備已經加載但檢查失敗）
            try
            {
                Logger.Warning("Equipment loading timeout, attempting refresh anyway...");
                HarmonyPatches.ForceRefreshAllEquipment();
            }
            catch (Exception e)
            {
                Logger.Error("Error refreshing equipment after timeout", e);
            }
        }

        /// <summary>
        /// 檢查裝備是否已加載
        /// </summary>
        private bool CheckEquipmentReady()
        {
            try
            {
                var mainCharacter = LevelManager.Instance?.MainCharacter;
                if (mainCharacter == null) return false;

                var controller = mainCharacter.GetComponent<CharacterEquipmentController>();
                if (controller == null) return false;

                // 檢查至少有一個槽位有裝備
                var armorSlot = HarmonyLib.Traverse.Create(controller).Field("armorSlot").GetValue<Slot>();
                var helmatSlot = HarmonyLib.Traverse.Create(controller).Field("helmatSlot").GetValue<Slot>();
                var faceMaskSlot = HarmonyLib.Traverse.Create(controller).Field("faceMaskSlot").GetValue<Slot>();
                var backpackSlot = HarmonyLib.Traverse.Create(controller).Field("backpackSlot").GetValue<Slot>();
                var headsetSlot = HarmonyLib.Traverse.Create(controller).Field("headsetSlot").GetValue<Slot>();

                // 至少有一個槽位有裝備就認為已加載
                return (armorSlot != null && armorSlot.Content != null) ||
                       (helmatSlot != null && helmatSlot.Content != null) ||
                       (faceMaskSlot != null && faceMaskSlot.Content != null) ||
                       (backpackSlot != null && backpackSlot.Content != null) ||
                       (headsetSlot != null && headsetSlot.Content != null);
            }
            catch
            {
                return false;
            }
        }

        void Update()
        {
            // 按 F7 鍵切換 UI
            if (Input.GetKeyDown(KeyCode.F7))
            {
                ToggleUI();
            }
        }

        private void InitializeDataManager()
        {
            try
            {
                // 載入保存的配置
                DataPersistence.LoadConfig();
                Logger.Info("Data manager initialized");
            }
            catch (Exception e)
            {
                Logger.Error("Failed to initialize data manager", e);
            }
        }

        private void InitializeUI()
        {
            try
            {
                GameObject uiManagerObj = new GameObject("EquipmentSkinUIManager");
                DontDestroyOnLoad(uiManagerObj);
                uiManagerObj.transform.SetParent(transform);

                _skinManagerUI = uiManagerObj.AddComponent<SkinManagerUI>();
                _skinManagerUI.Initialize();

                Logger.Info("UI initialized");
            }
            catch (Exception e)
            {
                Logger.Error("Failed to initialize UI", e);
            }
        }

        private void ApplyHarmonyPatches()
        {
            try
            {
                _harmony = new Harmony(HarmonyID);

                // 僅使用 Type 與 Harmony 的 CreateClassProcessor，避免在舊版 Mono 上存取 System.Reflection.Assembly
                Type[] patchTypes =
                {
                    typeof(HarmonyPatches.ChangeEquipmentModelPatch),
                    typeof(HarmonyPatches.EquipmentChangeLogger)
                };

                int patchCount = 0;
                foreach (var patchType in patchTypes)
                {
                    var processor = _harmony.CreateClassProcessor(patchType);
                    processor.Patch();
                    patchCount++;
                    Logger.Debug($"Patched class: {patchType.FullName}");
                }

                if (patchCount == 0)
                {
                    Logger.Warning("No Harmony patches were applied. Please verify target CLR support.");
                }
                else
                {
                    Logger.Info($"Total patches applied: {patchCount}");
                }
            }
            catch (Exception e)
            {
                Logger.Error("Failed to apply Harmony patches", e);
            }
        }

        private void ToggleUI()
        {
            try
            {
                // 延遲初始化 UI（第一次按 F7 時才創建）
                if (_skinManagerUI == null)
                {
                    Logger.Debug("Creating UI for the first time...");
                    InitializeUI();
                }
                
                if (_skinManagerUI != null)
                {
                    _skinManagerUI.ToggleUI();
                    Logger.Debug("UI toggled");
                }
            }
            catch (Exception e)
            {
                Logger.Error("Error toggling UI", e);
            }
        }

        void OnDestroy()
        {
            try
            {
                // 取消訂閱事件
                LevelManager.OnLevelInitialized -= OnLevelInitialized;
                LevelManager.OnAfterLevelInitialized -= OnAfterLevelInitialized;
                SceneManager.sceneLoaded -= OnSceneLoaded;

                // 保存配置
                DataPersistence.SaveConfig();

                // 移除 Harmony 補丁
                if (_harmony != null)
                {
                    _harmony.UnpatchAll(HarmonyID);
                    Logger.Info("Harmony patches removed");
                }

                Logger.Info("Mod unloaded");
            }
            catch (Exception e)
            {
                Logger.Error("Error during cleanup", e);
            }
        }

        void OnApplicationQuit()
        {
            // 確保在遊戲退出時保存配置
            DataPersistence.SaveConfig();
        }
    }
}
