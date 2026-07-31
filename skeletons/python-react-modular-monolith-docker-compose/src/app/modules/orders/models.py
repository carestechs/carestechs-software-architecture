import uuid
from datetime import datetime, timezone

from sqlalchemy import DateTime
from sqlalchemy.orm import Mapped, mapped_column

from app.core.database import Base


class Order(Base):
    __tablename__ = "orders"

    id: Mapped[uuid.UUID] = mapped_column(primary_key=True, default=uuid.uuid4)
    # Plain cross-module reference: no ForeignKey, no relationship(). The catalog
    # module owns products; orders resolves them through the contract, which keeps
    # the module extractable together with its tables
    # (adrs/dotnet/cross-module-by-id.md family rule, adrs/python/modular-packages.md).
    product_id: Mapped[uuid.UUID] = mapped_column()
    quantity: Mapped[int] = mapped_column()
    created_at: Mapped[datetime] = mapped_column(
        DateTime(timezone=True), default=lambda: datetime.now(timezone.utc)
    )
