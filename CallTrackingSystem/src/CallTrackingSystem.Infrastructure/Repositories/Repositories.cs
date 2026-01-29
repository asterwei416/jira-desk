using CallTrackingSystem.Core.Entities;
using CallTrackingSystem.Core.Interfaces;
using CallTrackingSystem.Infrastructure.Data;
using Microsoft.EntityFrameworkCore;

namespace CallTrackingSystem.Infrastructure.Repositories;

public class CallRecordRepository : ICallRecordRepository
{
    private readonly ApplicationDbContext _context;

    public CallRecordRepository(ApplicationDbContext context)
    {
        _context = context;
    }

    public async Task<CallRecord?> GetByIdAsync(int id, CancellationToken cancellationToken = default)
    {
        return await _context.CallRecords
            .Include(x => x.InquirySystem)
            .Include(x => x.Handlers)
            .Include(x => x.ChangeHistories)
            .FirstOrDefaultAsync(x => x.Id == id, cancellationToken);
    }

    public async Task<(List<CallRecord> Items, int TotalCount)> GetPagedAsync(
        CallTrackingSystem.Core.DTOs.CallRecordSearchCriteria criteria,
        int pageNumber,
        int pageSize,
        string sortBy,
        string sortOrder,
        CancellationToken cancellationToken = default)
    {
        var query = _context.CallRecords
            .Include(x => x.InquirySystem)
            .Include(x => x.Handlers)
            .AsQueryable();

        // 搜尋條件
        if (!string.IsNullOrWhiteSpace(criteria.Keyword))
        {
            var keyword = criteria.Keyword.Trim();
            query = query.Where(x => 
                x.Subject.Contains(keyword) || 
                x.Content.Contains(keyword) ||
                x.ContactName.Contains(keyword));
        }

        if (criteria.InquirySystemId.HasValue)
        {
            query = query.Where(x => x.InquirySystemId == criteria.InquirySystemId.Value);
        }

        if (criteria.Status.HasValue)
        {
            query = query.Where(x => x.Status == criteria.Status.Value);
        }

        if (criteria.UrgencyLevel.HasValue)
        {
            query = query.Where(x => x.UrgencyLevel == criteria.UrgencyLevel.Value);
        }

        if (criteria.StartDateUtc.HasValue)
        {
            query = query.Where(x => x.CreatedAt >= criteria.StartDateUtc.Value);
        }

        if (criteria.EndDateUtc.HasValue)
        {
            query = query.Where(x => x.CreatedAt <= criteria.EndDateUtc.Value);
        }

        var totalCount = await query.CountAsync(cancellationToken);

        var isDesc = sortOrder.Equals("desc", StringComparison.OrdinalIgnoreCase);
        query = sortBy.ToLowerInvariant() switch
        {
            "updatedat" => isDesc ? query.OrderByDescending(x => x.UpdatedAt) : query.OrderBy(x => x.UpdatedAt),
            "urgencylevel" => isDesc ? query.OrderByDescending(x => x.UrgencyLevel) : query.OrderBy(x => x.UrgencyLevel),
            _ => isDesc ? query.OrderByDescending(x => x.CreatedAt) : query.OrderBy(x => x.CreatedAt)
        };

        var items = await query
            .Skip((pageNumber - 1) * pageSize)
            .Take(pageSize)
            .ToListAsync(cancellationToken);

        return (items, totalCount);
    }

    public async Task<List<CallRecord>> SearchAsync(
        CallTrackingSystem.Core.DTOs.CallRecordSearchCriteria criteria,
        CancellationToken cancellationToken = default)
    {
        var query = _context.CallRecords
            .Include(x => x.InquirySystem)
            .Include(x => x.Handlers)
            .AsQueryable();

        if (!string.IsNullOrWhiteSpace(criteria.Keyword))
        {
            var keyword = criteria.Keyword.Trim();
            query = query.Where(x => x.Subject.Contains(keyword) ||
                                     x.Content.Contains(keyword) ||
                                     x.ContactName.Contains(keyword));
        }

        if (criteria.InquirySystemId.HasValue)
        {
            query = query.Where(x => x.InquirySystemId == criteria.InquirySystemId.Value);
        }

        if (criteria.Status.HasValue)
        {
            query = query.Where(x => x.Status == criteria.Status.Value);
        }

        if (criteria.UrgencyLevel.HasValue)
        {
            query = query.Where(x => x.UrgencyLevel == criteria.UrgencyLevel.Value);
        }

        if (criteria.StartDateUtc.HasValue)
        {
            query = query.Where(x => x.CreatedAt >= criteria.StartDateUtc.Value);
        }

        if (criteria.EndDateUtc.HasValue)
        {
            query = query.Where(x => x.CreatedAt <= criteria.EndDateUtc.Value);
        }

        return await query
            .OrderByDescending(x => x.CreatedAt)
            .ToListAsync(cancellationToken);
    }

    public async Task<CallRecord> AddAsync(CallRecord callRecord, CancellationToken cancellationToken = default)
    {
        _context.CallRecords.Add(callRecord);
        await _context.SaveChangesAsync(cancellationToken);
        return callRecord;
    }

    public async Task UpdateAsync(CallRecord callRecord, CancellationToken cancellationToken = default)
    {
        _context.CallRecords.Update(callRecord);
        await _context.SaveChangesAsync(cancellationToken);
    }

    public async Task DeleteAsync(CallRecord callRecord, CancellationToken cancellationToken = default)
    {
        _context.CallRecords.Remove(callRecord);
        await _context.SaveChangesAsync(cancellationToken);
    }

    public async Task<bool> ExistsAsync(int id, CancellationToken cancellationToken = default)
    {
        return await _context.CallRecords.AnyAsync(x => x.Id == id, cancellationToken);
    }
}

public class InquirySystemRepository : IInquirySystemRepository
{
    private readonly ApplicationDbContext _context;

    public InquirySystemRepository(ApplicationDbContext context)
    {
        _context = context;
    }

    public async Task<InquirySystem?> GetByIdAsync(int id, CancellationToken cancellationToken = default)
    {
        return await _context.InquirySystems.FindAsync(new object[] { id }, cancellationToken);
    }

    public async Task<List<InquirySystem>> GetActiveSystemsAsync(CancellationToken cancellationToken = default)
    {
        return await _context.InquirySystems
            .Where(x => x.IsActive)
            .OrderBy(x => x.Name)
            .ToListAsync(cancellationToken);
    }
}

public class HandlerRepository : IHandlerRepository
{
    private readonly ApplicationDbContext _context;

    public HandlerRepository(ApplicationDbContext context)
    {
        _context = context;
    }

    public async Task<List<Handler>> GetHandlersByInquirySystemIdAsync(
        int inquirySystemId, 
        CancellationToken cancellationToken = default)
    {
        return await _context.HandlerMappings
            .Where(x => x.InquirySystemId == inquirySystemId)
            .Include(x => x.Handler)
            .Where(x => x.Handler.IsActive)
            .Select(x => x.Handler)
            .ToListAsync(cancellationToken);
    }

    public async Task<List<Handler>> GetByIdsAsync(
        IReadOnlyCollection<int> handlerIds,
        CancellationToken cancellationToken = default)
    {
        if (handlerIds.Count == 0)
        {
            return new List<Handler>();
        }

        return await _context.Handlers
            .Where(x => handlerIds.Contains(x.Id))
            .ToListAsync(cancellationToken);
    }
}

public class ChangeHistoryRepository : IChangeHistoryRepository
{
    private readonly ApplicationDbContext _context;

    public ChangeHistoryRepository(ApplicationDbContext context)
    {
        _context = context;
    }

    public async Task<List<ChangeHistory>> GetByCallRecordIdAsync(int callRecordId, CancellationToken cancellationToken = default)
    {
        return await _context.ChangeHistories
            .Where(x => x.CallRecordId == callRecordId)
            .OrderByDescending(x => x.ChangedAt)
            .ToListAsync(cancellationToken);
    }

    public async Task AddRangeAsync(IEnumerable<ChangeHistory> histories, CancellationToken cancellationToken = default)
    {
        var list = histories.ToList();
        if (list.Count == 0)
        {
            return;
        }

        await _context.ChangeHistories.AddRangeAsync(list, cancellationToken);
        await _context.SaveChangesAsync(cancellationToken);
    }
}
