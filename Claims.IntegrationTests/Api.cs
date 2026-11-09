using System.Net;
using System.Net.Http.Json;
using System.Text.Json;
using System.Text.Json.Serialization;
using Claims.Application.Contracts;
using Claims.Domain;
using Xunit;

namespace Claims.IntegrationTests;

/// <summary>Requests the tests repeat, with the same JSON settings as the app (string enums).</summary>
public static class Api
{
    public static readonly JsonSerializerOptions Json = new(JsonSerializerDefaults.Web)
    {
        Converters = { new JsonStringEnumConverter() }
    };

    private static CancellationToken CancellationToken => TestContext.Current.CancellationToken;

    public static Task<HttpResponseMessage> PostCoverAsync(this HttpClient client, CreateCoverRequest request) =>
        client.PostAsJsonAsync("/Covers", request, Json, CancellationToken);

    public static Task<HttpResponseMessage> PostClaimAsync(this HttpClient client, CreateClaimRequest request) =>
        client.PostAsJsonAsync("/Claims", request, Json, CancellationToken);

    public static async Task<CoverResponse> CreateCoverAsync(this HttpClient client)
    {
        var response = await client.PostCoverAsync(
            new CreateCoverRequest(ClaimsApiFactory.Today, ClaimsApiFactory.Today.AddDays(9), CoverType.Yacht));
        Assert.Equal(HttpStatusCode.Created, response.StatusCode);
        return (await response.Content.ReadFromJsonAsync<CoverResponse>(Json, CancellationToken))!;
    }

    public static async Task<ClaimResponse> CreateClaimAsync(this HttpClient client, string coverId)
    {
        var response = await client.PostClaimAsync(
            new CreateClaimRequest(coverId, ClaimsApiFactory.Now, "Hull damage", ClaimType.Collision, 5_000m));
        Assert.Equal(HttpStatusCode.Created, response.StatusCode);
        return (await response.Content.ReadFromJsonAsync<ClaimResponse>(Json, CancellationToken))!;
    }

    public static async Task<T> ReadAsync<T>(this HttpResponseMessage response) =>
        (await response.Content.ReadFromJsonAsync<T>(Json, CancellationToken))!;
}
