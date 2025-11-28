#!/bin/bash

echo "🔍 尋找《逃離鴨科夫》遊戲安裝位置..."
echo ""

# 可能的路徑列表
PATHS=(
    "/mnt/c/Program Files (x86)/Steam/steamapps/common/Escape from Duckov"
    "/mnt/d/Steam/steamapps/common/Escape from Duckov"
    "/mnt/e/Steam/steamapps/common/Escape from Duckov"
    "/mnt/e/Program Files (x86)/Steam/steamapps/common/Escape from Duckov"
    "$HOME/.steam/steam/steamapps/common/Escape from Duckov"
    "$HOME/.local/share/Steam/steamapps/common/Escape from Duckov"
)

FOUND=0

for path in "${PATHS[@]}"; do
    if [ -d "$path" ]; then
        echo "✅ 找到遊戲！"
        echo "   路徑: $path"
        echo ""

        # 檢查關鍵檔案
        if [ -f "$path/Duckov_Data/Managed/TeamSoda.dll" ] || [ -f "$path/Duckov.app/Contents/Resources/Data/Managed/TeamSoda.dll" ]; then
            echo "✅ 確認遊戲檔案完整"
            echo ""
            echo "📝 請將以下路徑複製到 .devcontainer/devcontainer.json 的 mounts 設定中："
            echo ""
            echo "\"source=$path,target=/duckov,type=bind,readonly\""
            echo ""
        else
            echo "⚠️  找到目錄但檔案可能不完整"
            echo ""
        fi

        FOUND=1
    fi
done

if [ $FOUND -eq 0 ]; then
    echo "❌ 在常見位置找不到遊戲"
    echo ""
    echo "請手動尋找遊戲目錄："
    echo "1. 在 Windows 中開啟 Steam"
    echo "2. 右鍵點擊遊戲 → 管理 → 瀏覽本機檔案"
    echo "3. 複製路徑列，例如: C:\\Program Files (x86)\\Steam\\steamapps\\common\\Escape from Duckov"
    echo "4. 轉換為 WSL2 路徑："
    echo "   - 將 C:\\ 改為 /mnt/c/"
    echo "   - 將 \\ 改為 /"
    echo "   - 例如: /mnt/c/Program Files (x86)/Steam/steamapps/common/Escape from Duckov"
fi
