from typing import Annotated

from fastapi import APIRouter, Cookie, Depends, Header, Response
from sqlalchemy.ext.asyncio import AsyncSession

from app.config import settings
from app.core.auth import ACCESS_TOKEN_TTL, create_access_token
from app.core.dependencies import get_session
from app.core.envelope import Envelope
from app.core.exceptions import ForbiddenError, UnauthorizedError
from app.modules.identity import service
from app.modules.identity.schemas import LoginRequest, TokenRead

router = APIRouter(prefix="/api/auth", tags=["identity"])

REFRESH_COOKIE = "refresh_token"


def _set_refresh_cookie(response: Response, raw: str) -> None:
    # httpOnly + SameSite=Strict + path-scoped to the auth endpoints; Secure
    # outside local development (adrs/api/jwt-bearer-auth.md)
    response.set_cookie(
        REFRESH_COOKIE,
        raw,
        max_age=int(service.REFRESH_TOKEN_LIFETIME.total_seconds()),
        path="/api/auth",
        httponly=True,
        secure=not settings.debug,
        samesite="strict",
    )


def _token_envelope(user_id, role) -> Envelope[TokenRead]:
    return Envelope(
        data=TokenRead(
            access_token=create_access_token(user_id, role),
            expires_in=int(ACCESS_TOKEN_TTL.total_seconds()),
        )
    )


@router.post("/login", response_model=Envelope[TokenRead])
async def login(
    payload: LoginRequest,
    response: Response,
    session: AsyncSession = Depends(get_session),
) -> Envelope[TokenRead]:
    user = await service.authenticate(session, payload.email, payload.password)
    raw_refresh = await service.issue_refresh_token(session, user.id)
    _set_refresh_cookie(response, raw_refresh)
    return _token_envelope(user.id, user.role)


@router.post("/logout", status_code=204)
async def logout(
    response: Response,
    session: AsyncSession = Depends(get_session),
    refresh_token: Annotated[str | None, Cookie()] = None,
    x_requested_with: Annotated[str, Header()] = "",
) -> None:
    # same CSRF guard as refresh — this endpoint is cookie-authenticated
    if x_requested_with != "XMLHttpRequest":
        raise ForbiddenError("Missing the X-Requested-With header.")
    if refresh_token:
        await service.revoke_refresh_family(session, refresh_token)
    response.delete_cookie(REFRESH_COOKIE, path="/api/auth")


@router.post("/refresh", response_model=Envelope[TokenRead])
async def refresh(
    response: Response,
    session: AsyncSession = Depends(get_session),
    refresh_token: Annotated[str | None, Cookie()] = None,
    x_requested_with: Annotated[str, Header()] = "",
) -> Envelope[TokenRead]:
    # CSRF guard for the cookie-authenticated endpoint: SameSite=Strict plus a
    # required custom header no cross-site form can set
    if x_requested_with != "XMLHttpRequest":
        raise ForbiddenError("Missing the X-Requested-With header.")
    if not refresh_token:
        raise UnauthorizedError("The refresh token is invalid or expired.")
    user, new_raw = await service.rotate_refresh_token(session, refresh_token)
    _set_refresh_cookie(response, new_raw)
    return _token_envelope(user.id, user.role)

