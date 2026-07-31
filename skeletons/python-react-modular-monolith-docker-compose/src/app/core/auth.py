"""Access-token validation and role guards (adrs/api/jwt-bearer-auth.md,
adrs/api/role-based-authorization.md).

Token VALIDATION lives in core so any module can guard its endpoints without
importing the identity module; token ISSUANCE (users, passwords, refresh
rotation) is the identity module's job.
"""
from dataclasses import dataclass
from datetime import datetime, timedelta, timezone
from typing import Annotated
from uuid import UUID

import jwt
from fastapi import Depends, Header

from app.config import settings
from app.core.exceptions import ForbiddenError, UnauthorizedError

JWT_ALGORITHMS = ["HS256"]  # explicit allowlist — never trust the token header
JWT_ISSUER = "skeleton-api"
JWT_AUDIENCE = "skeleton-clients"
ACCESS_TOKEN_TTL = timedelta(minutes=15)  # MUST NOT exceed 60 minutes
CLOCK_SKEW_SECONDS = 60


@dataclass(frozen=True)
class CurrentUser:
    id: UUID
    role: str


def create_access_token(user_id: UUID, role: str) -> str:
    now = datetime.now(timezone.utc)
    claims = {
        "sub": str(user_id),
        "role": role,
        "iat": now,
        "exp": now + ACCESS_TOKEN_TTL,
        "iss": JWT_ISSUER,
        "aud": JWT_AUDIENCE,
    }
    return jwt.encode(claims, settings.jwt_secret, algorithm=JWT_ALGORITHMS[0])


def decode_access_token(token: str) -> CurrentUser:
    try:
        claims = jwt.decode(
            token,
            settings.jwt_secret,
            algorithms=JWT_ALGORITHMS,
            issuer=JWT_ISSUER,
            audience=JWT_AUDIENCE,
            leeway=CLOCK_SKEW_SECONDS,
        )
    except jwt.InvalidTokenError as exc:
        raise UnauthorizedError("The access token is missing, expired, or invalid.") from exc
    return CurrentUser(id=UUID(claims["sub"]), role=claims["role"])


async def get_current_user(authorization: Annotated[str, Header()] = "") -> CurrentUser:
    scheme, _, token = authorization.partition(" ")
    if scheme.lower() != "bearer" or not token:
        raise UnauthorizedError("The access token is missing, expired, or invalid.")
    return decode_access_token(token)


def require_role(*roles: str):
    """Endpoint-layer role gate; ownership checks stay in services
    (adrs/api/role-based-authorization.md)."""

    async def guard(user: CurrentUser = Depends(get_current_user)) -> CurrentUser:
        if user.role not in roles:
            raise ForbiddenError("This action requires a role you do not have.")
        return user

    return guard
