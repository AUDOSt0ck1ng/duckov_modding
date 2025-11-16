using UnityEngine;

namespace EquipmentSkinSystem
{
    /// <summary>
    /// 統一的日誌管理系統
    /// 可透過配置文件控制是否輸出日誌
    /// </summary>
    public static class Logger
    {
        private const string PREFIX = "[EquipmentSkinSystem]";
        private static bool _configLoaded = false;

        /// <summary>
        /// 是否啟用調試日誌（從配置文件讀取）
        /// </summary>
        public static bool EnableDebugLog => GetConfigValue("EnableDebugLog", false);

        /// <summary>
        /// 是否啟用資訊日誌（從配置文件讀取）
        /// </summary>
        public static bool EnableInfoLog => GetConfigValue("EnableInfoLog", true);

        /// <summary>
        /// 是否啟用警告日誌（從配置文件讀取）
        /// </summary>
        public static bool EnableWarningLog => GetConfigValue("EnableWarningLog", true);

        /// <summary>
        /// 是否啟用錯誤日誌（從配置文件讀取）
        /// </summary>
        public static bool EnableErrorLog => GetConfigValue("EnableErrorLog", true);

        /// <summary>
        /// 從配置文件讀取布林值
        /// </summary>
        private static bool GetConfigValue(string key, bool defaultValue)
        {
            if (!_configLoaded)
            {
                // 延遲載入配置，避免在靜態構造時出錯
                _configLoaded = true;
            }
            return ConfigReader.GetBool("Logger", key, defaultValue);
        }

        /// <summary>
        /// 調試日誌（詳細的執行流程）
        /// </summary>
        public static void Debug(string message)
        {
            if (EnableDebugLog)
            {
                UnityEngine.Debug.Log($"{PREFIX} {message}");
            }
        }

        /// <summary>
        /// 資訊日誌（重要的狀態變更）
        /// </summary>
        public static void Info(string message)
        {
            if (EnableInfoLog)
            {
                UnityEngine.Debug.Log($"{PREFIX} ✅ {message}");
            }
        }

        /// <summary>
        /// 警告日誌（可能的問題）
        /// </summary>
        public static void Warning(string message)
        {
            if (EnableWarningLog)
            {
                UnityEngine.Debug.LogWarning($"{PREFIX} ⚠️ {message}");
            }
        }

        /// <summary>
        /// 錯誤日誌（必須輸出）
        /// </summary>
        public static void Error(string message)
        {
            if (EnableErrorLog)
            {
                UnityEngine.Debug.LogError($"{PREFIX} ❌ {message}");
            }
        }

        /// <summary>
        /// 錯誤日誌（帶堆疊追蹤）
        /// </summary>
        public static void Error(string message, System.Exception exception)
        {
            if (EnableErrorLog)
            {
                UnityEngine.Debug.LogError($"{PREFIX} ❌ {message}");
                UnityEngine.Debug.LogError($"{PREFIX} Exception: {exception.Message}");
                UnityEngine.Debug.LogError($"{PREFIX} Stack trace: {exception.StackTrace}");
            }
        }
    }
}
