using CallTrackingSystem.Core.Entities;
using CallTrackingSystem.Core.Interfaces;
using CallTrackingSystem.Core.Services;
using FluentAssertions;
using Moq;
using Xunit;

namespace CallTrackingSystem.UnitTests.Services;

/// <summary>
/// HandlerMappingService 單元測試
/// </summary>
public class HandlerMappingServiceTests
{
    private readonly Mock<IHandlerMappingRepository> _mappingRepository;
    private readonly Mock<IHandlerRepository> _handlerRepository;
    private readonly Mock<IInquirySystemRepository> _inquirySystemRepository;
    private readonly HandlerMappingService _service;

    public HandlerMappingServiceTests()
    {
        _mappingRepository = new Mock<IHandlerMappingRepository>();
        _handlerRepository = new Mock<IHandlerRepository>();
        _inquirySystemRepository = new Mock<IInquirySystemRepository>();

        _service = new HandlerMappingService(
            _mappingRepository.Object,
            _handlerRepository.Object,
            _inquirySystemRepository.Object);
    }

    [Fact]
    public async Task CreateMappingAsync_WhenHandlerNotFound_ShouldThrow()
    {
        _handlerRepository
            .Setup(r => r.GetByIdAsync(1, It.IsAny<CancellationToken>()))
            .ReturnsAsync((Handler?)null);

        var act = async () => await _service.CreateMappingAsync(1, 2);

        await act.Should().ThrowAsync<InvalidOperationException>()
            .WithMessage("找不到指定處理人員");
    }

    [Fact]
    public async Task CreateMappingAsync_WhenInquirySystemNotFound_ShouldThrow()
    {
        _handlerRepository
            .Setup(r => r.GetByIdAsync(1, It.IsAny<CancellationToken>()))
            .ReturnsAsync(Handler.Create("測試"));

        _inquirySystemRepository
            .Setup(r => r.GetByIdAsync(2, It.IsAny<CancellationToken>()))
            .ReturnsAsync((InquirySystem?)null);

        var act = async () => await _service.CreateMappingAsync(1, 2);

        await act.Should().ThrowAsync<InvalidOperationException>()
            .WithMessage("找不到指定詢問系統");
    }

    [Fact]
    public async Task CreateMappingAsync_WhenMappingExists_ShouldThrow()
    {
        _handlerRepository
            .Setup(r => r.GetByIdAsync(1, It.IsAny<CancellationToken>()))
            .ReturnsAsync(Handler.Create("測試"));

        _inquirySystemRepository
            .Setup(r => r.GetByIdAsync(2, It.IsAny<CancellationToken>()))
            .ReturnsAsync(InquirySystem.Create("系統"));

        _mappingRepository
            .Setup(r => r.ExistsAsync(1, 2, It.IsAny<CancellationToken>()))
            .ReturnsAsync(true);

        var act = async () => await _service.CreateMappingAsync(1, 2);

        await act.Should().ThrowAsync<InvalidOperationException>()
            .WithMessage("此處理人員已對應到該詢問系統");
    }
}
