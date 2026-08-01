using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using MyApp.Contracts;
using MyApp.Modules.Catalog.DTOs;
using ICatalogService = MyApp.Modules.Catalog.Services.ICatalogService;

namespace MyApp.Modules.Catalog.Controllers;

/// <summary>Thin controller: validate, delegate, wrap (adrs/dotnet/service-layer-logic.md,
/// adrs/api/rest-envelope.md). Every endpoint declares its access level explicitly
/// (adrs/api/role-based-authorization.md).</summary>
[ApiController]
[Route("api/products")]
public class CatalogController(ICatalogService catalogService) : ControllerBase
{
    [AllowAnonymous] // public catalog reads, deliberate
    [HttpGet]
    public async Task<ActionResult<ApiListResponse<ProductDto>>> ListProducts(
        [FromQuery] PaginationParams pagination, CancellationToken cancellationToken)
    {
        var (products, total) = await catalogService.ListProductsAsync(pagination, cancellationToken);
        return Ok(new ApiListResponse<ProductDto>(
            products, new ResponseMeta(total, pagination.Page, pagination.PageSize)));
    }

    [Authorize(Roles = "admin")] // endpoint-layer role gate
    [HttpPost]
    public async Task<ActionResult<ApiResponse<ProductDto>>> CreateProduct(
        CreateProductRequest request, CancellationToken cancellationToken)
    {
        var product = await catalogService.CreateProductAsync(request, cancellationToken);
        return CreatedAtAction(
            nameof(GetProduct), new { productId = product.Id }, new ApiResponse<ProductDto>(product));
    }

    [AllowAnonymous] // public catalog reads, deliberate
    [HttpGet("{productId:guid}")]
    public async Task<ActionResult<ApiResponse<ProductDto>>> GetProduct(
        Guid productId, CancellationToken cancellationToken)
    {
        var product = await catalogService.GetProductAsync(productId, cancellationToken);
        return Ok(new ApiResponse<ProductDto>(product));
    }
}
