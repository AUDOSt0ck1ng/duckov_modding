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
