#!/bin/bash

echo "🎮 複製《逃離鴨科夫》DLL 檔案到 WSL2"
echo "=========================================="
echo ""

# 目標目錄（在專案根目錄下）
SCRIPT_DIR="$(cd "$(dirname "${BASH_SOURCE[0]}")" && pwd)"
PROJECT_ROOT="$(dirname "$SCRIPT_DIR")"
TARGET_DIR="$PROJECT_ROOT/duckov-dlls/Duckov_Data/Managed"

# 可能的遊戲路徑
GAME_PATHS=(
    "/mnt/c/Program Files (x86)/Steam/steamapps/common/Escape from Duckov"
    "/mnt/d/Steam/steamapps/common/Escape from Duckov"
    "/mnt/e/Steam/steamapps/common/Escape from Duckov"
    "/mnt/e/Program Files (x86)/Steam/steamapps/common/Escape from Duckov"
)

# 尋找遊戲目錄
GAME_DIR=""
for path in "${GAME_PATHS[@]}"; do
    if [ -d "$path/Duckov_Data/Managed" ]; then
        GAME_DIR="$path"
        echo "✅ 找到遊戲目錄: $GAME_DIR"
        break
    fi
done

if [ -z "$GAME_DIR" ]; then
    echo "❌ 找不到遊戲目錄！"
    echo ""
    echo "請手動指定遊戲路徑："
    echo "  bash $0 \"/mnt/c/您的遊戲路徑/Escape from Duckov\""
    exit 1
fi

# 如果提供了參數，使用參數作為遊戲目錄
if [ ! -z "$1" ]; then
    GAME_DIR="$1"
    echo "📁 使用指定的遊戲目錄: $GAME_DIR"
fi

SOURCE_DIR="$GAME_DIR/Duckov_Data/Managed"

# 檢查來源目錄
if [ ! -d "$SOURCE_DIR" ]; then
    echo "❌ 找不到 DLL 目錄: $SOURCE_DIR"
    exit 1
fi

# 建立目標目錄
echo ""
echo "📂 建立目標目錄: $TARGET_DIR"
mkdir -p "$TARGET_DIR"

# 複製 DLL 檔案
echo ""
echo "📦 複製 DLL 檔案..."
echo ""

# 複製所有需要的 DLL
cp -v "$SOURCE_DIR"/TeamSoda.*.dll "$TARGET_DIR/" 2>/dev/null
cp -v "$SOURCE_DIR"/ItemStatsSystem.dll "$TARGET_DIR/" 2>/dev/null
cp -v "$SOURCE_DIR"/Unity*.dll "$TARGET_DIR/" 2>/dev/null
cp -v "$SOURCE_DIR"/UnityEngine*.dll "$TARGET_DIR/" 2>/dev/null
cp -v "$SOURCE_DIR"/netstandard.dll "$TARGET_DIR/" 2>/dev/null
cp -v "$SOURCE_DIR"/mscorlib.dll "$TARGET_DIR/" 2>/dev/null

# 統計
DLL_COUNT=$(ls -1 "$TARGET_DIR"/*.dll 2>/dev/null | wc -l)

echo ""
echo "=========================================="
echo "✅ 完成！共複製 $DLL_COUNT 個 DLL 檔案"
echo ""
echo "📍 檔案位置: $TARGET_DIR"
echo ""
echo "🔧 接下來："
echo "   1. 確認 .devcontainer/devcontainer.json 使用方案 A"
echo "   2. 在 VS Code 中重新開啟容器"
echo "   3. 執行 'dotnet build' 測試編譯"
echo ""

# 顯示檔案列表
echo "📋 已複製的檔案："
ls -lh "$TARGET_DIR"/*.dll 2>/dev/null | awk '{print "   " $9 " (" $5 ")"}'

