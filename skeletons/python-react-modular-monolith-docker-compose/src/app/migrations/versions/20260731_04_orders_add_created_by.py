"""orders: add created_by (plain cross-module reference to the identity user)

No ForeignKey — users belong to the identity module
(adrs/dotnet/cross-module-by-id.md family rule).
"""
import sqlalchemy as sa
from alembic import op

revision = "20260731_04"
down_revision = "20260731_03"
branch_labels = None
depends_on = None


def upgrade() -> None:
    op.add_column("orders", sa.Column("created_by", sa.Uuid(), nullable=False))


def downgrade() -> None:
    op.drop_column("orders", "created_by")
