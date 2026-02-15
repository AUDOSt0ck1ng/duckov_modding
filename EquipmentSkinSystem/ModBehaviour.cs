using System;
using System.Collections;
using System.IO;
using HarmonyLib;
using ItemStatsSystem.Items;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace EquipmentSkinSystem
{
    /// <summary>
    /// 裝備外觀系統 Mod 主類
    /// </summary>
    public class ModBehaviour : Duckov.Modding.ModBehaviour
    {
        public static ModBehaviour? Instance { get; private set; }

        private SkinManagerUI? _skinManagerUI;
        private Harmony? _harmony;
        private const string HarmonyID = "com.equipmentskin.mod";

        void Awake()
        {
            Instance = this; // 設置單例

            Logger.Info("==========================================================");
            Logger.Info("=== Equipment Skin System Mod Loaded ===");
            Logger.Info("Version 1.1.12");
            Logger.Info("Awake called - Mod is loading...");
            Logger.Info("==========================================================");

            // 立即應用 Harmony patches，確保在遊戲開始前就攔截裝備渲染
            Logger.Info("Applying Harmony patches in Awake()...");
            ApplyHarmonyPatches();
        }

        void Start()
        {
            Logger.Info(">>> Start() method called <<<");
            Logger.Info("Starting initialization coroutine to avoid blocking Unity's main thread");
            StartCoroutine(InitializeAsync());
        }

        private IEnumerator InitializeAsync()
        {
            Logger.Info(">>> InitializeAsync() coroutine started <<<");

            // 等待一幀，讓 Unity 完成資源卸載
            yield return null;
            Logger.Info("Waited one frame, Unity resource unloading should be complete");

            Logger.Info("Step 1: ConfigReader.ReloadConfig()");
            try
            { ConfigReader.ReloadConfig(); }
            catch (Exception e) { Logger.Error("Step 1 failed", e); }

            Logger.Info("Step 2: InitializeDataManager()");
            try
            { InitializeDataManager(); }
            catch (Exception e) { Logger.Error("Step 2 failed", e); }

            // 再等一幀，確保資料管理器初始化完成
            yield return null;
            Logger.Info("Waited another frame after data manager initialization");

            Logger.Info("Step 3: Localization.Initialize()");
            try
            { Localization.Initialize(); }
            catch (Exception e) { Logger.Error("Step 3 failed", e); }

            Logger.Info("Step 4: Localization.SubscribeToGameLanguageChange()");
            try
            { Localization.SubscribeToGameLanguageChange(); }
            catch (Exception e) { Logger.Error("Step 4 failed", e); }

            Logger.Info("Step 5: ApplyHarmonyPatches()");
            try
            { ApplyHarmonyPatches(); }
            catch (Exception e) { Logger.Error("Step 5 failed", e); }

            Logger.Info("Step 6: Subscribe to LevelManager events");
            try
            {
                LevelManager.OnLevelInitialized += OnLevelInitialized;
                LevelManager.OnAfterLevelInitialized += OnAfterLevelInitialized;
            }
            catch (Exception e) { Logger.Error("Step 6 failed", e); }

            Logger.Info("Step 7: Subscribe to SceneManager events");
            try
            { SceneManager.sceneLoaded += OnSceneLoaded; }
            catch (Exception e) { Logger.Error("Step 7 failed", e); }

            Logger.Info("Initialization completed successfully!");
            try
            {
                KeyCode toggleKey = EquipmentSkinDataManager.Instance.AppSettings.GetToggleUIKey();
                Logger.Info($"Press {toggleKey} to open UI (UI will be created on first use)");
                Logger.Info($"Config file location: {Path.Combine(Application.persistentDataPath, "EquipmentSkinSystem", "skin_config.json")}");
            }
            catch (Exception e) { Logger.Error("Failed to get toggle key", e); }
        }

        /// <summary>
        /// 場景加載完成時觸發（僅記錄日誌）
        /// </summary>
        private void OnSceneLoaded(Scene scene, LoadSceneMode mode)
        {
            try
            {
                Logger.Debug($"Scene loaded: {scene.name}, mode: {mode}");
            }
            catch (Exception e)
            {
                Logger.Error("Error in OnSceneLoaded", e);
            }
        }

        /// <summary>
        /// 關卡初始化完成時觸發（僅記錄日誌）
        /// </summary>
        private void OnLevelInitialized()
        {
            try
            {
                Logger.Debug("Level initialized");
            }
            catch (Exception e)
            {
                Logger.Error("Error in OnLevelInitialized", e);
            }
        }

        /// <summary>
        /// 關卡初始化後觸發（唯一刷新裝備的時機）
        /// </summary>
        private void OnAfterLevelInitialized()
        {
            try
            {
                Logger.Debug("After level initialized, checking and refreshing equipment...");

                // 直接檢查物件並刷新，不用等待
                if (LevelManager.Instance != null)
                {
                    HarmonyPatches.ForceRefreshAllEquipment();
                }
                else
                {
                    Logger.Warning("LevelManager.Instance is null, cannot refresh equipment");
                }
            }
            catch (Exception e)
            {
                Logger.Error("Error in OnAfterLevelInitialized", e);
            }
        }

        /// <summary>
        /// 檢查裝備是否已加載
        /// </summary>

        // 移除了 ShouldRenderInScene 和 HasValidLevelConfig
        // 所有檢查統一在 HarmonyPatches.IsValidGameplayScene() 中進行

        void Update()
        {
            // 使用設定檔中的快捷鍵切換 UI
            KeyCode toggleKey = EquipmentSkinDataManager.Instance.AppSettings.GetToggleUIKey();
            if (Input.GetKeyDown(toggleKey))
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

                Type[] patchTypes =
                {
                    typeof(HarmonyPatches.ChangeEquipmentModelPatch),
                    typeof(HarmonyPatches.DiagnosticPatch_ChangeArmorModel),
                    typeof(HarmonyPatches.DiagnosticPatch_ChangeHelmatModel),
                    typeof(HarmonyPatches.DiagnosticPatch_ChangeHeadsetModel),
                    typeof(HarmonyPatches.DiagnosticPatch_ChangeBackpackModel),
                    typeof(HarmonyPatches.DiagnosticPatch_ChangeFaceMaskModel),
                    typeof(HarmonyPatches.DiagnosticPatch_ChangeEquipmentModel)
                };

                foreach (var patchType in patchTypes)
                {
                    var processor = _harmony.CreateClassProcessor(patchType);
                    processor.Patch();
                }

                Logger.Info($"✅ Harmony patches applied successfully");
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
            // 清除單例
            if (Instance == this)
            {
                Instance = null;
            }

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
