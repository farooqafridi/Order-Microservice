using System.Net.Http.Headers;
using System.Net.Http.Json;
using OrderService.DTOs;

namespace OrderService.Services;

public sealed class CartServiceClient : ICartServiceClient
{
    private readonly HttpClient _httpClient;

    public CartServiceClient(HttpClient httpClient)
    {
        _httpClient = httpClient;
    }

    public async Task<CartResponse?> GetCartAsync(
        string accessToken,
        CancellationToken cancellationToken)
    {
        using var request = new HttpRequestMessage(
            HttpMethod.Get, "api/cart");

        request.Headers.Authorization =
            new AuthenticationHeaderValue("Bearer", accessToken);

        using var response = await _httpClient.SendAsync(
            request, cancellationToken);

        response.EnsureSuccessStatusCode();

        return await response.Content.ReadFromJsonAsync<CartResponse>(
            cancellationToken: cancellationToken);
    }
}