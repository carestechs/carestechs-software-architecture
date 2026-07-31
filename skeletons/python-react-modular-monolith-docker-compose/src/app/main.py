from fastapi import FastAPI

from app.config import settings
from app.core.exceptions import register_exception_handlers
from app.core.logging import configure_logging, request_id_middleware
from app.modules.catalog.router import router as catalog_router


def create_app() -> FastAPI:
    configure_logging(settings.debug)
    app = FastAPI(title="Golden Skeleton API", docs_url="/docs" if settings.debug else None)
    app.middleware("http")(request_id_middleware)
    register_exception_handlers(app)
    app.include_router(catalog_router)

    @app.get("/health")
    async def health() -> dict[str, dict[str, str]]:
        return {"data": {"status": "ok"}}

    return app


app = create_app()
