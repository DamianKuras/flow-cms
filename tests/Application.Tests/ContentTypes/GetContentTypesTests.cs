using Application.ContentTypes;
using Application.Interfaces;
using Domain.Common;
using Domain.ContentTypes;
using Domain.Permissions;
using Microsoft.Extensions.Logging;
using Moq;
using Xunit;

namespace Application.Tests.ContentTypes;

public class GetContentTypesHandlerTests
{
    private readonly Mock<IContentTypeRepository> _mockRepository;
    private readonly Mock<IAuthorizationService> _mockAuth;
    private readonly Mock<ILogger<GetContentTypesHandler>> _mockLogger;
    private readonly GetContentTypesHandler _handler;

    public GetContentTypesHandlerTests()
    {
        _mockRepository = new Mock<IContentTypeRepository>();
        _mockAuth = new Mock<IAuthorizationService>();
        _mockLogger = new Mock<ILogger<GetContentTypesHandler>>();
        _handler = new GetContentTypesHandler(
            _mockRepository.Object,
            _mockAuth.Object,
            _mockLogger.Object
        );
    }

    [Fact]
    public async Task Handle_WhenAuthorized_ReturnsPagedContent()
    {
        // Arrange
        var query = new GetContentTypesQuery(
            new PaginationParameters(1, 10),
            "name.asc",
            "published",
            "test"
        );
        var items = new List<PagedContentType>
        {
            new PagedContentType(Guid.NewGuid(), "Test1", "published", 1, DateTime.UtcNow),
            new PagedContentType(Guid.NewGuid(), "Test2", "published", 1, DateTime.UtcNow),
        };
        var pagedList = new PagedList<PagedContentType>(items, 1, 10, 2);

        _mockAuth
            .Setup(a =>
                a.IsAllowedForTypeAsync(
                    CmsAction.List,
                    ResourceType.ContentType,
                    It.IsAny<CancellationToken>()
                )
            )
            .ReturnsAsync(true);

        _mockRepository
            .Setup(r =>
                r.Get(
                    query.PaginationParameters,
                    query.Sort,
                    query.Status,
                    query.NameFilter,
                    It.IsAny<CancellationToken>()
                )
            )
            .ReturnsAsync(pagedList);

        // Act
        Result<GetContentTypeResponse> result = await _handler.Handle(
            query,
            CancellationToken.None
        );

        // Assert
        Assert.True(result.IsSuccess);
        Assert.NotNull(result.Value);
        Assert.Equal(2, result.Value.Data.Items.Count);
        Assert.Equal(2, result.Value.Data.TotalCount);
        _mockRepository.Verify(
            r =>
                r.Get(
                    It.IsAny<PaginationParameters>(),
                    It.IsAny<string>(),
                    It.IsAny<string>(),
                    It.IsAny<string>(),
                    It.IsAny<CancellationToken>()
                ),
            Times.Once
        );
    }

    [Fact]
    public async Task Handle_WhenUnauthorized_ReturnsForbidden()
    {
        // Arrange
        var query = new GetContentTypesQuery(
            new PaginationParameters(1, 10),
            "name.asc",
            "published",
            "test"
        );

        _mockAuth
            .Setup(a =>
                a.IsAllowedForTypeAsync(
                    CmsAction.List,
                    ResourceType.ContentType,
                    It.IsAny<CancellationToken>()
                )
            )
            .ReturnsAsync(false);

        // Act
        Result<GetContentTypeResponse> result = await _handler.Handle(
            query,
            CancellationToken.None
        );

        // Assert
        Assert.False(result.IsSuccess);
        Assert.Equal(ErrorTypes.Forbidden, result.Error?.Type);
        _mockRepository.Verify(
            r =>
                r.Get(
                    It.IsAny<PaginationParameters>(),
                    It.IsAny<string>(),
                    It.IsAny<string>(),
                    It.IsAny<string>(),
                    It.IsAny<CancellationToken>()
                ),
            Times.Never
        );
    }

    [Fact]
    public async Task Handle_WhenRepositoryReturnsEmptyArray_ReturnsEmptyPagedList()
    {
        // Arrange
        var query = new GetContentTypesQuery(new PaginationParameters(1, 10), "", "", "");
        var items = new List<PagedContentType>();
        var pagedList = new PagedList<PagedContentType>(items, 1, 10, 0);

        _mockAuth
            .Setup(a =>
                a.IsAllowedForTypeAsync(
                    CmsAction.List,
                    ResourceType.ContentType,
                    It.IsAny<CancellationToken>()
                )
            )
            .ReturnsAsync(true);

        _mockRepository
            .Setup(r =>
                r.Get(
                    query.PaginationParameters,
                    query.Sort,
                    query.Status,
                    query.NameFilter,
                    It.IsAny<CancellationToken>()
                )
            )
            .ReturnsAsync(pagedList);

        // Act
        Result<GetContentTypeResponse> result = await _handler.Handle(
            query,
            CancellationToken.None
        );

        // Assert
        Assert.True(result.IsSuccess);
        Assert.NotNull(result.Value);
        Assert.Empty(result.Value.Data.Items);
        Assert.Equal(0, result.Value.Data.TotalCount);
        _mockRepository.Verify(
            r =>
                r.Get(
                    It.IsAny<PaginationParameters>(),
                    It.IsAny<string>(),
                    It.IsAny<string>(),
                    It.IsAny<string>(),
                    It.IsAny<CancellationToken>()
                ),
            Times.Once
        );
    }
}
