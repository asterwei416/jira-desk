using CallTrackingSystem.Core.Entities;
using CallTrackingSystem.Core.Enums;
using CallTrackingSystem.Core.Interfaces;
using CallTrackingSystem.Core.Services;
using FluentAssertions;
using Moq;
using Xunit;

namespace CallTrackingSystem.UnitTests.Services;

/// <summary>
/// AuthService 單元測試
/// </summary>
public class AuthServiceTests
{
    private readonly Mock<IUserRepository> _userRepository;
    private readonly Mock<IJwtTokenService> _jwtTokenService;
    private readonly AuthService _authService;

    public AuthServiceTests()
    {
        _userRepository = new Mock<IUserRepository>();
        _jwtTokenService = new Mock<IJwtTokenService>();
        _authService = new AuthService(_userRepository.Object, _jwtTokenService.Object);
    }

    [Fact]
    public async Task LoginAsync_WhenUserNotFound_ShouldThrow()
    {
        _userRepository
            .Setup(r => r.GetByUsernameAsync("user", It.IsAny<CancellationToken>()))
            .ReturnsAsync((User?)null);

        var act = async () => await _authService.LoginAsync("user", "Password1");

        await act.Should().ThrowAsync<InvalidOperationException>()
            .WithMessage("帳號或密碼錯誤");
    }

    [Fact]
    public async Task LoginAsync_WhenUserInactive_ShouldThrow()
    {
        var user = CreateUser(isActive: false);

        _userRepository
            .Setup(r => r.GetByUsernameAsync(user.Username, It.IsAny<CancellationToken>()))
            .ReturnsAsync(user);

        var act = async () => await _authService.LoginAsync(user.Username, "Password1");

        await act.Should().ThrowAsync<InvalidOperationException>()
            .WithMessage("帳號已停用");
    }

    [Fact]
    public async Task LoginAsync_WhenPasswordMismatch_ShouldThrow()
    {
        var user = CreateUser();

        _userRepository
            .Setup(r => r.GetByUsernameAsync(user.Username, It.IsAny<CancellationToken>()))
            .ReturnsAsync(user);

        var act = async () => await _authService.LoginAsync(user.Username, "WrongPassword");

        await act.Should().ThrowAsync<InvalidOperationException>()
            .WithMessage("帳號或密碼錯誤");
    }

    [Fact]
    public async Task LoginAsync_WhenValid_ShouldReturnToken()
    {
        var user = CreateUser();

        _userRepository
            .Setup(r => r.GetByUsernameAsync(user.Username, It.IsAny<CancellationToken>()))
            .ReturnsAsync(user);
        _jwtTokenService
            .Setup(s => s.GenerateToken(user))
            .Returns("token-value");

        var result = await _authService.LoginAsync(user.Username, "Password1");

        result.AccessToken.Should().Be("token-value");
        result.ExpiresIn.Should().Be(86400);
        result.User.Username.Should().Be(user.Username);
    }

    [Fact]
    public async Task ChangePasswordAsync_WhenUserNotFound_ShouldThrow()
    {
        _userRepository
            .Setup(r => r.GetByIdAsync("user-1", It.IsAny<CancellationToken>()))
            .ReturnsAsync((User?)null);

        var act = async () => await _authService.ChangePasswordAsync("user-1", "OldPass1", "NewPass1");

        await act.Should().ThrowAsync<InvalidOperationException>()
            .WithMessage("使用者不存在");
    }

    [Fact]
    public async Task ChangePasswordAsync_WhenOldPasswordWrong_ShouldThrow()
    {
        var user = CreateUser();

        _userRepository
            .Setup(r => r.GetByIdAsync(user.Id, It.IsAny<CancellationToken>()))
            .ReturnsAsync(user);

        var act = async () => await _authService.ChangePasswordAsync(user.Id, "WrongPass1", "NewPass1");

        await act.Should().ThrowAsync<InvalidOperationException>()
            .WithMessage("舊密碼錯誤");
    }

    [Fact]
    public async Task ChangePasswordAsync_WhenNewPasswordInvalid_ShouldThrow()
    {
        var user = CreateUser();

        _userRepository
            .Setup(r => r.GetByIdAsync(user.Id, It.IsAny<CancellationToken>()))
            .ReturnsAsync(user);

        var act = async () => await _authService.ChangePasswordAsync(user.Id, "Password1", "short");

        await act.Should().ThrowAsync<InvalidOperationException>()
            .WithMessage("新密碼格式不正確，需至少 8 碼且包含大小寫英文字母與數字");
    }

    [Fact]
    public async Task ChangePasswordAsync_WhenValid_ShouldUpdatePassword()
    {
        var user = CreateUser();

        _userRepository
            .Setup(r => r.GetByIdAsync(user.Id, It.IsAny<CancellationToken>()))
            .ReturnsAsync(user);

        await _authService.ChangePasswordAsync(user.Id, "Password1", "NewPass1");

        _userRepository.Verify(r => r.UpdateAsync(user, It.IsAny<CancellationToken>()), Times.Once);
        BCrypt.Net.BCrypt.Verify("NewPass1", user.PasswordHash).Should().BeTrue();
    }

    private static User CreateUser(bool isActive = true)
    {
        var hash = BCrypt.Net.BCrypt.HashPassword("Password1");
        var user = User.Create("testuser", hash, "測試使用者", UserRole.Admin);
        if (!isActive)
        {
            user.Deactivate();
        }
        return user;
    }
}
