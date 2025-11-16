# 裝備外觀系統 (Equipment Skin System)

讓你的角色實際裝備和外觀裝備分離！

## 🚀 快速開始

### 安裝

1. 將 `ReleaseExample/EquipmentSkinSystem` 文件夾複製到：
   - **Windows**: `[遊戲目錄]/Duckov_Data/Mods/EquipmentSkinSystem/`
   - **Mac**: `[遊戲目錄]/Duckov.app/Contents/Mods/EquipmentSkinSystem/`

2. 啟動遊戲，進入 Mods 菜單，啟用「裝備外觀系統」

3. 重新啟動遊戲

### 使用方法

1. **查看物品 ID**：
   - 裝備任何物品時，遊戲日誌會自動顯示物品 ID
   - 日誌位置：`%AppData%/../LocalLow/[遊戲]/Player.log`
   - 搜索：`[EquipmentSkinSystem] 📦 裝備變更`

2. **設置外觀**：
   - 按 **F7** 打開管理界面
   - 在「實際ID」輸入框輸入實際裝備的物品 ID
   - 在「外觀ID」輸入框輸入想要顯示的外觀物品 ID
   - 勾選綠色開關啟用外觀
   - 點擊「保存配置」或按 **F8**

3. **快捷鍵**：
   - **F7** - 打開/關閉管理界面
   - **F8** - 快速保存配置

**提示：** UI 打開時會自動暫停遊戲並解鎖滑鼠。

## ✨ 功能特色

- 🎨 實際裝備提供屬性，外觀裝備決定視覺效果
- 💾 自動保存和載入配置
- 🎮 簡單易用的 UI 界面
- ⚡ 支持多個裝備槽位
- 🔄 **配置版本控制**：自動遷移舊版本配置，保留所有設定
- 📦 跨平台支援（Windows/Linux/macOS）

## 🔄 配置版本控制

從 v1.0 開始，配置檔案包含版本號。當 Mod 更新後：

- ✅ 自動檢測版本不匹配
- ✅ 自動遷移舊配置（**保留所有已設定的值**）
- ✅ 自動保存遷移後的配置
- ✅ 詳細的遷移日誌

**範例日誌**:
```
[EquipmentSkinSystem] Config version mismatch: saved=0, current=1
[EquipmentSkinSystem] Migrating config to new version...
[EquipmentSkinSystem] Migrated Helmet: SkinID=123, UseSkin=True
[EquipmentSkinSystem] ✅ Config migration completed
```

詳細資訊請參考：[CONFIG_VERSION_HISTORY.md](CONFIG_VERSION_HISTORY.md)

## 🔧 開發

### 編譯

```bash
cd EquipmentSkinSystem
dotnet build -c Release
```

### 快速發布腳本

```bash
cd EquipmentSkinSystem
chmod +x build_release.sh   # 第一次使用需要賦予執行權限
./build_release.sh
```

腳本會：
- 執行 `dotnet build -c Release`
- 將 `bin/Release/netstandard2.1/EquipmentSkinSystem.dll` 複製到 `ReleaseExample/EquipmentSkinSystem/`
- 若存在 `0Harmony.dll` 也會一併同步
- 輸出日誌位置並提示後續部署

### 反編譯遊戲 DLL（需要時）

```bash
# 安裝工具
dotnet tool install --global ilspycmd

# 運行反編譯腳本
./DECOMPILE_COMMANDS.sh
```

### 依賴項

- .NET Standard 2.1
- Harmony 2.4.1
- Unity TextMeshPro
- 遊戲 DLL（TeamSoda.*, ItemStatsSystem.dll, Unity*）

詳見 `requirements.txt`

## 📝 已知問題

1. **物品選擇器未實作** - 目前「設置」按鈕使用測試 ID
2. **需要手動輸入物品 ID** - 未來會添加物品瀏覽器
3. **實際裝備追蹤** - 需要手動設置實際裝備 ID

## 🐛 故障排除

### UI 無法操作
✅ **已修復** - UI 打開時會自動暫停遊戲並解鎖滑鼠

### Mod 無法加載
1. 檢查文件結構是否正確
2. 確認所有 DLL 文件都已複製
3. 查看遊戲日誌：`Player.log`

### 外觀不生效
1. 確認已勾選「啟用外觀」
2. 確認外觀物品 ID 正確
3. 查看日誌中的 `[EquipmentSkinSystem]` 訊息

## 📂 專案結構

```
EquipmentSkinSystem/
├── ModBehaviour.cs              # Mod 主程序
├── EquipmentSkinData.cs         # 數據模型
├── DataPersistence.cs           # 數據持久化
├── SkinManagerUI.cs             # UI 管理
├── HarmonyPatches.cs            # Harmony 補丁
├── build_release.sh             # 編譯+發布腳本
├── requirements.txt             # 依賴清單
├── DECOMPILE_COMMANDS.sh        # 反編譯腳本
└── ReleaseExample/              # 發布文件
    └── EquipmentSkinSystem/
        ├── EquipmentSkinSystem.dll
        ├── 0Harmony.dll
        ├── info.ini
        └── preview.png
```

## 🎓 技術細節

### Harmony 補丁

攔截 `CharacterEquipmentController.ChangeEquipmentModel` 方法，這是所有裝備外觀更新的核心。

### 配置文件

保存在：`Application.persistentDataPath/EquipmentSkinSystem/skin_config.json`

## 📜 授權

MIT License

## 🙏 致謝

感謝《逃離鴨科夫》開發團隊提供的 Modding 支持！
