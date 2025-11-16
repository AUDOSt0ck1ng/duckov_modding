using System;
using System.Collections.Generic;
using UnityEngine;

namespace EquipmentSkinSystem
{
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
            
            Debug.Log($"[EquipmentSkinSystem] CharacterSkinProfile created with {SlotConfigsList.Count} slots");
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

        private CharacterSkinProfile _currentProfile;
        public CharacterSkinProfile CurrentProfile => _currentProfile;

        private EquipmentSkinDataManager()
        {
            _currentProfile = new CharacterSkinProfile("Default");
        }

        /// <summary>
        /// 保存配置到 JSON（手動序列化，因為 JsonUtility 不支援 List）
        /// </summary>
        public string SaveToJson()
        {
            try
            {
                // 保存前同步 Dictionary 到 List
                _currentProfile.SyncToList();
                
                Debug.Log($"[EquipmentSkinSystem] SaveToJson - SlotConfigsList count: {_currentProfile.SlotConfigsList?.Count ?? 0}");
                
                // 手動構建 JSON（因為 JsonUtility 對 List 支援不佳）
                var sb = new System.Text.StringBuilder();
                sb.AppendLine("{");
                sb.AppendLine($"    \"ConfigVersion\": {_currentProfile.ConfigVersion},");
                sb.AppendLine($"    \"ProfileName\": \"{_currentProfile.ProfileName}\",");
                sb.AppendLine("    \"SlotConfigsList\": [");
                
                if (_currentProfile.SlotConfigsList != null)
                {
                    for (int i = 0; i < _currentProfile.SlotConfigsList.Count; i++)
                    {
                        var slot = _currentProfile.SlotConfigsList[i];
                        sb.AppendLine("        {");
                        sb.AppendLine($"            \"SlotType\": {(int)slot.SlotType},");
                        sb.AppendLine($"            \"SkinItemTypeID\": {slot.SkinItemTypeID},");
                        sb.AppendLine($"            \"UseSkin\": {slot.UseSkin.ToString().ToLower()}");
                        sb.Append("        }");
                        if (i < _currentProfile.SlotConfigsList.Count - 1)
                        {
                            sb.AppendLine(",");
                        }
                        else
                        {
                            sb.AppendLine();
                        }
                        
                        Debug.Log($"[EquipmentSkinSystem] SaveToJson - Slot {slot.SlotType}: SkinID={slot.SkinItemTypeID}, UseSkin={slot.UseSkin}");
                    }
                }
                
                sb.AppendLine("    ]");
                sb.Append("}");
                
                string json = sb.ToString();
                Debug.Log($"[EquipmentSkinSystem] SaveToJson - JSON length: {json.Length}");
                return json;
            }
            catch (Exception e)
            {
                Debug.LogError($"[EquipmentSkinSystem] Failed to save profile: {e.Message}");
                Debug.LogError($"[EquipmentSkinSystem] Stack trace: {e.StackTrace}");
                return string.Empty;
            }
        }

        /// <summary>
        /// 從 JSON 載入配置（手動解析，因為 JsonUtility 不支援 List）
        /// </summary>
        public void LoadFromJson(string json)
        {
            try
            {
                if (!string.IsNullOrEmpty(json))
                {
                    // 手動解析 JSON
                    _currentProfile = ParseJson(json);
                    
                    if (_currentProfile == null || _currentProfile.SlotConfigsList == null || _currentProfile.SlotConfigsList.Count == 0)
                    {
                        Debug.LogWarning("[EquipmentSkinSystem] Invalid profile, creating new one");
                        _currentProfile = new CharacterSkinProfile("Default");
                    }
                    else
                    {
                        // 版本檢查與自動遷移
                        int currentVersion = 1; // 當前程式碼的版本
                        if (_currentProfile.ConfigVersion < currentVersion)
                        {
                            Debug.LogWarning($"[EquipmentSkinSystem] Config version mismatch: saved={_currentProfile.ConfigVersion}, current={currentVersion}");
                            Debug.Log("[EquipmentSkinSystem] Migrating config to new version...");
                            _currentProfile = MigrateConfig(_currentProfile, currentVersion);
                            Debug.Log("[EquipmentSkinSystem] ✅ Config migration completed");
                            
                            // 自動保存遷移後的配置
                            DataPersistence.SaveConfig();
                        }
                        else
                        {
                            Debug.Log($"[EquipmentSkinSystem] Config version OK: {_currentProfile.ConfigVersion}");
                        }
                        
                        // 載入後重建 Dictionary
                        _currentProfile.RebuildDictionary();
                        Debug.Log($"[EquipmentSkinSystem] LoadFromJson - Loaded {_currentProfile.SlotConfigsList.Count} slots");
                        
                        // 驗證所有槽位都存在，如果缺少就補齊（保留原有設定）
                        bool needsUpdate = false;
                        foreach (EquipmentSlotType slotType in Enum.GetValues(typeof(EquipmentSlotType)))
                        {
                            if (!_currentProfile.SlotConfigs.ContainsKey(slotType))
                            {
                                Debug.LogWarning($"[EquipmentSkinSystem] Missing slot {slotType}, adding with default settings...");
                                _currentProfile.SlotConfigsList.Add(new SlotSkinConfig(slotType));
                                needsUpdate = true;
                            }
                        }
                        
                        if (needsUpdate)
                        {
                            _currentProfile.RebuildDictionary();
                            // 自動保存補齊後的配置
                            DataPersistence.SaveConfig();
                            Debug.Log("[EquipmentSkinSystem] ✅ Config updated with missing slots");
                        }
                    }
                }
                else
                {
                    _currentProfile = new CharacterSkinProfile("Default");
                }
            }
            catch (Exception e)
            {
                Debug.LogError($"[EquipmentSkinSystem] Failed to load profile: {e.Message}");
                Debug.LogError($"[EquipmentSkinSystem] Stack trace: {e.StackTrace}");
                _currentProfile = new CharacterSkinProfile("Default");
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
                    
                    Debug.Log($"[EquipmentSkinSystem] ParseJson - Parsed {profile.SlotConfigsList.Count} slots");
                }
            }
            catch (Exception e)
            {
                Debug.LogError($"[EquipmentSkinSystem] Failed to parse JSON: {e.Message}");
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
                            Debug.Log($"[EquipmentSkinSystem] Migrated {oldSlot.SlotType}: SkinID={oldSlot.SkinItemTypeID}, UseSkin={oldSlot.UseSkin}");
                        }
                    }
                    else
                    {
                        Debug.LogWarning($"[EquipmentSkinSystem] Slot type {oldSlot.SlotType} no longer exists in new version, skipping...");
                    }
                }
            }
            
            return newProfile;
        }

        /// <summary>
        /// 重置所有配置
        /// </summary>
        public void ResetProfile()
        {
            _currentProfile = new CharacterSkinProfile("Default");
        }
    }
}

