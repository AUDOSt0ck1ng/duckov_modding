# WSL2 環境 Dev Container 快速設定指南

## 🎯 兩種設定方式

### 方案 A：複製 DLL 到 WSL2（推薦）✨
- ✅ 效能更好
- ✅ 路徑簡單
- ✅ 不需要 `/mnt` 掛載
- ⚠️ 需要約 500MB 空間
- ⚠️ 遊戲更新後需要重新複製

### 方案 B：直接掛載 Windows 遊戲目錄
- ✅ 不佔用 WSL2 空間
- ✅ 遊戲更新自動同步
- ⚠️ 效能較慢
- ⚠️ 需要處理 `/mnt` 路徑

---

## 📋 前置需求檢查

在 WSL2 終端機中執行以下命令確認環境：

```bash
# 檢查是否在 WSL2 中
uname -r
# 應該會看到類似 "5.x.x-microsoft-standard-WSL2" 的輸出

# 檢查 Docker 是否可用
docker --version

# 檢查 VS Code 是否已安裝 Remote 擴充套件
code --list-extensions | grep ms-vscode-remote.remote-containers
```

## 🎯 方案 A：複製 DLL 到 WSL2（推薦）

### 1. 執行自動複製腳本

```bash
cd /home/hhc102u/docs/Github/duckov/duckov_modding
bash .devcontainer/copy-dlls.sh
```

腳本會自動：
- 尋找遊戲目錄
- 複製所有需要的 DLL 檔案到 `~/duckov-dlls/`
- 顯示複製結果

如果自動尋找失敗，可以手動指定路徑：

```bash
bash .devcontainer/copy-dlls.sh "/mnt/c/您的遊戲路徑/Escape from Duckov"
```

### 2. 確認 devcontainer.json 設定

開啟 `.devcontainer/devcontainer.json`，確認使用方案 A（預設已設定）：

```json
"mounts": [
  "source=${localEnv:HOME}/duckov-dlls,target=/duckov,type=bind,readonly"
],
```

### 3. 啟動 Dev Container

完成！現在可以啟動容器了。

---

## 🎯 方案 B：直接掛載 Windows 遊戲目錄

### 1. 找到您的遊戲安裝路徑

在 WSL2 終端機中執行：

```bash
# 檢查 C 槽的 Steam 預設路徑
ls "/mnt/c/Program Files (x86)/Steam/steamapps/common/Escape from Duckov"

# 如果遊戲在 D 槽
ls "/mnt/d/Steam/steamapps/common/Escape from Duckov"

# 如果遊戲在 E 槽
ls "/mnt/e/Games/Escape from Duckov"
```

找到正確路徑後，記下來！

### 2. 修改 devcontainer.json

開啟 `.devcontainer/devcontainer.json`，將方案 A 註解掉，啟用方案 B：

```json
// 方案 A：使用 WSL2 本地路徑（推薦，效能更好）
// "mounts": [
//   "source=${localEnv:HOME}/duckov-dlls,target=/duckov,type=bind,readonly"
// ],

// 方案 B：直接從 Windows 掛載（如果您想用原始遊戲目錄）
"mounts": [
  "source=/mnt/c/Program Files (x86)/Steam/steamapps/common/Escape from Duckov,target=/duckov,type=bind,readonly"
],
```

**將 `source=` 後面的路徑改為您在步驟 1 找到的路徑。**

### 3. 啟動 Dev Container

1. 確保 Docker Desktop 正在運行（在 Windows 中啟動）
2. 在 WSL2 中用 VS Code 開啟專案：
   ```bash
   cd /home/hhc102u/docs/Github/duckov/duckov_modding
   code .
   ```
3. 在 VS Code 中按 `F1`，輸入：`Dev Containers: Reopen in Container`
4. 等待容器建立（首次需要幾分鐘下載映像檔）

### 4. 驗證環境

容器啟動後，在 VS Code 的終端機中執行：

```bash
# 檢查 .NET SDK
dotnet --version

# 檢查遊戲 DLL 是否可訪問
ls /duckov/Duckov_Data/Managed/TeamSoda*.dll

# 嘗試編譯專案
cd DisplayItemValue
dotnet build
```

如果都成功，恭喜！環境設定完成！🎉

## 🔧 常見問題

### Q: 找不到遊戲 DLL 檔案

**A:** 檢查以下幾點：
1. 確認 `devcontainer.json` 中的路徑正確
2. 確認路徑中的空格是否正確（如 "Program Files (x86)"）
3. 重新建立容器：`Dev Containers: Rebuild Container`

### Q: Docker 無法啟動

**A:** 在 Windows 中：
1. 確認 Docker Desktop 已啟動
2. 確認 Docker Desktop 設定中啟用了 "Use the WSL 2 based engine"
3. 在 Docker Desktop 的 Resources > WSL Integration 中啟用您的 WSL2 發行版

### Q: 編譯時找不到參考

**A:** 檢查 `.csproj` 檔案：
- 現在已經自動偵測容器環境
- 如果在容器中，會自動使用 `/duckov` 路徑
- 如果在本地，會使用您設定的 Windows 路徑

### Q: 權限問題

**A:** 如果遇到權限問題：
```bash
# 在 WSL2 中修改專案資料夾權限
sudo chown -R $USER:$USER /home/hhc102u/docs/Github/duckov/duckov_modding
```

## 📦 編譯和部署

### 編譯 Mod

```bash
cd DisplayItemValue
dotnet build -c Release
```

### 找到編譯輸出

```bash
ls -la bin/Release/netstandard2.1/DisplayItemValue.dll
```

### 部署到遊戲

從 WSL2 複製到 Windows 遊戲目錄：

```bash
# 建立 Mod 資料夾
mkdir -p "/mnt/c/Program Files (x86)/Steam/steamapps/common/Escape from Duckov/Duckov_Data/Mods/DisplayItemValue"

# 複製檔案
cp bin/Release/netstandard2.1/DisplayItemValue.dll "/mnt/c/Program Files (x86)/Steam/steamapps/common/Escape from Duckov/Duckov_Data/Mods/DisplayItemValue/"
cp ReleaseExample/DisplayItemValue/info.ini "/mnt/c/Program Files (x86)/Steam/steamapps/common/Escape from Duckov/Duckov_Data/Mods/DisplayItemValue/"
cp ReleaseExample/DisplayItemValue/preview.png "/mnt/c/Program Files (x86)/Steam/steamapps/common/Escape from Duckov/Duckov_Data/Mods/DisplayItemValue/"
```

## 🚀 開發工作流程

1. **在容器中編輯程式碼**
2. **編譯**：`dotnet build`
3. **複製到遊戲目錄**（使用上面的命令）
4. **啟動遊戲測試**
5. **重複步驟 1-4**

## 💡 提示

- 容器環境與本地環境隔離，不會影響您的 WSL2 系統
- 所有變更都會同步到您的本地檔案系統
- 可以隨時退出容器：`Dev Containers: Reopen Folder Locally`
- 容器會保留，下次啟動會更快

## 📚 更多資源

- [WSL2 文檔](https://docs.microsoft.com/windows/wsl/)
- [Docker Desktop for Windows](https://docs.docker.com/desktop/windows/wsl/)
- [VS Code Dev Containers](https://code.visualstudio.com/docs/devcontainers/containers)

