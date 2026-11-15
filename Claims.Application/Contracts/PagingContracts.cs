namespace Claims.Application.Contracts;

/// <param name="Page">The page number, starting at 1.</param>
/// <param name="PageSize">Items per page.</param>
public record PageRequest(int Page, int PageSize)
{
    public const int DefaultPageSize = 10;

    public const int MaxPageSize = 100;
}

/// <param name="Items">The items on this page.</param>
/// <param name="Page">The page number, starting at 1.</param>
/// <param name="PageSize">The requested items per page; the last page can hold fewer.</param>
/// <param name="TotalCount">All items, on every page.</param>
public record PagedResponse<T>(IReadOnlyList<T> Items, int Page, int PageSize, int TotalCount);
