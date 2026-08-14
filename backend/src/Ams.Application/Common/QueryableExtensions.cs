using Microsoft.EntityFrameworkCore;

namespace Ams.Application.Common;

public static class QueryableExtensions
{
    /// <summary>Runs the count and page queries and wraps the result.</summary>
    public static async Task<PagedResult<T>> ToPagedResultAsync<T>(
        this IQueryable<T> query, int page, int pageSize, CancellationToken ct = default)
    {
        var totalCount = await query.CountAsync(ct);
        var items = await query
            .Skip((page - 1) * pageSize)
            .Take(pageSize)
            .ToListAsync(ct);

        return PagedResult<T>.Create(items, page, pageSize, totalCount);
    }
}
