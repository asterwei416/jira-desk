using CallTrackingSystem.Core.DTOs;
using CallTrackingSystem.Core.Entities;
using CallTrackingSystem.Core.Enums;
using CallTrackingSystem.Core.Interfaces;
using CallTrackingSystem.Core.Services;
using FluentAssertions;
using Moq;
using Xunit;

namespace CallTrackingSystem.UnitTests.Services;

/// <summary>
/// CallRecordService 單元測試
/// </summary>
public class CallRecordServiceTests
{
    private readonly Mock<ICallRecordRepository> _callRecordRepository;
    private readonly Mock<IInquirySystemRepository> _inquirySystemRepository;
    private readonly Mock<IHandlerRepository> _handlerRepository;
    private readonly Mock<IChangeHistoryRepository> _changeHistoryRepository;
    private readonly Mock<ILineNotificationService> _lineNotificationService;
    private readonly Mock<INotificationLogService> _notificationLogService;
    private readonly CallRecordService _service;

    public CallRecordServiceTests()
    {
        _callRecordRepository = new Mock<ICallRecordRepository>();
        _inquirySystemRepository = new Mock<IInquirySystemRepository>();
        _handlerRepository = new Mock<IHandlerRepository>();
        _changeHistoryRepository = new Mock<IChangeHistoryRepository>();
        _lineNotificationService = new Mock<ILineNotificationService>();
        _notificationLogService = new Mock<INotificationLogService>();

        _service = new CallRecordService(
            _callRecordRepository.Object,
            _inquirySystemRepository.Object,
            _handlerRepository.Object,
            _changeHistoryRepository.Object,
            _lineNotificationService.Object,
            _notificationLogService.Object);
    }

    [Fact]
    public async Task CreateAsync_WhenInquirySystemNotFound_ShouldThrow()
    {
        _inquirySystemRepository
            .Setup(r => r.GetByIdAsync(1, It.IsAny<CancellationToken>()))
            .ReturnsAsync((InquirySystem?)null);

        var request = CreateRequest();

        var act = async () => await _service.CreateAsync(request, "user-1");

        await act.Should().ThrowAsync<InvalidOperationException>()
            .WithMessage("詢問系統不存在");
    }

    [Fact]
    public async Task CreateAsync_WhenValid_ShouldAssignHandlersAndNotify()
    {
        var inquirySystem = InquirySystem.Create("系統");
        typeof(InquirySystem).GetProperty("Id")!.SetValue(inquirySystem, 1);

        var handler = Handler.Create("處理人員", "U123");
        typeof(Handler).GetProperty("Id")!.SetValue(handler, 10);

        var callRecord = CallRecord.Create("主旨", "內容", 1, UrgencyLevel.Medium, "聯絡人", "0912345678", "user-1");
        typeof(CallRecord).GetProperty("Id")!.SetValue(callRecord, 100);
        callRecord.InquirySystem = inquirySystem;
        callRecord.Handlers = new List<Handler> { handler };

        _inquirySystemRepository
            .Setup(r => r.GetByIdAsync(1, It.IsAny<CancellationToken>()))
            .ReturnsAsync(inquirySystem);

        _handlerRepository
            .Setup(r => r.GetHandlersByInquirySystemIdAsync(1, It.IsAny<CancellationToken>()))
            .ReturnsAsync(new List<Handler> { handler });

        _callRecordRepository
            .Setup(r => r.AddAsync(It.IsAny<CallRecord>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(callRecord);

        _callRecordRepository
            .Setup(r => r.GetByIdAsync(100, It.IsAny<CancellationToken>()))
            .ReturnsAsync(callRecord);

        var result = await _service.CreateAsync(CreateRequest(), "user-1");

        result.Id.Should().Be(100);
        result.Handlers.Should().HaveCount(1);
        _lineNotificationService.Verify(s => s.SendCallRecordNotificationAsync(callRecord, It.IsAny<IReadOnlyCollection<string>>(), It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task UpdateAsync_WhenLockedByOther_ShouldThrow()
    {
        var record = CallRecord.Create("主旨", "內容", 1, UrgencyLevel.Low, "聯絡人", "0912345678", "user-1");
        typeof(CallRecord).GetProperty("Id")!.SetValue(record, 5);
        record.LockedByUserId = "other-user";
        record.LockedAt = DateTime.UtcNow;

        _callRecordRepository
            .Setup(r => r.GetByIdAsync(5, It.IsAny<CancellationToken>()))
            .ReturnsAsync(record);

        var request = new UpdateCallRecordRequest
        {
            Subject = "新主旨",
            Content = "新內容",
            UrgencyLevel = UrgencyLevel.High,
            ContactName = "新聯絡人",
            ContactPhone = "0912345678",
            FaqReference = null
        };

        var act = async () => await _service.UpdateAsync(5, request, "user-1");

        await act.Should().ThrowAsync<InvalidOperationException>()
            .WithMessage("此紀錄正被其他使用者編輯中（鎖定者: other-user）");
    }

    private static CreateCallRecordRequest CreateRequest()
    {
        return new CreateCallRecordRequest
        {
            Subject = "主旨",
            Content = "內容",
            InquirySystemId = 1,
            UrgencyLevel = UrgencyLevel.Medium,
            ContactName = "聯絡人",
            ContactPhone = "0912345678",
            FaqReference = null
        };
    }
}
