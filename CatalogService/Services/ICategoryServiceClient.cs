namespace CatalogService.Services
{
    public interface ICategoryServiceClient
    {
        Task<bool> IsActiveAsync(
            Guid categoryId,
            CancellationToken cancellationToken = default);
    }
}
