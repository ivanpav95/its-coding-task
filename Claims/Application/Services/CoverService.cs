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
    IValidator<CreateCoverRequest> validator) : ICoverService
{
    public async Task<IReadOnlyList<CoverResponse>> GetAllAsync(CancellationToken cancellationToken = default)
    {
        var all = await covers.GetAllAsync(cancellationToken);
        return all.Select(CoverResponse.From).ToList();
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
            Type = request.Type,
            Premium = PremiumCalculator.Calculate(request.StartDate, request.EndDate, request.Type)
        };

        await covers.AddAsync(cover, cancellationToken);
        auditer.AuditCover(cover.Id, "POST");
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

        auditer.AuditCover(id, "DELETE");
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
