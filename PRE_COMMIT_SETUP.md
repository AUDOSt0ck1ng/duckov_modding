# Pre-commit 設定指南

本專案使用 [pre-commit](https://pre-commit.com/) 來自動檢查程式碼品質，確保每次提交的程式碼都符合規範。

## 功能特色

### 自動檢查項目

1. **通用檢查**
   - 移除行尾空白字元
   - 確保文件結尾有換行
   - 檢查 YAML 語法
   - 防止提交大型文件 (>1MB)
   - 檢查大小寫衝突
   - 檢查合併衝突標記
   - 統一換行符號 (LF)
   - 偵測私鑰洩漏

2. **C# 專用檢查**
   - `dotnet format` - 自動檢查程式碼格式
   - `dotnet build` - 驗證編譯是否成功
   - 禁止使用 `Console.WriteLine` (應使用 `Logger`)
   - 檢查 TODO/FIXME 註解（提醒但不阻止）

3. **其他語言**
   - JSON/YAML/Markdown 自動格式化 (Prettier)
   - Markdown 文件 Lint 檢查
   - Shell 腳本檢查 (ShellCheck)

---

## 📦 安裝步驟

### 方法一：使用 pip（推薦）

```bash
# 1. 安裝 pre-commit
pip install pre-commit

# 或使用 pipx（隔離安裝）
pipx install pre-commit

# 2. 安裝 Git hooks
cd /workspace
pre-commit install

# 3. （可選）安裝 commit-msg hook
pre-commit install --hook-type commit-msg
```

### 方法二：使用系統套件管理器

```bash
# Ubuntu/Debian
sudo apt-get install pre-commit

# macOS (Homebrew)
brew install pre-commit

# 然後安裝 hooks
cd /workspace
pre-commit install
```

---

## 🚀 使用方法

### 自動運行（推薦）

安裝後，每次 `git commit` 時會自動執行檢查：

```bash
git add .
git commit -m "Your commit message"
# Pre-commit 會自動運行所有檢查
```

如果檢查失敗：
- 自動修復的問題（如格式化）會直接修改文件
- 需要手動修復的問題會顯示錯誤訊息
- 修復後重新 `git add` 並 `git commit`

### 手動運行

```bash
# 檢查所有暫存的文件
pre-commit run

# 檢查所有文件（不論是否暫存）
pre-commit run --all-files

# 只運行特定 hook
pre-commit run dotnet-format --all-files
pre-commit run shellcheck --all-files
```

### 跳過檢查（緊急情況）

```bash
# 跳過 pre-commit 檢查（不推薦）
git commit -m "Emergency fix" --no-verify

# 或使用環境變數
SKIP=dotnet-format git commit -m "Skip format check"
```

---

## 🔧 常見問題排查

### 問題 1：dotnet format 失敗

**錯誤訊息：**
```
❌ Code formatting issues found. Run: cd EquipmentSkinSystem && dotnet format
```

**解決方法：**
```bash
cd EquipmentSkinSystem
dotnet format
cd ..
git add .
git commit -m "Your message"
```

### 問題 2：dotnet build 失敗

**錯誤訊息：**
```
❌ Build failed. Please fix compilation errors.
```

**解決方法：**
```bash
cd EquipmentSkinSystem
dotnet build -c Release
# 查看編譯錯誤並修復
# 修復後重新提交
```

### 問題 3：發現 Console.WriteLine

**錯誤訊息：**
```
❌ Found Console.WriteLine/Write. Please use Logger instead.
```

**解決方法：**
將所有 `Console.WriteLine` 替換為 `Logger.Info` 或其他 Logger 方法：

```csharp
// ❌ 錯誤
Console.WriteLine("Hello");

// ✅ 正確
Logger.Info("Hello");
```

### 問題 4：Pre-commit 運行很慢

**原因：** `dotnet build` 在每次 commit 時都會編譯整個專案

**優化方法：**

1. 暫時禁用 build 檢查：
```bash
SKIP=dotnet-build-check git commit -m "Your message"
```

2. 或者編輯 `.pre-commit-config.yaml`，註解掉 `dotnet-build-check`：
```yaml
# - id: dotnet-build-check
#   name: dotnet build (verify compilation)
#   ...
```

---

## 📝 自訂配置

### 修改 .pre-commit-config.yaml

編輯 `/workspace/.pre-commit-config.yaml` 來客製化檢查規則：

```yaml
# 範例：禁用特定 hook
repos:
  - repo: https://github.com/pre-commit/pre-commit-hooks
    rev: v4.5.0
    hooks:
      - id: trailing-whitespace
      # - id: end-of-file-fixer  # 註解掉不需要的檢查
```

### 修改 .editorconfig

編輯 `/workspace/.editorconfig` 來調整程式碼風格：

```ini
# 範例：修改縮排大小
[*.cs]
indent_size = 2  # 改為 2 個空格
```

---

## 🔄 更新 Pre-commit Hooks

定期更新 hooks 版本：

```bash
# 更新所有 hooks 到最新版本
pre-commit autoupdate

# 重新安裝 hooks
pre-commit install --install-hooks
```

---

## 📊 CI/CD 整合

在 CI/CD 流程中也可以運行 pre-commit：

```bash
# 在 CI 環境中安裝
pip install pre-commit

# 運行所有檢查
pre-commit run --all-files

# 或者只運行特定檢查
pre-commit run --all-files --hook-stage manual
```

---

## 🎯 最佳實踐

1. **定期運行完整檢查**
   ```bash
   pre-commit run --all-files
   ```

2. **提交前先格式化**
   ```bash
   cd EquipmentSkinSystem
   dotnet format
   ```

3. **使用有意義的 commit message**
   ```bash
   git commit -m "fix: 修復裝備渲染 bug"
   git commit -m "feat: 新增狗的外觀配置"
   ```

4. **不要跳過檢查**
   - 只在緊急情況下使用 `--no-verify`
   - 事後補上修復 commit

---

## 📚 延伸閱讀

- [Pre-commit 官方文檔](https://pre-commit.com/)
- [.editorconfig 規範](https://editorconfig.org/)
- [dotnet format 文檔](https://learn.microsoft.com/en-us/dotnet/core/tools/dotnet-format)
- [C# 程式碼風格指南](https://docs.microsoft.com/en-us/dotnet/csharp/fundamentals/coding-style/coding-conventions)

---

## 🆘 需要幫助？

如果遇到問題：

1. 查看錯誤訊息
2. 參考本文檔的「常見問題排查」
3. 運行 `pre-commit run --all-files --verbose` 查看詳細輸出
4. 查看 pre-commit 日誌：`cat ~/.cache/pre-commit/pre-commit.log`
