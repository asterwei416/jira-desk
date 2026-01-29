using CallTrackingSystem.Core.Entities;
using CallTrackingSystem.Core.Interfaces;
using Line.Messaging;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;

namespace CallTrackingSystem.Infrastructure.Services;

/// <summary>
/// LINE 通知服務
/// </summary>
public class LineNotificationService : ILineNotificationService
{
    private const string DefaultDetailUrl = "https://localhost:5001/CallRecord/Details/";
    private readonly ILineMessagingClient _client;
    private readonly INotificationLogService _notificationLogService;
    private readonly ILogger<LineNotificationService> _logger;
    private readonly string _detailUrlBase;

    public LineNotificationService(
        IConfiguration configuration,
        ILineMessagingClient client,
        INotificationLogService notificationLogService,
        ILogger<LineNotificationService> logger)
    {
        _client = client;
        _notificationLogService = notificationLogService;
        _logger = logger;
        _detailUrlBase = configuration["LineMessaging:DetailUrlBase"] ?? DefaultDetailUrl;
    }

    public async Task SendCallRecordNotificationAsync(
        CallRecord callRecord,
        IReadOnlyCollection<string> lineUserIds,
        CancellationToken cancellationToken = default)
    {
        if (lineUserIds.Count == 0)
        {
            return;
        }

        var flexMessage = BuildCallRecordFlexMessage(callRecord);

        foreach (var lineUserId in lineUserIds)
        {
            try
            {
                await _client.PushMessageAsync(
                    lineUserId,
                    new List<ISendMessage> { flexMessage },
                    cancellationToken);

                await _notificationLogService.LogNotificationAsync(
                    callRecord.Id,
                    lineUserId,
                    NotificationMessageType.FlexMessage,
                    success: true,
                    cancellationToken: cancellationToken);
            }
            catch (LineResponseException ex)
            {
                _logger.LogWarning(ex, "LINE 通知發送失敗: {Message}", ex.Message);
                await _notificationLogService.LogNotificationAsync(
                    callRecord.Id,
                    lineUserId,
                    NotificationMessageType.FlexMessage,
                    success: false,
                    errorMessage: ex.Message,
                    cancellationToken: cancellationToken);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "LINE 通知發送失敗: 未預期錯誤");
                await _notificationLogService.LogNotificationAsync(
                    callRecord.Id,
                    lineUserId,
                    NotificationMessageType.FlexMessage,
                    success: false,
                    errorMessage: "LINE 通知發送失敗",
                    cancellationToken: cancellationToken);
            }
        }
    }

    private FlexMessage BuildCallRecordFlexMessage(CallRecord record)
    {
        var bubble = new BubbleContainer
        {
            Body = new BoxComponent
            {
                Layout = BoxLayout.Vertical,
                Contents = new List<IFlexComponent>
                {
                    new TextComponent
                    {
                        Text = "新來電問題",
                        Weight = Weight.Bold,
                        Size = ComponentSize.Xl
                    },
                    new BoxComponent
                    {
                        Layout = BoxLayout.Vertical,
                        Margin = Spacing.Lg,
                        Spacing = Spacing.Sm,
                        Contents = new List<IFlexComponent>
                        {
                            CreateInfoRow("詢問系統", record.InquirySystem?.Name ?? "-") ,
                            CreateInfoRow("主旨", record.Subject),
                            CreateInfoRow("緊急度", record.UrgencyLevel.ToString()),
                            CreateInfoRow("聯絡人", record.ContactName),
                            CreateInfoRow("電話", record.ContactPhone)
                        }
                    }
                }
            },
            Footer = new BoxComponent
            {
                Layout = BoxLayout.Vertical,
                Contents = new List<IFlexComponent>
                {
                    new ButtonComponent
                    {
                        Style = ButtonStyle.Link,
                        Action = new UriTemplateAction("查看詳情", $"{_detailUrlBase}{record.Id}")
                    }
                }
            }
        };

        return new FlexMessage("新來電問題通知")
        {
            Contents = bubble
        };
    }

    private static BoxComponent CreateInfoRow(string label, string value)
    {
        return new BoxComponent
        {
            Layout = BoxLayout.Baseline,
            Spacing = Spacing.Sm,
            Contents = new List<IFlexComponent>
            {
                new TextComponent
                {
                    Text = label,
                    Size = ComponentSize.Sm,
                    Color = "#64748B",
                    Flex = 2
                },
                new TextComponent
                {
                    Text = value,
                    Size = ComponentSize.Sm,
                    Color = "#0F172A",
                    Wrap = true,
                    Flex = 5
                }
            }
        };
    }
}
