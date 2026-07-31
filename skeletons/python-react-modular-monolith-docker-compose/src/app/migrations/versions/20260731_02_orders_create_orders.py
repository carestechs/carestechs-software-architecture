"""orders: create orders table

Module-prefixed slug per adrs/python/modular-packages.md. product_id is a plain
UUID column — cross-module references carry no ForeignKey constraint, so the
module stays extractable together with its tables.
"""
import sqlalchemy as sa
from alembic import op

revision = "20260731_02"
down_revision = "20260730_01"
branch_labels = None
depends_on = None


def upgrade() -> None:
    op.create_table(
        "orders",
        sa.Column("id", sa.Uuid(), primary_key=True),
        sa.Column("product_id", sa.Uuid(), nullable=False),
        sa.Column("quantity", sa.Integer(), nullable=False),
        sa.Column("created_at", sa.DateTime(timezone=True), nullable=False),
    )


def downgrade() -> None:
    op.drop_table("orders")
