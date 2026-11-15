using Claims.Application.Contracts;
using Claims.Application.Validation;
using FluentValidation.TestHelper;
using Xunit;

namespace Claims.UnitTests;

public class PageRequestValidatorTests
{
    private static TestValidationResult<PageRequest> Validate(int page, int pageSize) =>
        new PageRequestValidator().TestValidate(new PageRequest(page, pageSize));

    [Theory]
    [InlineData(1, 1)]
    [InlineData(1, PageRequest.DefaultPageSize)]
    [InlineData(1, PageRequest.MaxPageSize)]
    [InlineData(PageRequestValidator.MaxPage, PageRequest.MaxPageSize)]
    public void A_page_in_range_is_valid(int page, int pageSize) =>
        Validate(page, pageSize).ShouldNotHaveAnyValidationErrors();

    [Theory]
    [InlineData(0)]
    [InlineData(-1)]
    [InlineData(PageRequestValidator.MaxPage + 1)]
    public void A_page_out_of_range_is_rejected(int page) =>
        Validate(page, PageRequest.DefaultPageSize).ShouldHaveValidationErrorFor(r => r.Page);

    [Theory]
    [InlineData(0)]
    [InlineData(PageRequest.MaxPageSize + 1)]
    public void A_page_size_out_of_range_is_rejected(int pageSize) =>
        Validate(1, pageSize).ShouldHaveValidationErrorFor(r => r.PageSize);
}
