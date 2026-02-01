using CallTrackingSystem.Core.Entities;
using CallTrackingSystem.Core.Interfaces;
using CallTrackingSystem.Core.Services;
using FluentAssertions;
using Moq;
using Xunit;

namespace CallTrackingSystem.UnitTests.Services;

/// <summary>
/// InquirySystemService 單元測試
/// </summary>
public class InquirySystemServiceTests
{
    private readonly Mock<IInquirySystemRepository> _repository;
    private readonly InquirySystemService _service;

    public InquirySystemServiceTests()
    {
        _repository = new Mock<IInquirySystemRepository>();
        _service = new InquirySystemService(_repository.Object);
    }

    [Fact]
    public async Task CreateAsync_WhenNameExists_ShouldThrow()
    {
        _repository
            .Setup(r => r.GetByNameAsync("測試系統", It.IsAny<CancellationToken>()))
            .ReturnsAsync(InquirySystem.Create("測試系統"));

        var act = async () => await _service.CreateAsync("測試系統");

        await act.Should().ThrowAsync<InvalidOperationException>()
            .WithMessage("系統名稱已存在");
    }

    [Fact]
    public async Task CreateAsync_WhenValid_ShouldReturnEntity()
    {
        _repository
            .Setup(r => r.GetByNameAsync("新系統", It.IsAny<CancellationToken>()))
            .ReturnsAsync((InquirySystem?)null);

        _repository
            .Setup(r => r.AddAsync(It.IsAny<InquirySystem>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync((InquirySystem input, CancellationToken _) => input);

        var result = await _service.CreateAsync("新系統");

        result.Name.Should().Be("新系統");
        result.IsActive.Should().BeTrue();
    }

    [Fact]
    public async Task UpdateAsync_WhenNotFound_ShouldThrow()
    {
        _repository
            .Setup(r => r.GetByIdAsync(1, It.IsAny<CancellationToken>()))
            .ReturnsAsync((InquirySystem?)null);

        var act = async () => await _service.UpdateAsync(1, "系統", true);

        await act.Should().ThrowAsync<InvalidOperationException>()
            .WithMessage("詢問系統不存在");
    }

    [Fact]
    public async Task UpdateAsync_WhenDuplicateName_ShouldThrow()
    {
        var existing = InquirySystem.Create("原始名稱");
        typeof(InquirySystem).GetProperty("Id")!.SetValue(existing, 1);

        _repository
            .Setup(r => r.GetByIdAsync(1, It.IsAny<CancellationToken>()))
            .ReturnsAsync(existing);

        var duplicate = InquirySystem.Create("重複名稱");
        typeof(InquirySystem).GetProperty("Id")!.SetValue(duplicate, 2);

        _repository
            .Setup(r => r.GetByNameAsync("重複名稱", It.IsAny<CancellationToken>()))
            .ReturnsAsync(duplicate);

        var act = async () => await _service.UpdateAsync(1, "重複名稱", true);

        await act.Should().ThrowAsync<InvalidOperationException>()
            .WithMessage("系統名稱已存在");
    }

    [Fact]
    public async Task DeleteAsync_WhenHasCallRecords_ShouldThrow()
    {
        var system = InquirySystem.Create("測試系統");
        typeof(InquirySystem).GetProperty("Id")!.SetValue(system, 5);

        _repository
            .Setup(r => r.GetByIdAsync(5, It.IsAny<CancellationToken>()))
            .ReturnsAsync(system);

        _repository
            .Setup(r => r.HasCallRecordsAsync(5, It.IsAny<CancellationToken>()))
            .ReturnsAsync(true);

        var act = async () => await _service.DeleteAsync(5);

        await act.Should().ThrowAsync<InvalidOperationException>()
            .WithMessage("此詢問系統有相關來電紀錄，無法刪除");
    }
}
