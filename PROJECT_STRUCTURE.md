# 專案結構說明

## 📁 目錄結構

```
/workspace/
├── .devcontainer/          # Dev Container 配置
│   ├── devcontainer.json   # Container 設定
│   ├── copy-dlls.sh        # DLL 複製腳本
│   └── find-game.sh        # 遊戲路徑查找腳本
│
├── dev-tools/              # 開發工具（新增）
│   ├── DECOMPILE_COMMANDS.sh  # 反編譯腳本
│   ├── requirements.txt       # 開發依賴清單
│   └── README.md             # 工具使用說明
│
├── EquipmentSkinSystem/    # 裝備外觀系統 Mod
│   ├── *.cs                # C# 源代碼
│   ├── *.csproj            # 專案文件
│   ├── *.sln               # 方案文件
│   ├── build_release.sh    # 編譯 + 複製腳本
│   └── README.md           # Mod 說明文件
│
├── Documents/              # 遊戲 API 文檔
│   └── NotableAPIs*.md     # API 說明
│
├── duckov-dlls/            # 遊戲 DLL 文件（.gitignore）
│   └── Duckov_Data/Managed/
│
├── decompiled/             # 反編譯產出（.gitignore）
│   ├── DuckovCore/
│   ├── DuckovUtilities/
│   └── ItemStatsSystem/
│
└── Extra/                  # 額外資源
```

## 🔧 開發工具使用

### 1. 反編譯遊戲 DLL

```bash
cd /workspace/dev-tools
./DECOMPILE_COMMANDS.sh
```

### 2. 查看開發依賴

```bash
cat /workspace/dev-tools/requirements.txt
```

## 📦 Mod 開發流程

### ⚡ 一鍵編譯 + 複製

```bash
cd /workspace/EquipmentSkinSystem
chmod +x build_release.sh   # 首次使用
./build_release.sh
```

腳本會自動執行下列步驟；若要手動操作，可依照以下流程：

### 1. 編譯 Mod

```bash
cd /workspace/EquipmentSkinSystem
dotnet build -c Release
```

### 2. 複製到發布資料夾

```bash
cp bin/Release/netstandard2.1/EquipmentSkinSystem.dll ReleaseExample/EquipmentSkinSystem/
```

### 3. 部署到遊戲

將 `ReleaseExample/EquipmentSkinSystem/` 資料夾複製到遊戲的 `Duckov_Data/Mods/` 目錄。

## 🚫 .gitignore 規則

以下資料夾不會提交到 Git：

- `duckov-dlls/` - 遊戲 DLL（版權問題）
- `decompiled/` - 反編譯產出（版權問題）
- `ReleaseExample/` - 編譯產出（用戶自行編譯）
- `bin/`, `obj/` - 編譯中間文件
- `.vs/`, `.vscode/` - IDE 配置

## 📝 Git 設定

首次使用時，請設定 Git 使用者資訊：

```bash
# 僅此專案
git config user.email "your@email.com"
git config user.name "Your Name"

# 或全域設定
git config --global user.email "your@email.com"
git config --global user.name "Your Name"
```

## 🔗 相關文件

- [Mod 開發說明](EquipmentSkinSystem/README.md)
- [配置版本歷史](EquipmentSkinSystem/CONFIG_VERSION_HISTORY.md)
- [開發工具說明](dev-tools/README.md)
- [遊戲 API 文檔](Documents/NotableAPIs_CN.md)
- [Dev Container 設定](.devcontainer/README.md)

