using System.ComponentModel.DataAnnotations;

namespace CallTrackingSystem.Web.Models;

/// <summary>
/// 來電紀錄搜尋條件
/// </summary>
public class SearchFilterModel : IValidatableObject
{
    public string? Keyword { get; init; }
    public int? InquirySystemId { get; init; }
    public string? Status { get; init; }
    public string? UrgencyLevel { get; init; }
    public DateTime? StartDate { get; init; }
    public DateTime? EndDate { get; init; }
    public int PageNumber { get; init; } = 1;
    public int PageSize { get; init; } = 20;
    public string SortBy { get; init; } = "createdAt";
    public string SortOrder { get; init; } = "desc";

    public IEnumerable<ValidationResult> Validate(ValidationContext validationContext)
    {
        if (PageNumber < 1)
        {
            yield return new ValidationResult("頁碼必須大於等於 1", new[] { nameof(PageNumber) });
        }

        if (PageSize < 1 || PageSize > 100)
        {
            yield return new ValidationResult("每頁筆數必須介於 1 到 100 之間", new[] { nameof(PageSize) });
        }

        if (StartDate.HasValue && EndDate.HasValue && StartDate > EndDate)
        {
            yield return new ValidationResult("開始日期不可晚於結束日期", new[] { nameof(StartDate), nameof(EndDate) });
        }

        var allowedSortBy = new HashSet<string>(StringComparer.OrdinalIgnoreCase)
        {
            "createdAt",
            "updatedAt",
            "urgencyLevel"
        };

        if (!allowedSortBy.Contains(SortBy))
        {
            yield return new ValidationResult("排序欄位不正確", new[] { nameof(SortBy) });
        }

        var allowedSortOrder = new HashSet<string>(StringComparer.OrdinalIgnoreCase)
        {
            "asc",
            "desc"
        };

        if (!allowedSortOrder.Contains(SortOrder))
        {
            yield return new ValidationResult("排序方向不正確", new[] { nameof(SortOrder) });
        }
    }
}
