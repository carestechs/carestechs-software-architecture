namespace MyApp.Contracts;

/// <summary>`{ data, meta }` envelope for 2xx responses (adrs/api/rest-envelope.md).</summary>
public sealed record ResponseMeta(int? TotalCount = null);

public sealed record ApiResponse<T>(T Data, ResponseMeta? Meta = null);

public sealed record ApiListResponse<T>(IReadOnlyList<T> Data, ResponseMeta Meta);
