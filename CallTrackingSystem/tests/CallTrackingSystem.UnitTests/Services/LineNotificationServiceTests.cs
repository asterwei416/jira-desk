using CallTrackingSystem.Core.Entities;
using CallTrackingSystem.Core.Enums;
using CallTrackingSystem.Core.Interfaces;
using CallTrackingSystem.Infrastructure.Services;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging.Abstractions;
using Moq;

namespace CallTrackingSystem.UnitTests.Services;

public class LineNotificationServiceTests
{
    [Fact]
    public async Task SendCallRecordNotificationAsync_logs_success()
    {
        var configuration = BuildConfiguration();
        var client = new Mock<ILineMessagingClient>();
        var logService = new Mock<INotificationLogService>();
        var service = new LineNotificationService(
            configuration,
            client.Object,
            logService.Object,
            NullLogger<LineNotificationService>.Instance);

        var record = CreateCallRecord();

        await service.SendCallRecordNotificationAsync(record, new List<string> { "U123" });

        logService.Verify(x => x.LogNotificationAsync(
                record.Id,
                "U123",
                NotificationMessageType.FlexMessage,
                true,
                null,
                It.IsAny<CancellationToken>()),
            Times.Once);
    }

    [Fact]
    public async Task SendCallRecordNotificationAsync_logs_failure_on_exception()
    {
        var configuration = BuildConfiguration();
        var client = new Mock<ILineMessagingClient>();
        client.Setup(x => x.PushMessageAsync(It.IsAny<string>(), It.IsAny<IEnumerable<Line.Messaging.ISendMessage>>(), It.IsAny<CancellationToken>()))
            .ThrowsAsync(new Exception("error"));

        var logService = new Mock<INotificationLogService>();
        var service = new LineNotificationService(
            configuration,
            client.Object,
            logService.Object,
            NullLogger<LineNotificationService>.Instance);

        var record = CreateCallRecord();

        await service.SendCallRecordNotificationAsync(record, new List<string> { "U123" });

        logService.Verify(x => x.LogNotificationAsync(
                record.Id,
                "U123",
                NotificationMessageType.FlexMessage,
                false,
                It.IsAny<string>(),
                It.IsAny<CancellationToken>()),
            Times.Once);
    }

    private static IConfiguration BuildConfiguration()
    {
        return new ConfigurationBuilder()
            .AddInMemoryCollection(new Dictionary<string, string?>
            {
                ["Line:Messaging:DetailUrlBase"] = "https://localhost:5001/CallRecord/Details/"
            })
            .Build();
    }

    private static CallRecord CreateCallRecord()
    {
        var record = CallRecord.Create(
            "帳務異常",
            "內容",
            1,
            UrgencyLevel.High,
            "張三",
            "0912",
            "user-1");

        record.InquirySystem = InquirySystem.Create("帳務系統");
        return record;
    }
}
