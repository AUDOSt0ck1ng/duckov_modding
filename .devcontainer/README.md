# Dev Container 使用說明

這個 Dev Container 配置提供了完整的 Duckov Modding 開發環境。

## 🚀 快速開始

### 1. 前置需求

- 安裝 [Docker Desktop](https://www.docker.com/products/docker-desktop/)
- 安裝 [Visual Studio Code](https://code.visualstudio.com/)
- 安裝 VS Code 擴充套件：[Dev Containers](https://marketplace.visualstudio.com/items?itemName=ms-vscode-remote.remote-containers)

### 2. 配置遊戲路徑

編輯 `.devcontainer/devcontainer.json`，找到 `mounts` 區塊，取消註解並修改為您的遊戲安裝路徑：

#### Windows (WSL2)
```json
"mounts": [
  "source=/mnt/c/Program Files (x86)/Steam/steamapps/common/Escape from Duckov,target=/duckov,type=bind,readonly"
]
```

#### Linux
```json
"mounts": [
  "source=/home/username/.steam/steam/steamapps/common/Escape from Duckov,target=/duckov,type=bind,readonly"
]
```

#### Mac
```json
"mounts": [
  "source=/Users/username/Library/Application Support/Steam/steamapps/common/Escape from Duckov,target=/duckov,type=bind,readonly"
]
```

### 3. 啟動 Dev Container

1. 在 VS Code 中開啟此專案資料夾
2. 按 `F1` 或 `Ctrl+Shift+P` 開啟命令面板
3. 輸入並選擇：`Dev Containers: Reopen in Container`
4. 等待容器建立完成（首次需要下載映像檔，可能需要幾分鐘）

### 4. 編譯專案

在容器內的終端機執行：

```bash
cd DisplayItemValue
dotnet build
```

或編譯 Release 版本：

```bash
dotnet build -c Release
```

## 📝 修改專案配置

如果您使用 Dev Container，需要修改 `DisplayItemValue.csproj` 以支援容器路徑。

將 `<DuckovPath>` 設定改為：

```xml
<PropertyGroup>
    <!-- 優先使用環境變數，如果沒有則使用預設路徑 -->
    <DuckovPath Condition="'$(DuckovPath)' == ''">E:\Program Files (x86)\Steam\steamapps\common\Escape from Duckov</DuckovPath>

    <!-- 容器環境會自動使用 /duckov -->
    <DuckovPath Condition="Exists('/duckov')">/duckov</DuckovPath>
</PropertyGroup>
```

## 🔧 常用命令

### 編譯專案
```bash
dotnet build DisplayItemValue/DisplayItemValue.csproj
```

### 清理編譯輸出
```bash
dotnet clean DisplayItemValue/DisplayItemValue.csproj
```

### 還原 NuGet 套件
```bash
dotnet restore DisplayItemValue/DisplayItemValue.csproj
```

### 查看專案資訊
```bash
dotnet list DisplayItemValue/DisplayItemValue.csproj reference
```

## 📦 部署 Mod

編譯完成後，您的 DLL 檔案會在：
```
DisplayItemValue/bin/Debug/netstandard2.1/DisplayItemValue.dll
```
或
```
DisplayItemValue/bin/Release/netstandard2.1/DisplayItemValue.dll
```

將此 DLL 連同 `info.ini` 和 `preview.png` 複製到：
- Windows: `Duckov_Data/Mods/YourModName/`
- Mac: `Duckov.app/Contents/Mods/YourModName/`

## 🛠️ 疑難排解

### 問題：找不到遊戲 DLL 檔案

確認：
1. `devcontainer.json` 中的 `mounts` 路徑正確
2. 遊戲已正確安裝
3. 重新建立容器：`Dev Containers: Rebuild Container`

### 問題：編譯錯誤

檢查：
1. `.csproj` 檔案中的路徑設定
2. 遊戲版本是否與 DLL 相容
3. 查看終端機的詳細錯誤訊息

### 問題：容器啟動失敗

嘗試：
1. 確認 Docker Desktop 正在運行
2. 重新啟動 Docker Desktop
3. 刪除舊的容器映像：`docker system prune -a`

## 📚 更多資源

- [Dev Containers 文檔](https://code.visualstudio.com/docs/devcontainers/containers)
- [.NET in Docker](https://learn.microsoft.com/dotnet/core/docker/introduction)
- [Duckov Modding APIs](../Documents/NotableAPIs.md)
