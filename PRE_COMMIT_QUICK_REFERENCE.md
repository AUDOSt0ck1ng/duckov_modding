# Pre-commit 快速參考卡

## 🚀 快速開始

```bash
# 一鍵安裝
./setup-pre-commit.sh

# 測試是否正常
pre-commit run --all-files
```

## 📝 常用命令

| 命令 | 說明 |
|------|------|
| `pre-commit run` | 檢查已暫存的文件 |
| `pre-commit run --all-files` | 檢查所有文件 |
| `pre-commit run <hook-id>` | 只運行特定 hook |
| `pre-commit install` | 安裝 Git hooks |
| `pre-commit uninstall` | 移除 Git hooks |
| `pre-commit autoupdate` | 更新 hooks 版本 |
| `pre-commit clean` | 清除快取 |

## 🔧 修復常見問題

### 格式問題

```bash
cd EquipmentSkinSystem
dotnet format
```

### 編譯失敗

```bash
cd EquipmentSkinSystem
dotnet build -c Release
# 修復錯誤後重新提交
```

### 找到 Console.WriteLine

將 `Console.WriteLine()` 改為 `Logger.Info()`

## ⚡ 跳過檢查（緊急用）

```bash
# 跳過所有檢查
git commit --no-verify

# 跳過特定檢查
SKIP=dotnet-build-check git commit -m "message"

# 跳過多個檢查
SKIP=dotnet-build-check,dotnet-format git commit -m "message"
```

## 🎯 Hook 列表

### 通用檢查
- `trailing-whitespace` - 移除行尾空白
- `end-of-file-fixer` - 確保文件結尾有換行
- `check-yaml` - YAML 語法檢查
- `check-added-large-files` - 防止提交大文件
- `check-merge-conflict` - 檢查合併衝突
- `mixed-line-ending` - 統一換行符號

### C# 檢查
- `dotnet-format` - C# 程式碼格式化
- `dotnet-build-check` - 驗證編譯
- `check-logger-usage` - 禁止 Console.WriteLine
- `check-debug-code` - 檢查 TODO/FIXME

### 其他
- `prettier` - JSON/YAML/Markdown 格式化
- `markdownlint` - Markdown lint
- `shellcheck` - Shell 腳本檢查

## 🔍 故障排除

| 問題 | 解決方法 |
|------|----------|
| 找不到 pre-commit | `export PATH="$HOME/.local/bin:$PATH"` |
| dotnet format 失敗 | `cd EquipmentSkinSystem && dotnet format` |
| 運行太慢 | 使用 `.pre-commit-config-minimal.yaml` |
| Hook 下載失敗 | `pre-commit clean && pre-commit install --install-hooks` |

## 📚 詳細文檔

- [PRE_COMMIT_SETUP.md](PRE_COMMIT_SETUP.md) - 完整安裝和使用指南
- [TEST_PRE_COMMIT.md](TEST_PRE_COMMIT.md) - 測試步驟
- [.editorconfig](.editorconfig) - 程式碼風格配置

## 💡 最佳實踐

1. **提交前先格式化**
   ```bash
   cd EquipmentSkinSystem && dotnet format
   ```

2. **定期更新 hooks**
   ```bash
   pre-commit autoupdate
   ```

3. **不要濫用 `--no-verify`**
   - 只在緊急情況使用
   - 事後補上修復 commit

4. **團隊協作**
   - 確保所有成員都安裝了 pre-commit
   - 統一使用相同的配置
