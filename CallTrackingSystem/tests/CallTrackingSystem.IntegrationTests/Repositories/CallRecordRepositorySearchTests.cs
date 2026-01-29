using System.Reflection;
using CallTrackingSystem.Core.DTOs;
using CallTrackingSystem.Core.Entities;
using CallTrackingSystem.Core.Enums;
using CallTrackingSystem.Infrastructure.Data;
using CallTrackingSystem.Infrastructure.Repositories;
using Microsoft.EntityFrameworkCore;

namespace CallTrackingSystem.IntegrationTests.Repositories;

public class CallRecordRepositorySearchTests
{
    [Fact]
    public async Task GetPagedAsync_filters_by_keyword()
    {
        var context = CreateContext();
        var repository = new CallRecordRepository(context);

        var system = InquirySystem.Create("帳務系統");
        context.InquirySystems.Add(system);
        await context.SaveChangesAsync();

        var record1 = CreateRecord(system, "帳務異常", "帳務內容", UrgencyLevel.High, "張三");
        var record2 = CreateRecord(system, "會員問題", "會員內容", UrgencyLevel.Low, "李四");
        context.CallRecords.AddRange(record1, record2);
        await context.SaveChangesAsync();

        var criteria = new CallRecordSearchCriteria
        {
            Keyword = "帳務"
        };

        var (items, total) = await repository.GetPagedAsync(criteria, 1, 20, "createdAt", "desc");

        Assert.Single(items);
        Assert.Equal(1, total);
        Assert.Contains(items, x => x.Subject.Contains("帳務"));
    }

    [Fact]
    public async Task GetPagedAsync_filters_by_multiple_conditions()
    {
        var context = CreateContext();
        var repository = new CallRecordRepository(context);

        var systemA = InquirySystem.Create("帳務系統");
        var systemB = InquirySystem.Create("會員系統");
        context.InquirySystems.AddRange(systemA, systemB);
        await context.SaveChangesAsync();

        var record1 = CreateRecord(systemA, "帳務異常", "內容 A", UrgencyLevel.High, "張三");
        record1.UpdateStatus(ProcessStatus.InProgress, "user-1");
        var record2 = CreateRecord(systemA, "帳務問題", "內容 B", UrgencyLevel.Low, "李四");
        var record3 = CreateRecord(systemB, "會員問題", "內容 C", UrgencyLevel.High, "王五");
        context.CallRecords.AddRange(record1, record2, record3);
        await context.SaveChangesAsync();

        var criteria = new CallRecordSearchCriteria
        {
            InquirySystemId = systemA.Id,
            Status = ProcessStatus.InProgress,
            UrgencyLevel = UrgencyLevel.High
        };

        var (items, total) = await repository.GetPagedAsync(criteria, 1, 20, "createdAt", "desc");

        Assert.Single(items);
        Assert.Equal(1, total);
        Assert.Equal(systemA.Id, items[0].InquirySystemId);
        Assert.Equal(ProcessStatus.InProgress, items[0].Status);
        Assert.Equal(UrgencyLevel.High, items[0].UrgencyLevel);
    }

    [Fact]
    public async Task GetPagedAsync_paginates_results()
    {
        var context = CreateContext();
        var repository = new CallRecordRepository(context);

        var system = InquirySystem.Create("帳務系統");
        context.InquirySystems.Add(system);
        await context.SaveChangesAsync();

        var record1 = CreateRecord(system, "問題 A", "內容 A", UrgencyLevel.Low, "張三");
        var record2 = CreateRecord(system, "問題 B", "內容 B", UrgencyLevel.Low, "李四");
        var record3 = CreateRecord(system, "問題 C", "內容 C", UrgencyLevel.Low, "王五");

        SetPrivateProperty(record1, "CreatedAt", new DateTime(2026, 1, 1, 0, 0, 0, DateTimeKind.Utc));
        SetPrivateProperty(record2, "CreatedAt", new DateTime(2026, 1, 2, 0, 0, 0, DateTimeKind.Utc));
        SetPrivateProperty(record3, "CreatedAt", new DateTime(2026, 1, 3, 0, 0, 0, DateTimeKind.Utc));

        context.CallRecords.AddRange(record1, record2, record3);
        await context.SaveChangesAsync();

        var criteria = new CallRecordSearchCriteria();

        var (items, total) = await repository.GetPagedAsync(criteria, 2, 1, "createdAt", "asc");

        Assert.Single(items);
        Assert.Equal(3, total);
        Assert.Equal("問題 B", items[0].Subject);
    }

    [Fact]
    public async Task GetPagedAsync_sorts_by_updated_at()
    {
        var context = CreateContext();
        var repository = new CallRecordRepository(context);

        var system = InquirySystem.Create("帳務系統");
        context.InquirySystems.Add(system);
        await context.SaveChangesAsync();

        var record1 = CreateRecord(system, "問題 A", "內容 A", UrgencyLevel.Low, "張三");
        var record2 = CreateRecord(system, "問題 B", "內容 B", UrgencyLevel.Low, "李四");

        SetPrivateProperty(record1, "UpdatedAt", new DateTime(2026, 1, 1, 8, 0, 0, DateTimeKind.Utc));
        SetPrivateProperty(record2, "UpdatedAt", new DateTime(2026, 1, 2, 8, 0, 0, DateTimeKind.Utc));

        context.CallRecords.AddRange(record1, record2);
        await context.SaveChangesAsync();

        var criteria = new CallRecordSearchCriteria();

        var (items, total) = await repository.GetPagedAsync(criteria, 1, 20, "updatedAt", "desc");

        Assert.Equal(2, total);
        Assert.Equal("問題 B", items[0].Subject);
    }

    private static ApplicationDbContext CreateContext()
    {
        var options = new DbContextOptionsBuilder<ApplicationDbContext>()
            .UseInMemoryDatabase(databaseName: Guid.NewGuid().ToString())
            .Options;

        return new ApplicationDbContext(options);
    }

    private static CallRecord CreateRecord(
        InquirySystem system,
        string subject,
        string content,
        UrgencyLevel urgencyLevel,
        string contactName)
    {
        var record = CallRecord.Create(
            subject,
            content,
            system.Id,
            urgencyLevel,
            contactName,
            "0912-000-000",
            "user-1");

        record.InquirySystem = system;
        return record;
    }

    private static void SetPrivateProperty(CallRecord record, string propertyName, DateTime value)
    {
        var property = typeof(CallRecord).GetProperty(propertyName, BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic);
        property?.SetValue(record, value);
    }
}
