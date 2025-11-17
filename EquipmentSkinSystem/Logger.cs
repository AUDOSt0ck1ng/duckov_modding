using UnityEngine;

namespace EquipmentSkinSystem
{
    /// <summary>
    /// 統一的日誌管理系統
    /// 可透過 AppSettings 控制是否輸出日誌
    /// </summary>
    public static class Logger
    {
        private const string PREFIX = "[EquipmentSkinSystem]";

        /// <summary>
        /// 是否啟用調試日誌（從 AppSettings 讀取，如果未初始化則使用預設值 false）
        /// </summary>
        public static bool EnableDebugLog => GetSettingValue(() => EquipmentSkinDataManager.Instance.AppSettings.EnableDebugLog, false);

        /// <summary>
        /// 是否啟用資訊日誌（從 AppSettings 讀取，如果未初始化則使用預設值 true）
        /// </summary>
        public static bool EnableInfoLog => GetSettingValue(() => EquipmentSkinDataManager.Instance.AppSettings.EnableInfoLog, true);

        /// <summary>
        /// 是否啟用警告日誌（從 AppSettings 讀取，如果未初始化則使用預設值 true）
        /// </summary>
        public static bool EnableWarningLog => GetSettingValue(() => EquipmentSkinDataManager.Instance.AppSettings.EnableWarningLog, true);

        /// <summary>
        /// 是否啟用錯誤日誌（從 AppSettings 讀取，如果未初始化則使用預設值 true）
        /// </summary>
        public static bool EnableErrorLog => GetSettingValue(() => EquipmentSkinDataManager.Instance.AppSettings.EnableErrorLog, true);

        /// <summary>
        /// 安全地獲取設定值，如果 AppSettings 尚未初始化則使用預設值
        /// </summary>
        private static bool GetSettingValue(System.Func<bool> getter, bool defaultValue)
        {
            try
            {
                return getter();
            }
            catch
            {
                // 如果 AppSettings 尚未初始化，使用預設值
                return defaultValue;
            }
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
