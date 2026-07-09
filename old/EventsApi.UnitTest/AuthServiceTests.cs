using EventsApi.Application.Interfaces;
using EventsApi.Application.Services;
using EventsApi.Domain.Entities;
using EventsApi.Domain.Enums;
using Moq;

namespace EventsApi.UnitTests;

public class AuthServiceTests
{
    private readonly Mock<IUserRepository> _userRepositoryMock;
    private readonly Mock<IPasswordHasher> _passwordHasherMock;
    private readonly Mock<ITokenGenerator> _tokenGeneratorMock;
    private readonly AuthenticationService _authService;

    public AuthServiceTests()
    {
        _userRepositoryMock = new Mock<IUserRepository>();
        _passwordHasherMock = new Mock<IPasswordHasher>();
        _tokenGeneratorMock = new Mock<ITokenGenerator>();
        _authService = new AuthenticationService(
            _passwordHasherMock.Object,
            _tokenGeneratorMock.Object,
            _userRepositoryMock.Object
        );
    }
    [Fact]
    public async Task LoginAsync_UserNotFound_ReturnsNull()
    {
        // Arrange
        var login = "testuser";
        var password = "password123";
        var ct = CancellationToken.None;

        _userRepositoryMock
            .Setup(x => x.GetUserByLoginAsync(login, ct))
            .ReturnsAsync((User?)null);

        // Act
        var result = await _authService.LoginAsync(login, password, ct);

        // Assert
        Assert.Null(result);
        _userRepositoryMock.Verify(x => x.GetUserByLoginAsync(login, ct), Times.Once);
        _passwordHasherMock.Verify(x => x.VerifyHashedPassword(It.IsAny<string>(), It.IsAny<string>()), Times.Never);
        _tokenGeneratorMock.Verify(x => x.GenerateToken(It.IsAny<User>(), It.IsAny<CancellationToken>()), Times.Never);
    }

    [Fact]
    public async Task LoginAsync_InvalidPassword_ReturnsNull()
    {
        // Arrange
        var login = "testuser";
        var password = "wrongpassword";
        var ct = CancellationToken.None;
        var user = User.Create(login, "hashedpassword", RoleType.Admin);

        _userRepositoryMock
            .Setup(x => x.GetUserByLoginAsync(login, ct))
            .ReturnsAsync(user);

        _passwordHasherMock
            .Setup(x => x.VerifyHashedPassword(password, user.PasswordHash))
            .Returns(false);

        // Act
        var result = await _authService.LoginAsync(login, password, ct);

        // Assert
        Assert.Null(result);
        _userRepositoryMock.Verify(x => x.GetUserByLoginAsync(login, ct), Times.Once);
        _passwordHasherMock.Verify(x => x.VerifyHashedPassword(password, user.PasswordHash), Times.Once);
        _tokenGeneratorMock.Verify(x => x.GenerateToken(It.IsAny<User>(), It.IsAny<CancellationToken>()), Times.Never);
    }

    [Fact]
    public async Task LoginAsync_ValidCredentials_ReturnsToken()
    {
        // Arrange
        var login = "testuser";
        var password = "correctpassword";
        var ct = CancellationToken.None;
        var user = User.Create(login, "hashedpassword", RoleType.User);
        var expectedToken = "jwt_token_123";

        _userRepositoryMock
            .Setup(x => x.GetUserByLoginAsync(login, ct))
            .ReturnsAsync(user);

        _passwordHasherMock
            .Setup(x => x.VerifyHashedPassword(password, user.PasswordHash))
            .Returns(true);

        _tokenGeneratorMock
            .Setup(x => x.GenerateToken(user, ct))
            .Returns(expectedToken);

        // Act
        var result = await _authService.LoginAsync(login, password, ct);

        // Assert
        Assert.Equal(expectedToken, result);
        _userRepositoryMock.Verify(x => x.GetUserByLoginAsync(login, ct), Times.Once);
        _passwordHasherMock.Verify(x => x.VerifyHashedPassword(password, user.PasswordHash), Times.Once);
        _tokenGeneratorMock.Verify(x => x.GenerateToken(user, ct), Times.Once);
    }


}