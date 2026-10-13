namespace CatalogService.Services;

public sealed class CategoryServiceClient : ICategoryServiceClient
{
    private readonly HttpClient _httpClient;

    public CategoryServiceClient(HttpClient httpClient)
    {
        _httpClient = httpClient;
    }

    public async Task<bool> IsActiveAsync(
        Guid categoryId,
        CancellationToken cancellationToken = default)
    {
        using var response = await _httpClient.GetAsync(
            $"api/categories/{categoryId}",
            cancellationToken);

        if (response.StatusCode == System.Net.HttpStatusCode.NotFound)
            return false;

        response.EnsureSuccessStatusCode();

        return true;
    }
}