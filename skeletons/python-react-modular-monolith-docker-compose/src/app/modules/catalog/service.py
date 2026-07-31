from uuid import UUID

from sqlalchemy import select
from sqlalchemy.ext.asyncio import AsyncSession

from app.core.exceptions import ConflictError, NotFoundError
from app.modules.catalog.models import Product
from app.modules.catalog.schemas import ProductCreate


async def list_products(session: AsyncSession) -> list[Product]:
    result = await session.execute(select(Product).order_by(Product.created_at))
    return list(result.scalars().all())


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
