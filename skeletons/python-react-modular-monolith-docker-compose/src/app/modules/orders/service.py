from uuid import UUID

from sqlalchemy.ext.asyncio import AsyncSession

from app.contracts.catalog import CatalogService, ProductSummary
from app.core.exceptions import NotFoundError
from app.modules.orders.models import Order
from app.modules.orders.schemas import OrderCreate


async def create_order(
    session: AsyncSession, catalog: CatalogService, payload: OrderCreate
) -> tuple[Order, ProductSummary]:
    product = await catalog.get_product_summary(payload.product_id)
    if product is None:
        raise NotFoundError(f"Product {payload.product_id} was not found.")
    order = Order(product_id=payload.product_id, quantity=payload.quantity)
    session.add(order)
    await session.flush()
    return order, product


async def get_order(
    session: AsyncSession, catalog: CatalogService, order_id: UUID
) -> tuple[Order, ProductSummary | None]:
    order = await session.get(Order, order_id)
    if order is None:
        raise NotFoundError(f"Order {order_id} was not found.")
    product = await catalog.get_product_summary(order.product_id)
    return order, product
