using CallTrackingSystem.Core.Entities;
using CallTrackingSystem.Core.Interfaces;
using CallTrackingSystem.Core.Services;
using FluentAssertions;
using Moq;
using Xunit;

namespace CallTrackingSystem.UnitTests.Services;

/// <summary>
/// HandlerService 單元測試
/// </summary>
public class HandlerServiceTests
{
    private readonly Mock<IHandlerRepository> _repository;
    private readonly HandlerService _service;

    public HandlerServiceTests()
    {
        _repository = new Mock<IHandlerRepository>();
        _service = new HandlerService(_repository.Object);
    }

    [Fact]
    public async Task CreateAsync_WhenLineUserIdExists_ShouldThrow()
    {
        _repository
            .Setup(r => r.GetByLineUserIdAsync("U123", It.IsAny<CancellationToken>()))
            .ReturnsAsync(Handler.Create("已存在", "U123"));

        var act = async () => await _service.CreateAsync("新處理人員", "U123");

        await act.Should().ThrowAsync<InvalidOperationException>()
            .WithMessage("此 LINE User ID 已被使用");
    }

    [Fact]
    public async Task UpdateAsync_WhenNotFound_ShouldThrow()
    {
        _repository
            .Setup(r => r.GetByIdAsync(1, It.IsAny<CancellationToken>()))
            .ReturnsAsync((Handler?)null);

        var act = async () => await _service.UpdateAsync(1, "名稱", null, true);

        await act.Should().ThrowAsync<InvalidOperationException>()
            .WithMessage("處理人員不存在");
    }

    [Fact]
    public async Task UpdateAsync_WhenLineUserIdUsedByOther_ShouldThrow()
    {
        var handler = Handler.Create("原始", "U999");
        typeof(Handler).GetProperty("Id")!.SetValue(handler, 1);

        _repository
            .Setup(r => r.GetByIdAsync(1, It.IsAny<CancellationToken>()))
            .ReturnsAsync(handler);

        var other = Handler.Create("其他", "U123");
        typeof(Handler).GetProperty("Id")!.SetValue(other, 2);

        _repository
            .Setup(r => r.GetByLineUserIdAsync("U123", It.IsAny<CancellationToken>()))
            .ReturnsAsync(other);

        var act = async () => await _service.UpdateAsync(1, "更新", "U123", true);

        await act.Should().ThrowAsync<InvalidOperationException>()
            .WithMessage("此 LINE User ID 已被使用");
    }
}
