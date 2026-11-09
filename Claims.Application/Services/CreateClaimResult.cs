using Claims.Application.Contracts;

namespace Claims.Application.Services;

public abstract record CreateClaimResult
{
    public sealed record Created(ClaimResponse Claim) : CreateClaimResult;

    /// <param name="Errors">Messages per request field, e.g. <c>CoverId</c> or <c>DamageCost</c>.</param>
    public sealed record Invalid(IDictionary<string, string[]> Errors) : CreateClaimResult;
}
