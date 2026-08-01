from typing import Generic, TypeVar

from pydantic import BaseModel, Field

T = TypeVar("T")


class Meta(BaseModel):
    total_count: int | None = Field(default=None, serialization_alias="totalCount")
    page: int | None = None
    page_size: int | None = Field(default=None, serialization_alias="pageSize")


class Envelope(BaseModel, Generic[T]):
    """`{ data, meta }` response envelope for 2xx responses (adrs/api/rest-envelope.md)."""

    data: T
    meta: Meta | None = None
