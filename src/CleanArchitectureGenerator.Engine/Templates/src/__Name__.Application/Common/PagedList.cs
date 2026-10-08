using Microsoft.EntityFrameworkCore;

namespace __Name__.Application.Common;

/// <summary>Sayfalı liste yanıtı.</summary>
public sealed record PagedList<T>(IReadOnlyList<T> Items, int Page, int PageSize, int TotalCount)
{
    public int TotalPages => (int)Math.Ceiling(TotalCount / (double)PageSize);

    public bool HasPreviousPage => Page > 1;

    public bool HasNextPage => Page < TotalPages;
}

/// <summary>
/// Sayfalama sınırları. Tek istekte dönebilecek kayıt sayısı ve sayfa derinliği sınırlıdır; böylece
/// tek bir istekle veritabanı ya da bellek tüketilemez (kaynak tüketimi / DoS koruması).
/// </summary>
public static class Pagination
{
    public const int DefaultPageSize = 20;
    public const int MaxPageSize = 100;
    public const int MaxPage = 10_000;

    public static async Task<PagedList<T>> ToPagedListAsync<T>(this IQueryable<T> query, int page, int pageSize, CancellationToken cancellationToken)
    {
        var totalCount = await query.CountAsync(cancellationToken);
        var items = await query.Skip((page - 1) * pageSize).Take(pageSize).ToListAsync(cancellationToken);
        return new PagedList<T>(items, page, pageSize, totalCount);
    }
}
