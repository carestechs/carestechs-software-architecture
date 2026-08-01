"""Shared pagination parameter binding (adrs/api/offset-pagination.md)."""
from dataclasses import dataclass
from typing import Annotated, Literal

from fastapi import Query


@dataclass(frozen=True)
class PaginationParams:
    page: int
    page_size: int
    sort_by: str
    sort_dir: Literal["asc", "desc"]

    @property
    def offset(self) -> int:
        return (self.page - 1) * self.page_size


def pagination_params(
    page: Annotated[int, Query(ge=1)] = 1,
    page_size: Annotated[int, Query(ge=1, le=100, alias="pageSize")] = 20,
    sort_by: Annotated[str, Query(alias="sortBy")] = "createdAt",
    sort_dir: Annotated[Literal["asc", "desc"], Query(alias="sortDir")] = "asc",
) -> PaginationParams:
    return PaginationParams(page=page, page_size=page_size, sort_by=sort_by, sort_dir=sort_dir)
