# Pre-commit 測試指南

本文檔說明如何測試 pre-commit 配置是否正確。

## 環境要求

在執行測試前，請確保已安裝：

- Python 3.x
- pip（Python 套件管理器）
- Git
- .NET SDK（用於 C# 檢查）

## 安裝 Pre-commit

### 選項 1：使用安裝腳本（推薦）

```bash
cd /workspace
./setup-pre-commit.sh
```

### 選項 2：手動安裝

```bash
# 1. 安裝 Python 和 pip（如果尚未安裝）
sudo apt-get update
sudo apt-get install -y python3 python3-pip

# 2. 安裝 pre-commit
pip3 install pre-commit --user

# 3. 將 pip 安裝路徑加入 PATH（如果需要）
export PATH="$HOME/.local/bin:$PATH"

# 4. 驗證安裝
pre-commit --version

# 5. 安裝 Git hooks
cd /workspace
pre-commit install
```

## 測試步驟

### 1. 驗證配置文件語法

```bash
cd /workspace

# 檢查 YAML 語法
pre-commit validate-config

# 應該輸出：
# .pre-commit-config.yaml is valid
```

### 2. 測試個別 Hook

#### 測試通用檢查

```bash
# 測試所有通用檢查
pre-commit run --all-files trailing-whitespace
pre-commit run --all-files end-of-file-fixer
pre-commit run --all-files check-yaml
```

#### 測試 C# 格式檢查

```bash
# 先確保專案可以編譯
cd EquipmentSkinSystem
dotnet restore
dotnet build -c Release
cd ..

# 測試 dotnet format
pre-commit run --all-files dotnet-format

# 測試編譯檢查
pre-commit run --all-files dotnet-build-check
```

#### 測試自訂檢查

```bash
# 檢查是否使用了 Console.WriteLine
pre-commit run --all-files check-logger-usage

# 檢查 debug 程式碼
pre-commit run --all-files check-debug-code
```

### 3. 運行所有檢查

```bash
cd /workspace

# 運行所有 hooks（針對已暫存的文件）
pre-commit run

# 運行所有 hooks（針對所有文件）
pre-commit run --all-files
```

預期輸出範例：
```
Trim trailing whitespace.................................................Passed
Fix end of files.........................................................Passed
Check Yaml...............................................................Passed
Check for added large files..............................................Passed
Check for case conflicts.................................................Passed
Check for merge conflicts................................................Passed
Check mixed line endings.................................................Passed
Detect private keys......................................................Passed
dotnet format (C# code formatting).......................................Passed
dotnet build (verify compilation)........................................Passed
Check Logger usage (no Console.WriteLine)................................Passed
Check for debug code.....................................................Passed
Prettier (JSON, YAML, Markdown)..........................................Passed
Markdown lint............................................................Passed
ShellCheck (bash scripts)................................................Passed
```

### 4. 測試 Git Commit 觸發

```bash
# 創建一個測試變更
cd /workspace
echo "# Test" > test_file.md
git add test_file.md

# 嘗試提交（會自動觸發 pre-commit）
git commit -m "test: pre-commit functionality"

# 如果所有檢查通過，commit 會成功
# 如果有檢查失敗，會顯示錯誤並阻止 commit

# 清理測試文件
git reset HEAD test_file.md
rm test_file.md
```

## 故障排除

### 問題 1：pre-commit 命令找不到

```bash
# 檢查 pre-commit 是否已安裝
which pre-commit

# 如果沒有輸出，嘗試將 pip 路徑加入 PATH
export PATH="$HOME/.local/bin:$PATH"

# 或者重新安裝
pip3 install --user --force-reinstall pre-commit
```

### 問題 2：dotnet 相關檢查失敗

```bash
# 確保 .NET SDK 已安裝
dotnet --version

# 確保專案可以正常編譯
cd EquipmentSkinSystem
dotnet restore
dotnet build -c Release

# 如果編譯失敗，修復錯誤後再運行 pre-commit
```

### 問題 3：某些 hook 一直下載很慢

```bash
# 手動安裝所有 hooks
pre-commit install --install-hooks

# 清除快取重新下載
pre-commit clean
pre-commit install --install-hooks
```

### 問題 4：YAML 配置錯誤

```bash
# 驗證配置文件
pre-commit validate-config

# 如果有錯誤，檢查 .pre-commit-config.yaml 的語法
# 確保縮排正確（使用 2 個空格）
```

## 常用測試命令

```bash
# 僅測試 C# 文件
pre-commit run --files EquipmentSkinSystem/*.cs

# 跳過某個 hook 進行測試
SKIP=dotnet-build-check pre-commit run --all-files

# 顯示詳細輸出
pre-commit run --all-files --verbose

# 強制重新運行（即使文件沒有變更）
pre-commit run --all-files --force

# 只運行快速檢查（跳過編譯）
SKIP=dotnet-build-check,dotnet-format pre-commit run --all-files
```

## 性能測試

測試 pre-commit 的執行時間：

```bash
# 測量運行時間
time pre-commit run --all-files

# 如果太慢（>30秒），考慮：
# 1. 禁用 dotnet-build-check
# 2. 只在 CI 中運行完整檢查
# 3. 使用 SKIP 跳過耗時的檢查
```

## 成功標準

如果以下測試都通過，說明 pre-commit 配置成功：

- ✅ `pre-commit validate-config` 成功
- ✅ `pre-commit run --all-files` 所有檢查通過
- ✅ Git commit 時自動觸發檢查
- ✅ 格式問題會被自動修復
- ✅ 編譯錯誤會阻止 commit

## 下一步

測試成功後：

1. 閱讀 [PRE_COMMIT_SETUP.md](PRE_COMMIT_SETUP.md) 了解詳細用法
2. 將 pre-commit 配置提交到 Git：
   ```bash
   git add .pre-commit-config.yaml .editorconfig
   git commit -m "chore: add pre-commit hooks configuration"
   ```
3. 在團隊中推廣使用 pre-commit
