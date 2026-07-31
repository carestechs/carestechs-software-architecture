import uuid
from datetime import datetime, timedelta, timezone

from sqlalchemy import select, update
from sqlalchemy.ext.asyncio import AsyncSession

from app.core.exceptions import ConflictError, UnauthorizedError
from app.modules.identity import security
from app.modules.identity.models import RefreshToken, User

REFRESH_TOKEN_LIFETIME = timedelta(days=30)  # absolute maximum, never extended by rotation


async def create_user(session: AsyncSession, email: str, password: str, role: str) -> User:
    existing = await session.execute(select(User.id).where(User.email == email))
    if existing.scalar_one_or_none() is not None:
        raise ConflictError(f"A user with email '{email}' already exists.")
    user = User(email=email, password_hash=security.hash_password(password), role=role)
    session.add(user)
    await session.flush()
    return user


async def authenticate(session: AsyncSession, email: str, password: str) -> User:
    result = await session.execute(select(User).where(User.email == email))
    user = result.scalar_one_or_none()
    # one generic message for unknown email and wrong password alike
    if user is None or not security.verify_password(password, user.password_hash):
        raise UnauthorizedError("Invalid credentials.")
    return user


async def issue_refresh_token(
    session: AsyncSession,
    user_id: uuid.UUID,
    family_id: uuid.UUID | None = None,
    expires_at: datetime | None = None,
) -> str:
    """New login -> new family; rotation reuses the family and its absolute bound."""
    raw = security.new_refresh_token()
    session.add(
        RefreshToken(
            user_id=user_id,
            token_hash=security.hash_refresh_token(raw),
            family_id=family_id or uuid.uuid4(),
            expires_at=expires_at or datetime.now(timezone.utc) + REFRESH_TOKEN_LIFETIME,
        )
    )
    await session.flush()
    return raw


async def rotate_refresh_token(session: AsyncSession, raw: str) -> tuple[User, str]:
    """Rotate on every use; reuse of an already-rotated token revokes the family
    (adrs/api/jwt-bearer-auth.md)."""
    now = datetime.now(timezone.utc)
    result = await session.execute(
        select(RefreshToken).where(RefreshToken.token_hash == security.hash_refresh_token(raw))
    )
    token = result.scalar_one_or_none()
    if token is None or token.revoked_at is not None or token.expires_at <= now:
        raise UnauthorizedError("The refresh token is invalid or expired.")

    if token.used_at is not None:
        # Reuse detected: this token was already rotated once. Revoke the family.
        await session.execute(
            update(RefreshToken)
            .where(RefreshToken.family_id == token.family_id, RefreshToken.revoked_at.is_(None))
            .values(revoked_at=now)
        )
        raise UnauthorizedError("Refresh token reuse detected; please sign in again.")

    token.used_at = now
    user = await session.get(User, token.user_id)
    if user is None:
        raise UnauthorizedError("The refresh token is invalid or expired.")
    new_raw = await issue_refresh_token(
        session, user.id, family_id=token.family_id, expires_at=token.expires_at
    )
    return user, new_raw
