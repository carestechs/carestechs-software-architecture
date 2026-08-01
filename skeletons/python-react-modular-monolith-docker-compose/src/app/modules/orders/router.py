from collections.abc import Callable
from uuid import UUID

from fastapi import APIRouter, Depends, status
from sqlalchemy.ext.asyncio import AsyncSession

from app.contracts.catalog import CatalogService
from app.core.auth import CurrentUser, get_current_user
from app.core.dependencies import get_session
from app.core.envelope import Envelope
from app.modules.orders import service
from app.modules.orders.models import Order
from app.modules.orders.schemas import OrderCreate, OrderRead, ReceiptTaskRead
from app.modules.orders.tasks import generate_order_receipt


def create_router(catalog_dependency: Callable[..., CatalogService]) -> APIRouter:
    """The composition root injects the catalog contract provider — this module
    never imports the catalog package (adrs/python/modular-packages.md)."""
    router = APIRouter(prefix="/api/orders", tags=["orders"])

    @router.post("", response_model=Envelope[OrderRead], status_code=status.HTTP_201_CREATED)
    async def create_order(
        payload: OrderCreate,
        session: AsyncSession = Depends(get_session),
        catalog: CatalogService = Depends(catalog_dependency),
        user: CurrentUser = Depends(get_current_user),  # any authenticated role
    ) -> Envelope[OrderRead]:
        order, product = await service.create_order(session, catalog, payload, created_by=user.id)
        return Envelope(data=_to_read(order, product.name))

    @router.post(
        "/{order_id}/receipt",
        response_model=Envelope[ReceiptTaskRead],
        status_code=status.HTTP_202_ACCEPTED,
    )
    async def request_receipt(
        order_id: UUID,
        session: AsyncSession = Depends(get_session),
        catalog: CatalogService = Depends(catalog_dependency),
        user: CurrentUser = Depends(get_current_user),
    ) -> Envelope[ReceiptTaskRead]:
        # ownership gate first; then enqueue and return immediately with the
        # task id (adrs/python/celery-background-jobs.md)
        await service.get_order(session, catalog, order_id, caller=user)
        task = generate_order_receipt.delay(str(order_id))
        return Envelope(data=ReceiptTaskRead(task_id=task.id, state="PENDING"))

    # registered before /{order_id} so "receipts" is never parsed as an order id
    @router.get("/receipts/{task_id}", response_model=Envelope[ReceiptTaskRead])
    async def receipt_status(
        task_id: str,
        user: CurrentUser = Depends(get_current_user),
    ) -> Envelope[ReceiptTaskRead]:
        from celery.result import AsyncResult

        from app.core.celery import celery_app

        result = AsyncResult(task_id, app=celery_app)
        return Envelope(
            data=ReceiptTaskRead(
                task_id=task_id,
                state=result.state,
                receipt=result.result if result.successful() else None,
            )
        )

    @router.get("/{order_id}", response_model=Envelope[OrderRead])
    async def get_order(
        order_id: UUID,
        session: AsyncSession = Depends(get_session),
        catalog: CatalogService = Depends(catalog_dependency),
        user: CurrentUser = Depends(get_current_user),
    ) -> Envelope[OrderRead]:
        order, product = await service.get_order(session, catalog, order_id, caller=user)
        return Envelope(data=_to_read(order, product.name if product else None))

    return router


def _to_read(order: Order, product_name: str | None) -> OrderRead:
    return OrderRead(
        id=order.id,
        product_id=order.product_id,
        product_name=product_name,
        created_by=order.created_by,
        quantity=order.quantity,
        created_at=order.created_at,
    )
