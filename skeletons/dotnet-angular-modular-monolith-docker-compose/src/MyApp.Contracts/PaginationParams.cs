using System.ComponentModel.DataAnnotations;

namespace MyApp.Contracts;

/// <summary>Shared query binding for list endpoints (adrs/api/offset-pagination.md):
/// 1-based page, pageSize capped at 100 (rejected with 400 past the cap),
/// allowlisted sorting applied by each module's service.</summary>
public sealed class PaginationParams
{
    [Range(1, int.MaxValue)]
    public int Page { get; set; } = 1;

    [Range(1, 100)]
    public int PageSize { get; set; } = 20;

    public string SortBy { get; set; } = "createdAt";

    [RegularExpression("asc|desc")]
    public string SortDir { get; set; } = "asc";
}
