using System.Text.Json;
using System.Text.RegularExpressions;
using CallTrackingSystem.Core.DTOs;
using CallTrackingSystem.Core.Entities;
using CallTrackingSystem.Core.Enums;
using CallTrackingSystem.Core.Interfaces;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;

namespace CallTrackingSystem.Core.Services;

/// <summary>
/// LINE Bot 訊息處理器
/// </summary>
public class LineBotMessageHandler : ILineBotMessageHandler
{
    private readonly IConversationStateService _conversationService;
    private readonly ILineMessagingApiClient _lineClient;
    private readonly IUserService _userService;
    private readonly IInquirySystemService _inquirySystemService;
    private readonly CallRecordService _callRecordService;
    private readonly IConfiguration _configuration;
    private readonly ILogger<LineBotMessageHandler> _logger;

    private static readonly Regex TaiwanMobileRegex = new(@"^09\d{2}-?\d{3}-?\d{3}$", RegexOptions.Compiled);
    private static readonly Regex TaiwanLandlineRegex = new(@"^0\d{1,2}-?\d{7,8}$", RegexOptions.Compiled);

    public LineBotMessageHandler(
        IConversationStateService conversationService,
        ILineMessagingApiClient lineClient,
        IUserService userService,
        IInquirySystemService inquirySystemService,
        CallRecordService callRecordService,
        IConfiguration configuration,
        ILogger<LineBotMessageHandler> logger)
    {
        _conversationService = conversationService;
        _lineClient = lineClient;
        _userService = userService;
        _inquirySystemService = inquirySystemService;
        _callRecordService = callRecordService;
        _configuration = configuration;
        _logger = logger;
    }

    public async Task HandleTextMessageAsync(string lineUserId, string messageText, string replyToken)
    {
        try
        {
            // 檢查使用者是否已綁定
            var user = await _userService.GetByLineUserIdAsync(lineUserId);
            if (user == null)
            {
                await _lineClient.ReplyMessageAsync(replyToken, "❌ 您尚未綁定系統帳號，請先至網頁端進行綁定。");
                return;
            }

            // 處理「取消」指令
            if (messageText.Trim().Equals("取消", StringComparison.OrdinalIgnoreCase))
            {
                await _conversationService.ClearConversationAsync(lineUserId);
                await _lineClient.ReplyMessageAsync(replyToken, "❌ 已取消回報流程。");
                return;
            }

            // 處理「回報問題」觸發詞
            if (messageText.Trim().Equals("回報問題", StringComparison.OrdinalIgnoreCase))
            {
                await StartNewConversationAsync(lineUserId, replyToken);
                return;
            }

            // 取得對話狀態
            var conversation = await _conversationService.GetConversationAsync(lineUserId);
            if (conversation == null)
            {
                await _lineClient.ReplyMessageAsync(replyToken, "請輸入「回報問題」開始新的回報流程。");
                return;
            }

            // 依步驟處理訊息
            switch (conversation.CurrentStep)
            {
                case ConversationStep.AwaitingSubject:
                    await HandleAwaitingSubjectAsync(lineUserId, messageText, replyToken, conversation);
                    break;

                case ConversationStep.AwaitingContent:
                    await HandleAwaitingContentAsync(lineUserId, messageText, replyToken, conversation);
                    break;

                case ConversationStep.AwaitingInquirySystem:
                    await _lineClient.ReplyMessageAsync(replyToken, "請使用下方按鈕選擇所屬單位。");
                    break;

                case ConversationStep.AwaitingUrgencyLevel:
                    await _lineClient.ReplyMessageAsync(replyToken, "請使用下方按鈕選擇緊急程度。");
                    break;

                case ConversationStep.AwaitingContactName:
                    await HandleAwaitingContactNameAsync(lineUserId, messageText, replyToken, conversation);
                    break;

                case ConversationStep.AwaitingContactPhone:
                    await HandleAwaitingContactPhoneAsync(lineUserId, messageText, replyToken, conversation);
                    break;

                case ConversationStep.AwaitingConfirmation:
                    await _lineClient.ReplyMessageAsync(replyToken, "請使用下方按鈕選擇「確認送出」或「重新填寫」。");
                    break;

                default:
                    await _lineClient.ReplyMessageAsync(replyToken, "⚠️ 發生錯誤，請重新輸入「回報問題」。");
                    await _conversationService.ClearConversationAsync(lineUserId);
                    break;
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "處理文字訊息失敗: LineUserId={LineUserId}", lineUserId);
            await _lineClient.ReplyMessageAsync(replyToken, "❌ 系統發生錯誤，請稍後再試。");
        }
    }

    public async Task HandlePostbackAsync(string lineUserId, string postbackData, string replyToken)
    {
        try
        {
            var user = await _userService.GetByLineUserIdAsync(lineUserId);
            if (user == null)
            {
                await _lineClient.ReplyMessageAsync(replyToken, "❌ 您尚未綁定系統帳號。");
                return;
            }

            var conversation = await _conversationService.GetConversationAsync(lineUserId);
            if (conversation == null)
            {
                await _lineClient.ReplyMessageAsync(replyToken, "⚠️ 對話已過期，請重新輸入「回報問題」。");
                return;
            }

            // 解析 Postback 資料
            var parts = postbackData.Split('=');
            if (parts.Length != 2)
            {
                await _lineClient.ReplyMessageAsync(replyToken, "⚠️ 無效的選擇。");
                return;
            }

            var action = parts[0];
            var value = parts[1];

            switch (action)
            {
                case "inquiry_system":
                    await HandleInquirySystemSelectionAsync(lineUserId, int.Parse(value), replyToken, conversation);
                    break;

                case "urgency":
                    await HandleUrgencyLevelSelectionAsync(lineUserId, value, replyToken, conversation);
                    break;

                case "confirm":
                    if (value == "submit")
                    {
                        await HandleConfirmSubmitAsync(lineUserId, replyToken, conversation, user);
                    }
                    else if (value == "reset")
                    {
                        await StartNewConversationAsync(lineUserId, replyToken);
                    }
                    else if (value == "cancel")
                    {
                        await _conversationService.ClearConversationAsync(lineUserId);
                        await _lineClient.ReplyMessageAsync(replyToken, "❌ 已取消回報流程。");
                    }
                    break;

                default:
                    await _lineClient.ReplyMessageAsync(replyToken, "⚠️ 無效的操作。");
                    break;
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "處理 Postback 失敗: LineUserId={LineUserId}, Data={Data}", lineUserId, postbackData);
            await _lineClient.ReplyMessageAsync(replyToken, "❌ 系統發生錯誤，請稍後再試。");
        }
    }

    #region Private Methods

    private async Task StartNewConversationAsync(string lineUserId, string replyToken)
    {
        await _conversationService.StartConversationAsync(lineUserId);
        await _lineClient.ReplyMessageAsync(replyToken, "請輸入問題標題（最多 50 字）：");
    }

    private async Task HandleAwaitingSubjectAsync(
        string lineUserId,
        string messageText,
        string replyToken,
        ConversationStateDto conversation)
    {
        if (string.IsNullOrWhiteSpace(messageText) || messageText.Length > 50)
        {
            await _lineClient.ReplyMessageAsync(replyToken, "❌ 標題不可為空且不可超過 50 字，請重新輸入：");
            return;
        }

        conversation.FormData.Subject = messageText.Trim();
        conversation = conversation with { CurrentStep = ConversationStep.AwaitingContent };
        await _conversationService.UpdateConversationAsync(conversation);

        await _lineClient.ReplyMessageAsync(replyToken, "請輸入問題內容（最多 150 字）：");
    }

    private async Task HandleAwaitingContentAsync(
        string lineUserId,
        string messageText,
        string replyToken,
        ConversationStateDto conversation)
    {
        if (string.IsNullOrWhiteSpace(messageText) || messageText.Length > 150)
        {
            await _lineClient.ReplyMessageAsync(replyToken, "❌ 內容不可為空且不可超過 150 字，請重新輸入：");
            return;
        }

        conversation.FormData.Content = messageText.Trim();
        conversation = conversation with { CurrentStep = ConversationStep.AwaitingInquirySystem };
        await _conversationService.UpdateConversationAsync(conversation);

        // 取得所有詢問系統
        var allSystems = await _inquirySystemService.GetAllAsync();
        var inquirySystems = allSystems.Where(s => s.IsActive).ToList();
        if (inquirySystems.Count > 13)
        {
            await _conversationService.ClearConversationAsync(lineUserId);
            var webUrl = _configuration["Line:Bot:WebReportUrl"] ?? "https://your-domain.com";
            await _lineClient.ReplyMessageAsync(replyToken, $"⚠️ 選項過多，請至網頁端回報：{webUrl}");
            return;
        }

        // 建立 Quick Reply 按鈕（LINE Messaging API Flex Message 格式）
        var quickReplyButtons = inquirySystems.Select(sys => new
        {
            type = "action",
            action = new
            {
                type = "postback",
                label = sys.Name,
                data = $"inquiry_system={sys.Id}"
            }
        }).ToList();

        var flexMessage = new
        {
            type = "flex",
            altText = "請選擇問題所屬單位",
            contents = new
            {
                type = "bubble",
                body = new
                {
                    type = "box",
                    layout = "vertical",
                    contents = new[]
                    {
                        new { type = "text", text = "請選擇問題所屬單位：", weight = "bold", size = "md" }
                    }
                },
                footer = new
                {
                    type = "box",
                    layout = "vertical",
                    spacing = "sm",
                    contents = quickReplyButtons.Select(btn => new
                    {
                        type = "button",
                        action = btn.action,
                        style = "primary",
                        height = "sm"
                    }).ToArray()
                }
            }
        };

        await _lineClient.PushFlexMessageAsync(lineUserId, flexMessage);
        await _lineClient.ReplyMessageAsync(replyToken, "請使用下方按鈕選擇所屬單位。");
    }

    private async Task HandleInquirySystemSelectionAsync(
        string lineUserId,
        int inquirySystemId,
        string replyToken,
        ConversationStateDto conversation)
    {
        conversation.FormData.InquirySystemId = inquirySystemId;
        conversation = conversation with { CurrentStep = ConversationStep.AwaitingUrgencyLevel };
        await _conversationService.UpdateConversationAsync(conversation);

        // 建立緊急程度 Quick Reply
        var flexMessage = new
        {
            type = "flex",
            altText = "請選擇緊急程度",
            contents = new
            {
                type = "bubble",
                body = new
                {
                    type = "box",
                    layout = "vertical",
                    contents = new[]
                    {
                        new { type = "text", text = "請選擇緊急程度：", weight = "bold", size = "md" }
                    }
                },
                footer = new
                {
                    type = "box",
                    layout = "vertical",
                    spacing = "sm",
                    contents = new[]
                    {
                        new
                        {
                            type = "button",
                            action = new { type = "postback", label = "🟢 低", data = "urgency=Low" },
                            style = "secondary",
                            height = "sm"
                        },
                        new
                        {
                            type = "button",
                            action = new { type = "postback", label = "🟡 中", data = "urgency=Medium" },
                            style = "primary",
                            height = "sm"
                        },
                        new
                        {
                            type = "button",
                            action = new { type = "postback", label = "🔴 高", data = "urgency=High" },
                            style = "danger",
                            height = "sm"
                        }
                    }
                }
            }
        };

        await _lineClient.PushFlexMessageAsync(lineUserId, flexMessage);
        await _lineClient.ReplyMessageAsync(replyToken, "請選擇緊急程度。");
    }

    private async Task HandleUrgencyLevelSelectionAsync(
        string lineUserId,
        string urgencyValue,
        string replyToken,
        ConversationStateDto conversation)
    {
        // 將字串轉成 enum
        if (!Enum.TryParse<UrgencyLevel>(urgencyValue, out var urgencyEnum))
        {
            await _lineClient.ReplyMessageAsync(replyToken, "⚠️ 無效的緊急程度，請重新選擇。");
            return;
        }
        
        conversation.FormData.UrgencyLevel = urgencyEnum;
        conversation = conversation with { CurrentStep = ConversationStep.AwaitingContactName };
        await _conversationService.UpdateConversationAsync(conversation);

        await _lineClient.ReplyMessageAsync(replyToken, "請輸入聯絡人姓名：");
    }

    private async Task HandleAwaitingContactNameAsync(
        string lineUserId,
        string messageText,
        string replyToken,
        ConversationStateDto conversation)
    {
        if (string.IsNullOrWhiteSpace(messageText))
        {
            await _lineClient.ReplyMessageAsync(replyToken, "❌ 聯絡人姓名不可為空，請重新輸入：");
            return;
        }

        conversation.FormData.ContactName = messageText.Trim();
        conversation = conversation with { CurrentStep = ConversationStep.AwaitingContactPhone };
        await _conversationService.UpdateConversationAsync(conversation);

        await _lineClient.ReplyMessageAsync(replyToken, "請輸入聯絡電話（例：0912-345-678 或 02-12345678）：");
    }

    private async Task HandleAwaitingContactPhoneAsync(
        string lineUserId,
        string messageText,
        string replyToken,
        ConversationStateDto conversation)
    {
        var phone = messageText.Trim();
        if (!TaiwanMobileRegex.IsMatch(phone) && !TaiwanLandlineRegex.IsMatch(phone))
        {
            await _lineClient.ReplyMessageAsync(replyToken, "❌ 電話格式不正確（手機：09xx-xxx-xxx，市話：0x-xxxx-xxxx），請重新輸入：");
            return;
        }

        conversation.FormData.ContactPhone = phone;
        conversation = conversation with { CurrentStep = ConversationStep.AwaitingConfirmation };
        await _conversationService.UpdateConversationAsync(conversation);

        // 顯示摘要與確認按鈕
        var inquirySystem = await _inquirySystemService.GetByIdAsync(conversation.FormData.InquirySystemId!.Value);
        var summary = $"📋 回報資訊確認\n\n" +
                      $"標題：{conversation.FormData.Subject}\n" +
                      $"內容：{conversation.FormData.Content}\n" +
                      $"所屬單位：{inquirySystem?.Name}\n" +
                      $"緊急程度：{GetUrgencyLevelText(conversation.FormData.UrgencyLevel!.Value)}\n" +
                      $"聯絡人：{conversation.FormData.ContactName}\n" +
                      $"電話：{conversation.FormData.ContactPhone}\n\n" +
                      $"請確認資訊是否正確。";

        var confirmFlexMessage = new
        {
            type = "flex",
            altText = "回報資訊確認",
            contents = new
            {
                type = "bubble",
                body = new
                {
                    type = "box",
                    layout = "vertical",
                    contents = new[]
                    {
                        new { type = "text", text = summary, wrap = true }
                    }
                },
                footer = new
                {
                    type = "box",
                    layout = "vertical",
                    spacing = "sm",
                    contents = new[]
                    {
                        new
                        {
                            type = "button",
                            action = new { type = "postback", label = "✅ 確認送出", data = "confirm=submit" },
                            style = "primary",
                            height = "sm"
                        },
                        new
                        {
                            type = "button",
                            action = new { type = "postback", label = "🔄 重新填寫", data = "confirm=reset" },
                            style = "secondary",
                            height = "sm"
                        },
                        new
                        {
                            type = "button",
                            action = new { type = "postback", label = "❌ 取消", data = "confirm=cancel" },
                            style = "secondary",
                            height = "sm"
                        }
                    }
                }
            }
        };

        await _lineClient.PushFlexMessageAsync(lineUserId, confirmFlexMessage);
        await _lineClient.ReplyMessageAsync(replyToken, "請確認資訊。");
    }

    private async Task HandleConfirmSubmitAsync(
        string lineUserId,
        string replyToken,
        ConversationStateDto conversation,
        User user)
    {
        try
        {
            // 建立 CallRecordRequest DTO
            var request = new CreateCallRecordRequest
            {
                Subject = conversation.FormData.Subject!,
                Content = conversation.FormData.Content!,
                InquirySystemId = conversation.FormData.InquirySystemId!.Value,
                UrgencyLevel = conversation.FormData.UrgencyLevel!.Value,
                ContactName = conversation.FormData.ContactName!,
                ContactPhone = conversation.FormData.ContactPhone!,
                FaqReference = null
            };

            // 呼叫 CallRecordService 建立紀錄
            var callRecord = await _callRecordService.CreateAsync(request, user.Id);

            // 清除對話狀態
            await _conversationService.ClearConversationAsync(lineUserId);

            // 回覆成功訊息
            var detailUrl = _configuration["Line:Bot:DetailUrlBase"] ?? "https://your-domain.com/callrecord";
            var successMessage = $"✅ 回報成功！\n\n" +
                                 $"回報單編號：{callRecord.Id}\n" +
                                 $"查看詳情：{detailUrl}/{callRecord.Id}";

            await _lineClient.ReplyMessageAsync(replyToken, successMessage);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "建立回報單失敗: LineUserId={LineUserId}", lineUserId);
            await _lineClient.ReplyMessageAsync(replyToken, "❌ 建立回報單失敗，請稍後再試或至網頁端回報。");
        }
    }

    private static string GetUrgencyLevelText(UrgencyLevel urgencyLevel)
    {
        return urgencyLevel switch
        {
            UrgencyLevel.Low => "🟢 低",
            UrgencyLevel.Medium => "🟡 中",
            UrgencyLevel.High => "🔴 高",
            _ => urgencyLevel.ToString()
        };
    }

    #endregion
}
