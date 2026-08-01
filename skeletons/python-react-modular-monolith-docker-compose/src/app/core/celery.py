"""Celery configuration lives in one dedicated module
(adrs/python/celery-background-jobs.md)."""
from celery import Celery

from app.config import settings

celery_app = Celery(
    "app",
    broker=settings.redis_url,
    backend=settings.celery_result_backend or settings.redis_url,
)
celery_app.conf.update(
    task_serializer="json",
    accept_content=["json"],
    result_serializer="json",
    timezone="UTC",
    enable_utc=True,
    broker_connection_retry_on_startup=True,
)
celery_app.autodiscover_tasks(["app.modules.orders"])
