using CallTrackingSystem.Core.Entities;
using CallTrackingSystem.Core.Enums;
using CallTrackingSystem.Core.Interfaces;
using CallTrackingSystem.Core.Services;
using FluentAssertions;
using Moq;
using Xunit;

namespace CallTrackingSystem.UnitTests.Services;

/// <summary>
/// UserManagementService 單元測試
/// </summary>
public class UserManagementServiceTests
{
    private readonly Mock<IUserRepository> _repository;
    private readonly UserManagementService _service;

    public UserManagementServiceTests()
    {
        _repository = new Mock<IUserRepository>();
        _service = new UserManagementService(_repository.Object);
    }

    [Fact]
    public async Task CreateAsync_WhenUsernameExists_ShouldThrow()
    {
        var existing = User.Create("existing", BCrypt.Net.BCrypt.HashPassword("Password1"), "已存在", UserRole.Staff);

        _repository
            .Setup(r => r.GetByUsernameAsync("existing", It.IsAny<CancellationToken>()))
            .ReturnsAsync(existing);

        var act = async () => await _service.CreateAsync("existing", "Password1", "新使用者", UserRole.Admin);

        await act.Should().ThrowAsync<InvalidOperationException>()
            .WithMessage("使用者名稱已存在");
    }

    [Fact]
    public async Task UpdateAsync_WhenUserNotFound_ShouldThrow()
    {
        _repository
            .Setup(r => r.GetByIdAsync("user-1", It.IsAny<CancellationToken>()))
            .ReturnsAsync((User?)null);

        var act = async () => await _service.UpdateAsync("user-1", "名稱", UserRole.Admin, true);

        await act.Should().ThrowAsync<InvalidOperationException>()
            .WithMessage("使用者不存在");
    }

    [Fact]
    public async Task ResetPasswordAsync_WhenInvalidPassword_ShouldThrow()
    {
        var user = User.Create("user", BCrypt.Net.BCrypt.HashPassword("Password1"), "使用者", UserRole.Staff);

        _repository
            .Setup(r => r.GetByIdAsync(user.Id, It.IsAny<CancellationToken>()))
            .ReturnsAsync(user);

        var act = async () => await _service.ResetPasswordAsync(user.Id, "short");

        await act.Should().ThrowAsync<InvalidOperationException>()
            .WithMessage("新密碼格式不正確，需至少 8 碼且包含大小寫英文字母與數字");
    }
}
