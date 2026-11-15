using Claims.Application.Auditing;
using Claims.Application.Contracts;
using Claims.Application.Repositories;
using Claims.Domain;
using FluentValidation;

namespace Claims.Application.Services;

public class CoverService(
    ICoverRepository covers,
    IClaimRepository claims,
    IAuditer auditer,
    IValidator<CreateCoverRequest> validator,
    IValidator<PageRequest> pageValidator,
    TimeProvider timeProvider) : ICoverService
{
    public async Task<GetPageResult<CoverResponse>> GetPageAsync(PageRequest request, CancellationToken cancellationToken = default)
    {
        var validation = await pageValidator.ValidateAsync(request, cancellationToken);
        if (!validation.IsValid)
        {
            return new GetPageResult<CoverResponse>.Invalid(validation.ToDictionary());
        }

        var page = await covers.GetPageAsync((request.Page - 1) * request.PageSize, request.PageSize, cancellationToken);
        var totalCount = await covers.CountAsync(cancellationToken);
        return new GetPageResult<CoverResponse>.Found(
            new PagedResponse<CoverResponse>(page.Select(CoverResponse.From).ToList(), request.Page, request.PageSize, totalCount));
    }

    public async Task<CoverResponse?> GetByIdAsync(string id, CancellationToken cancellationToken = default)
    {
        var cover = await covers.GetByIdAsync(id, cancellationToken);
        return cover is null ? null : CoverResponse.From(cover);
    }

    public async Task<CreateCoverResult> CreateAsync(CreateCoverRequest request, CancellationToken cancellationToken = default)
    {
        var validation = await validator.ValidateAsync(request, cancellationToken);
        if (!validation.IsValid)
        {
            return new CreateCoverResult.Invalid(validation.ToDictionary());
        }

        var cover = new Cover
        {
            Id = Guid.NewGuid().ToString(),
            StartDate = request.StartDate,
            EndDate = request.EndDate,
            StartsAt = CoverPeriod.StartsAt(request.StartDate, timeProvider.GetUtcNow(), timeProvider.LocalTimeZone).UtcDateTime,
            EndsAt = CoverPeriod.EndsAt(request.EndDate, timeProvider.LocalTimeZone).UtcDateTime,
            Type = request.Type,
            Premium = PremiumCalculator.Calculate(request.StartDate, request.EndDate, request.Type)
        };

        await covers.AddAsync(cover, cancellationToken);
        await auditer.AuditCoverAsync(cover.Id, AuditAction.Created);
        return new CreateCoverResult.Created(CoverResponse.From(cover));
    }

    public async Task<DeleteCoverResult> DeleteAsync(string id, CancellationToken cancellationToken = default)
    {
        if (await claims.AnyForCoverAsync(id, cancellationToken))
        {
            return DeleteCoverResult.HasClaims;
        }

        if (!await covers.DeleteAsync(id, cancellationToken))
        {
            return DeleteCoverResult.NotFound;
        }

        await auditer.AuditCoverAsync(id, AuditAction.Deleted);
        return DeleteCoverResult.Deleted;
    }

    public async Task<ComputePremiumResult> ComputePremiumAsync(CreateCoverRequest request, CancellationToken cancellationToken = default)
    {
        var validation = await validator.ValidateAsync(request, cancellationToken);
        if (!validation.IsValid)
        {
            return new ComputePremiumResult.Invalid(validation.ToDictionary());
        }

        return new ComputePremiumResult.Computed(
            PremiumCalculator.Calculate(request.StartDate, request.EndDate, request.Type));
    }
}
