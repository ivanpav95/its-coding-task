using Claims.Domain;

namespace Claims.Application.Contracts;

public record CreateClaimRequest(string CoverId, DateTimeOffset Created, string Name, ClaimType Type, decimal DamageCost);

public record ClaimResponse(string Id, string CoverId, DateTimeOffset Created, string Name, ClaimType Type, decimal DamageCost)
{
    public static ClaimResponse From(Claim claim) =>
        new(claim.Id, claim.CoverId, new DateTimeOffset(claim.Created, TimeSpan.Zero), claim.Name, claim.Type, claim.DamageCost);
}
