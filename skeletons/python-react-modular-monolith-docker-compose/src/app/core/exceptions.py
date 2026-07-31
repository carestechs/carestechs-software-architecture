import logging

from fastapi import FastAPI, Request, status
from fastapi.exceptions import RequestValidationError
from fastapi.responses import JSONResponse
from starlette.exceptions import HTTPException as StarletteHTTPException

logger = logging.getLogger(__name__)

PROBLEM_MEDIA_TYPE = "application/problem+json"


class AppError(Exception):
    """Base typed application error (adrs/python/rfc7807-errors.md)."""

    status_code = status.HTTP_500_INTERNAL_SERVER_ERROR
    title = "Internal Server Error"

    def __init__(self, detail: str) -> None:
        super().__init__(detail)
        self.detail = detail


class NotFoundError(AppError):
    status_code = status.HTTP_404_NOT_FOUND
    title = "Not Found"


class ConflictError(AppError):
    status_code = status.HTTP_409_CONFLICT
    title = "Conflict"


def problem(status_code: int, title: str, detail: str, **extensions: object) -> JSONResponse:
    body: dict[str, object] = {"title": title, "status": status_code, "detail": detail}
    body.update(extensions)
    return JSONResponse(body, status_code=status_code, media_type=PROBLEM_MEDIA_TYPE)


def register_exception_handlers(app: FastAPI) -> None:
    @app.exception_handler(AppError)
    async def app_error_handler(request: Request, exc: AppError) -> JSONResponse:
        return problem(exc.status_code, exc.title, exc.detail)

    @app.exception_handler(RequestValidationError)
    async def validation_handler(request: Request, exc: RequestValidationError) -> JSONResponse:
        errors = [
            {"field": ".".join(str(part) for part in e["loc"][1:]), "message": e["msg"]}
            for e in exc.errors()
        ]
        return problem(
            422,
            "Validation Failed",
            "The request body failed validation.",
            errors=errors,
        )

    @app.exception_handler(StarletteHTTPException)
    async def http_exception_handler(request: Request, exc: StarletteHTTPException) -> JSONResponse:
        return problem(exc.status_code, "Error", str(exc.detail))

    @app.exception_handler(Exception)
    async def unhandled_handler(request: Request, exc: Exception) -> JSONResponse:
        logger.exception("unhandled error on %s %s", request.method, request.url.path)
        return problem(
            status.HTTP_500_INTERNAL_SERVER_ERROR,
            "Internal Server Error",
            "An unexpected error occurred.",
        )
