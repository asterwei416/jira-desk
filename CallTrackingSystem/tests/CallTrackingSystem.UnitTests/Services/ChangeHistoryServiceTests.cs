using CallTrackingSystem.Core.Entities;
using CallTrackingSystem.Core.Interfaces;
using CallTrackingSystem.Core.Services;
using Moq;

namespace CallTrackingSystem.UnitTests.Services;

public class ChangeHistoryServiceTests
{
    [Fact]
    public async Task LogChangeAsync_creates_history_record()
    {
        var repository = new Mock<IChangeHistoryRepository>();
        var service = new ChangeHistoryService(repository.Object);

        await service.LogChangeAsync(1, "Status", "Pending", "Completed", "user-1");

        repository.Verify(repo => repo.AddRangeAsync(
            It.Is<IEnumerable<ChangeHistory>>(items => items.Any(h => h.FieldName == "Status" && h.OldValue == "Pending" && h.NewValue == "Completed")),
            It.IsAny<CancellationToken>()),
            Times.Once);
    }

    [Fact]
    public async Task GetHistoryAsync_returns_history_list()
    {
        var repository = new Mock<IChangeHistoryRepository>();
        var expected = new List<ChangeHistory>
        {
            ChangeHistory.Create(1, "Status", "Pending", "Completed", "user-1")
        };

        repository.Setup(repo => repo.GetByCallRecordIdAsync(1, It.IsAny<CancellationToken>()))
            .ReturnsAsync(expected);

        var service = new ChangeHistoryService(repository.Object);

        var result = await service.GetHistoryAsync(1);

        Assert.Single(result);
        Assert.Equal("Status", result[0].FieldName);
    }
}
