#!/usr/bin/env pwsh
# 快速啟動腳本

$ErrorActionPreference = "Stop"

Write-Host "🚀 正在啟動客服來電問題紀錄與分析系統..." -ForegroundColor Cyan

# 設定環境變數
$env:ASPNETCORE_ENVIRONMENT = "Development"

# 切換到專案目錄
$scriptPath = Split-Path -Parent $MyInvocation.MyCommand.Path
$projectPath = Join-Path $scriptPath "CallTrackingSystem\src\CallTrackingSystem.Web"

if (!(Test-Path $projectPath)) {
    Write-Host "❌ 找不到專案目錄: $projectPath" -ForegroundColor Red
    exit 1
}

Write-Host "📁 專案路徑: $projectPath" -ForegroundColor Yellow
Set-Location $projectPath

# 啟動應用程式
Write-Host "▶️  正在執行 dotnet run..." -ForegroundColor Green
Write-Host ""
Write-Host "📝 Swagger UI: https://localhost:5001/swagger" -ForegroundColor Cyan
Write-Host "🏥 健康檢查: https://localhost:5001/health" -ForegroundColor Cyan
Write-Host ""

dotnet run --launch-profile https
