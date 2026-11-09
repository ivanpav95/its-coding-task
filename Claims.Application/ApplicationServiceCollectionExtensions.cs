using Claims.Application.Contracts;
using Claims.Application.Services;
using Claims.Application.Validation;
using FluentValidation;
using Microsoft.Extensions.DependencyInjection;

namespace Claims.Application;

public static class ApplicationServiceCollectionExtensions
{
    public static IServiceCollection AddApplication(this IServiceCollection services)
    {
        services.AddScoped<ICoverService, CoverService>();
        services.AddScoped<IClaimService, ClaimService>();
        services.AddSingleton<IValidator<CreateCoverRequest>, CreateCoverRequestValidator>();
        services.AddSingleton<IValidator<CreateClaimRequest>, CreateClaimRequestValidator>();
        return services;
    }
}
