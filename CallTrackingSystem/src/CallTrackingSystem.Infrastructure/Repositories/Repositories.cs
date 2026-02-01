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

    public async Task<InquirySystem?> GetByNameAsync(string name, CancellationToken cancellationToken = default)
    {
        var normalized = name.Trim();
        return await _context.InquirySystems
            .FirstOrDefaultAsync(x => x.Name == normalized, cancellationToken);
    }

    public async Task<List<InquirySystem>> GetAllAsync(CancellationToken cancellationToken = default)
    {
        return await _context.InquirySystems
            .OrderBy(x => x.Name)
            .ToListAsync(cancellationToken);
    }

    public async Task<List<InquirySystem>> GetActiveSystemsAsync(CancellationToken cancellationToken = default)
    {
        return await _context.InquirySystems
            .Where(x => x.IsActive)
            .OrderBy(x => x.Name)
            .ToListAsync(cancellationToken);
    }

    public async Task<InquirySystem> AddAsync(InquirySystem inquirySystem, CancellationToken cancellationToken = default)
    {
        _context.InquirySystems.Add(inquirySystem);
        await _context.SaveChangesAsync(cancellationToken);
        return inquirySystem;
    }

    public async Task UpdateAsync(InquirySystem inquirySystem, CancellationToken cancellationToken = default)
    {
        _context.InquirySystems.Update(inquirySystem);
        await _context.SaveChangesAsync(cancellationToken);
    }

    public async Task DeleteAsync(InquirySystem inquirySystem, CancellationToken cancellationToken = default)
    {
        _context.InquirySystems.Remove(inquirySystem);
        await _context.SaveChangesAsync(cancellationToken);
    }

    public async Task<bool> ExistsByNameAsync(string name, CancellationToken cancellationToken = default)
    {
        var normalized = name.Trim();
        return await _context.InquirySystems
            .AnyAsync(x => x.Name == normalized, cancellationToken);
    }

    public async Task<bool> HasCallRecordsAsync(int inquirySystemId, CancellationToken cancellationToken = default)
    {
        return await _context.CallRecords
            .AnyAsync(x => x.InquirySystemId == inquirySystemId, cancellationToken);
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

    public async Task<Handler?> GetByIdAsync(int id, CancellationToken cancellationToken = default)
    {
        return await _context.Handlers.FirstOrDefaultAsync(x => x.Id == id, cancellationToken);
    }

    public async Task<List<Handler>> GetAllAsync(CancellationToken cancellationToken = default)
    {
        return await _context.Handlers
            .OrderBy(x => x.Name)
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

    public async Task<Handler?> GetByLineUserIdAsync(string lineUserId, CancellationToken cancellationToken = default)
    {
        var normalized = lineUserId.Trim();
        return await _context.Handlers
            .FirstOrDefaultAsync(x => x.LineUserId == normalized, cancellationToken);
    }

    public async Task<Handler> AddAsync(Handler handler, CancellationToken cancellationToken = default)
    {
        _context.Handlers.Add(handler);
        await _context.SaveChangesAsync(cancellationToken);
        return handler;
    }

    public async Task UpdateAsync(Handler handler, CancellationToken cancellationToken = default)
    {
        _context.Handlers.Update(handler);
        await _context.SaveChangesAsync(cancellationToken);
    }

    public async Task DeleteAsync(Handler handler, CancellationToken cancellationToken = default)
    {
        _context.Handlers.Remove(handler);
        await _context.SaveChangesAsync(cancellationToken);
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

public class HandlerMappingRepository : IHandlerMappingRepository
{
    private readonly ApplicationDbContext _context;

    public HandlerMappingRepository(ApplicationDbContext context)
    {
        _context = context;
    }

    public async Task<List<HandlerMapping>> GetMappingsAsync(
        int? inquirySystemId,
        int? handlerId,
        CancellationToken cancellationToken = default)
    {
        var query = _context.HandlerMappings
            .Include(x => x.Handler)
            .Include(x => x.InquirySystem)
            .AsQueryable();

        if (inquirySystemId.HasValue)
        {
            query = query.Where(x => x.InquirySystemId == inquirySystemId.Value);
        }

        if (handlerId.HasValue)
        {
            query = query.Where(x => x.HandlerId == handlerId.Value);
        }

        return await query
            .OrderByDescending(x => x.CreatedAt)
            .ToListAsync(cancellationToken);
    }

    public async Task<HandlerMapping?> GetByIdAsync(int id, CancellationToken cancellationToken = default)
    {
        return await _context.HandlerMappings
            .Include(x => x.Handler)
            .Include(x => x.InquirySystem)
            .FirstOrDefaultAsync(x => x.Id == id, cancellationToken);
    }

    public async Task<HandlerMapping> AddAsync(HandlerMapping mapping, CancellationToken cancellationToken = default)
    {
        _context.HandlerMappings.Add(mapping);
        await _context.SaveChangesAsync(cancellationToken);
        return mapping;
    }

    public async Task DeleteAsync(HandlerMapping mapping, CancellationToken cancellationToken = default)
    {
        _context.HandlerMappings.Remove(mapping);
        await _context.SaveChangesAsync(cancellationToken);
    }

    public async Task<bool> ExistsAsync(int handlerId, int inquirySystemId, CancellationToken cancellationToken = default)
    {
        return await _context.HandlerMappings
            .AnyAsync(x => x.HandlerId == handlerId && x.InquirySystemId == inquirySystemId, cancellationToken);
    }
}

public class UserRepository : IUserRepository
{
    private readonly ApplicationDbContext _context;

    public UserRepository(ApplicationDbContext context)
    {
        _context = context;
    }

    public async Task<User?> GetByIdAsync(string id, CancellationToken cancellationToken = default)
    {
        return await _context.Users.FirstOrDefaultAsync(x => x.Id == id, cancellationToken);
    }

    public async Task<User?> GetByUsernameAsync(string username, CancellationToken cancellationToken = default)
    {
        return await _context.Users.FirstOrDefaultAsync(
            x => x.Username == username,
            cancellationToken);
    }

    public async Task<User?> GetByLineUserIdAsync(string lineUserId, CancellationToken cancellationToken = default)
    {
        return await _context.Users.FirstOrDefaultAsync(
            x => x.LineUserId == lineUserId,
            cancellationToken);
    }

    public async Task<User> AddAsync(User user, CancellationToken cancellationToken = default)
    {
        _context.Users.Add(user);
        await _context.SaveChangesAsync(cancellationToken);
        return user;
    }

    public async Task UpdateAsync(User user, CancellationToken cancellationToken = default)
    {
        _context.Users.Update(user);
        await _context.SaveChangesAsync(cancellationToken);
    }
}

public class NotificationLogRepository : INotificationLogRepository
{
    private readonly ApplicationDbContext _context;

    public NotificationLogRepository(ApplicationDbContext context)
    {
        _context = context;
    }

    public async Task<List<NotificationLog>> GetByCallRecordIdAsync(int callRecordId, CancellationToken cancellationToken = default)
    {
        return await _context.NotificationLogs
            .Where(x => x.CallRecordId == callRecordId)
            .OrderByDescending(x => x.SentAt)
            .ToListAsync(cancellationToken);
    }

    public async Task<List<NotificationLog>> GetFailedAsync(CancellationToken cancellationToken = default)
    {
        return await _context.NotificationLogs
            .Where(x => !x.Success)
            .OrderByDescending(x => x.SentAt)
            .ToListAsync(cancellationToken);
    }

    public async Task AddAsync(NotificationLog log, CancellationToken cancellationToken = default)
    {
        _context.NotificationLogs.Add(log);
        await _context.SaveChangesAsync(cancellationToken);
    }
}
