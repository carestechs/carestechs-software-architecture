from uuid import UUID

from sqlalchemy import func, select
from sqlalchemy.ext.asyncio import AsyncSession

from app.core.exceptions import BadRequestError, ConflictError, NotFoundError
from app.core.pagination import PaginationParams
from app.modules.catalog.models import Product
from app.modules.catalog.schemas import ProductCreate

# Sortable columns are an allowlist — raw client input never reaches ORDER BY
# (adrs/api/offset-pagination.md)
SORTABLE = {"createdAt": Product.created_at, "name": Product.name, "sku": Product.sku}


async def list_products(
    session: AsyncSession, params: PaginationParams
) -> tuple[list[Product], int]:
    column = SORTABLE.get(params.sort_by)
    if column is None:
        raise BadRequestError(
            f"Unknown sortBy '{params.sort_by}'. Sortable: {', '.join(sorted(SORTABLE))}."
        )
    total = await session.scalar(select(func.count()).select_from(Product)) or 0
    result = await session.execute(
        select(Product)
        .order_by(column.desc() if params.sort_dir == "desc" else column.asc())
        .offset(params.offset)
        .limit(params.page_size)
    )
    return list(result.scalars().all()), total


async def create_product(session: AsyncSession, payload: ProductCreate) -> Product:
    existing = await session.execute(select(Product.id).where(Product.sku == payload.sku))
    if existing.scalar_one_or_none() is not None:
        raise ConflictError(f"A product with SKU '{payload.sku}' already exists.")
    product = Product(sku=payload.sku, name=payload.name)
    session.add(product)
    await session.flush()
    return product


async def get_product(session: AsyncSession, product_id: UUID) -> Product:
    product = await session.get(Product, product_id)
    if product is None:
        raise NotFoundError(f"Product {product_id} was not found.")
    return product
