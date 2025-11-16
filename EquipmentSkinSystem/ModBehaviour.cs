using System;
using UnityEngine;
using HarmonyLib;

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
            Debug.Log("==========================================================");
            Debug.Log("=== Equipment Skin System Mod Loaded ===");
            Debug.Log("[EquipmentSkinSystem] Version 1.0.0");
            Debug.Log("[EquipmentSkinSystem] Awake called - Mod is loading...");
            Debug.Log("==========================================================");
        }

        void Start()
        {
            try
            {
                // 初始化數據管理器
                InitializeDataManager();

                // 應用 Harmony 補丁
                ApplyHarmonyPatches();

                Debug.Log("[EquipmentSkinSystem] Initialization completed successfully!");
                Debug.Log("[EquipmentSkinSystem] Press F7 to open UI (UI will be created on first use)");
            }
            catch (Exception e)
            {
                Debug.LogError($"[EquipmentSkinSystem] Initialization failed: {e.Message}");
                Debug.LogError($"[EquipmentSkinSystem] Stack trace: {e.StackTrace}");
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
                Debug.Log("[EquipmentSkinSystem] Data manager initialized");
            }
            catch (Exception e)
            {
                Debug.LogError($"[EquipmentSkinSystem] Failed to initialize data manager: {e.Message}");
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

                Debug.Log("[EquipmentSkinSystem] UI initialized");
            }
            catch (Exception e)
            {
                Debug.LogError($"[EquipmentSkinSystem] Failed to initialize UI: {e.Message}");
            }
        }

        private void ApplyHarmonyPatches()
        {
            try
            {
                _harmony = new Harmony(HarmonyID);
                
                // 應用所有補丁（使用 GetExecutingAssembly 避免反射問題）
                _harmony.PatchAll(System.Reflection.Assembly.GetExecutingAssembly());
                
                Debug.Log("[EquipmentSkinSystem] Harmony patches applied successfully");
                
                // 列出所有已應用的補丁
                var patchedMethods = _harmony.GetPatchedMethods();
                int patchCount = 0;
                foreach (var method in patchedMethods)
                {
                    patchCount++;
                    Debug.Log($"[EquipmentSkinSystem] Patched: {method.DeclaringType?.Name}.{method.Name}");
                }
                Debug.Log($"[EquipmentSkinSystem] Total patches applied: {patchCount}");
            }
            catch (Exception e)
            {
                Debug.LogError($"[EquipmentSkinSystem] Failed to apply Harmony patches: {e.Message}");
                Debug.LogError($"[EquipmentSkinSystem] Stack trace: {e.StackTrace}");
            }
        }

        private void ToggleUI()
        {
            try
            {
                // 延遲初始化 UI（第一次按 F7 時才創建）
                if (_skinManagerUI == null)
                {
                    Debug.Log("[EquipmentSkinSystem] Creating UI for the first time...");
                    InitializeUI();
                }
                
                if (_skinManagerUI != null)
                {
                    _skinManagerUI.ToggleUI();
                    Debug.Log("[EquipmentSkinSystem] UI toggled");
                }
            }
            catch (Exception e)
            {
                Debug.LogError($"[EquipmentSkinSystem] Error toggling UI: {e.Message}");
                Debug.LogError($"[EquipmentSkinSystem] Stack trace: {e.StackTrace}");
            }
        }

        void OnDestroy()
        {
            try
            {
                // 保存配置
                DataPersistence.SaveConfig();

                // 移除 Harmony 補丁
                if (_harmony != null)
                {
                    _harmony.UnpatchAll(HarmonyID);
                    Debug.Log("[EquipmentSkinSystem] Harmony patches removed");
                }

                Debug.Log("[EquipmentSkinSystem] Mod unloaded");
            }
            catch (Exception e)
            {
                Debug.LogError($"[EquipmentSkinSystem] Error during cleanup: {e.Message}");
            }
        }

        void OnApplicationQuit()
        {
            // 確保在遊戲退出時保存配置
            DataPersistence.SaveConfig();
        }
    }
}

