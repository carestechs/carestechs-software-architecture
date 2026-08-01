from uuid import UUID

from fastapi import APIRouter, Depends, status
from sqlalchemy.ext.asyncio import AsyncSession

from app.core.auth import require_role
from app.core.dependencies import get_session
from app.core.pagination import PaginationParams, pagination_params
from app.core.envelope import Envelope, Meta
from app.modules.catalog import service
from app.modules.catalog.schemas import ProductCreate, ProductRead

router = APIRouter(prefix="/api/products", tags=["catalog"])


@router.get("", response_model=Envelope[list[ProductRead]])
async def list_products(
    session: AsyncSession = Depends(get_session),
    params: PaginationParams = Depends(pagination_params),
) -> Envelope[list[ProductRead]]:
    products, total = await service.list_products(session, params)
    return Envelope(
        data=[ProductRead.model_validate(p) for p in products],
        meta=Meta(total_count=total, page=params.page, page_size=params.page_size),
    )


@router.post(
    "",
    response_model=Envelope[ProductRead],
    status_code=status.HTTP_201_CREATED,
    # endpoint-layer role gate (adrs/api/role-based-authorization.md); reads stay public
    dependencies=[Depends(require_role("admin"))],
)
async def create_product(
    payload: ProductCreate, session: AsyncSession = Depends(get_session)
) -> Envelope[ProductRead]:
    product = await service.create_product(session, payload)
    return Envelope(data=ProductRead.model_validate(product))


@router.get("/{product_id}", response_model=Envelope[ProductRead])
async def get_product(
    product_id: UUID, session: AsyncSession = Depends(get_session)
) -> Envelope[ProductRead]:
    product = await service.get_product(session, product_id)
    return Envelope(data=ProductRead.model_validate(product))
