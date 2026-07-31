from datetime import datetime
from uuid import UUID

from pydantic import BaseModel, ConfigDict, Field
from pydantic.alias_generators import to_camel


class OrderCreate(BaseModel):
    model_config = ConfigDict(alias_generator=to_camel, populate_by_name=True)

    product_id: UUID
    quantity: int = Field(ge=1, le=999)


class OrderRead(BaseModel):
    model_config = ConfigDict(alias_generator=to_camel, populate_by_name=True)

    id: UUID
    product_id: UUID
    # Resolved through the catalog contract at read time — never stored on the
    # order row and never joined (adrs/dotnet/cross-module-by-id.md family rule).
    product_name: str | None
    created_by: UUID
    quantity: int
    created_at: datetime
