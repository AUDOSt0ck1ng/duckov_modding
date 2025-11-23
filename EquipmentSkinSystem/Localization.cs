using System;
using System.Collections.Generic;
using System.Linq;
using HarmonyLib;
using UnityEngine;

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
        /// 語言變更事件
        /// </summary>
        public static event System.Action<string>? OnLanguageChanged;

        /// <summary>
        /// 初始化語言系統
        /// </summary>
        public static void Initialize()
        {
            // 優先從遊戲本身取得語言設定
            string gameLanguage = GetGameLanguage();
            if (!string.IsNullOrEmpty(gameLanguage))
            {
                _currentLanguage = gameLanguage;
                Logger.Info($"Language initialized from game: {_currentLanguage}");
            }
            else
            {
                // 如果無法從遊戲取得，則使用保存的設定
                _currentLanguage = EquipmentSkinDataManager.Instance.AppSettings.Language;
                Logger.Info($"Language initialized from saved settings: {_currentLanguage}");
            }
            LoadTranslations();
        }

        /// <summary>
        /// 從遊戲的 LocalizationManager 取得當前語言
        /// </summary>
        private static string GetGameLanguage()
        {
            try
            {
                // 使用 Harmony Traverse 存取遊戲的 LocalizationManager
                // 遊戲的命名空間是 SodaCraft.Localizations.LocalizationManager
                var localizationManagerType = AccessTools.TypeByName("SodaCraft.Localizations.LocalizationManager");
                if (localizationManagerType == null)
                {
                    Logger.Warning("Cannot find SodaCraft.Localizations.LocalizationManager type");
                    return string.Empty;
                }

                SystemLanguage currentLanguage = default(SystemLanguage);

                // 嘗試從 Instance 取得當前語言
                var instance = Traverse.Create(localizationManagerType)
                    .Property("Instance")
                    .GetValue();
                
                if (instance != null)
                {
                    // 嘗試取得 CurrentLanguage 屬性
                    currentLanguage = Traverse.Create(instance)
                        .Property("CurrentLanguage")
                        .GetValue<SystemLanguage>();
                    
                    // 如果屬性不存在，嘗試字段
                    if (currentLanguage == default(SystemLanguage))
                    {
                        currentLanguage = Traverse.Create(instance)
                            .Field("CurrentLanguage")
                            .GetValue<SystemLanguage>();
                    }
                }
                else
                {
                    // 如果沒有 Instance，嘗試靜態屬性或字段
                    currentLanguage = Traverse.Create(localizationManagerType)
                        .Property("CurrentLanguage")
                        .GetValue<SystemLanguage>();
                    
                    if (currentLanguage == default(SystemLanguage))
                    {
                        currentLanguage = Traverse.Create(localizationManagerType)
                            .Field("CurrentLanguage")
                            .GetValue<SystemLanguage>();
                    }
                }

                // 如果還是無法取得，使用 Unity 的系統語言作為後備
                if (currentLanguage == default(SystemLanguage))
                {
                    Logger.Debug("Cannot get language from LocalizationManager, using Application.systemLanguage");
                    currentLanguage = Application.systemLanguage;
                }

                // 將 SystemLanguage 轉換為我們的語言代碼格式
                return ConvertSystemLanguageToLanguageCode(currentLanguage);
            }
            catch (Exception e)
            {
                Logger.Warning($"Failed to get game language: {e.Message}");
                // 後備方案：使用 Unity 的系統語言
                try
                {
                    return ConvertSystemLanguageToLanguageCode(Application.systemLanguage);
                }
                catch
                {
                    return string.Empty;
                }
            }
        }

        /// <summary>
        /// 將 Unity SystemLanguage 轉換為語言代碼
        /// </summary>
        private static string ConvertSystemLanguageToLanguageCode(SystemLanguage systemLanguage)
        {
            // 記錄實際收到的語言值，方便調試
            Logger.Info($"[Language] Converting SystemLanguage: {systemLanguage} ({(int)systemLanguage})");
            
            switch (systemLanguage)
            {
                case SystemLanguage.ChineseTraditional:
                    Logger.Info("[Language] Detected: Traditional Chinese (zh-TW)");
                    return "zh-TW";
                case SystemLanguage.Chinese:
                case SystemLanguage.ChineseSimplified:
                    Logger.Info("[Language] Detected: Simplified Chinese (zh-CN)");
                    return "zh-CN";
                case SystemLanguage.English:
                    Logger.Info("[Language] Detected: English (en-US)");
                    return "en-US";
                case SystemLanguage.French:
                    Logger.Info("[Language] Detected: French (fr-FR)");
                    return "fr-FR";
                case SystemLanguage.German:
                    Logger.Info("[Language] Detected: German (de-DE)");
                    return "de-DE";
                case SystemLanguage.Japanese:
                    Logger.Info("[Language] Detected: Japanese (ja-JP)");
                    return "ja-JP";
                case SystemLanguage.Korean:
                    Logger.Info("[Language] Detected: Korean (ko-KR)");
                    return "ko-KR";
                case SystemLanguage.Portuguese:
                    Logger.Info("[Language] Detected: Portuguese (pt-BR)");
                    return "pt-BR";
                case SystemLanguage.Russian:
                    Logger.Info("[Language] Detected: Russian (ru-RU)");
                    return "ru-RU";
                case SystemLanguage.Spanish:
                    Logger.Info("[Language] Detected: Spanish (es-ES)");
                    return "es-ES";
                default:
                    // 所有未列出的語言 fallback 到英文
                    Logger.Info($"[Language] Detected: {systemLanguage} ({(int)systemLanguage}) - unsupported, using en-US as fallback");
                    return "en-US";
            }
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
                ["UI_Preview"] = "角色預覽",
                ["UI_SkinID_Placeholder"] = "外觀ID",
                
                // 左側裝備清單面板
                ["UI_EquipmentList"] = "裝備清單",
                ["UI_FilterByTag"] = "依標籤過濾",
                ["UI_HideEquipment"] = "不顯示",
                
                // Tag 名稱（用於下拉選單顯示）
                ["Tag_Armor"] = "護甲",
                ["Tag_Helmat"] = "頭盔",
                ["Tag_FaceMask"] = "面罩",
                ["Tag_Backpack"] = "背包",
                ["Tag_Headset"] = "耳機",

                // 裝備槽位名稱
                ["Slot_Armor"] = "護甲",
                ["Slot_Helmet"] = "頭盔",
                ["Slot_FaceMask"] = "面罩",
                ["Slot_Backpack"] = "背包",
                ["Slot_Headset"] = "耳機",

                // 設定面板
                ["Settings_Title"] = "功能設定",
                ["Settings_Tab_Log"] = "日誌",
                ["Settings_Tab_Language"] = "語言",
                
                // 日誌設定
                ["Log_Debug"] = "調試日誌 (Debug)",
                ["Log_Info"] = "資訊日誌 (Info)",
                ["Log_Warning"] = "警告日誌 (Warning)",
                ["Log_Error"] = "錯誤日誌 (Error)",

                // 語言設定
                ["Language_Select"] = "當前語言",
                ["Language_TraditionalChinese"] = "繁體中文",
                ["Language_Note"] = "語言設定會自動跟隨遊戲設定",
            };

            _translations["zh-TW"] = zhTW;

            // 簡體中文翻譯
            var zhCN = new Dictionary<string, string>
            {
                // 主 UI
                ["UI_Title"] = "装备外观",
                ["UI_Player"] = "鸭",
                ["UI_Pet"] = "狗",
                ["UI_Current"] = "当前: ",
                ["UI_Clear"] = "清空",
                ["UI_Save"] = "保存",
                ["UI_Reset"] = "重置",
                ["UI_Close"] = "关闭",
                ["UI_Preview"] = "角色预览",
                ["UI_SkinID_Placeholder"] = "外观ID",
                
                // 左側裝備清單面板
                ["UI_EquipmentList"] = "装备清单",
                ["UI_FilterByTag"] = "依标签过滤",
                ["UI_HideEquipment"] = "不显示",
                
                // Tag 名稱（用於下拉選單顯示）
                ["Tag_Armor"] = "护甲",
                ["Tag_Helmat"] = "头盔",
                ["Tag_FaceMask"] = "面罩",
                ["Tag_Backpack"] = "背包",
                ["Tag_Headset"] = "耳机",

                // 裝備槽位名稱
                ["Slot_Armor"] = "护甲",
                ["Slot_Helmet"] = "头盔",
                ["Slot_FaceMask"] = "面罩",
                ["Slot_Backpack"] = "背包",
                ["Slot_Headset"] = "耳机",

                // 設定面板
                ["Settings_Title"] = "功能设定",
                ["Settings_Tab_Log"] = "日志",
                ["Settings_Tab_Language"] = "语言",
                
                // 日誌設定
                ["Log_Debug"] = "调试日志 (Debug)",
                ["Log_Info"] = "信息日志 (Info)",
                ["Log_Warning"] = "警告日志 (Warning)",
                ["Log_Error"] = "错误日志 (Error)",

                // 語言設定
                ["Language_Select"] = "当前语言",
                ["Language_TraditionalChinese"] = "简体中文",
                ["Language_Note"] = "语言设定会自动跟随游戏设定",
            };

            _translations["zh-CN"] = zhCN;

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
                ["UI_Preview"] = "Character Preview",
                ["UI_SkinID_Placeholder"] = "Skin ID",
                
                // 左側裝備清單面板
                ["UI_EquipmentList"] = "Equipment List",
                ["UI_FilterByTag"] = "Filter by Tag",
                ["UI_HideEquipment"] = "Hide",
                
                // Tag 名稱（用於下拉選單顯示）
                ["Tag_Armor"] = "Armor",
                ["Tag_Helmat"] = "Helmet",
                ["Tag_FaceMask"] = "Face Mask",
                ["Tag_Backpack"] = "Backpack",
                ["Tag_Headset"] = "Headset",

                // 裝備槽位名稱
                ["Slot_Armor"] = "Armor",
                ["Slot_Helmet"] = "Helmet",
                ["Slot_FaceMask"] = "Face Mask",
                ["Slot_Backpack"] = "Backpack",
                ["Slot_Headset"] = "Headset",

                // 設定面板
                ["Settings_Title"] = "Settings",
                ["Settings_Tab_Log"] = "Log",
                ["Settings_Tab_Language"] = "Language",
                
                // 日誌設定
                ["Log_Debug"] = "Debug Log",
                ["Log_Info"] = "Info Log",
                ["Log_Warning"] = "Warning Log",
                ["Log_Error"] = "Error Log",

                // 語言設定
                ["Language_Select"] = "Current Language",
                ["Language_TraditionalChinese"] = "Traditional Chinese",
                ["Language_English"] = "English",
                ["Language_Note"] = "Language setting automatically follows game setting",
            };

            _translations["en-US"] = enUS;

            // 法文翻譯
            var frFR = new Dictionary<string, string>
            {
                ["UI_Title"] = "Apparence d'Équipement",
                ["UI_Player"] = "Canard",
                ["UI_Pet"] = "Chien",
                ["UI_Current"] = "Actuel: ",
                ["UI_Clear"] = "Effacer",
                ["UI_Save"] = "Enregistrer",
                ["UI_Reset"] = "Réinitialiser",
                ["UI_Close"] = "Fermer",
                ["UI_Preview"] = "Aperçu du Personnage",
                ["UI_SkinID_Placeholder"] = "ID d'Apparence",
                ["UI_EquipmentList"] = "Liste d'Équipement",
                ["UI_FilterByTag"] = "Filtrer par Tag",
                ["UI_HideEquipment"] = "Masquer",
                ["Tag_Armor"] = "Corps",
                ["Tag_Helmat"] = "Tête",
                ["Tag_FaceMask"] = "Visage",
                ["Tag_Backpack"] = "Sac à dos",
                ["Tag_Headset"] = "Casque audio",
                ["Slot_Armor"] = "Corps",
                ["Slot_Helmet"] = "Tête",
                ["Slot_FaceMask"] = "Visage",
                ["Slot_Backpack"] = "Sac à dos",
                ["Slot_Headset"] = "Casque audio",
                ["Settings_Title"] = "Paramètres",
                ["Settings_Tab_Log"] = "Journal",
                ["Settings_Tab_Language"] = "Langue",
                ["Log_Debug"] = "Journal de Débogage",
                ["Log_Info"] = "Journal d'Information",
                ["Log_Warning"] = "Journal d'Avertissement",
                ["Log_Error"] = "Journal d'Erreur",
                ["Language_Select"] = "Langue Actuelle",
                ["Language_TraditionalChinese"] = "Chinois Traditionnel",
                ["Language_English"] = "Anglais",
                ["Language_Note"] = "Le paramètre de langue suit automatiquement le paramètre du jeu",
            };

            _translations["fr-FR"] = frFR;

            // 德文翻譯
            var deDE = new Dictionary<string, string>
            {
                ["UI_Title"] = "Ausrüstungs-Skin",
                ["UI_Player"] = "Ente",
                ["UI_Pet"] = "Hund",
                ["UI_Current"] = "Aktuell: ",
                ["UI_Clear"] = "Löschen",
                ["UI_Save"] = "Speichern",
                ["UI_Reset"] = "Zurücksetzen",
                ["UI_Close"] = "Schließen",
                ["UI_Preview"] = "Charakter-Vorschau",
                ["UI_SkinID_Placeholder"] = "Skin-ID",
                ["UI_EquipmentList"] = "Ausrüstungsliste",
                ["UI_FilterByTag"] = "Nach Tag filtern",
                ["UI_HideEquipment"] = "Verstecken",
                ["Tag_Armor"] = "Körper",
                ["Tag_Helmat"] = "Kopf",
                ["Tag_FaceMask"] = "Gesicht",
                ["Tag_Backpack"] = "Rucksack",
                ["Tag_Headset"] = "Kopfhörer",
                ["Slot_Armor"] = "Körper",
                ["Slot_Helmet"] = "Kopf",
                ["Slot_FaceMask"] = "Gesicht",
                ["Slot_Backpack"] = "Rucksack",
                ["Slot_Headset"] = "Kopfhörer",
                ["Settings_Title"] = "Einstellungen",
                ["Settings_Tab_Log"] = "Protokoll",
                ["Settings_Tab_Language"] = "Sprache",
                ["Log_Debug"] = "Debug-Protokoll",
                ["Log_Info"] = "Info-Protokoll",
                ["Log_Warning"] = "Warnungs-Protokoll",
                ["Log_Error"] = "Fehler-Protokoll",
                ["Language_Select"] = "Aktuelle Sprache",
                ["Language_TraditionalChinese"] = "Traditionelles Chinesisch",
                ["Language_English"] = "Englisch",
                ["Language_Note"] = "Die Spracheinstellung folgt automatisch der Spieleinstellung",
            };

            _translations["de-DE"] = deDE;

            // 日文翻譯
            var jaJP = new Dictionary<string, string>
            {
                ["UI_Title"] = "装備スキン",
                ["UI_Player"] = "アヒル",
                ["UI_Pet"] = "犬",
                ["UI_Current"] = "現在: ",
                ["UI_Clear"] = "クリア",
                ["UI_Save"] = "保存",
                ["UI_Reset"] = "リセット",
                ["UI_Close"] = "閉じる",
                ["UI_Preview"] = "キャラクタープレビュー",
                ["UI_SkinID_Placeholder"] = "スキンID",
                ["UI_EquipmentList"] = "装備リスト",
                ["UI_FilterByTag"] = "タグでフィルター",
                ["UI_HideEquipment"] = "非表示",
                ["Tag_Armor"] = "ボディ",
                ["Tag_Helmat"] = "ヘッド",
                ["Tag_FaceMask"] = "フェイス",
                ["Tag_Backpack"] = "バッグ",
                ["Tag_Headset"] = "ヘッドホン",
                ["Slot_Armor"] = "ボディ",
                ["Slot_Helmet"] = "ヘッド",
                ["Slot_FaceMask"] = "フェイス",
                ["Slot_Backpack"] = "バッグ",
                ["Slot_Headset"] = "ヘッドホン",
                ["Settings_Title"] = "設定",
                ["Settings_Tab_Log"] = "ログ",
                ["Settings_Tab_Language"] = "言語",
                ["Log_Debug"] = "デバッグログ",
                ["Log_Info"] = "情報ログ",
                ["Log_Warning"] = "警告ログ",
                ["Log_Error"] = "エラーログ",
                ["Language_Select"] = "現在の言語",
                ["Language_TraditionalChinese"] = "繁体中国語",
                ["Language_English"] = "英語",
                ["Language_Note"] = "言語設定は自動的にゲーム設定に従います",
            };

            _translations["ja-JP"] = jaJP;

            // 韓文翻譯
            var koKR = new Dictionary<string, string>
            {
                ["UI_Title"] = "장비 스킨",
                ["UI_Player"] = "오리",
                ["UI_Pet"] = "개",
                ["UI_Current"] = "현재: ",
                ["UI_Clear"] = "지우기",
                ["UI_Save"] = "저장",
                ["UI_Reset"] = "재설정",
                ["UI_Close"] = "닫기",
                ["UI_Preview"] = "캐릭터 미리보기",
                ["UI_SkinID_Placeholder"] = "스킨 ID",
                ["UI_EquipmentList"] = "장비 목록",
                ["UI_FilterByTag"] = "태그로 필터",
                ["UI_HideEquipment"] = "숨기기",
                ["Tag_Armor"] = "신체",
                ["Tag_Helmat"] = "머리",
                ["Tag_FaceMask"] = "얼굴",
                ["Tag_Backpack"] = "가방",
                ["Tag_Headset"] = "헤드폰",
                ["Slot_Armor"] = "신체",
                ["Slot_Helmet"] = "머리",
                ["Slot_FaceMask"] = "얼굴",
                ["Slot_Backpack"] = "가방",
                ["Slot_Headset"] = "헤드폰",
                ["Settings_Title"] = "설정",
                ["Settings_Tab_Log"] = "로그",
                ["Settings_Tab_Language"] = "언어",
                ["Log_Debug"] = "디버그 로그",
                ["Log_Info"] = "정보 로그",
                ["Log_Warning"] = "경고 로그",
                ["Log_Error"] = "오류 로그",
                ["Language_Select"] = "현재 언어",
                ["Language_TraditionalChinese"] = "번체 중국어",
                ["Language_English"] = "영어",
                ["Language_Note"] = "언어 설정은 자동으로 게임 설정을 따릅니다",
            };

            _translations["ko-KR"] = koKR;

            // 葡萄牙文（巴西）翻譯
            var ptBR = new Dictionary<string, string>
            {
                ["UI_Title"] = "Aparência de Equipamento",
                ["UI_Player"] = "Pato",
                ["UI_Pet"] = "Cachorro",
                ["UI_Current"] = "Atual: ",
                ["UI_Clear"] = "Limpar",
                ["UI_Save"] = "Salvar",
                ["UI_Reset"] = "Redefinir",
                ["UI_Close"] = "Fechar",
                ["UI_Preview"] = "Pré-visualização do Personagem",
                ["UI_SkinID_Placeholder"] = "ID da Aparência",
                ["UI_EquipmentList"] = "Lista de Equipamentos",
                ["UI_FilterByTag"] = "Filtrar por Tag",
                ["UI_HideEquipment"] = "Ocultar",
                ["Tag_Armor"] = "Corpo",
                ["Tag_Helmat"] = "Cabeça",
                ["Tag_FaceMask"] = "Rosto",
                ["Tag_Backpack"] = "Mochila",
                ["Tag_Headset"] = "Fones de Ouvido",
                ["Slot_Armor"] = "Corpo",
                ["Slot_Helmet"] = "Cabeça",
                ["Slot_FaceMask"] = "Rosto",
                ["Slot_Backpack"] = "Mochila",
                ["Slot_Headset"] = "Fones de Ouvido",
                ["Settings_Title"] = "Configurações",
                ["Settings_Tab_Log"] = "Log",
                ["Settings_Tab_Language"] = "Idioma",
                ["Log_Debug"] = "Log de Depuração",
                ["Log_Info"] = "Log de Informação",
                ["Log_Warning"] = "Log de Aviso",
                ["Log_Error"] = "Log de Erro",
                ["Language_Select"] = "Idioma Atual",
                ["Language_TraditionalChinese"] = "Chinês Tradicional",
                ["Language_English"] = "Inglês",
                ["Language_Note"] = "A configuração de idioma segue automaticamente a configuração do jogo",
            };

            _translations["pt-BR"] = ptBR;

            // 俄文翻譯
            var ruRU = new Dictionary<string, string>
            {
                ["UI_Title"] = "Внешний Вид Снаряжения",
                ["UI_Player"] = "Утка",
                ["UI_Pet"] = "Собака",
                ["UI_Current"] = "Текущее: ",
                ["UI_Clear"] = "Очистить",
                ["UI_Save"] = "Сохранить",
                ["UI_Reset"] = "Сбросить",
                ["UI_Close"] = "Закрыть",
                ["UI_Preview"] = "Предпросмотр Персонажа",
                ["UI_SkinID_Placeholder"] = "ID Внешнего Вида",
                ["UI_EquipmentList"] = "Список Снаряжения",
                ["UI_FilterByTag"] = "Фильтр по Тегу",
                ["UI_HideEquipment"] = "Скрыть",
                ["Tag_Armor"] = "Тело",
                ["Tag_Helmat"] = "Голова",
                ["Tag_FaceMask"] = "Лицо",
                ["Tag_Backpack"] = "Рюкзак",
                ["Tag_Headset"] = "Наушники",
                ["Slot_Armor"] = "Тело",
                ["Slot_Helmet"] = "Голова",
                ["Slot_FaceMask"] = "Лицо",
                ["Slot_Backpack"] = "Рюкзак",
                ["Slot_Headset"] = "Наушники",
                ["Settings_Title"] = "Настройки",
                ["Settings_Tab_Log"] = "Логирование",
                ["Settings_Tab_Language"] = "Язык",
                ["Log_Debug"] = "Отладочный Лог",
                ["Log_Info"] = "Информационный Лог",
                ["Log_Warning"] = "Лог Предупреждений",
                ["Log_Error"] = "Лог Ошибок",
                ["Language_Select"] = "Текущий Язык",
                ["Language_TraditionalChinese"] = "Традиционный Китайский",
                ["Language_English"] = "Английский",
                ["Language_Note"] = "Настройка языка автоматически следует настройке игры",
            };

            _translations["ru-RU"] = ruRU;

            // 西班牙文翻譯
            var esES = new Dictionary<string, string>
            {
                ["UI_Title"] = "Aspecto de Equipamiento",
                ["UI_Player"] = "Pato",
                ["UI_Pet"] = "Perro",
                ["UI_Current"] = "Actual: ",
                ["UI_Clear"] = "Limpiar",
                ["UI_Save"] = "Guardar",
                ["UI_Reset"] = "Restablecer",
                ["UI_Close"] = "Cerrar",
                ["UI_Preview"] = "Vista Previa del Personaje",
                ["UI_SkinID_Placeholder"] = "ID de Aspecto",
                ["UI_EquipmentList"] = "Lista de Equipamiento",
                ["UI_FilterByTag"] = "Filtrar por Etiqueta",
                ["UI_HideEquipment"] = "Ocultar",
                ["Tag_Armor"] = "Cuerpo",
                ["Tag_Helmat"] = "Cabeza",
                ["Tag_FaceMask"] = "Rostro",
                ["Tag_Backpack"] = "Mochila",
                ["Tag_Headset"] = "Auriculares",
                ["Slot_Armor"] = "Cuerpo",
                ["Slot_Helmet"] = "Cabeza",
                ["Slot_FaceMask"] = "Rostro",
                ["Slot_Backpack"] = "Mochila",
                ["Slot_Headset"] = "Auriculares",
                ["Settings_Title"] = "Configuración",
                ["Settings_Tab_Log"] = "Registro",
                ["Settings_Tab_Language"] = "Idioma",
                ["Log_Debug"] = "Registro de Depuración",
                ["Log_Info"] = "Registro de Información",
                ["Log_Warning"] = "Registro de Advertencias",
                ["Log_Error"] = "Registro de Errores",
                ["Language_Select"] = "Idioma Actual",
                ["Language_TraditionalChinese"] = "Chino Tradicional",
                ["Language_English"] = "Inglés",
                ["Language_Note"] = "La configuración de idioma sigue automáticamente la configuración del juego",
            };

            _translations["es-ES"] = esES;
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

                // 如果找不到翻譯，fallback 到英文
                if (_currentLanguage != "en-US" && 
                    _translations != null)
                {
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
        /// 注意：物品名稱會自動使用遊戲的 LocalizationManager，不需要手動同步
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

        /// <summary>
        /// 取得語言的顯示名稱
        /// </summary>
        public static string GetLanguageDisplayName(string languageCode)
        {
            switch (languageCode)
            {
                case "zh-TW":
                    return Get("Language_TraditionalChinese", "繁體中文");
                case "zh-CN":
                    return "简体中文";
                case "en-US":
                    return Get("Language_English", "English");
                case "fr-FR":
                    return "Français";
                case "de-DE":
                    return "Deutsch";
                case "ja-JP":
                    return "日本語";
                case "ko-KR":
                    return "한국어";
                case "pt-BR":
                    return "Português (Brasil)";
                case "ru-RU":
                    return "Русский";
                case "es-ES":
                    return "Español";
                default:
                    return Get("Language_English", "English");
            }
        }

        /// <summary>
        /// 檢查並更新語言（如果遊戲語言已變更）
        /// 這個方法可以在 UI 打開時或定期調用來確保語言同步
        /// </summary>
        public static void CheckAndUpdateLanguage()
        {
            try
            {
                string gameLanguage = GetGameLanguage();
                if (!string.IsNullOrEmpty(gameLanguage) && gameLanguage != _currentLanguage)
                {
                    Logger.Info($"Language changed detected: {_currentLanguage} -> {gameLanguage}");
                    _currentLanguage = gameLanguage;
                    
                    // 觸發語言變更事件，通知 UI 更新
                    OnLanguageChanged?.Invoke(_currentLanguage);
                }
            }
            catch (Exception e)
            {
                Logger.Warning($"Error checking language: {e.Message}");
            }
        }



        /// <summary>
        /// 訂閱遊戲的語言變更事件
        /// </summary>
        public static void SubscribeToGameLanguageChange()
        {
            try
            {
                var localizationManagerType = AccessTools.TypeByName("SodaCraft.Localizations.LocalizationManager");
                if (localizationManagerType == null)
                {
                    Logger.Warning("Cannot find SodaCraft.Localizations.LocalizationManager type for subscription");
                    return;
                }

                System.Action<SystemLanguage>? onSetLanguageEvent = null;

                // 嘗試從 Instance 取得事件
                var instance = Traverse.Create(localizationManagerType)
                    .Property("Instance")
                    .GetValue();
                
                if (instance != null)
                {
                    // 嘗試取得 OnSetLanguage 字段
                    onSetLanguageEvent = Traverse.Create(instance)
                        .Field("OnSetLanguage")
                        .GetValue<System.Action<SystemLanguage>>();
                    
                    // 如果字段不存在，嘗試屬性
                    if (onSetLanguageEvent == null)
                    {
                        onSetLanguageEvent = Traverse.Create(instance)
                            .Property("OnSetLanguage")
                            .GetValue<System.Action<SystemLanguage>>();
                    }
                }
                else
                {
                    // 如果沒有 Instance，嘗試靜態字段或屬性
                    onSetLanguageEvent = Traverse.Create(localizationManagerType)
                        .Field("OnSetLanguage")
                        .GetValue<System.Action<SystemLanguage>>();
                    
                    if (onSetLanguageEvent == null)
                    {
                        onSetLanguageEvent = Traverse.Create(localizationManagerType)
                            .Property("OnSetLanguage")
                            .GetValue<System.Action<SystemLanguage>>();
                    }
                }

                if (onSetLanguageEvent != null)
                {
                    // 訂閱事件
                    onSetLanguageEvent += OnGameLanguageChanged;
                    Logger.Info("Subscribed to game language change event");
                }
                else
                {
                    Logger.Warning("Cannot find OnSetLanguage event in LocalizationManager");
                }
            }
            catch (Exception e)
            {
                Logger.Warning($"Failed to subscribe to game language change: {e.Message}");
            }
        }

        /// <summary>
        /// 當遊戲語言變更時的回調
        /// </summary>
        private static void OnGameLanguageChanged(SystemLanguage newLanguage)
        {
            try
            {
                string newLanguageCode = ConvertSystemLanguageToLanguageCode(newLanguage);
                if (newLanguageCode != _currentLanguage)
                {
                    _currentLanguage = newLanguageCode;
                    Logger.Info($"Language changed to: {_currentLanguage} (from game)");
                    
                    // 觸發語言變更事件，通知 UI 更新
                    OnLanguageChanged?.Invoke(_currentLanguage);
                }
            }
            catch (Exception e)
            {
                Logger.Error($"Error handling game language change: {e.Message}", e);
            }
        }
    }
}

