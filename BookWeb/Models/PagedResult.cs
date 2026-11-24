using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace BookWeb.Models
{
    public interface IPagedResult
    {
        int Page { get; }
        int TotalPages { get; }
        int TotalItems { get; }
    }

    public class PagedResult<T>: IPagedResult
    {
        public List<T> Items { get; set; } = new();
        public int Page { get; set; }
        public int PageSize { get; set; }
        public int TotalItems { get; set; }
        public int TotalPages => (int)Math.Ceiling(TotalItems / (double)PageSize);

        public static async Task<PagedResult<T>> CreateAsync(IQueryable<T> source, int page, int pageSize)
        {
            var totalItems = await source.CountAsync();
            var totalPages = Math.Max(1, (int)Math.Ceiling(totalItems / (double)pageSize));

            page = Math.Clamp(page, 1, totalPages);
            var items = await source
                .Skip((page - 1) * pageSize)
                .Take(pageSize)
                .ToListAsync();

            return new PagedResult<T>
            {
                Items = items,
                Page = page,
                PageSize = pageSize,
                TotalItems = totalItems
            };
        }
    }
}
