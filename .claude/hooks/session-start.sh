#!/bin/bash
set -euo pipefail

# 僅在 Claude Code 遠端環境（web）中執行
if [ "${CLAUDE_CODE_REMOTE:-}" != "true" ]; then
  exit 0
fi

# 若 dotnet 已在 PATH 可使用，直接結束
if command -v dotnet &>/dev/null; then
  echo "[session-start] dotnet $(dotnet --version) 已安裝，略過安裝步驟。"
  exit 0
fi

# 若先前透過 apt 安裝過，補設後離開
if [ -x "/usr/bin/dotnet" ]; then
  echo "[session-start] 偵測到 /usr/bin/dotnet，已就緒：$(/usr/bin/dotnet --version)"
  exit 0
fi

# 使用 apt 安裝 .NET 8 SDK
echo "[session-start] 未偵測到 dotnet，使用 apt 安裝 .NET 8 SDK..."
apt-get update -qq
apt-get install -y --no-install-recommends dotnet-sdk-8.0

echo "[session-start] dotnet $(dotnet --version) 安裝完成。"
