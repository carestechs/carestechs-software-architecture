from uuid import UUID

from sqlalchemy import select
from sqlalchemy.ext.asyncio import AsyncSession

from app.contracts.catalog import ProductSummary
from app.modules.catalog.models import Product


class SqlCatalogService:
    """Concrete provider of the catalog contract (app.contracts.catalog.CatalogService).

    Lives inside the catalog module: only the owning module touches its tables.
    Consumers receive ProductSummary DTOs, never the Product model
    (adrs/python/modular-packages.md).
    """

    def __init__(self, session: AsyncSession) -> None:
        self._session = session

    async def get_product_summary(self, product_id: UUID) -> ProductSummary | None:
        result = await self._session.execute(
            select(Product.id, Product.name).where(Product.id == product_id)
        )
        row = result.one_or_none()
        return None if row is None else ProductSummary(id=row.id, name=row.name)
