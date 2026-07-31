from typing import Protocol
from uuid import UUID

from pydantic import BaseModel


class ProductSummary(BaseModel):
    """Cross-module DTO owned by the contracts package (adrs/python/modular-packages.md)."""

    id: UUID
    name: str


class CatalogService(Protocol):
    """What other modules may ask the catalog module for."""

    async def get_product_summary(self, product_id: UUID) -> ProductSummary | None: ...
