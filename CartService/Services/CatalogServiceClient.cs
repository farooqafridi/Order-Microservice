using System.Net;
using System.Net.Http.Json;
using CartService.Models;

namespace CartService.Services;

public sealed class CatalogServiceClient : ICatalogServiceClient
{
    private readonly HttpClient _httpClient;

    public CatalogServiceClient(HttpClient httpClient)
    {
        _httpClient = httpClient;
    }

    public async Task<ProductSnapshot?> GetProductAsync(
        Guid productId,
        CancellationToken cancellationToken = default)
    {
        using var response = await _httpClient.GetAsync(
            $"api/products/{productId}",
            cancellationToken);

        if (response.StatusCode == HttpStatusCode.NotFound)
            return null;

        response.EnsureSuccessStatusCode();

        var product = await response.Content
            .ReadFromJsonAsync<ProductSnapshot>(
                cancellationToken: cancellationToken);

        return product is { IsActive: true } ? product : null;
    }
}