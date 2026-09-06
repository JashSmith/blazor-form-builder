using BlazorFormBuilder.Core.Models;

namespace BlazorFormBuilder.Core.Storage;

public interface IPublishedFormStore
{
    ValueTask<IReadOnlyList<PublishedFormVersion>> ListAsync(CancellationToken cancellationToken = default);

    ValueTask<PublishedFormVersion> PublishAsync(Guid formId, CancellationToken cancellationToken = default);
}
