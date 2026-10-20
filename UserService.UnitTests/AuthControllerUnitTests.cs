
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Options;
using Moq;
using System.Timers;
using UserService.Configuration;
using UserService.Controllers;
using UserService.DTOs;
using UserService.Models;
using UserService.Services;
using Xunit;

namespace UserService.UnitTests;

public sealed class AuthControllerUnitTests
{
    private readonly Mock<UserManager<ApplicationUser>> _userManager;
    private readonly Mock<SignInManager<ApplicationUser>> _signInManager;
    private readonly Mock<ITokenService> _tokenService;
    private readonly AuthController _controller;

    public AuthControllerUnitTests()
    {
        var store = new Mock<IUserStore<ApplicationUser>>();

        _userManager = new Mock<UserManager<ApplicationUser>>(
            store.Object,
            null!, null!, null!, null!, null!, null!, null!, null!);

        var contextAccessor =
            new Mock<Microsoft.AspNetCore.Http.IHttpContextAccessor>();

        var claimsFactory =
            new Mock<IUserClaimsPrincipalFactory<ApplicationUser>>();

        _signInManager = new Mock<SignInManager<ApplicationUser>>(
            _userManager.Object,
            contextAccessor.Object,
            claimsFactory.Object,
            null!, null!, null!, null!);

        _tokenService = new Mock<ITokenService>();

        var jwtOptions = Options.Create(new JwtOptions
        {
            AccessTokenMinutes = 30
        });

        _controller = new AuthController(
            _userManager.Object,
            _signInManager.Object,
            _tokenService.Object,
            jwtOptions);
    }

    [Fact]
    public async Task Login_UserDoesNotExist_ReturnsUnauthorized()
    {
        // Arrange
        _userManager
            .Setup(x => x.FindByEmailAsync("missing@example.com"))
            .ReturnsAsync((ApplicationUser?)null);

        var request = new LoginRequest
        {
            Email = "missing@example.com",
            Password = "Password123!"
        };

        // Act
        var result = await _controller.Login(request);

        // Assert
        Assert.IsType<UnauthorizedObjectResult>(result);

        _signInManager.Verify(
            x => x.CheckPasswordSignInAsync(
                It.IsAny<ApplicationUser>(),
                It.IsAny<string>(),
                It.IsAny<bool>()),
            Times.Never);

        _tokenService.Verify(
            x => x.CreateAccessTokenAsync(
                It.IsAny<ApplicationUser>()),
            Times.Never);
    }

    [Fact]
    public async Task Login_InvalidPassword_ReturnsUnauthorized()
    {
        // Arrange
        var user = new ApplicationUser
        {
            Id = "user-123",
            UserName = "customer@example.com",
            Email = "customer@example.com",
            DisplayName = "Test Customer"
        };

        _userManager
            .Setup(x => x.FindByEmailAsync("customer@example.com"))
            .ReturnsAsync(user);

        _signInManager
            .Setup(x => x.CheckPasswordSignInAsync(
                user, "WrongPassword123!", true))
            .ReturnsAsync(Microsoft.AspNetCore.Identity.SignInResult.Failed);

        var request = new LoginRequest
        {
            Email = "customer@example.com",
            Password = "WrongPassword123!"
        };

        // Act
        var result = await _controller.Login(request);

        // Assert
        Assert.IsType<UnauthorizedObjectResult>(result);

        _tokenService.Verify(
            x => x.CreateAccessTokenAsync(
                It.IsAny<ApplicationUser>()),
            Times.Never);
    }

    [Fact]
    public async Task Login_ValidCredentials_ReturnsTokenAndUserDetails()
    {
        // Arrange
        var user = new ApplicationUser
        {
            Id = "user-123",
            UserName = "customer@example.com",
            Email = "customer@example.com",
            DisplayName = "Test Customer"
        };

        _userManager
            .Setup(x => x.FindByEmailAsync("customer@example.com"))
            .ReturnsAsync(user);

        _signInManager
            .Setup(x => x.CheckPasswordSignInAsync(
                user, "CorrectPassword123!", true))
            .ReturnsAsync(Microsoft.AspNetCore.Identity.SignInResult.Success);

        _tokenService
            .Setup(x => x.CreateAccessTokenAsync(user))
            .ReturnsAsync("fake-test-jwt");

        _userManager
            .Setup(x => x.GetRolesAsync(user))
            .ReturnsAsync(new List<string> { "Customer" });

        var request = new LoginRequest
        {
            Email = "customer@example.com",
            Password = "CorrectPassword123!"
        };

        // Act
        var result = await _controller.Login(request);

        // Assert
        var okResult = Assert.IsType<OkObjectResult>(result);
        var response = Assert.IsType<AuthResponse>(okResult.Value);

        Assert.Equal("fake-test-jwt", response.AccessToken);
        Assert.Equal("Bearer", response.TokenType);
        Assert.Equal("user-123", response.UserId);
        Assert.Equal("customer@example.com", response.Email);
        Assert.Equal("Test Customer", response.DisplayName);
        Assert.Contains("Customer", response.Roles);
        Assert.True(response.ExpiresAtUtc > DateTime.UtcNow);

        _tokenService.Verify(
            x => x.CreateAccessTokenAsync(user),
            Times.Once);
    }

    [Fact]
    public async Task Login_LockedOutUser_ReturnsUnauthorized()
    {
        // Arrange
        var user = new ApplicationUser
        {
            Id = "user-456",
            UserName = "locked@example.com",
            Email = "locked@example.com",
            DisplayName = "Locked Customer"
        };

        _userManager
            .Setup(x => x.FindByEmailAsync("locked@example.com"))
            .ReturnsAsync(user);

        _signInManager
            .Setup(x => x.CheckPasswordSignInAsync(
                user, "Password123!", true))
            .ReturnsAsync(Microsoft.AspNetCore.Identity.SignInResult.LockedOut);

        var request = new LoginRequest
        {
            Email = "locked@example.com",
            Password = "Password123!"
        };

        // Act
        var result = await _controller.Login(request);

        // Assert
        Assert.IsType<UnauthorizedObjectResult>(result);

        _tokenService.Verify(
            x => x.CreateAccessTokenAsync(
                It.IsAny<ApplicationUser>()),
            Times.Never);
    }
}
