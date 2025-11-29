#!/usr/bin/env bash
set -euo pipefail

SCRIPT_DIR="$(cd "$(dirname "${BASH_SOURCE[0]}")" && pwd)"
REPO_ROOT="$(cd "${SCRIPT_DIR}/.." && pwd)"
PROJECT_DIR="${SCRIPT_DIR}"
PROJECT_FILE="${PROJECT_DIR}/EasyPanel.csproj"
BUILD_CONFIG="Release"
TARGET_FRAMEWORK="netstandard2.1"
OUTPUT_DLL="${PROJECT_DIR}/bin/${BUILD_CONFIG}/${TARGET_FRAMEWORK}/EasyPanel.dll"
RELEASE_DIR="${PROJECT_DIR}/ReleaseExample/EasyPanel"
RELEASE_DLL="${RELEASE_DIR}/EasyPanel.dll"

echo "==============================================="
echo "[EasyPanel] Build & Release Script"
echo "Project : ${PROJECT_FILE}"
echo "Release : ${RELEASE_DIR}"
echo "==============================================="

if [ ! -f "${PROJECT_FILE}" ]; then
    echo "❌ 找不到專案文件: ${PROJECT_FILE}"
    exit 1
fi

echo "🔨 正在編譯 (dotnet build -c ${BUILD_CONFIG})..."
dotnet build "${PROJECT_FILE}" -c "${BUILD_CONFIG}" >/tmp/easypanel_build.log 2>&1
echo "✅ 編譯完成，日誌位於 /tmp/easypanel_build.log"

if [ ! -f "${OUTPUT_DLL}" ]; then
    echo "❌ 無法在預期位置找到輸出 DLL: ${OUTPUT_DLL}"
    echo "查看編譯日誌："
    cat /tmp/easypanel_build.log
    exit 1
fi

mkdir -p "${RELEASE_DIR}"
cp "${OUTPUT_DLL}" "${RELEASE_DLL}"
echo "📦 已複製 EasyPanel.dll → ${RELEASE_DLL}"

echo "🎉 完成。請將 ${RELEASE_DIR} 內容部署到遊戲 Mods 目錄。"
