using CallTrackingSystem.Core.DTOs;
using CallTrackingSystem.Core.Entities;
using CallTrackingSystem.Core.Enums;
using CallTrackingSystem.Core.Interfaces;
using CallTrackingSystem.Core.Services;
using FluentAssertions;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using Moq;
using Xunit;

namespace CallTrackingSystem.UnitTests.Services;

/// <summary>
/// LineBotMessageHandler 單元測試
/// </summary>
public class LineBotMessageHandlerTests
{
    private readonly Mock<IConversationStateService> _mockConversationService;
    private readonly Mock<ILineMessagingApiClient> _mockLineClient;
    private readonly Mock<IUserService> _mockUserService;
    private readonly Mock<IInquirySystemService> _mockInquirySystemService;
    private readonly Mock<CallRecordService> _mockCallRecordService;
    private readonly Mock<IConfiguration> _mockConfiguration;
    private readonly Mock<ILogger<LineBotMessageHandler>> _mockLogger;
    private readonly LineBotMessageHandler _sut;

    public LineBotMessageHandlerTests()
    {
        _mockConversationService = new Mock<IConversationStateService>();
        _mockLineClient = new Mock<ILineMessagingApiClient>();
        _mockUserService = new Mock<IUserService>();
        _mockInquirySystemService = new Mock<IInquirySystemService>();
        _mockCallRecordService = new Mock<CallRecordService>(
            Mock.Of<ICallRecordRepository>(),
            Mock.Of<IInquirySystemRepository>(),
            Mock.Of<IHandlerRepository>(),
            Mock.Of<IHandlerMappingRepository>(),
            Mock.Of<IChangeHistoryService>(),
            Mock.Of<ILineNotificationService>(),
            Mock.Of<IEditLockManager>(),
            Mock.Of<ILogger<CallRecordService>>());
        _mockConfiguration = new Mock<IConfiguration>();
        _mockLogger = new Mock<ILogger<LineBotMessageHandler>>();

        _sut = new LineBotMessageHandler(
            _mockConversationService.Object,
            _mockLineClient.Object,
            _mockUserService.Object,
            _mockInquirySystemService.Object,
            _mockCallRecordService.Object,
            _mockConfiguration.Object,
            _mockLogger.Object);
    }

    [Fact]
    public async Task HandleTextMessageAsync_WhenUserNotBound_ShouldReplyError()
    {
        // Arrange
        var lineUserId = "U123456789";
        var messageText = "回報問題";
        var replyToken = "token123";
        _mockUserService.Setup(x => x.GetByLineUserIdAsync(lineUserId, default))
            .ReturnsAsync((User?)null);

        // Act
        await _sut.HandleTextMessageAsync(lineUserId, messageText, replyToken);

        // Assert
        _mockLineClient.Verify(x => x.ReplyMessageAsync(
            replyToken,
            It.Is<string>(s => s.Contains("尚未綁定"))),
            Times.Once);
    }

    [Fact]
    public async Task HandleTextMessageAsync_WhenCancelCommand_ShouldClearConversation()
    {
        // Arrange
        var lineUserId = "U123456789";
        var messageText = "取消";
        var replyToken = "token123";
        var user = User.Create("testuser", "hash", "測試使用者", UserRole.Staff);
        _mockUserService.Setup(x => x.GetByLineUserIdAsync(lineUserId, default))
            .ReturnsAsync(user);

        // Act
        await _sut.HandleTextMessageAsync(lineUserId, messageText, replyToken);

        // Assert
        _mockConversationService.Verify(x => x.ClearConversationAsync(lineUserId), Times.Once);
        _mockLineClient.Verify(x => x.ReplyMessageAsync(
            replyToken,
            It.Is<string>(s => s.Contains("已取消"))),
            Times.Once);
    }

    [Fact]
    public async Task HandleTextMessageAsync_WhenStartCommand_ShouldStartNewConversation()
    {
        // Arrange
        var lineUserId = "U123456789";
        var messageText = "回報問題";
        var replyToken = "token123";
        var user = User.Create("testuser", "hash", "測試使用者", UserRole.Staff);
        _mockUserService.Setup(x => x.GetByLineUserIdAsync(lineUserId, default))
            .ReturnsAsync(user);

        // Act
        await _sut.HandleTextMessageAsync(lineUserId, messageText, replyToken);

        // Assert
        _mockConversationService.Verify(x => x.StartConversationAsync(lineUserId), Times.Once);
        _mockLineClient.Verify(x => x.ReplyMessageAsync(
            replyToken,
            It.Is<string>(s => s.Contains("問題標題"))),
            Times.Once);
    }

    [Fact]
    public async Task HandleTextMessageAsync_AwaitingSubject_WhenValidInput_ShouldMoveToAwaitingContent()
    {
        // Arrange
        var lineUserId = "U123456789";
        var messageText = "測試問題標題";
        var replyToken = "token123";
        var user = User.Create("testuser", "hash", "測試使用者", UserRole.Staff);
        var conversation = new ConversationStateDto
        {
            LineUserId = lineUserId,
            CurrentStep = ConversationStep.AwaitingSubject,
            FormData = new CallRecordFormData(),
            StartedAt = DateTime.UtcNow,
            LastActivityAt = DateTime.UtcNow
        };

        _mockUserService.Setup(x => x.GetByLineUserIdAsync(lineUserId, default))
            .ReturnsAsync(user);
        _mockConversationService.Setup(x => x.GetConversationAsync(lineUserId))
            .ReturnsAsync(conversation);

        // Act
        await _sut.HandleTextMessageAsync(lineUserId, messageText, replyToken);

        // Assert
        _mockConversationService.Verify(x => x.UpdateConversationAsync(
            It.Is<ConversationStateDto>(c => 
                c.CurrentStep == ConversationStep.AwaitingContent &&
                c.FormData.Subject == messageText)),
            Times.Once);
        _mockLineClient.Verify(x => x.ReplyMessageAsync(
            replyToken,
            It.Is<string>(s => s.Contains("問題內容"))),
            Times.Once);
    }

    [Fact]
    public async Task HandleTextMessageAsync_AwaitingSubject_WhenTooLong_ShouldReplyError()
    {
        // Arrange
        var lineUserId = "U123456789";
        var messageText = new string('a', 51); // 超過 50 字
        var replyToken = "token123";
        var user = User.Create("testuser", "hash", "測試使用者", UserRole.Staff);
        var conversation = new ConversationStateDto
        {
            LineUserId = lineUserId,
            CurrentStep = ConversationStep.AwaitingSubject,
            FormData = new CallRecordFormData(),
            StartedAt = DateTime.UtcNow,
            LastActivityAt = DateTime.UtcNow
        };

        _mockUserService.Setup(x => x.GetByLineUserIdAsync(lineUserId, default))
            .ReturnsAsync(user);
        _mockConversationService.Setup(x => x.GetConversationAsync(lineUserId))
            .ReturnsAsync(conversation);

        // Act
        await _sut.HandleTextMessageAsync(lineUserId, messageText, replyToken);

        // Assert
        _mockLineClient.Verify(x => x.ReplyMessageAsync(
            replyToken,
            It.Is<string>(s => s.Contains("不可超過 50 字"))),
            Times.Once);
    }

    [Fact]
    public async Task HandleTextMessageAsync_AwaitingContactPhone_WhenValidMobile_ShouldMoveToConfirmation()
    {
        // Arrange
        var lineUserId = "U123456789";
        var messageText = "0912-345-678";
        var replyToken = "token123";
        var user = User.Create("testuser", "hash", "測試使用者", UserRole.Staff);
        var conversation = new ConversationStateDto
        {
            LineUserId = lineUserId,
            CurrentStep = ConversationStep.AwaitingContactPhone,
            FormData = new CallRecordFormData
            {
                Subject = "測試",
                Content = "內容",
                InquirySystemId = 1,
                UrgencyLevel = UrgencyLevel.Medium,
                ContactName = "測試人員"
            },
            StartedAt = DateTime.UtcNow,
            LastActivityAt = DateTime.UtcNow
        };

        _mockUserService.Setup(x => x.GetByLineUserIdAsync(lineUserId, default))
            .ReturnsAsync(user);
        _mockConversationService.Setup(x => x.GetConversationAsync(lineUserId))
            .ReturnsAsync(conversation);
        var mockInquirySystem = InquirySystem.Create("測試系統");
        _mockInquirySystemService.Setup(x => x.GetByIdAsync(1, default))
            .ReturnsAsync(mockInquirySystem);

        // Act
        await _sut.HandleTextMessageAsync(lineUserId, messageText, replyToken);

        // Assert
        _mockConversationService.Verify(x => x.UpdateConversationAsync(
            It.Is<ConversationStateDto>(c => 
                c.CurrentStep == ConversationStep.AwaitingConfirmation &&
                c.FormData.ContactPhone == messageText)),
            Times.Once);
    }

    [Fact]
    public async Task HandleTextMessageAsync_AwaitingContactPhone_WhenInvalidFormat_ShouldReplyError()
    {
        // Arrange
        var lineUserId = "U123456789";
        var messageText = "123456"; // 無效格式
        var replyToken = "token123";
        var user = User.Create("testuser", "hash", "測試使用者", UserRole.Staff);
        var conversation = new ConversationStateDto
        {
            LineUserId = lineUserId,
            CurrentStep = ConversationStep.AwaitingContactPhone,
            FormData = new CallRecordFormData(),
            StartedAt = DateTime.UtcNow,
            LastActivityAt = DateTime.UtcNow
        };

        _mockUserService.Setup(x => x.GetByLineUserIdAsync(lineUserId, default))
            .ReturnsAsync(user);
        _mockConversationService.Setup(x => x.GetConversationAsync(lineUserId))
            .ReturnsAsync(conversation);

        // Act
        await _sut.HandleTextMessageAsync(lineUserId, messageText, replyToken);

        // Assert
        _mockLineClient.Verify(x => x.ReplyMessageAsync(
            replyToken,
            It.Is<string>(s => s.Contains("電話格式不正確"))),
            Times.Once);
    }
}
