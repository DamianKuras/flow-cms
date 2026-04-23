using Application.Interfaces;
using Domain.Common;
using Domain.ContentTypes;
using Domain.Permissions;

namespace Application.ContentTypes;

/// <summary>One entry per content type name with its current published and draft state.</summary>
public record ContentTypeSummaryDto(
    string Name,
    Guid? PublishedId,
    int? PublishedVersion,
    Guid? DraftId
);

/// <summary>Returns all content type summaries grouped by name.</summary>
public sealed record GetContentTypeSummariesQuery;

/// <summary>Handler for <see cref="GetContentTypeSummariesQuery"/>.</summary>
public sealed class GetContentTypeSummariesHandler(
    IContentTypeRepository contentTypeRepository,
    IAuthorizationService authorizationService
) : IQueryHandler<GetContentTypeSummariesQuery, IReadOnlyList<ContentTypeSummaryDto>>
{
    public async Task<Result<IReadOnlyList<ContentTypeSummaryDto>>> Handle(
        GetContentTypeSummariesQuery query,
        CancellationToken cancellationToken
    )
    {
        bool allowed = await authorizationService.IsAllowedForTypeAsync(
            CmsAction.List,
            ResourceType.ContentType,
            cancellationToken
        );
        if (!allowed)
            return Result<IReadOnlyList<ContentTypeSummaryDto>>.Failure(Error.Forbidden("Forbidden"));

        IReadOnlyList<ContentTypeNameSummary> summaries =
            await contentTypeRepository.GetNameSummariesAsync(cancellationToken);

        List<ContentTypeSummaryDto> dtos = summaries
            .Select(s => new ContentTypeSummaryDto(s.Name, s.PublishedId, s.PublishedVersion, s.DraftId))
            .ToList();

        return Result<IReadOnlyList<ContentTypeSummaryDto>>.Success(dtos);
    }
}
