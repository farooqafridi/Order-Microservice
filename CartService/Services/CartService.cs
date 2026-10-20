using CartService.Data;
using CartService.Dto;
using CartService.EfCoreUtils;
using Microsoft.EntityFrameworkCore;

namespace CartService.Services
{
    public class CartService
    {
        CartDbContext _dbContext;
        public CartService(CartDbContext dbContext)
        {
            _dbContext = dbContext;
        }
        public async Task<PagedResponse<CartDto>> GetAllCartsAsync(CartQueryFilter filter, CancellationToken cancellationToken = default)
        {
            var pageNumber = Math.Max(1, filter.PageNumber);
            var pageSize = Math.Clamp(filter.PageSize, 1, 50);

            var query = _dbContext.Carts.AsNoTracking().AsQueryable();

            // 1. Apply search filter (reduces the dataset)
            query = query.ApplySearch(filter.Search);

            // 2. Count total records AFTER filtering, BEFORE pagination
            var totalRecords = await query.CountAsync(cancellationToken);

            // 3. Apply sorting (default to Title if not specified)
            query = query.ApplySort(
                string.IsNullOrWhiteSpace(filter.SortBy) ? "Title" : filter.SortBy);

            // 4. Apply pagination and project to DTOs
            var movies = await query
                .ApplyPagination(pageNumber, pageSize)
                .Select(m => new CartDto {
                    Id = m.Id,
                    CustomerId = m.CustomerId
                })
                .ToListAsync(cancellationToken);

            return new PagedResponse<CartDto>
            {
                Data = movies,
                PageNumber = pageNumber,
                PageSize = pageSize,
                TotalRecords = totalRecords,
                TotalPages = (int)Math.Ceiling(totalRecords / (double)pageSize)
            };
        }
    }
}
