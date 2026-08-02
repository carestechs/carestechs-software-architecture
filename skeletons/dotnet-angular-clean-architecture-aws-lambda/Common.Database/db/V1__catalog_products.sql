-- Hand-written DDL applied by Flyway (adrs/deployment/flyway-migrations.md).
-- Lowercase identifiers, no naming package (adrs/database/lowercase-naming.md).
CREATE TABLE products (
    id uuid PRIMARY KEY,
    sku varchar(64) NOT NULL UNIQUE,
    name varchar(200) NOT NULL,
    createdat timestamptz NOT NULL
);
