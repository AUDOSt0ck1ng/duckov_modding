#!/usr/bin/env bash
set -euo pipefail

SCRIPT_DIR="$(cd "$(dirname "${BASH_SOURCE[0]}")" && pwd)"
REPO_ROOT="$(cd "${SCRIPT_DIR}/.." && pwd)"
PROJECT_DIR="${REPO_ROOT}/EquipmentSkinSystem"
PROJECT_FILE="${PROJECT_DIR}/EquipmentSkinSystem.csproj"
BUILD_CONFIG="Release"
TARGET_FRAMEWORK="netstandard2.1"
OUTPUT_DLL="${PROJECT_DIR}/bin/${BUILD_CONFIG}/${TARGET_FRAMEWORK}/EquipmentSkinSystem.dll"
RELEASE_DIR="${PROJECT_DIR}/ReleaseExample/EquipmentSkinSystem"
RELEASE_DLL="${RELEASE_DIR}/EquipmentSkinSystem.dll"

echo "==============================================="
echo "[EquipmentSkinSystem] Build & Release Script"
echo "Project : ${PROJECT_FILE}"
echo "Release : ${RELEASE_DIR}"
echo "==============================================="

if [ ! -f "${PROJECT_FILE}" ]; then
    echo "❌ 找不到專案文件: ${PROJECT_FILE}"
    exit 1
fi

echo "🔨 正在編譯 (dotnet build -c ${BUILD_CONFIG})..."
dotnet build "${PROJECT_FILE}" -c "${BUILD_CONFIG}" >/tmp/equipment_skin_build.log
echo "✅ 編譯完成，日誌位於 /tmp/equipment_skin_build.log"

if [ ! -f "${OUTPUT_DLL}" ]; then
    echo "❌ 無法在預期位置找到輸出 DLL: ${OUTPUT_DLL}"
    exit 1
fi

mkdir -p "${RELEASE_DIR}"
cp "${OUTPUT_DLL}" "${RELEASE_DLL}"
echo "📦 已複製 EquipmentSkinSystem.dll → ${RELEASE_DLL}"

HARMONY_DLL_SOURCE="${PROJECT_DIR}/bin/${BUILD_CONFIG}/${TARGET_FRAMEWORK}/0Harmony.dll"
if [ -f "${HARMONY_DLL_SOURCE}" ]; then
    cp "${HARMONY_DLL_SOURCE}" "${RELEASE_DIR}/0Harmony.dll"
    echo "📦 已同步 0Harmony.dll → ${RELEASE_DIR}/0Harmony.dll"
else
    echo "⚠️ 未在輸出目錄找到 0Harmony.dll，跳過同步（視需求手動更新）"
fi

echo "🎉 完成。請將 ${RELEASE_DIR} 內容部署到遊戲 Mods 目錄。"
