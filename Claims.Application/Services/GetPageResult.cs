using Claims.Application.Contracts;

namespace Claims.Application.Services;

public abstract record GetPageResult<T>
{
    public sealed record Found(PagedResponse<T> Page) : GetPageResult<T>;

    /// <param name="Errors">Messages per request field, e.g. <c>PageSize</c>.</param>
    public sealed record Invalid(IDictionary<string, string[]> Errors) : GetPageResult<T>;
}
