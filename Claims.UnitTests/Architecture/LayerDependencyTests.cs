using System.Reflection;
using Claims.Api.Controllers;
using Claims.Application.Services;
using Claims.Domain;
using Claims.Infrastructure.Auditing;
using NetArchTest.Rules;
using Xunit;

namespace Claims.UnitTests.Architecture;

public class LayerDependencyTests
{
    private static readonly Assembly Domain = typeof(Cover).Assembly;
    private static readonly Assembly Application = typeof(ICoverService).Assembly;
    private static readonly Assembly Infrastructure = typeof(ChannelAuditer).Assembly;
    private static readonly Assembly Api = typeof(CoversController).Assembly;

    [Fact]
    public void Domain_depends_on_nothing() =>
        AssertSuccessful(Types.InAssembly(Domain)
            .Should().NotHaveDependencyOnAny(
                "Claims.Application", "Claims.Infrastructure", "Claims.Api",
                "Microsoft.EntityFrameworkCore", "MongoDB", "FluentValidation", "Microsoft.AspNetCore")
            .GetResult());

    [Fact]
    public void Application_depends_only_on_Domain() =>
        AssertSuccessful(Types.InAssembly(Application)
            .Should().NotHaveDependencyOnAny(
                "Claims.Infrastructure", "Claims.Api",
                "Microsoft.EntityFrameworkCore", "MongoDB", "Microsoft.AspNetCore")
            .GetResult());

    [Fact]
    public void Infrastructure_does_not_depend_on_the_API() =>
        AssertSuccessful(Types.InAssembly(Infrastructure)
            .Should().NotHaveDependencyOnAny("Claims.Api", "Microsoft.AspNetCore")
            .GetResult());

    // Claims.Api references Infrastructure to compose it, so only this test keeps controllers on the services.
    [Fact]
    public void Controllers_use_services_not_Infrastructure() =>
        AssertSuccessful(Types.InAssembly(Api)
            .That().ResideInNamespace("Claims.Api.Controllers")
            .Should().NotHaveDependencyOnAny("Claims.Infrastructure", "Microsoft.EntityFrameworkCore", "MongoDB")
            .GetResult());

    private static void AssertSuccessful(NetArchTest.Rules.TestResult result) =>
        Assert.True(result.IsSuccessful,
            "Not allowed: " + string.Join(", ", result.FailingTypes?.Select(type => type.FullName) ?? []));
}
