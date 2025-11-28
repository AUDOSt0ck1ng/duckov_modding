#!/bin/bash
# Pre-commit 快速安裝腳本

set -e  # 遇到錯誤時立即退出

echo "======================================"
echo "  Pre-commit 快速安裝腳本"
echo "======================================"
echo ""

# 檢查是否在正確的目錄
if [ ! -f ".pre-commit-config.yaml" ]; then
    echo "❌ 錯誤：找不到 .pre-commit-config.yaml"
    echo "   請在專案根目錄運行此腳本"
    exit 1
fi

# 檢查是否已安裝 pre-commit
if ! command -v pre-commit &> /dev/null; then
    echo "📦 Pre-commit 未安裝，開始安裝..."

    # 嘗試使用 pip 安裝
    if command -v pip3 &> /dev/null; then
        echo "   使用 pip3 安裝 pre-commit..."
        pip3 install pre-commit --user
    elif command -v pip &> /dev/null; then
        echo "   使用 pip 安裝 pre-commit..."
        pip install pre-commit --user
    else
        echo "❌ 錯誤：找不到 pip 或 pip3"
        echo "   請手動安裝 pre-commit："
        echo "   - Ubuntu/Debian: sudo apt-get install pre-commit"
        echo "   - macOS: brew install pre-commit"
        echo "   - 或參考：https://pre-commit.com/#installation"
        exit 1
    fi
else
    echo "✅ Pre-commit 已安裝：$(pre-commit --version)"
fi

echo ""
echo "🔧 安裝 Git hooks..."
pre-commit install

echo ""
echo "📥 下載並安裝所有 hook 依賴..."
pre-commit install --install-hooks

echo ""
echo "======================================"
echo "✅ Pre-commit 安裝完成！"
echo "======================================"
echo ""
echo "📝 下一步："
echo ""
echo "1. 測試 pre-commit："
echo "   pre-commit run --all-files"
echo ""
echo "2. 查看詳細文檔："
echo "   cat PRE_COMMIT_SETUP.md"
echo ""
echo "3. 進行第一次提交："
echo "   git add ."
echo "   git commit -m \"chore: setup pre-commit hooks\""
echo ""
echo "🎉 Pre-commit 將在每次 git commit 時自動運行！"
echo ""
