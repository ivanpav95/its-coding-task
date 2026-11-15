using Claims.Application.Auditing;
using Claims.Application.Contracts;
using Claims.Application.Repositories;
using Claims.Domain;
using FluentValidation;

namespace Claims.Application.Services;

public class ClaimService(
    IClaimRepository claims,
    ICoverRepository covers,
    IAuditer auditer,
    IValidator<CreateClaimRequest> validator,
    IValidator<PageRequest> pageValidator) : IClaimService
{
    public async Task<GetPageResult<ClaimResponse>> GetPageAsync(PageRequest request, CancellationToken cancellationToken = default)
    {
        var validation = await pageValidator.ValidateAsync(request, cancellationToken);
        if (!validation.IsValid)
        {
            return new GetPageResult<ClaimResponse>.Invalid(validation.ToDictionary());
        }

        var page = await claims.GetPageAsync((request.Page - 1) * request.PageSize, request.PageSize, cancellationToken);
        var totalCount = await claims.CountAsync(cancellationToken);
        return new GetPageResult<ClaimResponse>.Found(
            new PagedResponse<ClaimResponse>(page.Select(ClaimResponse.From).ToList(), request.Page, request.PageSize, totalCount));
    }

    public async Task<ClaimResponse?> GetByIdAsync(string id, CancellationToken cancellationToken = default)
    {
        var claim = await claims.GetByIdAsync(id, cancellationToken);
        return claim is null ? null : ClaimResponse.From(claim);
    }

    public async Task<CreateClaimResult> CreateAsync(CreateClaimRequest request, CancellationToken cancellationToken = default)
    {
        var validation = await validator.ValidateAsync(request, cancellationToken);
        if (!validation.IsValid)
        {
            return new CreateClaimResult.Invalid(validation.ToDictionary());
        }

        var cover = await covers.GetByIdAsync(request.CoverId, cancellationToken);
        if (cover is null)
        {
            return Invalid(nameof(request.CoverId), $"Cover '{request.CoverId}' does not exist.");
        }

        var created = request.Created.UtcDateTime;
        if (created < cover.StartsAt || created >= cover.EndsAt)
        {
            return Invalid(nameof(request.Created),
                $"The claim must be within the cover period ({cover.StartsAt:yyyy-MM-dd HH:mm} to {cover.EndsAt:yyyy-MM-dd HH:mm} UTC).");
        }

        var claim = new Claim
        {
            Id = Guid.NewGuid().ToString(),
            CoverId = request.CoverId,
            Created = created,
            Name = request.Name,
            Type = request.Type,
            DamageCost = request.DamageCost
        };

        await claims.AddAsync(claim, cancellationToken);
        await auditer.AuditClaimAsync(claim.Id, AuditAction.Created);
        return new CreateClaimResult.Created(ClaimResponse.From(claim));
    }

    public async Task<bool> DeleteAsync(string id, CancellationToken cancellationToken = default)
    {
        if (!await claims.DeleteAsync(id, cancellationToken))
        {
            return false;
        }

        await auditer.AuditClaimAsync(id, AuditAction.Deleted);
        return true;
    }

    private static CreateClaimResult.Invalid Invalid(string field, string message) =>
        new(new Dictionary<string, string[]> { [field] = [message] });
}
