using Application.Interfaces;
using Application.Roles;
using Domain.Common;
using Domain.Roles;
using Domain.Users;
using Microsoft.Extensions.Logging;
using Moq;

namespace Application.Tests.Roles;

public class CreateRoleTests
{
    private readonly Mock<IRoleRepository> _mockRoleRepo = new();
    private readonly Mock<IUserContext> _mockUserContext = new();
    private readonly CreateRoleCommandHandler _handler;

    public CreateRoleTests() =>
        _handler = new CreateRoleCommandHandler(
            _mockRoleRepo.Object,
            _mockUserContext.Object,
            Mock.Of<ILogger<CreateRoleCommandHandler>>()
        );

    [Fact]
    public async Task Handle_WhenAdmin_CreatesRoleAndReturnsId()
    {
        var roleId = Guid.NewGuid();
        _mockUserContext.Setup(c => c.IsInRoleAsync("Admin")).ReturnsAsync(true);
        _mockRoleRepo
            .Setup(r => r.CreateAsync("Editor", It.IsAny<CancellationToken>()))
            .ReturnsAsync(Result<Guid>.Success(roleId));

        Result<Guid> result = await _handler.Handle(
            new CreateRoleCommand("Editor"),
            CancellationToken.None
        );

        Assert.True(result.IsSuccess);
        Assert.Equal(roleId, result.Value);
        _mockRoleRepo.Verify(
            r => r.CreateAsync("Editor", It.IsAny<CancellationToken>()),
            Times.Once
        );
    }

    [Fact]
    public async Task Handle_WhenNotAdmin_ReturnsForbidden()
    {
        _mockUserContext.Setup(c => c.IsInRoleAsync("Admin")).ReturnsAsync(false);

        Result<Guid> result = await _handler.Handle(
            new CreateRoleCommand("Editor"),
            CancellationToken.None
        );

        Assert.False(result.IsSuccess);
        Assert.Equal(ErrorTypes.Forbidden, result.Error?.Type);
        _mockRoleRepo.Verify(
            r => r.CreateAsync(It.IsAny<string>(), It.IsAny<CancellationToken>()),
            Times.Never
        );
    }

    [Theory]
    [InlineData("")]
    [InlineData("   ")]
    public async Task Handle_WhenNameIsEmpty_ReturnsValidationError(string name)
    {
        _mockUserContext.Setup(c => c.IsInRoleAsync("Admin")).ReturnsAsync(true);

        Result<Guid> result = await _handler.Handle(
            new CreateRoleCommand(name),
            CancellationToken.None
        );

        Assert.False(result.IsSuccess);
        Assert.Equal(ErrorTypes.Validation, result.Error?.Type);
        _mockRoleRepo.Verify(
            r => r.CreateAsync(It.IsAny<string>(), It.IsAny<CancellationToken>()),
            Times.Never
        );
    }
}
