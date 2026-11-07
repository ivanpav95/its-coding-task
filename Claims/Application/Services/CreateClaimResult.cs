using Claims.Application.Contracts;

namespace Claims.Application.Services;

public abstract record CreateClaimResult
{
    public sealed record Created(ClaimResponse Claim) : CreateClaimResult;

    public sealed record CoverNotFound(string CoverId) : CreateClaimResult;
}
