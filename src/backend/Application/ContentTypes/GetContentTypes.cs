using Application.Interfaces;
using Domain.Common;
using Infrastructure.Data;
using Microsoft.EntityFrameworkCore;

namespace Application.ContentTypes;

public record ContentTypeListDto(Guid Id, string Name, string Status, int Version);

public sealed record GetContentTypesQuery;

public sealed class GetContentTypesHandler(AppDbContext dbContext)
    : IQueryHandler<GetContentTypesQuery, IReadOnlyList<ContentTypeListDto>>
{
    async Task<Result<IReadOnlyList<ContentTypeListDto>>> IQueryHandler<
        GetContentTypesQuery,
        IReadOnlyList<ContentTypeListDto>
    >.Handle(GetContentTypesQuery query, CancellationToken cancellationToken)
    {
        List<ContentTypeListDto> contentTypes = await dbContext
            .ContentTypes.AsNoTracking()
            .Select(ct => new ContentTypeListDto(ct.Id, ct.Name, ct.Status.ToString(), ct.Version))
            .ToListAsync(cancellationToken);
        return Result<IReadOnlyList<ContentTypeListDto>>.Success(contentTypes);
    }
}
