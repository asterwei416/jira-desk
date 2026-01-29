# LINE 通知測試腳本
# 用途: 快速建立來電紀錄並觸發 LINE 通知

param(
    [string]$ApiUrl = "http://localhost:5099/api/CallRecords",
    [string]$CustomerName = "測試客戶",
    [string]$PhoneNumber = "0912345678",
    [int]$InquirySystemId = 12,
    [string]$ProblemDescription = "測試 LINE 通知功能 - $(Get-Date -Format 'HH:mm:ss')",
    [string]$ProblemKeywords = "測試,LINE通知"
)

Write-Host "========================================" -ForegroundColor Cyan
Write-Host "  LINE 通知整合測試" -ForegroundColor Cyan
Write-Host "========================================" -ForegroundColor Cyan

Write-Host "`n📋 測試參數:" -ForegroundColor Yellow
Write-Host "  API URL: $ApiUrl"
Write-Host "  客戶姓名: $CustomerName"
Write-Host "  電話: $PhoneNumber"
Write-Host "  詢問系統 ID: $InquirySystemId"
Write-Host "  問題描述: $ProblemDescription"

Write-Host "`n🚀 正在建立來電紀錄..." -ForegroundColor Yellow

$body = @{
    subject = $CustomerName + " - " + $ProblemDescription.Substring(0, [Math]::Min(30, $ProblemDescription.Length))
    content = $ProblemDescription
    inquirySystemId = $InquirySystemId
    urgencyLevel = "Medium"
    contactName = $CustomerName
    contactPhone = $PhoneNumber
    faqReference = ""
} | ConvertTo-Json

try {
    $response = Invoke-RestMethod `
        -Uri $ApiUrl `
        -Method POST `
        -ContentType "application/json; charset=utf-8" `
        -Body $body `
        -ErrorAction Stop
    
    Write-Host "`n✅ 成功建立來電紀錄！" -ForegroundColor Green
    Write-Host "  紀錄 ID: $($response.id)" -ForegroundColor Green
    Write-Host "  狀態: $($response.status)" -ForegroundColor Green
    Write-Host "  建立時間: $($response.createdAt)" -ForegroundColor Green
    
    Write-Host "`n📱 請檢查您的 LINE 手機 App" -ForegroundColor Cyan
    Write-Host "  應該會收到一則 Flex Message 通知" -ForegroundColor Cyan
    
    # 查詢通知記錄
    Write-Host "`n🔍 查詢通知記錄..." -ForegroundColor Yellow
    Start-Sleep -Seconds 2
    
    $notificationUrl = "$ApiUrl/$($response.id)/notifications"
    $notifications = Invoke-RestMethod -Uri $notificationUrl -Method GET -ErrorAction Stop
    
    if ($notifications.Count -gt 0) {
        Write-Host "`n📊 通知記錄 ($($notifications.Count) 筆):" -ForegroundColor Yellow
        foreach ($notif in $notifications) {
            $status = if ($notif.success) { "✅ 成功" } else { "❌ 失敗" }
            Write-Host "  $status - LINE User: $($notif.lineUserId) - 時間: $($notif.sentAt)"
            if (-not $notif.success -and $notif.errorMessage) {
                Write-Host "    錯誤: $($notif.errorMessage)" -ForegroundColor Red
            }
        }
    } else {
        Write-Host "  ⚠️  尚未找到通知記錄（可能仍在處理中）" -ForegroundColor Yellow
    }
    
    Write-Host "`n========================================" -ForegroundColor Cyan
    Write-Host "  測試完成！" -ForegroundColor Green
    Write-Host "========================================" -ForegroundColor Cyan
    
} catch {
    Write-Host "`n❌ 測試失敗！" -ForegroundColor Red
    Write-Host "錯誤訊息: $($_.Exception.Message)" -ForegroundColor Red
    
    if ($_.ErrorDetails.Message) {
        Write-Host "`n詳細錯誤:" -ForegroundColor Red
        $errorDetails = $_.ErrorDetails.Message | ConvertFrom-Json
        $errorDetails | Format-List
    }
    
    Write-Host "`n💡 檢查事項:" -ForegroundColor Yellow
    Write-Host "  1. 應用程式是否正在執行？ (http://localhost:5099)"
    Write-Host "  2. InquirySystemId 是否正確？ (應為 9)"
    Write-Host "  3. 資料庫中是否有處理人員資料？"
    Write-Host "  4. appsettings.Development.json 是否已配置 LINE 設定？"
    
    exit 1
}
