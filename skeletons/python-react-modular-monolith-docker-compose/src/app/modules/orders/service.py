from uuid import UUID

from sqlalchemy.ext.asyncio import AsyncSession

from app.contracts.catalog import CatalogService, ProductSummary
from app.core.auth import CurrentUser
from app.core.exceptions import NotFoundError
from app.modules.orders.models import Order
from app.modules.orders.schemas import OrderCreate


async def create_order(
    session: AsyncSession, catalog: CatalogService, payload: OrderCreate, created_by: UUID
) -> tuple[Order, ProductSummary]:
    product = await catalog.get_product_summary(payload.product_id)
    if product is None:
        raise NotFoundError(f"Product {payload.product_id} was not found.")
    order = Order(product_id=payload.product_id, quantity=payload.quantity, created_by=created_by)
    session.add(order)
    await session.flush()
    return order, product


async def build_receipt(order_id: UUID) -> dict:
    """Runs outside any HTTP request (Celery worker): the session comes from the
    shared factory, never from FastAPI Depends
    (adrs/python/celery-background-jobs.md)."""
    from app.core.database import async_session_factory

    async with async_session_factory() as session:
        order = await session.get(Order, order_id)
        if order is None:
            return {"status": "not_found", "orderId": str(order_id)}
        return {
            "status": "ready",
            "orderId": str(order.id),
            "productId": str(order.product_id),
            "quantity": order.quantity,
            "createdAt": order.created_at.isoformat(),
        }


async def get_order(
    session: AsyncSession, catalog: CatalogService, order_id: UUID, caller: CurrentUser
) -> tuple[Order, ProductSummary | None]:
    order = await session.get(Order, order_id)
    # Ownership is enforced here, next to the data — a 404 for both "missing" and
    # "not yours" so order IDs leak nothing (adrs/api/role-based-authorization.md)
    if order is None or (order.created_by != caller.id and caller.role != "admin"):
        raise NotFoundError(f"Order {order_id} was not found.")
    product = await catalog.get_product_summary(order.product_id)
    return order, product
