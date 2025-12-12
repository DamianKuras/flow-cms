using Application.Interfaces;
using Domain.Common;
using Domain.ContentTypes;

namespace Application.ContentTypes;

public sealed record DeleteContentTypeCommand(Guid Id);

public sealed class DeleteContentTypeHandler(IContentTypeRepository contentTypeRepository)
    : ICommandHandler<DeleteContentTypeCommand, Guid>
{
    public async Task<Result<Guid>> Handle(
        DeleteContentTypeCommand command,
        CancellationToken cancellationToken
    )
    {
        ContentType? contentType = await contentTypeRepository.GetByIdAsync(
            command.Id,
            cancellationToken
        );

        if (contentType is null)
        {
            return Result<Guid>.Failure(
                Error.NotFound($"Content type with id {command.Id} not found")
            );
        }

        contentTypeRepository.Delete(contentType);
        await contentTypeRepository.SaveChangesAsync(cancellationToken);

        return Result<Guid>.Success(command.Id);
    }
}
