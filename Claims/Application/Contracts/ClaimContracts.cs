using Claims.Domain;

namespace Claims.Application.Contracts;

public record CreateClaimRequest(string CoverId, DateOnly Created, string Name, ClaimType Type, decimal DamageCost);

public record ClaimResponse(string Id, string CoverId, DateOnly Created, string Name, ClaimType Type, decimal DamageCost)
{
    public static ClaimResponse From(Claim claim) =>
        new(claim.Id, claim.CoverId, claim.Created, claim.Name, claim.Type, claim.DamageCost);
}
