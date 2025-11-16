using System;
using System.Collections.Generic;
using System.IO;
using UnityEngine;

namespace EquipmentSkinSystem
{
    /// <summary>
    /// INI 配置文件讀取器
    /// </summary>
    public static class ConfigReader
    {
        private static Dictionary<string, Dictionary<string, string>>? _configCache;
        
        /// <summary>
        /// 獲取 info.ini 文件路徑
        /// info.ini 和 DLL 在同一目錄（Mod 文件夾根目錄）
        /// </summary>
        private static string GetConfigFilePath()
        {
            // Mod 文件夾路徑：Duckov_Data/Mods/EquipmentSkinSystem/
            // info.ini 就在這個目錄下
            string modDirectory = Path.Combine(Application.dataPath, "Mods", "EquipmentSkinSystem");
            string configPath = Path.Combine(modDirectory, "info.ini");
            
            // 如果文件存在，直接返回
            if (File.Exists(configPath))
            {
                return configPath;
            }
            
            // 如果不存在，返回這個路徑用於創建
            return configPath;
        }
        
        private static string ConfigFilePath => GetConfigFilePath();

        /// <summary>
        /// 讀取配置值
        /// </summary>
        public static string GetValue(string section, string key, string defaultValue = "")
        {
            try
            {
                if (_configCache == null)
                {
                    LoadConfig();
                }

                if (_configCache != null && 
                    _configCache.TryGetValue(section, out var sectionDict) &&
                    sectionDict.TryGetValue(key, out var value))
                {
                    return value;
                }

                return defaultValue;
            }
            catch (Exception e)
            {
                UnityEngine.Debug.LogError($"[EquipmentSkinSystem] Error reading config: {e.Message}");
                return defaultValue;
            }
        }

        /// <summary>
        /// 讀取布林值
        /// </summary>
        public static bool GetBool(string section, string key, bool defaultValue = false)
        {
            string value = GetValue(section, key, defaultValue.ToString().ToLower());
            return value.ToLower() == "true" || value == "1" || value == "yes";
        }

        /// <summary>
        /// 讀取整數值
        /// </summary>
        public static int GetInt(string section, string key, int defaultValue = 0)
        {
            string value = GetValue(section, key, defaultValue.ToString());
            if (int.TryParse(value, out int result))
            {
                return result;
            }
            return defaultValue;
        }

        /// <summary>
        /// 載入配置文件
        /// </summary>
        private static void LoadConfig()
        {
            _configCache = new Dictionary<string, Dictionary<string, string>>();
            
            string filePath = ConfigFilePath;
            if (!File.Exists(filePath))
            {
                // 如果配置文件不存在，創建默認配置
                CreateDefaultConfig(filePath);
                return;
            }

            try
            {
                string currentSection = "";
                string[] lines = File.ReadAllLines(filePath);

                foreach (string line in lines)
                {
                    string trimmedLine = line.Trim();
                    
                    // 跳過空行和註釋
                    if (string.IsNullOrEmpty(trimmedLine) || trimmedLine.StartsWith(";") || trimmedLine.StartsWith("#"))
                        continue;

                    // 檢查是否是節（section）
                    if (trimmedLine.StartsWith("[") && trimmedLine.EndsWith("]"))
                    {
                        currentSection = trimmedLine.Substring(1, trimmedLine.Length - 2).Trim();
                        if (!_configCache.ContainsKey(currentSection))
                        {
                            _configCache[currentSection] = new Dictionary<string, string>();
                        }
                    }
                    // 檢查是否是鍵值對
                    else if (trimmedLine.Contains("="))
                    {
                        int equalIndex = trimmedLine.IndexOf('=');
                        string key = trimmedLine.Substring(0, equalIndex).Trim();
                        string value = trimmedLine.Substring(equalIndex + 1).Trim();

                        if (!string.IsNullOrEmpty(currentSection))
                        {
                            if (!_configCache.ContainsKey(currentSection))
                            {
                                _configCache[currentSection] = new Dictionary<string, string>();
                            }
                            _configCache[currentSection][key] = value;
                        }
                    }
                }
            }
            catch (Exception e)
            {
                UnityEngine.Debug.LogError($"[EquipmentSkinSystem] Error loading config file: {e.Message}");
            }
        }

        /// <summary>
        /// 創建或更新配置文件
        /// </summary>
        private static void CreateDefaultConfig(string filePath)
        {
            try
            {
                string directory = Path.GetDirectoryName(filePath);
                if (!string.IsNullOrEmpty(directory) && !Directory.Exists(directory))
                {
                    Directory.CreateDirectory(directory);
                }

                string existingContent = "";
                bool hasLoggerSection = false;

                // 如果文件已存在，讀取現有內容
                if (File.Exists(filePath))
                {
                    existingContent = File.ReadAllText(filePath);
                    hasLoggerSection = existingContent.Contains("[Logger]");
                }

                // 如果沒有 Logger 節，添加它
                if (!hasLoggerSection)
                {
                    string loggerSection = @"

[Logger]
; 是否啟用調試日誌（詳細的執行流程）
EnableDebugLog = false

; 是否啟用資訊日誌（重要的狀態變更）
EnableInfoLog = true

; 是否啟用警告日誌（可能的問題）
EnableWarningLog = true

; 是否啟用錯誤日誌（必須輸出）
EnableErrorLog = true
";

                    // 如果文件不存在，創建完整的默認配置
                    if (string.IsNullOrEmpty(existingContent))
                    {
                        existingContent = @"name = EquipmentSkinSystem

displayName = 裝備外觀系統

description = 讓你的角色實際裝備和外觀裝備分離！按 F7 打開管理界面，自由設定每個裝備槽位的外觀。
";
                    }
                    else
                    {
                        // 確保現有內容以換行結尾
                        if (!existingContent.EndsWith("\n") && !existingContent.EndsWith("\r\n"))
                        {
                            existingContent += "\n";
                        }
                    }

                    existingContent += loggerSection;
                    File.WriteAllText(filePath, existingContent);
                    UnityEngine.Debug.Log($"[EquipmentSkinSystem] Added Logger section to config file: {filePath}");
                }
            }
            catch (Exception e)
            {
                UnityEngine.Debug.LogError($"[EquipmentSkinSystem] Error creating/updating config: {e.Message}");
            }
        }

        /// <summary>
        /// 重新載入配置（用於運行時更新）
        /// </summary>
        public static void ReloadConfig()
        {
            _configCache = null;
            LoadConfig();
        }
    }
}

