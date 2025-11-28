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
                    Logger.Debug($"Created directory: {path}");
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
                Logger.Debug("SaveConfig called");
                Logger.Debug($"Save directory: {SaveDirectory}");
                Logger.Debug($"Save file path: {SaveFilePath}");

                string json = EquipmentSkinDataManager.Instance.SaveToJson();
                Logger.Debug($"JSON length: {json?.Length ?? 0}");
                Logger.Debug($"JSON content: {json}");

                File.WriteAllText(SaveFilePath, json);
                Logger.Info($"Configuration saved to: {SaveFilePath}");

                // 驗證文件是否真的存在
                if (File.Exists(SaveFilePath))
                {
                    string verifyJson = File.ReadAllText(SaveFilePath);
                    Logger.Info($"Verified file exists, size: {verifyJson.Length}");
                }
                else
                {
                    Logger.Error("❌ File does not exist after save!");
                }
            }
            catch (Exception e)
            {
                Logger.Error("Failed to save configuration", e);
                Logger.Error($"Stack trace: {e.StackTrace}");
            }
        }

        /// <summary>
        /// 從文件載入配置
        /// </summary>
        public static void LoadConfig()
        {
            try
            {
                Logger.Debug("LoadConfig called");
                Logger.Debug($"Looking for file: {SaveFilePath}");

                if (File.Exists(SaveFilePath))
                {
                    string json = File.ReadAllText(SaveFilePath);
                    Logger.Debug($"Loaded JSON length: {json.Length}");
                    Logger.Debug($"Loaded JSON content: {json}");

                    EquipmentSkinDataManager.Instance.LoadFromJson(json);
                    Logger.Info($"Configuration loaded from: {SaveFilePath}");
                }
                else
                {
                    Logger.Info($"No saved configuration found at: {SaveFilePath}");
                    Logger.Debug("Using default configuration and saving it for the first time.");

                    // 第一次載入時，使用預設配置並保存
                    EquipmentSkinDataManager.Instance.LoadFromJson(""); // 這會創建預設配置
                    SaveConfig(); // 保存預設配置
                    Logger.Info("Default configuration saved for the first time.");
                }
            }
            catch (Exception e)
            {
                Logger.Error("Failed to load configuration", e);
                Logger.Error($"Stack trace: {e.StackTrace}");
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
                    Logger.Debug("Configuration deleted.");
                }
            }
            catch (Exception e)
            {
                Logger.Error("Failed to delete configuration", e);
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
