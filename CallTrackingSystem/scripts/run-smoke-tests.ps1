param(
    [string]$BaseUrl = "http://localhost:5000",
    [string]$AdminUsername = "admin",
    [string]$AdminPassword = "Admin@123",
    [string]$OutputDir = "",
    [switch]$AutoStartServer,
    [int]$StartupTimeoutSeconds = 60,
    [switch]$SkipExcel
)

Set-StrictMode -Version Latest
$ErrorActionPreference = "Stop"

function Write-Step([string]$message)
{
    Write-Host "[STEP] $message" -ForegroundColor Cyan
}

function Write-Ok([string]$message)
{
    Write-Host "[OK]   $message" -ForegroundColor Green
}

function Get-JsonPropertyValue($obj, [string[]]$names)
{
    foreach ($name in $names)
    {
        if ($null -eq $obj) { continue }
        $prop = $obj.PSObject.Properties | Where-Object { $_.Name -eq $name } | Select-Object -First 1
        if ($null -ne $prop) { return $prop.Value }

        $prop2 = $obj.PSObject.Properties | Where-Object { $_.Name -ieq $name } | Select-Object -First 1
        if ($null -ne $prop2) { return $prop2.Value }
    }

    return $null
}

function Invoke-Json(
    [string]$Method,
    [string]$Url,
    $Body,
    [hashtable]$Headers = @{}
)
{
    $jsonBody = $null
    if ($null -ne $Body)
    {
        $jsonBody = $Body | ConvertTo-Json -Depth 10
    }

    return Invoke-RestMethod -Method $Method -Uri $Url -Headers $Headers -ContentType "application/json" -Body $jsonBody
}

function Invoke-ExpectStatus(
    [string]$Method,
    [string]$Url,
    $Body,
    [hashtable]$Headers,
    [int[]]$ExpectedStatusCodes
)
{
    try
    {
        if ($Method -in @("GET", "DELETE"))
        {
            $resp = Invoke-WebRequest -Method $Method -Uri $Url -Headers $Headers -UseBasicParsing
        }
        else
        {
            $jsonBody = $Body | ConvertTo-Json -Depth 10
            $resp = Invoke-WebRequest -Method $Method -Uri $Url -Headers $Headers -ContentType "application/json" -Body $jsonBody -UseBasicParsing
        }

        if ($ExpectedStatusCodes -notcontains $resp.StatusCode)
        {
            throw "HTTP $($resp.StatusCode)（預期：$($ExpectedStatusCodes -join ', ')）"
        }

        if ([string]::IsNullOrWhiteSpace($resp.Content))
        {
            return $null
        }

        try
        {
            return $resp.Content | ConvertFrom-Json
        }
        catch
        {
            return $resp.Content
        }
    }
    catch
    {
        $message = $_.Exception.Message
        $exceptionType = $_.Exception.GetType().FullName
        $statusCodeText = $null
        $responseBody = $null

        # Invoke-WebRequest 在 4xx/5xx 會丟出例外：
        # - Windows PowerShell 5.1: WebException，Response 是 WebResponse (可 GetResponseStream)
        # - PowerShell 7+: HttpResponseException，Response 是 HttpResponseMessage (可 ReadAsStringAsync)
        if ($null -ne $_.Exception.Response)
        {
            $respObj = $_.Exception.Response

            # PowerShell 7: HttpResponseMessage
            if ($respObj -is [System.Net.Http.HttpResponseMessage])
            {
                try { $statusCodeText = ([int]$respObj.StatusCode).ToString() } catch { $statusCodeText = $null }
                try
                {
                    if ($null -ne $respObj.Content)
                    {
                        $responseBody = $respObj.Content.ReadAsStringAsync().GetAwaiter().GetResult()
                    }
                }
                catch
                {
                    $responseBody = $null
                }
            }
            else
            {
                # Windows PowerShell: WebResponse
                try { $statusCodeText = ([int]$respObj.StatusCode).ToString() } catch { $statusCodeText = $null }
                try
                {
                    if ($respObj.PSObject.Methods.Name -contains "GetResponseStream")
                    {
                        $stream = $respObj.GetResponseStream()
                        if ($null -ne $stream)
                        {
                            $reader = New-Object System.IO.StreamReader($stream)
                            $responseBody = $reader.ReadToEnd()
                        }
                    }
                }
                catch
                {
                    $responseBody = $null
                }
            }
        }

        # PowerShell 常把伺服器回應內容放在 ErrorDetails.Message
        if ([string]::IsNullOrWhiteSpace($responseBody) -and $null -ne $_.ErrorDetails -and -not [string]::IsNullOrWhiteSpace($_.ErrorDetails.Message))
        {
            $responseBody = $_.ErrorDetails.Message
        }

        $detail = "呼叫失敗：$Method $Url。原因：$message（ExceptionType=$exceptionType）"
        if (-not [string]::IsNullOrWhiteSpace($statusCodeText))
        {
            $detail += "；HTTP=$statusCodeText"
        }
        if (-not [string]::IsNullOrWhiteSpace($responseBody))
        {
            $detail += "`nResponse Body:`n$responseBody"
        }

        throw $detail
    }
}

if ([string]::IsNullOrWhiteSpace($OutputDir))
{
    $OutputDir = Join-Path $PSScriptRoot "..\artifacts\smoke"
}

New-Item -ItemType Directory -Path $OutputDir -Force | Out-Null

$startedServerProcess = $null

function Wait-UntilHealthy([string]$url, [int]$timeoutSeconds)
{
    $deadline = (Get-Date).AddSeconds($timeoutSeconds)
    while ((Get-Date) -lt $deadline)
    {
        try
        {
            $null = Invoke-WebRequest -Method "GET" -Uri $url -UseBasicParsing -TimeoutSec 5
            return $true
        }
        catch
        {
            Start-Sleep -Seconds 1
        }
    }
    return $false
}

function Start-LocalServer([string]$baseUrl, [string]$outputDir)
{
    $projectPath = Join-Path $PSScriptRoot "..\src\CallTrackingSystem.Web\CallTrackingSystem.Web.csproj"
    if (-not (Test-Path $projectPath))
    {
        throw "找不到 Web 專案檔：$projectPath"
    }

    $logOut = Join-Path $outputDir "server.stdout.log"
    $logErr = Join-Path $outputDir "server.stderr.log"

    $env:ASPNETCORE_ENVIRONMENT = "Development"

    return Start-Process -FilePath "dotnet" -ArgumentList @(
        "run",
        "--project", $projectPath,
        "--urls", $baseUrl
    ) -WorkingDirectory (Split-Path $projectPath -Parent) -PassThru -NoNewWindow -RedirectStandardOutput $logOut -RedirectStandardError $logErr
}

try
{
    Write-Step "1) 健康檢查 /health"
    $healthUrl = "$BaseUrl/health"
    try
    {
        $healthResp = Invoke-ExpectStatus -Method "GET" -Url $healthUrl -Body $null -Headers @{} -ExpectedStatusCodes @(200)
        Write-Ok "服務存活：$healthUrl"
    }
    catch
    {
        if (-not $AutoStartServer)
        {
            throw $_
        }

        Write-Step "偵測到服務未啟動，開始自動啟動本機伺服器"
        $startedServerProcess = Start-LocalServer -baseUrl $BaseUrl -outputDir $OutputDir

        if (-not (Wait-UntilHealthy -url $healthUrl -timeoutSeconds $StartupTimeoutSeconds))
        {
            try { Stop-Process -Id $startedServerProcess.Id -Force -ErrorAction SilentlyContinue } catch { }
            throw "伺服器啟動逾時（$StartupTimeoutSeconds 秒）。請查看 $OutputDir\server.stdout.log 與 $OutputDir\server.stderr.log"
        }

        Write-Ok "伺服器已啟動：$BaseUrl"
    }

    Write-Step "2) 登入取得 JWT（/api/auth/login）"
    $loginUrl = "$BaseUrl/api/auth/login"
    $loginBody = @{ username = $AdminUsername; password = $AdminPassword }
    $loginResp = Invoke-ExpectStatus -Method "POST" -Url $loginUrl -Body $loginBody -Headers @{} -ExpectedStatusCodes @(200)
    $accessToken = Get-JsonPropertyValue -obj $loginResp -names @("accessToken", "AccessToken")
    if ([string]::IsNullOrWhiteSpace($accessToken))
    {
        throw "登入成功但回應缺少 accessToken，請檢查 LoginResponse 序列化或 API 回應。"
    }
    Write-Ok "取得 accessToken"

    $authHeaders = @{ Authorization = "Bearer $accessToken" }

    Write-Step "3) 驗證 Admin API（/api/admin/users）"
    $adminUsersUrl = "$BaseUrl/api/admin/users"
    $adminUsersResp = Invoke-ExpectStatus -Method "GET" -Url $adminUsersUrl -Body $null -Headers $authHeaders -ExpectedStatusCodes @(200)
    Write-Ok "Admin API 可用：/api/admin/users"

    Write-Step "4) 建立一筆來電紀錄（/api/CallRecords）"
    $subject = "SOP Smoke Test $(Get-Date -Format 'yyyyMMdd-HHmmss')"

    Write-Step "4.1) 取得詢問系統清單（/api/InquirySystems）"
    $inquirySystemsUrl = "$BaseUrl/api/InquirySystems"
    $inquirySystems = Invoke-ExpectStatus -Method "GET" -Url $inquirySystemsUrl -Body $null -Headers @{} -ExpectedStatusCodes @(200)
    $firstInquirySystemId = $null
    if ($null -ne $inquirySystems)
    {
        if ($inquirySystems -is [System.Array] -and $inquirySystems.Count -gt 0)
        {
            $firstInquirySystemId = Get-JsonPropertyValue -obj $inquirySystems[0] -names @("id", "Id")
        }
        elseif ($inquirySystems.PSObject.Properties.Name -contains "items")
        {
            if ($inquirySystems.items.Count -gt 0)
            {
                $firstInquirySystemId = Get-JsonPropertyValue -obj $inquirySystems.items[0] -names @("id", "Id")
            }
        }
    }

    if ($null -eq $firstInquirySystemId)
    {
        throw "找不到可用的詢問系統，無法建立來電紀錄。"
    }

    Write-Ok "使用詢問系統 ID=$firstInquirySystemId"

    $createUrl = "$BaseUrl/api/CallRecords"
    $createBody = @{
        inquirySystemId = $firstInquirySystemId
        subject = $subject
        content = "這是一筆自動化 Smoke Test 建立的測試資料"
        urgencyLevel = 1
        contactName = "測試使用者"
        contactPhone = "0912-345678"
        faqReference = "https://example.com"
    }
    $created = Invoke-ExpectStatus -Method "POST" -Url $createUrl -Body $createBody -Headers @{} -ExpectedStatusCodes @(201)
    $callRecordId = Get-JsonPropertyValue -obj $created -names @("id", "Id")
    if ($null -eq $callRecordId)
    {
        throw "建立來電紀錄成功但回應缺少 id。"
    }
    Write-Ok "已建立來電紀錄 ID=$callRecordId"

    Write-Step "5) 查詢詳情（/api/CallRecords/{id}）"
    $getUrl = "$BaseUrl/api/CallRecords/$callRecordId"
    $detail = Invoke-ExpectStatus -Method "GET" -Url $getUrl -Body $null -Headers @{} -ExpectedStatusCodes @(200)
    $detailSubject = Get-JsonPropertyValue -obj $detail -names @("subject", "Subject")
    if ($detailSubject -ne $subject)
    {
        throw "查詢結果不符：subject 預期 '$subject'，實際 '$detailSubject'。"
    }
    Write-Ok "查詢詳情 OK"

    Write-Step "6) 取得鎖定 → 釋放鎖定（/api/CallRecords/{id}/lock）"
    $lockUrl = "$BaseUrl/api/CallRecords/$callRecordId/lock"
    $lockResp = Invoke-ExpectStatus -Method "POST" -Url $lockUrl -Body $null -Headers @{} -ExpectedStatusCodes @(200, 409)
    if ($null -ne $lockResp)
    {
        $acquired = Get-JsonPropertyValue -obj $lockResp -names @("acquired", "Acquired")
        if ($acquired -eq $true)
        {
            Write-Ok "取得鎖定成功"
        }
        else
        {
            Write-Ok "取得鎖定未成功（可能已被鎖定），仍可繼續其他測試"
        }
    }

    $unlockResp = Invoke-ExpectStatus -Method "DELETE" -Url $lockUrl -Body $null -Headers @{} -ExpectedStatusCodes @(200, 404)
    Write-Ok "釋放鎖定呼叫完成"

    Write-Step "7) 更新狀態（/api/CallRecords/{id}/status）"
    $statusUrl = "$BaseUrl/api/CallRecords/$callRecordId/status"
    $statusBody = @{ status = "Completed" }
    $statusResp = Invoke-ExpectStatus -Method "PATCH" -Url $statusUrl -Body $statusBody -Headers @{} -ExpectedStatusCodes @(200)
    Write-Ok "狀態更新 OK"

    Write-Step "8) 查詢變更歷史（/api/CallRecords/{id}/change-history）"
    $historyUrl = "$BaseUrl/api/CallRecords/$callRecordId/change-history"
    $historyResp = Invoke-ExpectStatus -Method "GET" -Url $historyUrl -Body $null -Headers @{} -ExpectedStatusCodes @(200)
    Write-Ok "變更歷史查詢 OK"

    if (-not $SkipExcel)
    {
        Write-Step "9) 匯出 Excel（/api/Reports/excel）"
        $excelUrl = "$BaseUrl/api/Reports/excel"
        $excelBody = @{
            keyword = ""
            startDate = (Get-Date).AddDays(-7)
            endDate = (Get-Date)
        }

        $excelOutFile = Join-Path $OutputDir ("CallRecords_{0}.xlsx" -f (Get-Date -Format "yyyyMMdd-HHmmss"))

        try
        {
            $jsonBody = $excelBody | ConvertTo-Json -Depth 10
            $excelResp = Invoke-WebRequest -Method "POST" -Uri $excelUrl -ContentType "application/json" -Body $jsonBody -OutFile $excelOutFile -UseBasicParsing
            if ($excelResp.StatusCode -ne 200)
            {
                throw "HTTP $($excelResp.StatusCode)"
            }
            Write-Ok "Excel 已下載：$excelOutFile"
        }
        catch
        {
            $message = $_.Exception.Message
            throw "匯出 Excel 失敗：$message"
        }
    }

    Write-Step "10) 刪除來電紀錄（/api/CallRecords/{id}）"
    $deleteUrl = "$BaseUrl/api/CallRecords/$callRecordId"
    $null = Invoke-ExpectStatus -Method "DELETE" -Url $deleteUrl -Body $null -Headers @{} -ExpectedStatusCodes @(204)
    Write-Ok "刪除 OK"

    Write-Host "" 
    Write-Host "全部 Smoke Test 完成" -ForegroundColor Green
    Write-Host "輸出資料夾：$OutputDir" -ForegroundColor DarkGray
}
finally
{
    if ($null -ne $startedServerProcess)
    {
        Write-Step "停止自動啟動的伺服器 (PID=$($startedServerProcess.Id))"
        try
        {
            Stop-Process -Id $startedServerProcess.Id -Force
            Write-Ok "已停止伺服器"
        }
        catch
        {
            Write-Host "[WARN] 停止伺服器失敗：$($_.Exception.Message)" -ForegroundColor Yellow
        }
    }
}
