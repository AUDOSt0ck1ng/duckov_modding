# 開發工具 (Development Tools)

這個資料夾包含用於開發 Escape from Duckov Mod 的工具和腳本。

## 📁 文件說明

### `requirements.txt`
開發環境所需的工具和依賴清單。

**安裝方式：**
```bash
# 安裝 .NET 工具
dotnet tool install --global ilspycmd

# 或者參考 requirements.txt 中的說明
```

### `DECOMPILE_COMMANDS.sh`
反編譯遊戲 DLL 的便捷腳本。

**使用方式：**
```bash
cd /workspace/dev-tools
chmod +x DECOMPILE_COMMANDS.sh
./DECOMPILE_COMMANDS.sh
```

**輸出位置：**
- 反編譯結果會輸出到 `/workspace/decompiled/`

**反編譯的 DLL：**
- `TeamSoda.Duckov.Core.dll` → `/workspace/decompiled/DuckovCore/`
- `TeamSoda.Duckov.Utilities.dll` → `/workspace/decompiled/DuckovUtilities/`
- `ItemStatsSystem.dll` → `/workspace/decompiled/ItemStatsSystem/`

## 🔍 常用搜索命令

反編譯完成後，可以使用以下命令搜索代碼：

```bash
# 搜索 Character 相關的類
grep -r 'class.*Character' /workspace/decompiled/

# 搜索 Equipment 相關的類
grep -r 'class.*Equipment' /workspace/decompiled/

# 搜索 Visual 相關的方法
grep -r 'Visual' /workspace/decompiled/ | grep 'public'

# 搜索 Slot 相關的類
grep -r 'class.*Slot' /workspace/decompiled/
```

## 📝 注意事項

1. **反編譯結果僅供開發參考**，不應該直接複製使用
2. **反編譯結果已加入 `.gitignore`**，不會提交到版本控制
3. 反編譯需要遊戲 DLL 文件，確保已正確掛載遊戲目錄

## 🔗 相關資源

- [ILSpy 文檔](https://github.com/icsharpcode/ILSpy)
- [Harmony 文檔](https://harmony.pardeike.net/)
- [遊戲 Modding 文檔](../Documents/)

