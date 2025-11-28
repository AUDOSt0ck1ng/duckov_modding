# 配置版本歷史

## 版本控制說明

從 v1.0 開始，配置檔案包含 `ConfigVersion` 欄位，用於追蹤配置結構的變更。

當 Mod 更新後，如果配置結構有變更（例如新增槽位、修改欄位），系統會自動：
1. 檢測版本不匹配
2. 遷移舊配置資料（保留所有已設定的值）
3. 更新到新版本結構
4. 自動保存遷移後的配置

---

## 版本歷史

### Version 1 (當前版本)
**發布日期**: 2025-11-16

**配置結構**:
```json
{
    "ConfigVersion": 1,
    "ProfileName": "Default",
    "SlotConfigsList": [
        {
            "SlotType": 0,  // Armor
            "SkinItemTypeID": 0,
            "UseSkin": false
        },
        {
            "SlotType": 1,  // Helmet
            "SkinItemTypeID": 0,
            "UseSkin": false
        },
        {
            "SlotType": 2,  // FaceMask
            "SkinItemTypeID": 0,
            "UseSkin": false
        },
        {
            "SlotType": 3,  // Backpack
            "SkinItemTypeID": 0,
            "UseSkin": false
        },
        {
            "SlotType": 4,  // Headset
            "SkinItemTypeID": 0,
            "UseSkin": false
        }
    ]
}
```

**支援的槽位**:
- `Armor` (0): 護甲（身體）
- `Helmet` (1): 頭盔
- `FaceMask` (2): 面罩
- `Backpack` (3): 背包
- `Headset` (4): 耳機

**變更內容**:
- ✅ 初始版本
- ✅ 使用 `List<SlotSkinConfig>` 支援 JsonUtility 序列化
- ✅ 支援 SkinItemTypeID: `-1` = 隱藏, `0` = 原樣, `>0` = 替換
- ✅ 版本控制系統

---

## 未來版本規劃

### Version 2 (預計)
**可能的變更**:
- 新增更多槽位類型（如果遊戲更新）
- 支援多個配置檔案（Profile 切換）
- 新增快捷鍵設定

**遷移策略**:
- 保留所有 v1 的槽位設定
- 新槽位使用預設值
- 自動更新 ConfigVersion 到 2

---

## 開發者指南

### 如何新增新版本

1. **修改 `EquipmentSkinData.cs`**:
   ```csharp
   // 在 LoadFromJson 中更新當前版本號
   int currentVersion = 2; // 從 1 改為 2
   ```

2. **更新 `MigrateConfig` 方法**（如果需要特殊遷移邏輯）:
   ```csharp
   private CharacterSkinProfile MigrateConfig(CharacterSkinProfile oldProfile, int targetVersion)
   {
       // 根據 oldProfile.ConfigVersion 和 targetVersion 決定遷移策略
       if (oldProfile.ConfigVersion == 1 && targetVersion == 2)
       {
           // v1 -> v2 的特殊遷移邏輯
       }

       // ... 其他遷移邏輯
   }
   ```

3. **更新此文檔**，記錄新版本的變更內容

### 遷移原則

- ✅ **向後兼容**: 舊配置必須能正確載入
- ✅ **保留資料**: 遷移時保留所有用戶設定
- ✅ **自動保存**: 遷移完成後自動保存新版本
- ✅ **詳細日誌**: 記錄遷移過程以便除錯

---

## 故障排除

### 配置無法載入
**症狀**: 遊戲日誌顯示 "Invalid profile, creating new one"

**可能原因**:
1. JSON 格式損壞
2. 版本遷移失敗

**解決方案**:
1. 檢查日誌中的詳細錯誤訊息
2. 備份並刪除舊配置檔案: `C:\Users\你的用戶名\AppData\LocalLow\TeamSoda\Duckov\EquipmentSkinSystem\skin_config.json`
3. 重新啟動遊戲，系統會創建新的預設配置

### 版本遷移失敗
**症狀**: 遊戲日誌顯示 "Config version mismatch" 但設定丟失

**解決方案**:
1. 查看日誌中的遷移詳情
2. 如果是特定槽位丟失，可能是該槽位在新版本中被移除
3. 手動重新設定該槽位

---

## 配置檔案位置

### Windows
```
C:\Users\你的用戶名\AppData\LocalLow\TeamSoda\Duckov\EquipmentSkinSystem\skin_config.json
```

### Linux
```
~/.config/unity3d/TeamSoda/Duckov/EquipmentSkinSystem/skin_config.json
```

### macOS
```
~/Library/Application Support/TeamSoda/Duckov/EquipmentSkinSystem/skin_config.json
```
