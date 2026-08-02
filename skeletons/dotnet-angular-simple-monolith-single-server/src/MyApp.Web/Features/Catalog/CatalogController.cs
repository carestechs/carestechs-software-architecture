using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using MyApp.Web.Infrastructure;

namespace MyApp.Web.Features.Catalog;

/// <summary>Thin controller (adrs/dotnet/service-layer-logic.md); every endpoint
/// declares its access level explicitly (adrs/api/role-based-authorization.md).</summary>
[ApiController]
[Route("api/products")]
public class CatalogController(ICatalogService catalogService) : ControllerBase
{
    [AllowAnonymous] // public catalog reads, deliberate
    [HttpGet]
    public async Task<ActionResult<ApiListResponse<ProductDto>>> ListProducts(CancellationToken cancellationToken)
    {
        var products = await catalogService.ListProductsAsync(cancellationToken);
        return Ok(new ApiListResponse<ProductDto>(products, new ResponseMeta(products.Count)));
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
