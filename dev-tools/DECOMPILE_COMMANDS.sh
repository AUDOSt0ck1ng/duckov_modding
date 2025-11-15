#!/bin/bash
# 反編譯遊戲 DLL 的便捷腳本

# 顏色定義
GREEN='\033[0;32m'
YELLOW='\033[1;33m'
RED='\033[0;31m'
NC='\033[0m' # No Color

echo -e "${GREEN}=== 遊戲 DLL 反編譯腳本 ===${NC}"
echo ""

# 檢查 ilspycmd 是否安裝
if ! command -v ilspycmd &> /dev/null; then
    echo -e "${RED}錯誤：ilspycmd 未安裝${NC}"
    echo "請執行以下命令安裝："
    echo "  dotnet tool install --global ilspycmd"
    exit 1
fi

# 設置路徑
GAME_DLL_PATH="/duckov/Duckov_Data/Managed"
OUTPUT_PATH="/workspace/decompiled"

# 創建輸出目錄
echo -e "${YELLOW}創建輸出目錄...${NC}"
mkdir -p "$OUTPUT_PATH"

# 反編譯函數
decompile_dll() {
    local dll_name=$1
    local output_dir=$2
    
    echo -e "${YELLOW}正在反編譯 ${dll_name}...${NC}"
    
    if [ -f "$GAME_DLL_PATH/$dll_name" ]; then
        ilspycmd "$GAME_DLL_PATH/$dll_name" -p -o "$OUTPUT_PATH/$output_dir/"
        echo -e "${GREEN}✓ ${dll_name} 反編譯完成${NC}"
    else
        echo -e "${RED}✗ 找不到 ${dll_name}${NC}"
    fi
}

# 反編譯主要的 DLL
echo ""
echo -e "${GREEN}開始反編譯遊戲 DLL...${NC}"
echo ""

decompile_dll "TeamSoda.Duckov.Core.dll" "DuckovCore"
decompile_dll "TeamSoda.Duckov.Utilities.dll" "DuckovUtilities"
decompile_dll "ItemStatsSystem.dll" "ItemStatsSystem"

echo ""
echo -e "${GREEN}=== 反編譯完成 ===${NC}"
echo ""
echo "輸出目錄：$OUTPUT_PATH"
echo ""
echo -e "${YELLOW}接下來可以執行以下命令搜索相關代碼：${NC}"
echo ""
echo "# 搜索 Character 相關的類"
echo "grep -r 'class.*Character' $OUTPUT_PATH/"
echo ""
echo "# 搜索 Equipment 相關的類"
echo "grep -r 'class.*Equipment' $OUTPUT_PATH/"
echo ""
echo "# 搜索 Visual 相關的方法"
echo "grep -r 'Visual' $OUTPUT_PATH/ | grep 'public'"
echo ""
echo "# 搜索 Render 相關的方法"
echo "grep -r 'Render' $OUTPUT_PATH/ | grep 'public'"
echo ""
echo "# 搜索 Slot 相關的類"
echo "grep -r 'class.*Slot' $OUTPUT_PATH/"
echo ""

