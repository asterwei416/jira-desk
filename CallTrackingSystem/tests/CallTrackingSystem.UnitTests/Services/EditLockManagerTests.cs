using System.Reflection;
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
/// EditLockManager 單元測試
/// </summary>
public class EditLockManagerTests
{
    private readonly Mock<ICallRecordRepository> _callRecordRepository;
    private readonly EditLockManager _editLockManager;

    public EditLockManagerTests()
    {
        _callRecordRepository = new Mock<ICallRecordRepository>();
        _editLockManager = new EditLockManager(_callRecordRepository.Object);
    }

    [Fact]
    public async Task AcquireLockAsync_WhenRecordNotFound_ShouldThrow()
    {
        _callRecordRepository
            .Setup(r => r.GetByIdAsync(99, It.IsAny<CancellationToken>()))
            .ReturnsAsync((CallRecord?)null);

        var act = async () => await _editLockManager.AcquireLockAsync(99, "user-1");

        await act.Should().ThrowAsync<InvalidOperationException>()
            .WithMessage("來電紀錄不存在");
    }

    [Fact]
    public async Task AcquireLockAsync_WhenUnlocked_ShouldAcquire()
    {
        var record = CreateCallRecord(1);

        _callRecordRepository
            .Setup(r => r.GetByIdAsync(1, It.IsAny<CancellationToken>()))
            .ReturnsAsync(record);

        var result = await _editLockManager.AcquireLockAsync(1, "user-1");

        result.Acquired.Should().BeTrue();
        result.IsLocked.Should().BeTrue();
        result.LockedByUserId.Should().Be("user-1");
        result.LockedAt.Should().NotBeNull();

        _callRecordRepository.Verify(r => r.UpdateAsync(record, It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task AcquireLockAsync_WhenLockedByOther_ShouldReturnConflictState()
    {
        var record = CreateCallRecord(2);
        record.LockedByUserId = "other-user";
        record.LockedAt = DateTime.UtcNow;

        _callRecordRepository
            .Setup(r => r.GetByIdAsync(2, It.IsAny<CancellationToken>()))
            .ReturnsAsync(record);

        var result = await _editLockManager.AcquireLockAsync(2, "user-1");

        result.Acquired.Should().BeFalse();
        result.IsLocked.Should().BeTrue();
        result.LockedByUserId.Should().Be("other-user");

        _callRecordRepository.Verify(r => r.UpdateAsync(record, It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task ReleaseLockAsync_WhenLockedBySelf_ShouldRelease()
    {
        var record = CreateCallRecord(3);
        record.LockedByUserId = "user-1";
        record.LockedAt = DateTime.UtcNow;

        _callRecordRepository
            .Setup(r => r.GetByIdAsync(3, It.IsAny<CancellationToken>()))
            .ReturnsAsync(record);

        var result = await _editLockManager.ReleaseLockAsync(3, "user-1");

        result.IsLocked.Should().BeFalse();
        result.LockedByUserId.Should().BeNull();
        result.LockedAt.Should().BeNull();

        _callRecordRepository.Verify(r => r.UpdateAsync(record, It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task ForceUnlockAsync_WhenLocked_ShouldClearLock()
    {
        var record = CreateCallRecord(4);
        record.LockedByUserId = "other-user";
        record.LockedAt = DateTime.UtcNow;

        _callRecordRepository
            .Setup(r => r.GetByIdAsync(4, It.IsAny<CancellationToken>()))
            .ReturnsAsync(record);

        var result = await _editLockManager.ForceUnlockAsync(4);

        result.IsLocked.Should().BeFalse();
        result.LockedByUserId.Should().BeNull();
        result.LockedAt.Should().BeNull();

        _callRecordRepository.Verify(r => r.UpdateAsync(record, It.IsAny<CancellationToken>()), Times.Once);
    }

    private static CallRecord CreateCallRecord(int id)
    {
        var record = CallRecord.Create(
            "主旨",
            "內容",
            1,
            UrgencyLevel.Low,
            "聯絡人",
            "0912345678",
            "user-1");

        SetPrivateProperty(record, "Id", id);
        record.InquirySystem = InquirySystem.Create("測試系統");
        return record;
    }

    private static void SetPrivateProperty<T>(T target, string propertyName, object? value)
    {
        var property = typeof(T).GetProperty(
            propertyName,
            BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic);

        property?.SetValue(target, value);
    }
}
