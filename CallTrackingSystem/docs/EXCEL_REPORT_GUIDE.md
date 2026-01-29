# T073 + T074：Excel 報表功能實作指南

**狀態**: 🔄 進行中  
**完成度**: 測試框架 ✅ 100% | Service 實作 ⏳ 需完成  
**預估時間**: 2-3 小時  

---

## 📋 任務概覽

### T073 - ReportService 單元測試 ✅ 已建立
- 檔案: `tests/CallTrackingSystem.UnitTests/Services/ReportServiceTests.cs`
- 測試案例: 11 個
- 涵蓋範圍:
  - ✅ Excel 三工作表結構驗證
  - ✅ 篩選摘要工作表內容
  - ✅ 明細資料工作表（欄位、日期格式）
  - ✅ 統計彙總工作表（月份分組、詢問系統分組）
  - ✅ 邊界情況（空資料、超限、特殊字符）

### T074 - 報表匯出整合測試 ✅ 已建立
- 檔案: `tests/CallTrackingSystem.IntegrationTests/Services/ExcelReportExportTests.cs`
- 測試案例: 12 個
- 涵蓋範圍:
  - ✅ 完整流程（DB → Excel）
  - ✅ 篩選條件驗證
  - ✅ 日期範圍驗證
  - ✅ 資料完整性檢查
  - ✅ 統計計算驗證
  - ✅ 邊界測試（超限、空結果）
  - ✅ 檔案性質驗證

---

## 🛠️ 下一步：實作 ReportService

根據測試要求，你需要實作以下檔案：

### 1️⃣ 核心 Service（Core 層）

**檔案**: `src/CallTrackingSystem.Core/Services/ReportService.cs`

```csharp
using CallTrackingSystem.Core.DTOs;
using CallTrackingSystem.Core.Interfaces;
using OfficeOpenXml;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace CallTrackingSystem.Core.Services;

/// <summary>
/// 報表生成服務
/// 負責 Excel 報表的生成邏輯
/// </summary>
public class ReportService : IReportService
{
    private const int MAX_EXPORT_ROWS = 5000;
    private const int MAX_COLUMNS = 9;

    private readonly ICallRecordRepository _callRecordRepository;
    private readonly IInquirySystemRepository _inquirySystemRepository;

    public ReportService(
        ICallRecordRepository callRecordRepository,
        IInquirySystemRepository inquirySystemRepository)
    {
        _callRecordRepository = callRecordRepository;
        _inquirySystemRepository = inquirySystemRepository;
    }

    /// <summary>
    /// 生成 Excel 報表（包含篩選摘要、明細、統計）
    /// </summary>
    public async Task<byte[]> GenerateExcelReportAsync(SearchFilterModel filter)
    {
        // 1. 驗證限制
        // 2. 查詢資料
        // 3. 建立工作簿
        // 4. 建立三個工作表
        // 5. 寫入資料
        // 6. 回傳 Excel 位元組

        // TODO: 實作詳細邏輯
        throw new NotImplementedException();
    }

    private ExcelWorksheet CreateSummarySheet(ExcelWorkbook workbook, SearchFilterModel filter)
    {
        // 建立「篩選摘要」工作表
        // 1. 新增工作表
        // 2. 寫入篩選條件（關鍵字、日期、詢問系統等）
        // 3. 格式化
        // TODO: 實作
        throw new NotImplementedException();
    }

    private ExcelWorksheet CreateDetailSheet(ExcelWorkbook workbook, List<CallRecordDto> records)
    {
        // 建立「明細資料」工作表
        // 1. 寫入標題列
        // 2. 寫入每筆紀錄
        // 3. 格式化日期為 yyyy/MM/dd HH:mm
        // 4. 調整欄寬
        // TODO: 實作
        throw new NotImplementedException();
    }

    private ExcelWorksheet CreateStatisticsSheet(
        ExcelWorkbook workbook,
        List<CallRecordDto> records)
    {
        // 建立「統計彙總」工作表
        // 1. 月份統計（yyyy/MM 分組，計算筆數）
        // 2. 詢問系統統計
        // 3. 合計行
        // TODO: 實作
        throw new NotImplementedException();
    }
}
```

### 2️⃣ 服務介面（Core 層）

**檔案**: `src/CallTrackingSystem.Core/Interfaces/IReportService.cs`

```csharp
using CallTrackingSystem.Core.DTOs;

namespace CallTrackingSystem.Core.Interfaces;

public interface IReportService
{
    /// <summary>
    /// 生成 Excel 報表
    /// </summary>
    /// <param name="filter">篩選條件</param>
    /// <returns>Excel 檔案位元組</returns>
    /// <exception cref="InvalidOperationException">資料超過 5000 筆限制時拋出</exception>
    Task<byte[]> GenerateExcelReportAsync(SearchFilterModel filter);
}
```

### 3️⃣ 在 DI 容器中註冊

**檔案**: `src/CallTrackingSystem.Web/Program.cs`

```csharp
// 在 DI 註冊區段中加入
builder.Services.AddScoped<IReportService, ReportService>();
```

---

## 🚀 實作建議

### 關鍵步驟

1. **建立 ReportService 骨架**
   - 複製上方提供的類別結構
   - 實作 `GenerateExcelReportAsync()` 主方法

2. **實作 CreateDetailSheet()**
   - 最直接，先從這個開始
   - 寫入標題列（9 欄）
   - 迴圈寫入每筆紀錄
   - 格式化日期

3. **實作 CreateSummarySheet()**
   - 顯示篩選條件
   - 使用簡單的鍵值對

4. **實作 CreateStatisticsSheet()**
   - 使用 LINQ GroupBy 分組
   - 月份: `c.CreatedAt.Year`, `c.CreatedAt.Month`
   - 詢問系統: `c.InquirySystemId`

5. **加入驗證邏輯**
   - 檢查資料筆數是否超過 5000
   - 拋出 InvalidOperationException

### 測試驅動開發流程

```powershell
# 步驟 1: 執行測試，看著它們失敗
cd CallTrackingSystem
dotnet test tests/CallTrackingSystem.UnitTests/Services/ReportServiceTests.cs -v

# 步驟 2: 實作 Service（逐個方法）
# 步驟 3: 測試應該陸續通過

# 步驟 4: 執行整合測試
dotnet test tests/CallTrackingSystem.IntegrationTests/Services/ExcelReportExportTests.cs -v

# 步驟 5: 檢查覆蓋率
dotnet test --collect:"XPlat Code Coverage" \
  tests/CallTrackingSystem.UnitTests/Services/ReportServiceTests.cs
```

---

## 📚 重要 API 參考

### EPPlus 常用操作

```csharp
// 建立工作簿
using (var package = new ExcelPackage())
{
    var worksheet = package.Workbook.Worksheets.Add("工作表名稱");
    
    // 寫入單元格
    worksheet.Cells[row, col].Value = "值";
    
    // 合併儲存格
    worksheet.Cells[1, 1, 1, 3].Merge = true;
    
    // 設定格式
    worksheet.Cells[row, col].NumberFormat = "yyyy/mm/dd hh:mm";
    
    // 調整欄寬
    worksheet.Column(1).Width = 15;
    
    // 設定字型
    worksheet.Cells[1, 1].Style.Font.Bold = true;
    
    // 轉換為位元組
    byte[] fileBytes = package.GetAsByteArray();
}
```

### LINQ 分組範例

```csharp
// 按月份分組
var monthlyGroups = records
    .GroupBy(r => new { r.CreatedAt.Year, r.CreatedAt.Month })
    .Select(g => new 
    { 
        Month = $"{g.Key.Year:D4}/{g.Key.Month:D2}",
        Count = g.Count()
    })
    .OrderBy(x => x.Month);

// 按詢問系統分組
var systemGroups = records
    .GroupBy(r => r.InquirySystem.Name)
    .Select(g => new 
    { 
        SystemName = g.Key,
        Count = g.Count()
    });
```

---

## ✅ 檢查清單

實作完成後，確認以下項目：

- [ ] Service 類別已建立在正確位置
- [ ] 介面已定義並在 Service 中實作
- [ ] 在 Program.cs 中註冊依賴
- [ ] 所有單元測試通過（ReportServiceTests）
- [ ] 所有整合測試通過（ExcelReportExportTests）
- [ ] 測試覆蓋率達到 80%+
- [ ] 沒有 TODO 和拋出 NotImplementedException

---

## 🐛 常見問題

### Q: 測試失敗 - "Excel 檔案無法開啟"
**A**: 確保呼叫 `package.GetAsByteArray()` 並回傳有效的位元組陣列

### Q: 統計數字不正確
**A**: 驗證 GroupBy 邏輯，確保分組鍵正確

### Q: 日期格式錯誤
**A**: 使用 `format.ToString("yyyy/MM/dd HH:mm")` 或 EPPlus 格式字串 `"yyyy/mm/dd hh:mm"`

### Q: 測試執行緩慢
**A**: InMemoryDatabase 可能需要時間。檢查是否有重複查詢

---

## 📞 需要幫助？

完成實作後，可以：
1. 提交代碼進行審查
2. 我可以協助除錯
3. 添加更多測試案例

---

**預計時間**:
- ⏱️ ReportService 實作: 60-90 分鐘
- ⏱️ 單元測試調試: 30-45 分鐘
- ⏱️ 整合測試調試: 30-45 分鐘
- ⏱️ 總計: **2-3 小時**

現在開始實作吧！🚀
