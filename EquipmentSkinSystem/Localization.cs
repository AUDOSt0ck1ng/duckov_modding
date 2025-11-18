using System;
using System.Collections.Generic;

namespace EquipmentSkinSystem
{
    /// <summary>
    /// 語言文字管理系統
    /// </summary>
    public static class Localization
    {
        private static Dictionary<string, Dictionary<string, string>>? _translations;
        private static string _currentLanguage = "zh-TW";

        /// <summary>
        /// 初始化語言系統
        /// </summary>
        public static void Initialize()
        {
            _currentLanguage = EquipmentSkinDataManager.Instance.AppSettings.Language;
            LoadTranslations();
        }

        /// <summary>
        /// 載入翻譯資料
        /// </summary>
        private static void LoadTranslations()
        {
            _translations = new Dictionary<string, Dictionary<string, string>>();

            // 繁體中文翻譯
            var zhTW = new Dictionary<string, string>
            {
                // 主 UI
                ["UI_Title"] = "裝備外觀",
                ["UI_Player"] = "鴨",
                ["UI_Pet"] = "狗",
                ["UI_Current"] = "當前: ",
                ["UI_Clear"] = "清空",
                ["UI_Save"] = "保存",
                ["UI_Reset"] = "重置",
                ["UI_Close"] = "關閉",

                // 裝備槽位名稱
                ["Slot_Armor"] = "護甲",
                ["Slot_Helmet"] = "頭盔",
                ["Slot_FaceMask"] = "面罩",
                ["Slot_Backpack"] = "背包",
                ["Slot_Headset"] = "耳機",

                // 設定面板
                ["Settings_Title"] = "功能設定",
                ["Settings_Tab_Log"] = "日誌設定",
                ["Settings_Tab_Language"] = "語言",
                
                // 日誌設定
                ["Log_Debug"] = "調試日誌 (Debug)",
                ["Log_Info"] = "資訊日誌 (Info)",
                ["Log_Warning"] = "警告日誌 (Warning)",
                ["Log_Error"] = "錯誤日誌 (Error)",

                // 語言設定
                ["Language_Select"] = "選擇語言",
                ["Language_TraditionalChinese"] = "繁體中文",
                ["Language_Note"] = "（其他選項將在未來版本中添加吧?呱呱）",
            };

            _translations["zh-TW"] = zhTW;

            // 英文翻譯
            var enUS = new Dictionary<string, string>
            {
                // 主 UI
                ["UI_Title"] = "Equipment Skin",
                ["UI_Player"] = "Duck",
                ["UI_Pet"] = "Dog",
                ["UI_Current"] = "Current: ",
                ["UI_Clear"] = "Clear",
                ["UI_Save"] = "Save",
                ["UI_Reset"] = "Reset",
                ["UI_Close"] = "Close",

                // 裝備槽位名稱
                ["Slot_Armor"] = "Armor",
                ["Slot_Helmet"] = "Helmet",
                ["Slot_FaceMask"] = "Face Mask",
                ["Slot_Backpack"] = "Backpack",
                ["Slot_Headset"] = "Headset",

                // 設定面板
                ["Settings_Title"] = "Settings",
                ["Settings_Tab_Log"] = "Log Settings",
                ["Settings_Tab_Language"] = "Language",
                
                // 日誌設定
                ["Log_Debug"] = "Debug Log",
                ["Log_Info"] = "Info Log",
                ["Log_Warning"] = "Warning Log",
                ["Log_Error"] = "Error Log",

                // 語言設定
                ["Language_Select"] = "Select Language",
                ["Language_TraditionalChinese"] = "Traditional Chinese",
                ["Language_English"] = "English",
                ["Language_Note"] = "(More options might be added in future? quack quack)",
            };

            _translations["en-US"] = enUS;
        }

        /// <summary>
        /// 獲取翻譯文字
        /// </summary>
        public static string Get(string key, string defaultValue = "")
        {
            try
            {
                if (_translations == null)
                {
                    LoadTranslations();
                }

                if (_translations != null && 
                    _translations.TryGetValue(_currentLanguage, out var langDict) &&
                    langDict.TryGetValue(key, out var value))
                {
                    return value;
                }

                // 如果找不到翻譯，嘗試使用後備語言
                // 優先使用 zh-TW，如果 zh-TW 也沒有則使用 en-US
                if (_currentLanguage != "zh-TW" && 
                    _translations != null)
                {
                    if (_translations.TryGetValue("zh-TW", out var fallbackDict) &&
                        fallbackDict.TryGetValue(key, out var fallbackValue))
                    {
                        return fallbackValue;
                    }
                    
                    if (_translations.TryGetValue("en-US", out var enDict) &&
                        enDict.TryGetValue(key, out var enValue))
                    {
                        return enValue;
                    }
                }

                return defaultValue;
            }
            catch
            {
                return defaultValue;
            }
        }

        /// <summary>
        /// 設置當前語言
        /// </summary>
        public static void SetLanguage(string languageCode)
        {
            _currentLanguage = languageCode;
            EquipmentSkinDataManager.Instance.AppSettings.Language = languageCode;
            DataPersistence.SaveConfig();
        }

        /// <summary>
        /// 獲取當前語言
        /// </summary>
        public static string GetCurrentLanguage()
        {
            return _currentLanguage;
        }
    }
}

