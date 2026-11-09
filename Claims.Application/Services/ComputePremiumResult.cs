namespace Claims.Application.Services;

public abstract record ComputePremiumResult
{
    public sealed record Computed(decimal Premium) : ComputePremiumResult;

    /// <param name="Errors">Messages per request field, e.g. <c>EndDate</c>.</param>
    public sealed record Invalid(IDictionary<string, string[]> Errors) : ComputePremiumResult;
}
