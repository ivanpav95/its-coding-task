using Claims.Application.Contracts;

namespace Claims.Application.Services;

public abstract record CreateCoverResult
{
    public sealed record Created(CoverResponse Cover) : CreateCoverResult;

    /// <param name="Errors">Messages per request field, e.g. <c>StartDate</c>.</param>
    public sealed record Invalid(IDictionary<string, string[]> Errors) : CreateCoverResult;
}
