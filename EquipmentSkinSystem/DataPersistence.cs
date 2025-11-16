using System;
using System.IO;
using UnityEngine;

namespace EquipmentSkinSystem
{
    /// <summary>
    /// 數據持久化管理器
    /// 負責保存和載入裝備外觀配置到本地文件
    /// </summary>
    public class DataPersistence
    {
        private static string SaveDirectory
        {
            get
            {
                // Path.Combine 會自動處理跨平台路徑分隔符
                string path = Path.Combine(Application.persistentDataPath, "EquipmentSkinSystem");
                
                if (!Directory.Exists(path))
                {
                    Directory.CreateDirectory(path);
                    Debug.Log($"[EquipmentSkinSystem] Created directory: {path}");
                }
                return path;
            }
        }

        private static string SaveFilePath => Path.Combine(SaveDirectory, "skin_config.json");

        /// <summary>
        /// 保存當前配置到文件
        /// </summary>
        public static void SaveConfig()
        {
            try
            {
                Debug.Log($"[EquipmentSkinSystem] SaveConfig called");
                Debug.Log($"[EquipmentSkinSystem] Save directory: {SaveDirectory}");
                Debug.Log($"[EquipmentSkinSystem] Save file path: {SaveFilePath}");
                
                string json = EquipmentSkinDataManager.Instance.SaveToJson();
                Debug.Log($"[EquipmentSkinSystem] JSON length: {json?.Length ?? 0}");
                Debug.Log($"[EquipmentSkinSystem] JSON content: {json}");
                
                File.WriteAllText(SaveFilePath, json);
                Debug.Log($"[EquipmentSkinSystem] ✅ Configuration saved to: {SaveFilePath}");
                
                // 驗證文件是否真的存在
                if (File.Exists(SaveFilePath))
                {
                    string verifyJson = File.ReadAllText(SaveFilePath);
                    Debug.Log($"[EquipmentSkinSystem] ✅ Verified file exists, size: {verifyJson.Length}");
                }
                else
                {
                    Debug.LogError($"[EquipmentSkinSystem] ❌ File does not exist after save!");
                }
            }
            catch (Exception e)
            {
                Debug.LogError($"[EquipmentSkinSystem] Failed to save configuration: {e.Message}");
                Debug.LogError($"[EquipmentSkinSystem] Stack trace: {e.StackTrace}");
            }
        }

        /// <summary>
        /// 從文件載入配置
        /// </summary>
        public static void LoadConfig()
        {
            try
            {
                string filePath = SaveFilePath.Replace('\\', '/');
                Debug.Log($"[EquipmentSkinSystem] LoadConfig called");
                Debug.Log($"[EquipmentSkinSystem] Looking for file: {filePath}");
                
                if (File.Exists(SaveFilePath))
                {
                    string json = File.ReadAllText(SaveFilePath);
                    Debug.Log($"[EquipmentSkinSystem] Loaded JSON length: {json.Length}");
                    Debug.Log($"[EquipmentSkinSystem] Loaded JSON content: {json}");
                    
                    EquipmentSkinDataManager.Instance.LoadFromJson(json);
                    Debug.Log($"[EquipmentSkinSystem] ✅ Configuration loaded from: {filePath}");
                }
                else
                {
                    Debug.Log($"[EquipmentSkinSystem] ❌ No saved configuration found at: {filePath}");
                    Debug.Log("[EquipmentSkinSystem] Using default configuration.");
                }
            }
            catch (Exception e)
            {
                Debug.LogError($"[EquipmentSkinSystem] Failed to load configuration: {e.Message}");
                Debug.LogError($"[EquipmentSkinSystem] Stack trace: {e.StackTrace}");
            }
        }

        /// <summary>
        /// 刪除保存的配置文件
        /// </summary>
        public static void DeleteConfig()
        {
            try
            {
                if (File.Exists(SaveFilePath))
                {
                    File.Delete(SaveFilePath);
                    Debug.Log("[EquipmentSkinSystem] Configuration deleted.");
                }
            }
            catch (Exception e)
            {
                Debug.LogError($"[EquipmentSkinSystem] Failed to delete configuration: {e.Message}");
            }
        }

        /// <summary>
        /// 檢查是否存在保存的配置
        /// </summary>
        public static bool HasSavedConfig()
        {
            return File.Exists(SaveFilePath);
        }
    }
}

