from pydantic_settings import BaseSettings, SettingsConfigDict


class Settings(BaseSettings):
    """Typed settings read from the environment (adrs/deployment/env-connection-urls.md)."""

    model_config = SettingsConfigDict(env_file=".env", extra="ignore")

    database_url: str = "postgresql+asyncpg://postgres:postgres@localhost:5432/app"
    debug: bool = False
    # HS256 signing secret — the dev default is for local use only; production
    # injects JWT_SECRET from the environment (adrs/deployment/env-connection-urls.md)
    jwt_secret: str = "dev-only-secret-change-me-minimum-32-bytes!"  # >= 32 bytes for HS256
    # Celery broker AND result backend (adrs/python/celery-background-jobs.md);
    # celery_result_backend only diverges in tests (cache+memory://)
    redis_url: str = "redis://localhost:6379/0"
    celery_result_backend: str | None = None


settings = Settings()
