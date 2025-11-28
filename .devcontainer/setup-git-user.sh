#!/bin/bash
# Git 使用者設定腳本

set -e

echo "======================================"
echo "  Git 使用者設定"
echo "======================================"
echo ""

# 讀取當前的 Git 設定
CURRENT_NAME=$(git config --global user.name 2>/dev/null || echo "")
CURRENT_EMAIL=$(git config --global user.email 2>/dev/null || echo "")

if [ -n "$CURRENT_NAME" ] && [ -n "$CURRENT_EMAIL" ]; then
    echo "✅ Git 使用者已設定："
    echo "   姓名: $CURRENT_NAME"
    echo "   Email: $CURRENT_EMAIL"
    echo ""
    read -p "是否要重新設定？(y/N): " -n 1 -r
    echo ""
    if [[ ! $REPLY =~ ^[Yy]$ ]]; then
        echo "保持現有設定"
        exit 0
    fi
fi

# 設定使用者名稱
echo "請輸入您的 Git 使用者名稱："
read -p "Name: " GIT_NAME

# 設定 Email
echo "請輸入您的 Git Email："
read -p "Email: " GIT_EMAIL

# 驗證輸入
if [ -z "$GIT_NAME" ] || [ -z "$GIT_EMAIL" ]; then
    echo "❌ 錯誤：姓名和 Email 不能為空"
    exit 1
fi

# 應用設定
git config --global user.name "$GIT_NAME"
git config --global user.email "$GIT_EMAIL"

echo ""
echo "======================================"
echo "✅ Git 設定完成！"
echo "======================================"
echo "   姓名: $(git config --global user.name)"
echo "   Email: $(git config --global user.email)"
echo ""
