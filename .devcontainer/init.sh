#!/bin/bash
set -e

echo "=========================================="
echo "初始化 Duckov Modding 開發環境..."
echo "=========================================="

# 安裝 Python3 和 pre-commit
echo "安裝 Python3 和 pre-commit..."
apt-get update -qq
apt-get install -y python3 python3-pip pre-commit > /dev/null 2>&1

# 安裝 pre-commit hooks
echo "配置 pre-commit hooks..."
cd /workspace
pre-commit install

# 設定 Git 安全目錄
echo "配置 Git..."
git config --global --add safe.directory /workspace

# 顯示版本資訊
echo ""
echo "=========================================="
echo "環境資訊："
echo "=========================================="
dotnet --version | awk '{print "  .NET SDK: " $0}'
python3 --version | awk '{print "  Python: " $0}'
pre-commit --version | awk '{print "  Pre-commit: " $0}'

echo ""
echo "=========================================="
echo "Dev Container 已就緒！"
echo "Pre-commit 已安裝並配置完成"
echo "=========================================="
