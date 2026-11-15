using Claims.Application.Contracts;
using FluentValidation;

namespace Claims.Application.Validation;

public class PageRequestValidator : AbstractValidator<PageRequest>
{
    // The highest page whose offset, (Page - 1) * PageSize, still fits in an int at the largest page size.
    public const int MaxPage = int.MaxValue / PageRequest.MaxPageSize;

    public PageRequestValidator()
    {
        RuleFor(r => r.Page).InclusiveBetween(1, MaxPage);
        RuleFor(r => r.PageSize).InclusiveBetween(1, PageRequest.MaxPageSize);
    }
}
