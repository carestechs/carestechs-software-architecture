from fastapi import FastAPI

from app.config import settings
from app.core.exceptions import register_exception_handlers
from app.core.logging import configure_logging, request_id_middleware
from app.modules.catalog.dependencies import get_catalog_service
from app.modules.catalog.router import router as catalog_router
from app.modules.orders.router import create_router as create_orders_router


def create_app() -> FastAPI:
    configure_logging(settings.debug)
    app = FastAPI(title="Golden Skeleton API", docs_url="/docs" if settings.debug else None)
    app.middleware("http")(request_id_middleware)
    register_exception_handlers(app)
    app.include_router(catalog_router)
    # Composition root: the orders module receives the catalog contract provider
    # here — modules never import each other (adrs/python/modular-packages.md).
    app.include_router(create_orders_router(get_catalog_service))

    @app.get("/health")
    async def health() -> dict[str, dict[str, str]]:
        return {"data": {"status": "ok"}}

    return app


app = create_app()
