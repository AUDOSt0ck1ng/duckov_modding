using System;
using System.Collections.Generic;
using UnityEngine;

namespace EquipmentSkinSystem
{
    /// <summary>
    /// 角色類型（玩家或狗）
    /// </summary>
    [Serializable]
    public enum CharacterType
    {
        Player,     // 玩家
        Pet         // 狗
    }

    /// <summary>
    /// 裝備槽位類型（對應遊戲實際的槽位）
    /// </summary>
    [Serializable]
    public enum EquipmentSlotType
    {
        Armor,      // 護甲（身體）
        Helmet,     // 頭盔
        FaceMask,   // 面罩
        Backpack,   // 背包
        Headset     // 耳機
    }

    /// <summary>
    /// 單個裝備槽位的外觀配置（簡化版）
    /// </summary>
    [Serializable]
    public class SlotSkinConfig
    {
        public EquipmentSlotType SlotType;
        public int SkinItemTypeID;      // 外觀物品的 TypeID（顯示外觀）
        public bool UseSkin;            // 是否啟用外觀覆蓋

        public SlotSkinConfig(EquipmentSlotType slotType)
        {
            SlotType = slotType;
            SkinItemTypeID = 0;  // 0 = 不套用外觀（預設）
            UseSkin = false;
        }
    }

    /// <summary>
    /// 玩家角色的完整裝備外觀配置
    /// </summary>
    [Serializable]
    public class CharacterSkinProfile
    {
        // 配置版本號（用於未來升級時的資料遷移）
        public int ConfigVersion;

        public string ProfileName;

        // 使用 List 以支援 JsonUtility 序列化
        // 注意：必須是 public 且不能在聲明時初始化（JsonUtility 的限制）
        public List<SlotSkinConfig> SlotConfigsList;

        // 運行時使用的 Dictionary（不序列化）
        [NonSerialized]
        private Dictionary<EquipmentSlotType, SlotSkinConfig>? _slotConfigsDict;

        public Dictionary<EquipmentSlotType, SlotSkinConfig> SlotConfigs
        {
            get
            {
                if (_slotConfigsDict == null)
                {
                    RebuildDictionary();
                }
                return _slotConfigsDict!;
            }
        }

        public CharacterSkinProfile(string name = "Default")
        {
            ProfileName = name;
            ConfigVersion = 1;
            SlotConfigsList = new List<SlotSkinConfig>();

            // 初始化所有槽位
            foreach (EquipmentSlotType slotType in Enum.GetValues(typeof(EquipmentSlotType)))
            {
                SlotConfigsList.Add(new SlotSkinConfig(slotType));
            }

            RebuildDictionary();

            // 注意：不在構造函數中使用 Logger，避免初始化順序問題
        }

        /// <summary>
        /// 從 List 重建 Dictionary（反序列化後調用）
        /// </summary>
        public void RebuildDictionary()
        {
            _slotConfigsDict = new Dictionary<EquipmentSlotType, SlotSkinConfig>();
            if (SlotConfigsList != null)
            {
                foreach (var config in SlotConfigsList)
                {
                    _slotConfigsDict[config.SlotType] = config;
                }
            }
        }

        /// <summary>
        /// 保存前同步 Dictionary 到 List
        /// </summary>
        public void SyncToList()
        {
            if (_slotConfigsDict != null)
            {
                SlotConfigsList = new List<SlotSkinConfig>(_slotConfigsDict.Values);
            }
        }

        /// <summary>
        /// 設置某個槽位的外觀（簡化版）
        /// </summary>
        public void SetSlotSkin(EquipmentSlotType slot, int skinItemID, bool useSkin)
        {
            if (SlotConfigs.ContainsKey(slot))
            {
                SlotConfigs[slot].SkinItemTypeID = skinItemID;
                SlotConfigs[slot].UseSkin = useSkin;
            }
        }

        /// <summary>
        /// 獲取某個槽位應該顯示的物品 ID（簡化版）
        /// </summary>
        public int GetDisplayItemID(EquipmentSlotType slot)
        {
            if (SlotConfigs.TryGetValue(slot, out var config))
            {
                if (config.UseSkin)
                {
                    return config.SkinItemTypeID; // -1 = 隱藏, 0 = 原樣, >0 = 替換
                }
            }
            return 0; // 不套用外觀
        }

        /// <summary>
        /// 切換某個槽位的外觀啟用狀態
        /// </summary>
        public void ToggleSlotSkin(EquipmentSlotType slot)
        {
            if (SlotConfigs.TryGetValue(slot, out var config))
            {
                config.UseSkin = !config.UseSkin;
            }
        }
    }

    /// <summary>
    /// 應用程式設定（Log 設定、語系等）
    /// </summary>
    [Serializable]
    public class AppSettings
    {
        // Log 設定
        public bool EnableDebugLog = false;
        public bool EnableInfoLog = true;
        public bool EnableWarningLog = true;
        public bool EnableErrorLog = true;

        // 語系設定（未來擴展用）
        public string Language = "zh-TW";  // 預設繁體中文

        public AppSettings()
        {
            // 預設值已在欄位初始化中設定
        }
    }

    /// <summary>
    /// 全局裝備外觀數據管理器
    /// </summary>
    public class EquipmentSkinDataManager
    {
        private static EquipmentSkinDataManager? _instance;
        public static EquipmentSkinDataManager Instance
        {
            get
            {
                if (_instance == null)
                {
                    _instance = new EquipmentSkinDataManager();
                }
                return _instance;
            }
        }

        private CharacterSkinProfile _playerProfile;
        private CharacterSkinProfile _petProfile;
        private CharacterType _currentCharacterType = CharacterType.Player;
        private AppSettings _appSettings;

        public CharacterSkinProfile PlayerProfile => _playerProfile;
        public CharacterSkinProfile PetProfile => _petProfile;
        public CharacterType CurrentCharacterType => _currentCharacterType;
        public AppSettings AppSettings => _appSettings;

        // 當前選中的角色配置
        public CharacterSkinProfile CurrentProfile => _currentCharacterType == CharacterType.Player ? _playerProfile : _petProfile;

        private EquipmentSkinDataManager()
        {
            _playerProfile = new CharacterSkinProfile("Player");
            _petProfile = new CharacterSkinProfile("Pet");
            _appSettings = new AppSettings();
        }

        /// <summary>
        /// 切換當前角色類型
        /// </summary>
        public void SetCurrentCharacterType(CharacterType type)
        {
            _currentCharacterType = type;
            Logger.Debug($"Switched to {type} profile");
        }

        /// <summary>
        /// 保存配置到 JSON（手動序列化，支援玩家和狗）
        /// </summary>
        public string SaveToJson()
        {
            try
            {
                // 保存前同步 Dictionary 到 List
                _playerProfile.SyncToList();
                _petProfile.SyncToList();

                var sb = new System.Text.StringBuilder();
                sb.AppendLine("{");
                sb.AppendLine($"    \"CurrentCharacterType\": {(int)_currentCharacterType},");
                sb.AppendLine("    \"PlayerProfile\": ");
                sb.Append(SerializeProfile(_playerProfile, "        "));
                sb.AppendLine(",");
                sb.AppendLine("    \"PetProfile\": ");
                sb.Append(SerializeProfile(_petProfile, "        "));
                sb.AppendLine(",");
                sb.AppendLine("    \"AppSettings\": ");
                sb.Append(SerializeAppSettings(_appSettings, "        "));
                sb.AppendLine();
                sb.Append("}");

                string json = sb.ToString();
                Logger.Debug($"SaveToJson - JSON length: {json.Length}");
                return json;
            }
            catch (Exception e)
            {
                Logger.Error("Failed to save profiles", e);
                Logger.Error($"Stack trace: {e.StackTrace}");
                return string.Empty;
            }
        }

        /// <summary>
        /// 序列化單個角色配置
        /// </summary>
        private string SerializeProfile(CharacterSkinProfile profile, string indent)
        {
            var sb = new System.Text.StringBuilder();
            sb.AppendLine("{");
            sb.AppendLine($"{indent}    \"ConfigVersion\": {profile.ConfigVersion},");
            sb.AppendLine($"{indent}    \"ProfileName\": \"{profile.ProfileName}\",");
            sb.AppendLine($"{indent}    \"SlotConfigsList\": [");

            if (profile.SlotConfigsList != null)
            {
                for (int i = 0; i < profile.SlotConfigsList.Count; i++)
                {
                    var slot = profile.SlotConfigsList[i];
                    sb.AppendLine($"{indent}        {{");
                    sb.AppendLine($"{indent}            \"SlotType\": {(int)slot.SlotType},");
                    sb.AppendLine($"{indent}            \"SkinItemTypeID\": {slot.SkinItemTypeID},");
                    sb.AppendLine($"{indent}            \"UseSkin\": {slot.UseSkin.ToString().ToLower()}");
                    sb.Append($"{indent}        }}");
                    if (i < profile.SlotConfigsList.Count - 1)
                    {
                        sb.AppendLine(",");
                    }
                    else
                    {
                        sb.AppendLine();
                    }
                }
            }

            sb.AppendLine($"{indent}    ]");
            sb.Append($"{indent}}}");

            return sb.ToString();
        }

        /// <summary>
        /// 序列化應用程式設定
        /// </summary>
        private string SerializeAppSettings(AppSettings settings, string indent)
        {
            var sb = new System.Text.StringBuilder();
            sb.AppendLine("{");
            sb.AppendLine($"{indent}    \"EnableDebugLog\": {settings.EnableDebugLog.ToString().ToLower()},");
            sb.AppendLine($"{indent}    \"EnableInfoLog\": {settings.EnableInfoLog.ToString().ToLower()},");
            sb.AppendLine($"{indent}    \"EnableWarningLog\": {settings.EnableWarningLog.ToString().ToLower()},");
            sb.AppendLine($"{indent}    \"EnableErrorLog\": {settings.EnableErrorLog.ToString().ToLower()},");
            sb.AppendLine($"{indent}    \"Language\": \"{settings.Language}\"");
            sb.Append($"{indent}}}");
            return sb.ToString();
        }

        /// <summary>
        /// 從 JSON 載入配置（手動解析，支援玩家和狗）
        /// </summary>
        public void LoadFromJson(string json)
        {
            try
            {
                if (!string.IsNullOrEmpty(json))
                {
                    // 檢查是否為新格式（包含 PlayerProfile 和 PetProfile）
                    if (json.Contains("\"PlayerProfile\"") && json.Contains("\"PetProfile\""))
                    {
                        // 新格式：分別載入玩家和狗的配置
                        // 重要：必須先恢復 CurrentCharacterType，因為後面的 CurrentProfile 會用到它
                        var charTypeMatch = System.Text.RegularExpressions.Regex.Match(json, @"""CurrentCharacterType"":\s*(\d+)");
                        if (charTypeMatch.Success)
                        {
                            int charTypeValue = int.Parse(charTypeMatch.Groups[1].Value);
                            _currentCharacterType = (CharacterType)charTypeValue;
                            Logger.Info($"LoadFromJson - Restored CurrentCharacterType: {_currentCharacterType} (value: {charTypeValue})");
                        }
                        else
                        {
                            Logger.Warning($"LoadFromJson - CurrentCharacterType not found in JSON, keeping default: {_currentCharacterType}");
                            Logger.Warning($"LoadFromJson - JSON snippet: {json.Substring(0, Math.Min(200, json.Length))}...");
                        }

                        var playerMatch = System.Text.RegularExpressions.Regex.Match(json, @"""PlayerProfile"":\s*(\{(?:[^{}]|(?<open>\{)|(?<-open>\}))+(?(open)(?!))\})", System.Text.RegularExpressions.RegexOptions.Singleline);
                        if (playerMatch.Success)
                        {
                            _playerProfile = ParseJson(playerMatch.Groups[1].Value);
                            ValidateAndFixProfile(_playerProfile, "Player");
                        }

                        var petMatch = System.Text.RegularExpressions.Regex.Match(json, @"""PetProfile"":\s*(\{(?:[^{}]|(?<open>\{)|(?<-open>\}))+(?(open)(?!))\})", System.Text.RegularExpressions.RegexOptions.Singleline);
                        if (petMatch.Success)
                        {
                            _petProfile = ParseJson(petMatch.Groups[1].Value);
                            ValidateAndFixProfile(_petProfile, "Pet");
                        }

                        // 載入 AppSettings
                        var appSettingsMatch = System.Text.RegularExpressions.Regex.Match(json, @"""AppSettings"":\s*(\{(?:[^{}]|(?<open>\{)|(?<-open>\}))+(?(open)(?!))\})", System.Text.RegularExpressions.RegexOptions.Singleline);
                        if (appSettingsMatch.Success)
                        {
                            _appSettings = ParseAppSettings(appSettingsMatch.Groups[1].Value);
                        }
                        else
                        {
                            // 如果沒有 AppSettings，使用預設值
                            _appSettings = new AppSettings();
                            Logger.Debug("No AppSettings found, using defaults");
                        }

                        Logger.Debug($"Loaded player and pet profiles, current: {_currentCharacterType}");
                    }
                    else
                    {
                        // 舊格式：只有單一配置，視為玩家配置
                        Logger.Warning("Old config format detected, migrating to new format...");
                        _playerProfile = ParseJson(json);
                        ValidateAndFixProfile(_playerProfile, "Player");
                        _petProfile = new CharacterSkinProfile("Pet");
                        _currentCharacterType = CharacterType.Player; // 舊格式默認為玩家
                        _appSettings = new AppSettings();

                        // 自動保存為新格式
                        DataPersistence.SaveConfig();
                        Logger.Info("Config migrated to new format");
                    }
                }
                else
                {
                    // 空 JSON 或配置文件不存在：使用默認值
                    _playerProfile = new CharacterSkinProfile("Player");
                    _petProfile = new CharacterSkinProfile("Pet");
                    _currentCharacterType = CharacterType.Player; // 默認是玩家（鴨子）
                    _appSettings = new AppSettings();
                    Logger.Debug("LoadFromJson - Empty JSON, using default values (Player)");
                }
            }
            catch (Exception e)
            {
                Logger.Error("Failed to load profiles", e);
                Logger.Error($"Stack trace: {e.StackTrace}");
                _playerProfile = new CharacterSkinProfile("Player");
                _petProfile = new CharacterSkinProfile("Pet");
                _appSettings = new AppSettings();
            }
        }

        /// <summary>
        /// 解析應用程式設定
        /// </summary>
        private AppSettings ParseAppSettings(string json)
        {
            var settings = new AppSettings();

            try
            {
                var debugMatch = System.Text.RegularExpressions.Regex.Match(json, @"""EnableDebugLog"":\s*(true|false)");
                if (debugMatch.Success)
                {
                    settings.EnableDebugLog = bool.Parse(debugMatch.Groups[1].Value);
                }

                var infoMatch = System.Text.RegularExpressions.Regex.Match(json, @"""EnableInfoLog"":\s*(true|false)");
                if (infoMatch.Success)
                {
                    settings.EnableInfoLog = bool.Parse(infoMatch.Groups[1].Value);
                }

                var warningMatch = System.Text.RegularExpressions.Regex.Match(json, @"""EnableWarningLog"":\s*(true|false)");
                if (warningMatch.Success)
                {
                    settings.EnableWarningLog = bool.Parse(warningMatch.Groups[1].Value);
                }

                var errorMatch = System.Text.RegularExpressions.Regex.Match(json, @"""EnableErrorLog"":\s*(true|false)");
                if (errorMatch.Success)
                {
                    settings.EnableErrorLog = bool.Parse(errorMatch.Groups[1].Value);
                }

                var languageMatch = System.Text.RegularExpressions.Regex.Match(json, @"""Language"":\s*""([^""]*)""");
                if (languageMatch.Success)
                {
                    settings.Language = languageMatch.Groups[1].Value;
                }

                Logger.Debug($"Parsed AppSettings: Debug={settings.EnableDebugLog}, Info={settings.EnableInfoLog}, Warning={settings.EnableWarningLog}, Error={settings.EnableErrorLog}, Language={settings.Language}");
            }
            catch (Exception e)
            {
                Logger.Error("Failed to parse AppSettings", e);
            }

            return settings;
        }

        /// <summary>
        /// 驗證並修正配置
        /// </summary>
        private void ValidateAndFixProfile(CharacterSkinProfile profile, string name)
        {
            if (profile == null || profile.SlotConfigsList == null || profile.SlotConfigsList.Count == 0)
            {
                Logger.Warning($"Invalid {name} profile, creating new one");
                profile = new CharacterSkinProfile(name);
                return;
            }

            // 載入後重建 Dictionary
            profile.RebuildDictionary();
            Logger.Debug($"Loaded {name} profile with {profile.SlotConfigsList.Count} slots");

            // 驗證所有槽位都存在
            bool needsUpdate = false;
            foreach (EquipmentSlotType slotType in Enum.GetValues(typeof(EquipmentSlotType)))
            {
                if (!profile.SlotConfigs.ContainsKey(slotType))
                {
                    Logger.Warning($"{name} profile missing slot {slotType}, adding...");
                    profile.SlotConfigsList.Add(new SlotSkinConfig(slotType));
                    needsUpdate = true;
                }
            }

            if (needsUpdate)
            {
                profile.RebuildDictionary();
                DataPersistence.SaveConfig();
                Logger.Info($"{name} profile updated with missing slots");
            }
        }

        /// <summary>
        /// 手動解析 JSON（簡單的解析器）
        /// </summary>
        private CharacterSkinProfile ParseJson(string json)
        {
            var profile = new CharacterSkinProfile("Default");

            try
            {
                // 提取 ConfigVersion
                var versionMatch = System.Text.RegularExpressions.Regex.Match(json, @"""ConfigVersion"":\s*(\d+)");
                if (versionMatch.Success)
                {
                    profile.ConfigVersion = int.Parse(versionMatch.Groups[1].Value);
                }

                // 提取 ProfileName
                var nameMatch = System.Text.RegularExpressions.Regex.Match(json, @"""ProfileName"":\s*""([^""]*)""");
                if (nameMatch.Success)
                {
                    profile.ProfileName = nameMatch.Groups[1].Value;
                }

                // 提取 SlotConfigsList
                var slotsMatch = System.Text.RegularExpressions.Regex.Match(json, @"""SlotConfigsList"":\s*\[(.*?)\]", System.Text.RegularExpressions.RegexOptions.Singleline);
                if (slotsMatch.Success)
                {
                    string slotsJson = slotsMatch.Groups[1].Value;
                    var slotMatches = System.Text.RegularExpressions.Regex.Matches(slotsJson, @"\{[^}]+\}");

                    foreach (System.Text.RegularExpressions.Match slotMatch in slotMatches)
                    {
                        string slotJson = slotMatch.Value;

                        var slotTypeMatch = System.Text.RegularExpressions.Regex.Match(slotJson, @"""SlotType"":\s*(\d+)");
                        var skinIDMatch = System.Text.RegularExpressions.Regex.Match(slotJson, @"""SkinItemTypeID"":\s*(-?\d+)");
                        var useSkinMatch = System.Text.RegularExpressions.Regex.Match(slotJson, @"""UseSkin"":\s*(true|false)");

                        if (slotTypeMatch.Success && skinIDMatch.Success && useSkinMatch.Success)
                        {
                            var slot = new SlotSkinConfig((EquipmentSlotType)int.Parse(slotTypeMatch.Groups[1].Value))
                            {
                                SkinItemTypeID = int.Parse(skinIDMatch.Groups[1].Value),
                                UseSkin = bool.Parse(useSkinMatch.Groups[1].Value)
                            };
                            profile.SlotConfigsList.Add(slot);
                        }
                    }

                    Logger.Debug($"ParseJson - Parsed {profile.SlotConfigsList.Count} slots");
                }
            }
            catch (Exception e)
            {
                Logger.Error("Failed to parse JSON", e);
            }

            return profile;
        }

        /// <summary>
        /// 配置版本遷移
        /// </summary>
        private CharacterSkinProfile MigrateConfig(CharacterSkinProfile oldProfile, int targetVersion)
        {
            // 創建新版本的配置
            CharacterSkinProfile newProfile = new CharacterSkinProfile(oldProfile.ProfileName);
            newProfile.ConfigVersion = targetVersion;

            // 遷移舊配置的資料（保留所有已設定的值）
            if (oldProfile.SlotConfigsList != null)
            {
                foreach (var oldSlot in oldProfile.SlotConfigsList)
                {
                    // 檢查新版本是否還有這個槽位類型
                    if (Enum.IsDefined(typeof(EquipmentSlotType), oldSlot.SlotType))
                    {
                        // 找到對應的新槽位並複製設定
                        var newSlot = newProfile.SlotConfigsList.Find(s => s.SlotType == oldSlot.SlotType);
                        if (newSlot != null)
                        {
                            newSlot.SkinItemTypeID = oldSlot.SkinItemTypeID;
                            newSlot.UseSkin = oldSlot.UseSkin;
                            Logger.Debug($"Migrated {oldSlot.SlotType}: SkinID={oldSlot.SkinItemTypeID}, UseSkin={oldSlot.UseSkin}");
                        }
                    }
                    else
                    {
                        Logger.Warning($"Slot type {oldSlot.SlotType} no longer exists in new version, skipping...");
                    }
                }
            }

            return newProfile;
        }

        /// <summary>
        /// 重置當前角色的配置
        /// </summary>
        public void ResetProfile()
        {
            if (_currentCharacterType == CharacterType.Player)
            {
                _playerProfile = new CharacterSkinProfile("Player");
            }
            else
            {
                _petProfile = new CharacterSkinProfile("Pet");
            }
            Logger.Debug($"Reset {_currentCharacterType} profile");
        }
    }
}
