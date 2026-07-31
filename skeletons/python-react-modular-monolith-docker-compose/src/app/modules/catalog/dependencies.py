from fastapi import Depends
from sqlalchemy.ext.asyncio import AsyncSession

from app.contracts.catalog import CatalogService
from app.core.dependencies import get_session
from app.modules.catalog.providers import SqlCatalogService


def get_catalog_service(session: AsyncSession = Depends(get_session)) -> CatalogService:
    """Provider handed to consuming modules by the composition root (app/main.py)."""
    return SqlCatalogService(session)
