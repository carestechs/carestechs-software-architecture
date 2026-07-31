"""catalog: create products table

Module-prefixed slug per adrs/python/modular-packages.md.
"""
import sqlalchemy as sa
from alembic import op

revision = "20260730_01"
down_revision = None
branch_labels = None
depends_on = None


def upgrade() -> None:
    op.create_table(
        "products",
        sa.Column("id", sa.Uuid(), primary_key=True),
        sa.Column("sku", sa.String(64), nullable=False, unique=True),
        sa.Column("name", sa.String(200), nullable=False),
        sa.Column("created_at", sa.DateTime(timezone=True), nullable=False),
    )


def downgrade() -> None:
    op.drop_table("products")
